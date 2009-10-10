//------------------------------------------------------------------------------
// JGR.IO.Parser library, part of MSTS Editors & Tools (http://jgrmsts.codeplex.com/).
// License: Microsoft Public License (Ms-PL).
//------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using Jgr.Grammar;

namespace Jgr.IO.Parser
{
	public class SimisReader //: BufferedMessageSource
	{
		public SimisStreamFormat StreamFormat { get; private set; }
		public bool StreamCompressed { get; private set; }
		public string SimisFormat { get; private set; }
		public bool EndOfStream { get; private set; }
		public BnfState BnfState { get; private set; }
		UnclosableStream BaseStream;
		SimisProvider SimisProvider;
		BinaryReader BinaryReader;
		bool DoneAutoDetect;
		long StreamLength;
		Stack<uint> BlockEndOffsets;
		Queue<SimisToken> PendingTokens;

		static readonly char[] WhitespaceChars = InitWhitespaceChars();
		static readonly char[] WhitespaceAndSpecialChars = InitWhitespaceAndSpecialChars();
		static readonly char[] HexDigits = InitHexDigits();
		static readonly string[] DataTypes = InitDataTypes();
		#region static init functions
		static char[] InitWhitespaceChars() {
			return new char[] { ' ', '\t', '\r', '\n' };
		}

		static char[] InitWhitespaceAndSpecialChars() {
			return new char[] { ' ', '\t', '\r', '\n', '(', ')', '"', ':' };
		}

		static char[] InitHexDigits() {
			return new char[] { '0', '1', '2', '3', '4', '5', '6', '7', '8', '9', 'a', 'b', 'c', 'd', 'e', 'f', 'A', 'B', 'C', 'D', 'E', 'F' };
		}

		static string[] InitDataTypes() {
			return new string[] { "string", "uint", "sint", "dword", "float", "buffer" };
		}
		#endregion

		public SimisReader(Stream stream, SimisProvider provider)
			: this(stream, provider, SimisStreamFormat.AutoDetect, false, "") {
		}

		public SimisReader(Stream stream, SimisProvider provider, SimisStreamFormat format, bool compressed, string simisFormat) {
			StreamFormat = format;
			StreamCompressed = compressed;
			SimisFormat = simisFormat;
			EndOfStream = false;

			if (!stream.CanRead) throw new InvalidDataException("Stream must support reading.");
			if (!stream.CanSeek) throw new InvalidDataException("Stream must support seeking.");
			BaseStream = new UnclosableStream(stream);
			SimisProvider = provider;
			BinaryReader = new BinaryReader(BaseStream, new ByteEncoding());
			DoneAutoDetect = format != SimisStreamFormat.AutoDetect;
			StreamLength = BaseStream.Length;

			BlockEndOffsets = new Stack<uint>();
			PendingTokens = new Queue<SimisToken>();
			if (SimisFormat.Length > 0) {
				BnfState = new BnfState(SimisProvider.GetBnf(SimisFormat, ""));
			}
		}

		public SimisToken ReadToken() {
			if (!DoneAutoDetect) AutodetectStreamFormat();

			// Any blocks that should have ended at or before this point, are now ended.
			while ((BlockEndOffsets.Count > 0) && (BinaryReader.BaseStream.Position >= BlockEndOffsets.Peek())) {
				PendingTokens.Enqueue(new SimisToken() { Kind = SimisTokenKind.BlockEnd }); //, String = String.Join(", ", BlockEndOffsets.Select<uint, string>(o => o.ToString("X8")).ToArray<string>()) });
				BlockEndOffsets.Pop();
			}

			// Any pending tokens go first.
			if (PendingTokens.Count > 0) {
				// If we've run out of stream and have no pending tokens, we're done.
				if ((BinaryReader.BaseStream.Position >= BinaryReader.BaseStream.Length) && (PendingTokens.Count == 1)) {
					EndOfStream = true;
				}
				try {
					if (PendingTokens.Peek().Kind == SimisTokenKind.BlockBegin) BnfState.EnterBlock();
					if (PendingTokens.Peek().Kind == SimisTokenKind.BlockEnd) BnfState.LeaveBlock();
				} catch (BnfStateException e) {
					throw new ReaderException(BinaryReader, StreamFormat == SimisStreamFormat.Binary, 0, "", e);
				}
				return PendingTokens.Dequeue();
			}

			var rv = new SimisToken();

			if (StreamFormat == SimisStreamFormat.Text) {
				PinReader();
				rv = ReadTokenAsText();
				try {
					if (rv.Kind == SimisTokenKind.BlockBegin) {
						BnfState.EnterBlock();
					} else if (rv.Kind == SimisTokenKind.BlockEnd) {
						BnfState.LeaveBlock();
					} else if (rv.Kind != SimisTokenKind.None) {
						if (rv.Type.Length > 0) {
							BnfState.MoveTo(rv.Type);
						}
					} else {
						throw new ReaderException(BinaryReader, false, PinReaderChanged(), "ReadTokenAsText returned invalid token type: " + rv.Kind);
					}
				} catch (BnfStateException e) {
					throw new ReaderException(BinaryReader, false, PinReaderChanged(), "", e);
				}

				// Consume all whitespace now that we've got a token.
				while ((BinaryReader.BaseStream.Position < BinaryReader.BaseStream.Length) && WhitespaceChars.Contains<char>((char)BinaryReader.PeekChar())) {
					BinaryReader.ReadChar();
				}
			} else {
				PinReader();
				rv = ReadTokenAsBinary();
				try {
					if ((rv.Kind != SimisTokenKind.BlockBegin) && (rv.Kind != SimisTokenKind.BlockEnd)) {
						BnfState.MoveTo(rv.Type);
					} else {
						throw new ReaderException(BinaryReader, false, PinReaderChanged(), "ReadTokenAsBinary returned invalid token type: " + rv.Type);
					}
				} catch (BnfStateException e) {
					throw new ReaderException(BinaryReader, true, PinReaderChanged(), "", e);
				}
			}

			// If we've run out of stream and have no pending tokens, we're done.
			if ((BinaryReader.BaseStream.Position >= BinaryReader.BaseStream.Length) && (PendingTokens.Count == 0)) {
				EndOfStream = true;
			}

			return rv;
		}

		SimisToken ReadTokenAsText() {
			SimisToken rv = new SimisToken();

			if ('(' == BinaryReader.PeekChar()) {
				BinaryReader.ReadChar();
				rv.Kind = SimisTokenKind.BlockBegin;
				return rv;
			}

			if (')' == BinaryReader.PeekChar()) {
				BinaryReader.ReadChar();
				rv.Kind = SimisTokenKind.BlockEnd;
				return rv;
			}

			if (':' == BinaryReader.PeekChar()) {
				BinaryReader.ReadChar();
				return ReadTokenAsText();
			}

			string token = ReadTokenOrString();

			if (BnfState == null) {
				try {
					BnfState = new BnfState(SimisProvider.GetBnf(SimisFormat, token));
				} catch (UnknownSimisFormatException e) {
					throw new ReaderException(BinaryReader, false, PinReaderChanged(), "", e);
				}
			}

			if (BnfState.IsEnterBlockTime) {
				// We should only end up here when called recursively by the
				// if (validStates.Contains(token)) code below.
				rv.String = token;
				rv.Kind = token.Length > 0 ? SimisTokenKind.Block : SimisTokenKind.None;
				return rv;
			}

			if (token.ToLower() == "skip") {
				while ((BinaryReader.BaseStream.Position < BinaryReader.BaseStream.Length) && (')' != BinaryReader.PeekChar())) {
					token += BinaryReader.ReadChar();
				}
				if (BinaryReader.BaseStream.Position >= BinaryReader.BaseStream.Length) throw new ReaderException(BinaryReader, false, 0, "SimisReader expected ')'; got EOF.");
				token += BinaryReader.ReadChar();
				rv.String = token;
				rv.Kind = SimisTokenKind.String;
				return rv;
			}

			var validStates = BnfState.ValidStates;
			if (validStates.Contains(token)) {
				// Token exactly matches a valid state transition, so let's use it.
				rv.Type = token;
				rv.Kind = SimisTokenKind.Block;

				// Do lookahead for block name. Since we've moved BNFState already, it'll
				// fall into the special BNFState.IsEnterBlockTime code if we have a
				// possible string token. The only possible Kind values are thus
				// BlockBegin, BlockEnd and Block. BlockEnd would be weird (and wrong).
				PinReader();
				while ((BinaryReader.BaseStream.Position < BinaryReader.BaseStream.Length) && WhitespaceChars.Contains<char>((char)BinaryReader.PeekChar())) {
					BinaryReader.ReadChar();
				}
				string name = ReadTokenOrString();
				if (name.Length > 0) {
					rv.String = name;
				} else {
					PinReaderReset();
				}

				return rv;
			}

			var validDataTypeStates = validStates.Where<string>(s => DataTypes.Contains(s)).Where<string>(s => {
				if (token.Contains(".")) return (s == "float") || (s == "string");
				if ((token.Length == 8) && (token.ToCharArray().All<char>(c => HexDigits.Contains(c)))) return s == "dword";
				return s != "dword";
			}).ToArray<string>();
			if (validDataTypeStates.Length == 0) throw new ReaderException(BinaryReader, false, PinReaderChanged(), "SimisReader found no data types available for parsing of token '" + token + "'.", new BnfStateException(BnfState, ""));

			rv.Type = validDataTypeStates[0];
			switch (rv.Type) {
				case "uint":
					if (token.EndsWith(",")) token = token.Substring(0, token.Length - 1);
					try {
						rv.Integer = UInt32.Parse(token);
						if (token.Length == 8) throw new ReaderException(BinaryReader, false, PinReaderChanged(), "SimisReader expected decimal number; got possible hex '" + token + "'.");
					} catch (FormatException ex) {
						throw new ReaderException(BinaryReader, false, PinReaderChanged(), "SimisReader failed to parse '" + token + "' as '" + rv.Type + "'.", ex);
					} catch (OverflowException ex) {
						throw new ReaderException(BinaryReader, false, PinReaderChanged(), "SimisReader failed to parse '" + token + "' as '" + rv.Type + "'.", ex);
					}
					rv.Kind = SimisTokenKind.Integer;
					break;
				case "sint":
					if (token.EndsWith(",")) token = token.Substring(0, token.Length - 1);
					try {
						rv.Integer = Int32.Parse(token);
					} catch (FormatException ex) {
						throw new ReaderException(BinaryReader, false, PinReaderChanged(), "SimisReader failed to parse '" + token + "' as '" + rv.Type + "'.", ex);
					} catch (OverflowException ex) {
						throw new ReaderException(BinaryReader, false, PinReaderChanged(), "SimisReader failed to parse '" + token + "' as '" + rv.Type + "'.", ex);
					}
					rv.Kind = SimisTokenKind.Integer;
					break;
				case "dword":
					if (token.EndsWith(",")) token = token.Substring(0, token.Length - 1);
					try {
						rv.Integer = UInt32.Parse(token, NumberStyles.HexNumber);
						if (token.Length != 8) throw new ReaderException(BinaryReader, false, PinReaderChanged(), "SimisReader expected 8-digit hex number; got '" + token + "'.");
					} catch (FormatException ex) {
						throw new ReaderException(BinaryReader, false, PinReaderChanged(), "SimisReader failed to parse '" + token + "' as '" + rv.Type + "'.", ex);
					} catch (OverflowException ex) {
						throw new ReaderException(BinaryReader, false, PinReaderChanged(), "SimisReader failed to parse '" + token + "' as '" + rv.Type + "'.", ex);
					}
					rv.Kind = SimisTokenKind.Integer;
					break;
				case "float":
					if (token.EndsWith(",")) token = token.Substring(0, token.Length - 1);
					try {
						rv.Float = Double.Parse(token);
					} catch (FormatException ex) {
						throw new ReaderException(BinaryReader, false, PinReaderChanged(), "SimisReader failed to parse '" + token + "' as '" + rv.Type + "'.", ex);
					} catch (OverflowException ex) {
						throw new ReaderException(BinaryReader, false, PinReaderChanged(), "SimisReader failed to parse '" + token + "' as '" + rv.Type + "'.", ex);
					}
					rv.Kind = SimisTokenKind.Float;
					break;
				case "string":
					rv.String = token;
					rv.Kind = SimisTokenKind.String;
					break;
			}
			return rv;
		}

		string ReadTokenOrString() {
			string token = "";
			if ('"' == BinaryReader.PeekChar()) {
				do {
					// Eat whitespace. (This is for the 2nd and further times through, to each whitespace after the "+".)
					if ('+' == BinaryReader.PeekChar()) BinaryReader.ReadChar();
					while ((BinaryReader.BaseStream.Position < BinaryReader.BaseStream.Length) && WhitespaceChars.Any<char>(c => c == BinaryReader.PeekChar())) {
						BinaryReader.ReadChar();
					}
					// Consume string.
					if (BinaryReader.BaseStream.Position >= BinaryReader.BaseStream.Length) throw new ReaderException(BinaryReader, false, 0, "SimisReader expected '\"'; got EOF.");
					BinaryReader.ReadChar(); // "\""
					while ((BinaryReader.BaseStream.Position < BinaryReader.BaseStream.Length) && ('"' != BinaryReader.PeekChar())) {
						if ('\\' == BinaryReader.PeekChar()) {
							BinaryReader.ReadChar();
							var ch = BinaryReader.ReadChar();
							switch (ch) {
								case '\\':
									token += "\\";
									break;
								case '"':
									token += "\"";
									break;
								case 't':
									token += "\t";
									break;
								case 'n':
									token += "\n";
									break;
								default:
									throw new ReaderException(BinaryReader, false, PinReaderChanged(), "SimisReader found unknown escape in string: \\" + ch + ".");
							}
						} else {
							token += BinaryReader.ReadChar();
						}
					}
					if (BinaryReader.BaseStream.Position >= BinaryReader.BaseStream.Length) throw new ReaderException(BinaryReader, false, 0, "SimisReader expected '\"'; got EOF.");
					BinaryReader.ReadChar(); // "\""
					// Eat whitespace. (This is for the 2nd and further times through, to each whitespace after the "+".)
					while ((BinaryReader.BaseStream.Position < BinaryReader.BaseStream.Length) && WhitespaceChars.Any<char>(c => c == BinaryReader.PeekChar())) {
						BinaryReader.ReadChar();
					}
				} while ('+' == BinaryReader.PeekChar());
			} else {
				// Consume all non-whitespace, non-special characters.
				while ((BinaryReader.BaseStream.Position < BinaryReader.BaseStream.Length) && !WhitespaceAndSpecialChars.Contains<char>((char)BinaryReader.PeekChar())) {
					token += BinaryReader.ReadChar();
				}
			}
			return token;
		}

		SimisToken ReadTokenAsBinary() {
			SimisToken rv = new SimisToken();

			var validStates = (BnfState == null ? new string[] { } : BnfState.ValidStates.Where<string>(s => !s.StartsWith("<")));
			//if (validStates.Length == 0) throw new ReaderException(BinaryReader, true, PinReaderChanged(), "SimisReader found no non-meta states available.", new BNFStateException(BNFState, ""));

			// If we have any valid data types, we read that instead of a block start. They should all be the same data type, too.
			var validDataTypes = validStates.Where<string>(s => DataTypes.Contains(s)).ToArray<string>();
			if (validDataTypes.Length > 0) {
				if (!validDataTypes.All<string>(s => s == validDataTypes[0])) throw new ReaderException(BinaryReader, true, PinReaderChanged(), "SimisReader found inconsistent data types available.", new BnfStateException(BnfState, ""));

				rv.Type = validDataTypes[0];
				switch (rv.Type) {
					case "uint":
						rv.Integer = BinaryReader.ReadUInt32();
						rv.Kind = SimisTokenKind.Integer;
						break;
					case "sint":
						rv.Integer = BinaryReader.ReadInt32();
						rv.Kind = SimisTokenKind.Integer;
						break;
					case "dword":
						rv.Integer = BinaryReader.ReadUInt32();
						rv.Kind = SimisTokenKind.Integer;
						break;
					case "float":
						rv.Float = BinaryReader.ReadSingle();
						rv.Kind = SimisTokenKind.Float;
						break;
					case "string":
						var stringLength = BinaryReader.ReadUInt16();
						if (stringLength > 10000) throw new ReaderException(BinaryReader, true, PinReaderChanged(), "SimisReader found a string longer than 10,000 characters.", new BnfStateException(BnfState, ""));
						for (var i = 0; i < stringLength; i++) {
							rv.String += (char)BinaryReader.ReadUInt16();
						}
						rv.Kind = SimisTokenKind.String;
						break;
					case "buffer":
						var bufferLength = BlockEndOffsets.Peek() - BinaryReader.BaseStream.Position;
						rv.String = String.Join("", BinaryReader.ReadBytes((int)bufferLength).Select<byte, string>(b => b.ToString("X2")).ToArray<string>());
						rv.Kind = SimisTokenKind.String;
						break;
					default:
						throw new ReaderException(BinaryReader, true, PinReaderChanged(), "SimisReader found unexpected data type '" + rv.Type + "'.", new BnfStateException(BnfState, ""));
				}
			} else {
				var tokenID = BinaryReader.ReadUInt16();
				var tokenType = BinaryReader.ReadUInt16();
				var token = ((uint)tokenType << 16) + tokenID;
				if (!SimisProvider.TokenNames.ContainsKey(token)) throw new ReaderException(BinaryReader, true, PinReaderChanged(), String.Format("SimisReader got invalid block: id={0:X4}, type={1:X4}.", tokenID, tokenType), new BnfStateException(BnfState, ""));
				if ((tokenType != 0x0000) && (tokenType != 0x0004)) throw new ReaderException(BinaryReader, true, PinReaderChanged(), String.Format("SimisReader got invalid block: id={0:X4}, type={1:X4}, name={2}.", tokenID, tokenType, SimisProvider.TokenNames[token]), new BnfStateException(BnfState, ""));

				rv.Type = SimisProvider.TokenNames[token];
				rv.Kind = SimisTokenKind.Block;

				if (BnfState == null) {
					try {
						BnfState = new BnfState(SimisProvider.GetBnf(SimisFormat, rv.Type));
					} catch(UnknownSimisFormatException e) {
						throw new ReaderException(BinaryReader, true, PinReaderChanged(), "", e);
					}
				}

				var contentsLength = BinaryReader.ReadUInt32();
				if (contentsLength == 0) throw new ReaderException(BinaryReader, true, PinReaderChanged(), "SimisReader got block with length of 0.", new BnfStateException(BnfState, ""));
				if (BinaryReader.BaseStream.Position + contentsLength > StreamLength) throw new ReaderException(BinaryReader, true, PinReaderChanged(), "SimisReader got block longer than stream.", new BnfStateException(BnfState, ""));

				BlockEndOffsets.Push((uint)BinaryReader.BaseStream.Position + contentsLength);
				PendingTokens.Enqueue(new SimisToken() { Kind = SimisTokenKind.BlockBegin, String = BlockEndOffsets.Peek().ToString("X8") }); // , String = String.Join(", ", BlockEndOffsets.Select<uint, string>(o => o.ToString("X8")).ToArray<string>())

				var nameLength = BinaryReader.Read();
				if (nameLength > 0) {
					if (BinaryReader.BaseStream.Position + nameLength * 2 > StreamLength) throw new ReaderException(BinaryReader, true, PinReaderChanged(), "SimisReader got block with name longer than stream.", new BnfStateException(BnfState, ""));
					for (var i = 0; i < nameLength; i++) {
						rv.String += (char)BinaryReader.ReadUInt16();
					}
				}
			}

			return rv;
		}

		#region PinReader code
		long PinReaderPosition;
		void PinReader() {
			PinReaderPosition = BinaryReader.BaseStream.Position;
		}

		int PinReaderChanged() {
			return (int)(BinaryReader.BaseStream.Position - PinReaderPosition);
		}

		void PinReaderReset() {
			BinaryReader.BaseStream.Position = PinReaderPosition;
		}
		#endregion

		static bool CompareCharArray(char[] a, char[] b) {
			if (a.Length != b.Length) return false;
			for (var i = 0; i < a.Length; i++) {
				if (a[i] != b[i]) return false;
			}
			return true;
		}

		void AutodetectStreamFormat() {
			var start = BaseStream.Position;
			var streamIsBinary = true;

			// Use the StreamReader's BOM auto-detection to populate our BinaryReader's encoding.
			{
				var sr = new StreamReader(BaseStream, true);
				sr.ReadLine();
				if (!(sr.CurrentEncoding is UTF8Encoding)) {
					streamIsBinary = false;
					BinaryReader.Close();
					BinaryReader = new BinaryReader(BaseStream, sr.CurrentEncoding);
					start += sr.CurrentEncoding.GetPreamble().Length;
				}
			}
			BaseStream.Position = start;

			{
				PinReader();
				var signature = String.Join("", BinaryReader.ReadChars(8).Select<char, string>(c => c.ToString()).ToArray<string>());
				if ((signature != "SIMISA@F") && (signature != "SIMISA@@")) {
					throw new ReaderException(BinaryReader, streamIsBinary, PinReaderChanged(), "Signature '" + signature + "' is invalid.");
				}
				StreamCompressed = (signature == "SIMISA@F");
			}

			if (StreamCompressed) {
				// This is a compressed stream. Read in the uncompressed size and DEFLATE the rest.
				var uncompressedSize = BinaryReader.ReadUInt32();
				StreamLength = BinaryReader.BaseStream.Position + uncompressedSize;
				{
					PinReader();
					var signature = String.Join("", BinaryReader.ReadChars(4).Select<char, string>(c => c.ToString()).ToArray<string>());
					if (signature != "@@@@") {
						throw new ReaderException(BinaryReader, streamIsBinary, PinReaderChanged(), "Signature '" + signature + "' is invalid.");
					}
				}
				// The stream is technically ZLIB, but we assume the selected ZLIB compression is DEFLATE (though we verify that here just in case). The ZLIB
				// header for DEFLATE is 0x78 0x9C (apparently).
				{
					PinReader();
					var zlibHeader = BinaryReader.ReadBytes(2);
					if ((zlibHeader[0] != 0x78) || (zlibHeader[1] != 0x9C)) {
						throw new ReaderException(BinaryReader, streamIsBinary, PinReaderChanged(), "ZLIB signature is invalid.");
					}
				}

				// BinaryReader -> BufferedInMemoryStream -> DeflateStream -> BinaryReader -> BaseStream.
				// The BufferedInMemoryStream is needed because DeflateStream only supports reading forwards - no seeking - and we'll potentially be jumping around.
				BinaryReader.Close();
				BinaryReader = new BinaryReader(new BufferedInMemoryStream(new DeflateStream(BaseStream, CompressionMode.Decompress)), new ByteEncoding());
			} else {
				PinReader();
				var signature = String.Join("", BinaryReader.ReadChars(8).Select<char, string>(c => c.ToString()).ToArray<string>());
				if (signature != "@@@@@@@@") {
					throw new ReaderException(BinaryReader, streamIsBinary, PinReaderChanged(), "Signature '" + signature + "' is invalid.");
				}
			}

			{
				// For uncompressed binary or test, we start from index 16. For compressed binary, we start from index 0 inside the compressed stream.
				PinReader();
				var signature = String.Join("", BinaryReader.ReadChars(4).Select<char, string>(c => c.ToString()).ToArray<string>());
				if (signature != "JINX") {
					throw new ReaderException(BinaryReader, streamIsBinary, PinReaderChanged(), "Signature '" + signature + "' is invalid.");
				}
			}
			{
				PinReader();
				var signature = String.Join("", BinaryReader.ReadChars(4).Select<char, string>(c => c.ToString()).ToArray<string>());
				if ((signature[3] != 'b') && (signature[3] != 't')) {
					throw new ReaderException(BinaryReader, streamIsBinary, PinReaderChanged(), "Signature '" + signature + "' is invalid. Final character must be 'b' or 't'.");
				}
				SimisFormat = signature.Substring(1, 2);
				if (signature[3] == 'b') {
					StreamFormat = SimisStreamFormat.Binary;
				} else {
					StreamFormat = SimisStreamFormat.Text;
				}
			}
			{
				PinReader();
				var signature = String.Join("", BinaryReader.ReadChars(8).Select<char, string>(c => c.ToString()).ToArray<string>());
				if (signature != "______\r\n") {
					throw new ReaderException(BinaryReader, streamIsBinary, PinReaderChanged(), "Signature '" + signature + "' is invalid.");
				}
			}

			if (StreamFormat == SimisStreamFormat.Text) {
				// Consume all whitespace up to the first token.
				while ((BinaryReader.BaseStream.Position < BinaryReader.BaseStream.Length) && WhitespaceChars.Any<char>(c => c == BinaryReader.PeekChar())) {
					BinaryReader.ReadChar();
				}
			}

			DoneAutoDetect = true;
		}
	}
}
