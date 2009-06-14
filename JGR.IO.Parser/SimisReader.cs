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
using JGR.Grammar;

namespace JGR.IO.Parser
{
	public enum SimisStreamFormat
	{
		Autodetect,
		Binary,
		Text
	}

	public enum SimisTokenKind
	{
		None,
		Block,
		BlockBegin,
		BlockEnd,
		String,
		Integer,
		Float
	}

	public class SimisToken
	{
		public SimisToken() {
			Type = "";
			String = "";
		}

		public SimisTokenKind Kind;
		public string Type;
		public string String;
		public long Integer;
		public double Float;
	}

	public class SimisReader //: BufferedMessageSource
	{
		public Stream BaseStream { get; protected set; }
		protected SimisProvider SimisProvider { get; set; }
		protected BinaryReader BinaryReader { get; set; }
		public SimisStreamFormat StreamFormat { get; protected set; }
		public bool StreamCompressed { get; protected set; }
		public string SimisFormat { get; protected set; }
		public bool EndOfStream { get; protected set; }
		protected bool DoneAutodetect { get; set; }
		protected long StreamLength { get; set; }

		protected readonly char[] WhitespaceChars;
		protected readonly char[] WhitespaceAndSpecialChars;
		protected readonly char[] HexDigits;
		protected readonly string[] DataTypes;

		private Stack<uint> BlockEndOffsets;
		private Queue<SimisToken> PendingTokens;
		public BNFState BNFState;

		public SimisReader(Stream stream, SimisProvider provider)
			: this(stream, provider, SimisStreamFormat.Autodetect, false, "") {
		}

		public SimisReader(Stream stream, SimisProvider provider, SimisStreamFormat format, bool compressed, string simisFormat) {
			if (!stream.CanRead) throw new InvalidDataException("Stream must support reading.");
			if (!stream.CanSeek) throw new InvalidDataException("Stream must support seeking.");
			BaseStream = stream;
			SimisProvider = provider;
			BinaryReader = new BinaryReader(BaseStream, new ByteEncoding());
			StreamFormat = format;
			StreamCompressed = compressed;
			SimisFormat = simisFormat;
			EndOfStream = false;
			DoneAutodetect = format != SimisStreamFormat.Autodetect;
			StreamLength = BaseStream.Length;

			WhitespaceChars = new char[] { ' ', '\t', '\r', '\n' };
			WhitespaceAndSpecialChars = new char[] { ' ', '\t', '\r', '\n', '(', ')', '"' };
			HexDigits = new char[] { '0', '1', '2', '3', '4', '5', '6', '7', '8', '9', 'a', 'b', 'c', 'd', 'e', 'f' };
			DataTypes = new string[] { "string", "uint", "sint", "dword", "float", "buffer" };

			BlockEndOffsets = new Stack<uint>();
			PendingTokens = new Queue<SimisToken>();
			BNFState = null;
			if (SimisFormat != "") {
				BNFState = new BNFState(SimisProvider.BNFs[SimisFormat]);
			}
		}

		public SimisToken ReadToken() {
			if (!DoneAutodetect) AutodetectStreamFormat();

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
					if (PendingTokens.Peek().Kind == SimisTokenKind.BlockBegin) BNFState.EnterBlock();
					if (PendingTokens.Peek().Kind == SimisTokenKind.BlockEnd) BNFState.LeaveBlock();
				} catch (BNFStateException e) {
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
						BNFState.EnterBlock();
					} else if (rv.Kind == SimisTokenKind.BlockEnd) {
						BNFState.LeaveBlock();
					} else if (rv.Kind != SimisTokenKind.None) {
						if (rv.Type.Length > 0) {
							BNFState.MoveTo(rv.Type);
						}
					} else {
						throw new ReaderException(BinaryReader, false, PinReaderChanged(), "ReadTokenAsText returned invalid token type: " + rv.Kind);
					}
				} catch (BNFStateException e) {
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
						BNFState.MoveTo(rv.Type);
					} else {
						throw new ReaderException(BinaryReader, false, PinReaderChanged(), "ReadTokenAsBinary returned invalid token type: " + rv.Type);
					}
				} catch (BNFStateException e) {
					throw new ReaderException(BinaryReader, true, PinReaderChanged(), "", e);
				}
			}

			// If we've run out of stream and have no pending tokens, we're done.
			if ((BinaryReader.BaseStream.Position >= BinaryReader.BaseStream.Length) && (PendingTokens.Count == 0)) {
				EndOfStream = true;
			}

			return rv;
		}

		private SimisToken ReadTokenAsText() {
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

			string token = ReadTokenOrString();

			if (BNFState.IsEnterBlockTime) {
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

			var validStates = BNFState.ValidStates;
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
			if (validDataTypeStates.Length == 0) throw new ReaderException(BinaryReader, false, PinReaderChanged(), "SimisReader found no data types available for parsing of token '" + token + "'.", new BNFStateException(BNFState, ""));

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

		private string ReadTokenOrString() {
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

		private SimisToken ReadTokenAsBinary() {
			SimisToken rv = new SimisToken();

			var validStates = BNFState.ValidStates.Where<string>(s => !s.StartsWith("<"));
			//if (validStates.Length == 0) throw new ReaderException(BinaryReader, true, PinReaderChanged(), "SimisReader found no non-meta states available.", new BNFStateException(BNFState, ""));

			// If we have any valid data types, we read that instead of a block start. They should all be the same data type, too.
			var validDataTypes = validStates.Where<string>(s => DataTypes.Contains(s)).ToArray<string>();
			if (validDataTypes.Length > 0) {
				if (!validDataTypes.All<string>(s => s == validDataTypes[0])) throw new ReaderException(BinaryReader, true, PinReaderChanged(), "SimisReader found inconsistent data types available.", new BNFStateException(BNFState, ""));

				rv.Type = validDataTypes[0];
				switch (rv.Type) {
					case "string":
						var stringLength = BinaryReader.ReadUInt16();
						if (stringLength > 10000) throw new ReaderException(BinaryReader, true, PinReaderChanged(), "SimisReader found a string longer than 10,000 characters.", new BNFStateException(BNFState, ""));
						for (var i = 0; i < stringLength; i++) {
							rv.String += (char)BinaryReader.ReadUInt16();
						}
						rv.Kind = SimisTokenKind.String;
						break;
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
					case "buffer":
						var bufferLength = BlockEndOffsets.Peek() - BinaryReader.BaseStream.Position;
						rv.String = String.Join("", BinaryReader.ReadBytes((int)bufferLength).Select<byte, string>(b => b.ToString("X2")).ToArray<string>());
						rv.Kind = SimisTokenKind.String;
						break;
					default:
						throw new ReaderException(BinaryReader, true, PinReaderChanged(), "SimisReader found unexpected data type '" + rv.Type + "'.", new BNFStateException(BNFState, ""));
				}
			} else {
				var tokenID = BinaryReader.ReadUInt16();
				var tokenType = BinaryReader.ReadUInt16();
				var token = ((uint)tokenType << 16) + tokenID;
				if (!SimisProvider.TokenNames.ContainsKey(token)) throw new ReaderException(BinaryReader, true, PinReaderChanged(), String.Format("SimisReader got invalid block: id={0:X4}, type={1:X4}.", tokenID, tokenType), new BNFStateException(BNFState, ""));
				if ((tokenType != 0x0000) && (tokenType != 0x0004)) throw new ReaderException(BinaryReader, true, PinReaderChanged(), String.Format("SimisReader got invalid block: id={0:X4}, type={1:X4}, name={2}.", tokenID, tokenType, SimisProvider.TokenNames[token]), new BNFStateException(BNFState, ""));

				rv.Type = SimisProvider.TokenNames[token];
				rv.Kind = SimisTokenKind.Block;

				var contentsLength = BinaryReader.ReadUInt32();
				if (contentsLength == 0) throw new ReaderException(BinaryReader, true, PinReaderChanged(), "SimisReader got block with length of 0.", new BNFStateException(BNFState, ""));
				if (BinaryReader.BaseStream.Position + contentsLength > StreamLength) throw new ReaderException(BinaryReader, true, PinReaderChanged(), "SimisReader got block longer than stream.", new BNFStateException(BNFState, ""));

				BlockEndOffsets.Push((uint)BinaryReader.BaseStream.Position + contentsLength);
				PendingTokens.Enqueue(new SimisToken() { Kind = SimisTokenKind.BlockBegin, String = BlockEndOffsets.Peek().ToString("X8") }); // , String = String.Join(", ", BlockEndOffsets.Select<uint, string>(o => o.ToString("X8")).ToArray<string>())

				var nameLength = BinaryReader.Read();
				if (nameLength > 0) {
					if (BinaryReader.BaseStream.Position + nameLength * 2 > StreamLength) throw new ReaderException(BinaryReader, true, PinReaderChanged(), "SimisReader got block with name longer than stream.", new BNFStateException(BNFState, ""));
					for (var i = 0; i < nameLength; i++) {
						rv.String += (char)BinaryReader.ReadUInt16();
					}
				}
			}

			return rv;
		}

		#region PinReader code
		private long PinReaderPosition = 0;
		private void PinReader() {
			PinReaderPosition = BinaryReader.BaseStream.Position;
		}

		private int PinReaderChanged() {
			return (int)(BinaryReader.BaseStream.Position - PinReaderPosition);
		}

		private void PinReaderReset() {
			BinaryReader.BaseStream.Position = PinReaderPosition;
		}
		#endregion

		private bool CompareCharArray(char[] a, char[] b) {
			if (a.Length != b.Length) return false;
			for (var i = 0; i < a.Length; i++) {
				if (a[i] != b[i]) return false;
			}
			return true;
		}

		private void AutodetectStreamFormat() {
			var start = BaseStream.Position;
			var streamIsBinary = true;

			// Use the StreamReader's BOM auto-detection to populate our BinaryReader's encoding.
			{
				var sr = new StreamReader(BaseStream, true);
				sr.ReadLine();
				if (!(sr.CurrentEncoding is UTF8Encoding)) {
					streamIsBinary = false;
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
				BinaryReader = new BinaryReader(new BufferedInMemoryStream(new DeflateStream(BinaryReader.BaseStream, CompressionMode.Decompress)), new ByteEncoding());
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

			BNFState = new BNFState(SimisProvider.BNFs[SimisFormat]);
			DoneAutodetect = true;
		}
	}
}
