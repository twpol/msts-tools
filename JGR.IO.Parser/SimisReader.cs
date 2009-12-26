//------------------------------------------------------------------------------
// JGR.IO.Parser library, part of MSTS Editors & Tools (http://jgrmsts.codeplex.com/).
// License: Microsoft Public License (Ms-PL).
//------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Diagnostics;
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
		public static TraceSwitch TraceSwitch = new TraceSwitch("jgr.io.parser.simisreader", "Trace SimisReader");

		public SimisFormat SimisFormat { get; private set; }
		public SimisStreamFormat StreamFormat { get; private set; }
		public bool StreamCompressed { get; private set; }
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
		static readonly char[] DecDigits = InitDecDigits();
		static readonly char[] DecFloatDigits = InitDecFloatDigits();
		static readonly char[] HexDigits = InitHexDigits();
		static readonly string[] DataTypes = InitDataTypes();
		#region static init functions
		static char[] InitWhitespaceChars() {
			return new char[] { ' ', '\t', '\r', '\n' };
		}

		static char[] InitWhitespaceAndSpecialChars() {
			return new char[] { ' ', '\t', '\r', '\n', '(', ')', '"', ':' };
		}

		static char[] InitDecDigits() {
			return new char[] { '0', '1', '2', '3', '4', '5', '6', '7', '8', '9' };
		}

		static char[] InitDecFloatDigits() {
			return InitDecDigits().Union(new char[] { '.', 'e', 'E', '+', '-' }).ToArray();
		}

		static char[] InitHexDigits() {
			return InitDecDigits().Union(new char[] { 'a', 'b', 'c', 'd', 'e', 'f', 'A', 'B', 'C', 'D', 'E', 'F' }).ToArray();
		}

		static string[] InitDataTypes() {
			return new string[] { "uint", "sint", "dword", "float", "string", "buffer" };
		}
		#endregion

		public SimisReader(Stream stream, SimisProvider provider)
			: this(stream, provider, null, SimisStreamFormat.AutoDetect, false) {
		}

		public SimisReader(Stream stream, SimisProvider provider, SimisFormat simisFormat)
			: this(stream, provider, simisFormat, SimisStreamFormat.AutoDetect, false) {
		}

		public SimisReader(Stream stream, SimisProvider provider, SimisFormat simisFormat, SimisStreamFormat format, bool compressed) {
			if (!stream.CanRead) throw new InvalidDataException("Stream must support reading.");
			if (!stream.CanSeek) throw new InvalidDataException("Stream must support seeking.");

			BaseStream = new UnclosableStream(stream);
			SimisProvider = provider;
			SimisFormat = simisFormat;
			StreamFormat = format;
			StreamCompressed = compressed;
			EndOfStream = false;
			BinaryReader = new BinaryReader(BaseStream, new ByteEncoding());
			DoneAutoDetect = format != SimisStreamFormat.AutoDetect;
			StreamLength = BaseStream.Length;
			BlockEndOffsets = new Stack<uint>();
			PendingTokens = new Queue<SimisToken>();
		}

		public SimisToken ReadToken() {
			if (!DoneAutoDetect) {
				AutodetectStreamFormat();
			}

			// Any pending tokens go first.
			if (PendingTokens.Count > 0) {
				try {
					if (PendingTokens.Peek().Kind == SimisTokenKind.BlockBegin) BnfState.EnterBlock();
					if (PendingTokens.Peek().Kind == SimisTokenKind.BlockEnd) BnfState.LeaveBlock();
				} catch (BnfStateException e) {
					throw new ReaderException(BinaryReader, StreamFormat == SimisStreamFormat.Binary, 0, "", e);
				}
				// If we've run out of stream and have no pending tokens, we're done.
				if ((BinaryReader.BaseStream.Position >= BinaryReader.BaseStream.Length) && (PendingTokens.Count == 1)) {
					if (!BnfState.IsEmpty) throw new ReaderException(BinaryReader, StreamFormat == SimisStreamFormat.Binary, 0, "Unexpected end of stream.");
					EndOfStream = true;
				}
				if (SimisReader.TraceSwitch.TraceInfo) {
					var token = PendingTokens.Dequeue();
					Trace.WriteLine("Token: " + token);
					return token;
				}
				return PendingTokens.Dequeue();
			}

			var rv = new SimisToken();

			switch (StreamFormat) {
				case SimisStreamFormat.Text:
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
								if (BnfState.State.Op is NamedReferenceOperator) {
									rv.Name = ((NamedReferenceOperator)BnfState.State.Op).Name;
								}
							}
						} else {
							throw new ReaderException(BinaryReader, false, PinReaderChanged(), "ReadTokenAsText returned invalid token type: " + rv.Kind);
						}
					} catch (BnfStateException e) {
						throw new ReaderException(BinaryReader, false, PinReaderChanged(), "", e);
					}

					// Consume all whitespace now that we've got a token.
					while ((BinaryReader.BaseStream.Position < BinaryReader.BaseStream.Length) && WhitespaceChars.Contains((char)BinaryReader.PeekChar())) {
						BinaryReader.ReadChar();
					}
					break;
				case SimisStreamFormat.Binary:
					PinReader();
					rv = ReadTokenAsBinary();
					try {
						if ((rv.Kind != SimisTokenKind.BlockBegin) && (rv.Kind != SimisTokenKind.BlockEnd)) {
							BnfState.MoveTo(rv.Type);
							if (BnfState.State.Op is NamedReferenceOperator) {
								rv.Name = ((NamedReferenceOperator)BnfState.State.Op).Name;
							}
						} else {
							throw new ReaderException(BinaryReader, true, PinReaderChanged(), "ReadTokenAsBinary returned invalid token type: " + rv.Type);
						}
					} catch (BnfStateException e) {
						throw new ReaderException(BinaryReader, true, PinReaderChanged(), "", e);
					}
					break;
			}

			// Any blocks that should have ended at or before this point, are now ended.
			while ((BlockEndOffsets.Count > 0) && (BinaryReader.BaseStream.Position >= BlockEndOffsets.Peek())) {
				if (BinaryReader.BaseStream.Position > BlockEndOffsets.Peek()) throw new ReaderException(BinaryReader, StreamFormat == SimisStreamFormat.Binary, (int)(BinaryReader.BaseStream.Position - BlockEndOffsets.Peek()), "SimisReader stream positioned at 0x" + BinaryReader.BaseStream.Position.ToString("X8") + " but a block ended at 0x" + BlockEndOffsets.Peek().ToString("X8") + "; overshot by " + (BinaryReader.BaseStream.Position - BlockEndOffsets.Peek()) + " bytes.");
				PendingTokens.Enqueue(new SimisToken() { Kind = SimisTokenKind.BlockEnd });
				BlockEndOffsets.Pop();
			}

			// If we've run out of stream and have no pending tokens, we're done.
			if ((BinaryReader.BaseStream.Position >= BinaryReader.BaseStream.Length) && (PendingTokens.Count == 0)) {
				if (!BnfState.IsEmpty) throw new ReaderException(BinaryReader, StreamFormat == SimisStreamFormat.Binary, 0, "Unexpected end of stream.");
				EndOfStream = true;
			}

			if (SimisReader.TraceSwitch.TraceInfo) {
				Trace.WriteLine("Token: " + rv);
				if (EndOfStream) {
					Trace.WriteLine("End Of Stream");
				}
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
					BnfState = new BnfState(SimisFormat.Bnf);
				} catch (UnknownSimisFormatException e) {
					throw new ReaderException(BinaryReader, false, PinReaderChanged(), "", e);
				}
			}

			if (BnfState.IsEnterBlockTime) {
				// We should only end up here when called recursively by the
				// if (validStates.Contains(token)) code below.
				rv.Name = token;
				rv.Kind = token.Length > 0 ? SimisTokenKind.Block : SimisTokenKind.None;
				return rv;
			}

			if ((token.ToLower() == "skip") || (token.ToLower() == "comment")) {
				var blockCount = 0;
				while ((BinaryReader.BaseStream.Position < BinaryReader.BaseStream.Length) && ((')' != BinaryReader.PeekChar()) || (blockCount > 1))) {
					if (BinaryReader.PeekChar() == '(') blockCount++;
					if (BinaryReader.PeekChar() == ')') blockCount--;
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
				while ((BinaryReader.BaseStream.Position < BinaryReader.BaseStream.Length) && WhitespaceChars.Contains((char)BinaryReader.PeekChar())) {
					BinaryReader.ReadChar();
				}
				string name = ReadTokenOrString();
				if (name.Length > 0) {
					rv.Name = name;
				} else {
					PinReaderReset();
				}

				return rv;
			}

			var validDataTypeStates = validStates.Where(s => {
				switch (s) {
					case "uint":
						return token.ToCharArray().All(c => DecDigits.Contains(c));
					case "sint":
						if (token.StartsWith("-")) return token.Substring(1).ToCharArray().All(c => DecDigits.Contains(c));
						return token.ToCharArray().All(c => DecDigits.Contains(c));
					case "dword":
						return (token.Length == 8) && (token.ToCharArray().All(c => HexDigits.Contains(c)));
					case "float":
						if (token.StartsWith("-")) return token.Substring(1).ToCharArray().All(c => DecFloatDigits.Contains(c));
						return token.ToCharArray().All(c => DecFloatDigits.Contains(c));
					case "string":
						return true;
					case "buffer":
					default:
						return false;
				}
			}).ToArray();
			if (validDataTypeStates.Length == 0) throw new ReaderException(BinaryReader, false, PinReaderChanged(), "SimisReader found no data types available for parsing of token '" + token + "'.", new BnfStateException(BnfState, ""));

			rv.Type = validDataTypeStates[0];
			switch (rv.Type) {
				case "uint":
					if (token.EndsWith(",")) token = token.Substring(0, token.Length - 1);
					try {
						rv.IntegerUnsigned = UInt32.Parse(token);
						if (token.Length == 8) throw new ReaderException(BinaryReader, false, PinReaderChanged(), "SimisReader expected decimal number; got possible hex '" + token + "'.");
					} catch (FormatException ex) {
						throw new ReaderException(BinaryReader, false, PinReaderChanged(), "SimisReader failed to parse '" + token + "' as '" + rv.Type + "'.", ex);
					} catch (OverflowException ex) {
						throw new ReaderException(BinaryReader, false, PinReaderChanged(), "SimisReader failed to parse '" + token + "' as '" + rv.Type + "'.", ex);
					}
					rv.Kind = SimisTokenKind.IntegerUnsigned;
					break;
				case "sint":
					if (token.EndsWith(",")) token = token.Substring(0, token.Length - 1);
					try {
						rv.IntegerSigned = Int32.Parse(token);
					} catch (FormatException ex) {
						throw new ReaderException(BinaryReader, false, PinReaderChanged(), "SimisReader failed to parse '" + token + "' as '" + rv.Type + "'.", ex);
					} catch (OverflowException ex) {
						throw new ReaderException(BinaryReader, false, PinReaderChanged(), "SimisReader failed to parse '" + token + "' as '" + rv.Type + "'.", ex);
					}
					rv.Kind = SimisTokenKind.IntegerSigned;
					break;
				case "dword":
					if (token.EndsWith(",")) token = token.Substring(0, token.Length - 1);
					if (token.Length != 8) throw new ReaderException(BinaryReader, false, PinReaderChanged(), "SimisReader expected 8-digit hex number; got '" + token + "'.");
					try {
						rv.IntegerDWord = UInt32.Parse(token, NumberStyles.HexNumber);
					} catch (FormatException ex) {
						throw new ReaderException(BinaryReader, false, PinReaderChanged(), "SimisReader failed to parse '" + token + "' as '" + rv.Type + "'.", ex);
					} catch (OverflowException ex) {
						throw new ReaderException(BinaryReader, false, PinReaderChanged(), "SimisReader failed to parse '" + token + "' as '" + rv.Type + "'.", ex);
					}
					rv.Kind = SimisTokenKind.IntegerDWord;
					break;
				case "float":
					if (token.EndsWith(",")) token = token.Substring(0, token.Length - 1);
					try {
						rv.Float = float.Parse(token);
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
					while ((BinaryReader.BaseStream.Position < BinaryReader.BaseStream.Length) && WhitespaceChars.Any(c => c == BinaryReader.PeekChar())) {
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
					while ((BinaryReader.BaseStream.Position < BinaryReader.BaseStream.Length) && WhitespaceChars.Any(c => c == BinaryReader.PeekChar())) {
						BinaryReader.ReadChar();
					}
				} while ('+' == BinaryReader.PeekChar());
			} else {
				// Consume all non-whitespace, non-special characters.
				while ((BinaryReader.BaseStream.Position < BinaryReader.BaseStream.Length) && !WhitespaceAndSpecialChars.Contains((char)BinaryReader.PeekChar())) {
					token += BinaryReader.ReadChar();
				}
			}
			return token;
		}

		SimisToken ReadTokenAsBinary() {
			SimisToken rv = new SimisToken();

			var validStates = (BnfState == null ? new string[] { } : BnfState.ValidStates);
			//if (validStates.Length == 0) throw new ReaderException(BinaryReader, true, PinReaderChanged(), "SimisReader found no non-meta states available.", new BNFStateException(BNFState, ""));

			// If we have any valid data types, we read that instead of a block start. They should all be the same data type, too.
			var dataType = validStates.FirstOrDefault(s => DataTypes.Contains(s));
			if (dataType != null) {
				if (!validStates.All(s => s == dataType || !DataTypes.Contains(s))) throw new ReaderException(BinaryReader, true, PinReaderChanged(), "SimisReader found inconsistent data types available.", new BnfStateException(BnfState, ""));

				rv.Type = dataType;
				switch (rv.Type) {
					case "uint":
						rv.IntegerUnsigned = BinaryReader.ReadUInt32();
						rv.Kind = SimisTokenKind.IntegerUnsigned;
						break;
					case "sint":
						rv.IntegerSigned = BinaryReader.ReadInt32();
						rv.Kind = SimisTokenKind.IntegerSigned;
						break;
					case "dword":
						rv.IntegerDWord = BinaryReader.ReadUInt32();
						rv.Kind = SimisTokenKind.IntegerDWord;
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
						rv.String = String.Join("", BinaryReader.ReadBytes((int)bufferLength).Select(b => b.ToString("X2")).ToArray());
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
						BnfState = new BnfState(SimisFormat.Bnf);
					} catch(UnknownSimisFormatException e) {
						throw new ReaderException(BinaryReader, true, PinReaderChanged(), "", e);
					}
				}

				var contentsLength = BinaryReader.ReadUInt32();
				if (contentsLength == 0) throw new ReaderException(BinaryReader, true, PinReaderChanged(), "SimisReader got block with length of 0.", new BnfStateException(BnfState, ""));
				if (BinaryReader.BaseStream.Position + contentsLength > StreamLength) throw new ReaderException(BinaryReader, true, PinReaderChanged(), "SimisReader got block longer than stream.", new BnfStateException(BnfState, ""));

				BlockEndOffsets.Push((uint)BinaryReader.BaseStream.Position + contentsLength);
				PendingTokens.Enqueue(new SimisToken() { Kind = SimisTokenKind.BlockBegin, String = BlockEndOffsets.Peek().ToString("X8") });

				var nameLength = BinaryReader.Read();
				if (nameLength > 0) {
					if (BinaryReader.BaseStream.Position + nameLength * 2 > StreamLength) throw new ReaderException(BinaryReader, true, PinReaderChanged(), "SimisReader got block with name longer than stream.", new BnfStateException(BnfState, ""));
					for (var i = 0; i < nameLength; i++) {
						rv.Name += (char)BinaryReader.ReadUInt16();
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
			if (SimisReader.TraceSwitch.TraceInfo) Trace.WriteLine("Autodetecting stream format...");

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
				var signature = String.Join("", BinaryReader.ReadChars(8).Select(c => c.ToString()).ToArray());
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
					var signature = String.Join("", BinaryReader.ReadChars(4).Select(c => c.ToString()).ToArray());
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
				var signature = String.Join("", BinaryReader.ReadChars(8).Select(c => c.ToString()).ToArray());
				if (signature != "@@@@@@@@") {
					throw new ReaderException(BinaryReader, streamIsBinary, PinReaderChanged(), "Signature '" + signature + "' is invalid.");
				}
			}

			{
				// For uncompressed binary or test, we start from index 16. For compressed binary, we start from index 0 inside the compressed stream.
				PinReader();
				var signature = String.Join("", BinaryReader.ReadChars(4).Select(c => c.ToString()).ToArray());
				// If we put ACE support into SimisReader...
				//if (signature == "\x01\x00\x00\x00") {
				//	StreamFormat = SimisStreamFormat.ACE;
				//	DoneAutoDetect = true;
				//	return;
				//}
				if (signature != "JINX") {
					throw new ReaderException(BinaryReader, streamIsBinary, PinReaderChanged(), "Signature '" + signature + "' is invalid.");
				}
			}
			{
				PinReader();
				var signature = String.Join("", BinaryReader.ReadChars(4).Select(c => c.ToString()).ToArray());
				if ((signature[3] != 'b') && (signature[3] != 't')) {
					throw new ReaderException(BinaryReader, streamIsBinary, PinReaderChanged(), "Signature '" + signature + "' is invalid. Final character must be 'b' or 't'.");
				}
				var simisFormat = signature.Substring(1, 2);
				if (SimisFormat != null) {
					if (SimisFormat.Format != simisFormat) {
						throw new ReaderException(BinaryReader, streamIsBinary, PinReaderChanged(), "Simis format '" + simisFormat + "' does not match format provided by caller '" + SimisFormat.Format + "'.");
					}
				} else {
					SimisFormat = SimisProvider.GetForFormat(simisFormat);
				}
				if (signature[3] == 'b') {
					StreamFormat = SimisStreamFormat.Binary;
				} else {
					StreamFormat = SimisStreamFormat.Text;
				}
			}
			{
				PinReader();
				var signature = String.Join("", BinaryReader.ReadChars(8).Select(c => c.ToString()).ToArray());
				if (signature != "______\r\n") {
					throw new ReaderException(BinaryReader, streamIsBinary, PinReaderChanged(), "Signature '" + signature + "' is invalid.");
				}
			}

			if (StreamFormat == SimisStreamFormat.Text) {
				// Consume all whitespace up to the first token.
				while ((BinaryReader.BaseStream.Position < BinaryReader.BaseStream.Length) && WhitespaceChars.Any(c => c == BinaryReader.PeekChar())) {
					BinaryReader.ReadChar();
				}
			}

			DoneAutoDetect = true;
			if (SimisReader.TraceSwitch.TraceInfo) Trace.WriteLine("Format: " + (StreamCompressed ? "(compressed) " : "") + StreamFormat + " " + SimisFormat.Name + " (*." + SimisFormat.Extension + ")");
		}
	}
}
