//------------------------------------------------------------------------------
// Jgr.IO.Parser library, part of MSTS Editors & Tools (http://jgrmsts.codeplex.com/).
// License: New BSD License (BSD).
//------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using Jgr.Grammar;

namespace Jgr.IO.Parser
{
	public class SimisJinxReader : SimisReader
	{
		public readonly bool JinxStreamIsBinary;
		public readonly SimisJinxFormat JinxStreamFormat;

		public bool EndOfStream { get; private set; }
		public BnfState BnfState { get; private set; }

		readonly SimisProvider SimisProvider;

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
			return new string[] { "uint", "sint", "dword", "word", "byte", "float", "string", "buffer" };
		}
		#endregion

		public SimisJinxReader(SimisStreamReader reader, SimisProvider provider)
			: this(reader, provider, null) {
		}

		public SimisJinxReader(SimisStreamReader reader, SimisProvider provider, SimisJinxFormat jinxStreamFormat)
			: base(reader) {
			JinxStreamFormat = jinxStreamFormat;
			SimisProvider = provider;
			StreamLength = reader.UncompressedLength;
			BlockEndOffsets = new Stack<uint>();
			PendingTokens = new Queue<SimisToken>();
			ReadStream(out JinxStreamIsBinary, ref JinxStreamFormat);
		}

		void ReadStream(out bool JinxStreamIsBinary, ref SimisJinxFormat JinxStreamFormat) {
			{
				PinReader();
				var signature = new String(Reader.ReadChars(4));
				if ((signature[3] != 'b') && (signature[3] != 't')) {
					throw new ReaderException(Reader, Reader.StreamIsBinary, PinReaderChanged(), "Signature '" + signature + "' is invalid. Final character must be 'b' or 't'.");
				}
				JinxStreamIsBinary = (signature[3] == 'b');
				var simisFormat = signature.Substring(1, 2);
				if (JinxStreamFormat != null) {
					if (JinxStreamFormat.Format != simisFormat) {
						throw new ReaderException(Reader, JinxStreamIsBinary, PinReaderChanged(), "Simis format '" + simisFormat + "' does not match format provided by caller '" + JinxStreamFormat.Format + "'.");
					}
				} else {
					JinxStreamFormat = SimisProvider.GetForFormat(simisFormat);
					if (JinxStreamFormat == null) {
						throw new ReaderException(Reader, JinxStreamIsBinary, PinReaderChanged(), "Simis format '" + simisFormat + "' is not known to " + SimisProvider + ".");
					}
				}
			}

			{
				PinReader();
				var signature = new String(Reader.ReadChars(8));
				if (signature != "______\r\n") {
					throw new ReaderException(Reader, JinxStreamIsBinary, PinReaderChanged(), "Signature '" + signature + "' is invalid.");
				}
			}

			if (!JinxStreamIsBinary) {
				// Consume all whitespace up to the first token.
				while ((Reader.BaseStream.Position < Reader.BaseStream.Length) && WhitespaceChars.Any(c => c == Reader.PeekChar())) {
					Reader.ReadChar();
				}
			}
		}

		public SimisToken ReadToken() {
			// Any pending tokens go first.
			if (PendingTokens.Count > 0) {
				try {
					if (PendingTokens.Peek().Kind == SimisTokenKind.BlockBegin) BnfState.EnterBlock();
					if (PendingTokens.Peek().Kind == SimisTokenKind.BlockEnd) BnfState.LeaveBlock();
				} catch (BnfStateException e) {
					throw new ReaderException(Reader, JinxStreamIsBinary, 0, "", e);
				}
				// If we've run out of stream and have no pending tokens, we're done.
				if ((Reader.BaseStream.Position >= Reader.BaseStream.Length) && (PendingTokens.Count == 1)) {
					try {
						BnfState.LeaveBlock();
					} catch (BnfStateException e) {
						throw new ReaderException(Reader, JinxStreamIsBinary, 0, "", e);
					}
					if (!BnfState.IsCompleted) throw new ReaderException(Reader, JinxStreamIsBinary, 0, "Unexpected end of stream.");
					EndOfStream = true;
				}
				return PendingTokens.Dequeue();
			}

			var rv = new SimisToken();

			if (JinxStreamIsBinary) {
				PinReader();
				rv = ReadTokenAsBinary();
				try {
					if ((rv.Kind != SimisTokenKind.BlockBegin) && (rv.Kind != SimisTokenKind.BlockEnd)) {
						BnfState.MoveTo(rv.Type);
						if (BnfState.State.Operator is NamedReferenceOperator) {
							rv.Name = ((NamedReferenceOperator)BnfState.State.Operator).Name;
						}
					} else {
						throw new ReaderException(Reader, JinxStreamIsBinary, PinReaderChanged(), "ReadTokenAsBinary returned invalid token type: " + rv.Type);
					}
				} catch (BnfStateException e) {
					throw new ReaderException(Reader, JinxStreamIsBinary, PinReaderChanged(), "", e);
				}
			} else {
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
							if (BnfState.State.Operator is NamedReferenceOperator) {
								rv.Name = ((NamedReferenceOperator)BnfState.State.Operator).Name;
							}
						}
					} else {
						throw new ReaderException(Reader, JinxStreamIsBinary, PinReaderChanged(), "ReadTokenAsText returned invalid token type: " + rv.Kind);
					}
				} catch (BnfStateException e) {
					throw new ReaderException(Reader, JinxStreamIsBinary, PinReaderChanged(), "", e);
				}

				// Consume all whitespace now that we've got a token.
				while ((Reader.BaseStream.Position < Reader.BaseStream.Length) && WhitespaceChars.Contains((char)Reader.PeekChar())) {
					Reader.ReadChar();
				}
			}

			// Any blocks that should have ended at or before this point, are now ended.
			while ((BlockEndOffsets.Count > 0) && (Reader.BaseStream.Position >= BlockEndOffsets.Peek())) {
				if (Reader.BaseStream.Position > BlockEndOffsets.Peek()) throw new ReaderException(Reader, JinxStreamIsBinary, (int)(Reader.BaseStream.Position - BlockEndOffsets.Peek()), "SimisReader stream positioned at 0x" + Reader.BaseStream.Position.ToString("X8", CultureInfo.CurrentCulture) + " but a block ended at 0x" + BlockEndOffsets.Peek().ToString("X8", CultureInfo.CurrentCulture) + "; overshot by " + (Reader.BaseStream.Position - BlockEndOffsets.Peek()) + " bytes.");
				PendingTokens.Enqueue(new SimisToken() { Kind = SimisTokenKind.BlockEnd });
				BlockEndOffsets.Pop();
			}

			// If we've run out of stream and have no pending tokens, we're done.
			if ((Reader.BaseStream.Position >= Reader.BaseStream.Length) && (PendingTokens.Count == 0)) {
				try {
					BnfState.LeaveBlock();
				} catch (BnfStateException e) {
					throw new ReaderException(Reader, JinxStreamIsBinary, 0, "", e);
				}
				if (!BnfState.IsCompleted) throw new ReaderException(Reader, JinxStreamIsBinary, 0, "Unexpected end of stream.");
				EndOfStream = true;
			}

			return rv;
		}

		SimisToken ReadTokenAsText() {
			SimisToken rv = new SimisToken();

			if ('(' == Reader.PeekChar()) {
				Reader.ReadChar();
				rv.Kind = SimisTokenKind.BlockBegin;
				return rv;
			}

			if (')' == Reader.PeekChar()) {
				Reader.ReadChar();
				rv.Kind = SimisTokenKind.BlockEnd;
				return rv;
			}

			if (':' == Reader.PeekChar()) {
				Reader.ReadChar();
				return ReadTokenAsText();
			}

			string token = ReadTokenOrString();

			if (BnfState == null) {
				try {
					BnfState = new BnfState(JinxStreamFormat.Bnf);
				} catch (UnknownSimisFormatException e) {
					throw new ReaderException(Reader, false, PinReaderChanged(), "", e);
				}
			}

			if (BnfState.IsEnterBlockTime) {
				// We should only end up here when called recursively by the
				// if (validStates.Contains(token)) code below.
				rv.Name = token;
				rv.Kind = token.Length > 0 ? SimisTokenKind.Block : SimisTokenKind.None;
				return rv;
			}

			if (token.StartsWith("_", StringComparison.InvariantCulture) || (token.ToUpperInvariant() == "COMMENT") || (token.ToUpperInvariant() == "INFO") || (token.ToUpperInvariant() == "SKIP")) {
				var oldPosition = Reader.BaseStream.Position;
				while ((Reader.BaseStream.Position < Reader.BaseStream.Length) && WhitespaceChars.Contains((char)Reader.PeekChar())) {
					Reader.ReadChar();
				}
				var nextToken = Reader.PeekChar();
				Reader.BaseStream.Position = oldPosition;
				if (nextToken == (int)'(') {
					var blockCount = 0;
					while ((Reader.BaseStream.Position < Reader.BaseStream.Length) && ((')' != Reader.PeekChar()) || (blockCount > 1))) {
						if (Reader.PeekChar() == '(') blockCount++;
						if (Reader.PeekChar() == ')') blockCount--;
						token += Reader.ReadChar();
					}
					if (Reader.BaseStream.Position >= Reader.BaseStream.Length) throw new ReaderException(Reader, false, 0, "SimisReader expected ')'; got EOF.");
					token += Reader.ReadChar();
					rv.String = token;
					rv.Name = "Comment";
					rv.Kind = SimisTokenKind.String;
					return rv;
				}
			}

			if (token.StartsWith("//", StringComparison.InvariantCulture)) {
				var blockCount = 0;
				while ((Reader.BaseStream.Position < Reader.BaseStream.Length) && ('\n' != Reader.PeekChar()) && ((')' != Reader.PeekChar()) || (blockCount > 0))) {
					if (Reader.PeekChar() == '(') blockCount++;
					if (Reader.PeekChar() == ')') blockCount--;
					token += Reader.ReadChar();
				}
				rv.String = token;
				rv.Name = "Comment";
				rv.Kind = SimisTokenKind.String;
				return rv;
			}

			var validStates = BnfState.ValidStates;
			if (validStates.Contains(token, StringComparer.InvariantCultureIgnoreCase)) {
				// Token exactly matches a valid state transition, so let's use it.
				rv.Type = validStates.First(s => s.Equals(token, StringComparison.InvariantCultureIgnoreCase));
				rv.Kind = SimisTokenKind.Block;

				// Do lookahead for block name.
				PinReader();
				while ((Reader.BaseStream.Position < Reader.BaseStream.Length) && WhitespaceChars.Contains((char)Reader.PeekChar())) {
					Reader.ReadChar();
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
						if (token.StartsWith("-", StringComparison.Ordinal)) return token.Substring(1).ToCharArray().All(c => DecDigits.Contains(c));
						return token.ToCharArray().All(c => DecDigits.Contains(c));
					case "dword":
						return (token.Length == 8) && (token.ToCharArray().All(c => HexDigits.Contains(c)));
					case "float":
						if (token.StartsWith("-", StringComparison.Ordinal)) return token.Substring(1).ToCharArray().All(c => DecFloatDigits.Contains(c));
						return token.ToCharArray().All(c => DecFloatDigits.Contains(c));
					case "string":
						return true;
					case "buffer":
					default:
						return false;
				}
			}).ToArray();

			if (validDataTypeStates.Length == 0) {
				try {
					// This is *expected* to throw! We're doing this so that we get a proper BNF exception from the failed state.
					BnfState.MoveTo(token);
				} catch (BnfStateException ex) {
					throw new ReaderException(Reader, false, PinReaderChanged(), "", ex);
				}
			}

			rv.Type = validDataTypeStates[0];
			switch (rv.Type) {
				case "uint":
					if (token.EndsWith(",", StringComparison.Ordinal)) token = token.Substring(0, token.Length - 1);
					try {
						rv.IntegerUnsigned = UInt32.Parse(token, CultureInfo.InvariantCulture);
						if (token.Length == 8) throw new ReaderException(Reader, false, PinReaderChanged(), "SimisReader expected decimal number; got possible hex '" + token + "'.");
					} catch (FormatException ex) {
						throw new ReaderException(Reader, false, PinReaderChanged(), "SimisReader failed to parse '" + token + "' as '" + rv.Type + "'.", ex);
					} catch (OverflowException ex) {
						throw new ReaderException(Reader, false, PinReaderChanged(), "SimisReader failed to parse '" + token + "' as '" + rv.Type + "'.", ex);
					}
					rv.Kind = SimisTokenKind.IntegerUnsigned;
					break;
				case "sint":
					if (token.EndsWith(",", StringComparison.Ordinal)) token = token.Substring(0, token.Length - 1);
					try {
						// Special-case -1 witten as an unsigned integer instead of signed.
						if (token == "4294967295") {
							rv.IntegerSigned = -1;
						} else {
							rv.IntegerSigned = Int32.Parse(token, CultureInfo.InvariantCulture);
						}
					} catch (FormatException ex) {
						throw new ReaderException(Reader, false, PinReaderChanged(), "SimisReader failed to parse '" + token + "' as '" + rv.Type + "'.", ex);
					} catch (OverflowException ex) {
						throw new ReaderException(Reader, false, PinReaderChanged(), "SimisReader failed to parse '" + token + "' as '" + rv.Type + "'.", ex);
					}
					rv.Kind = SimisTokenKind.IntegerSigned;
					break;
				case "dword":
					if (token.EndsWith(",", StringComparison.Ordinal)) token = token.Substring(0, token.Length - 1);
					if (token.Length != 8) throw new ReaderException(Reader, false, PinReaderChanged(), "SimisReader expected 8-digit hex number; got '" + token + "'.");
					try {
						rv.IntegerDWord = UInt32.Parse(token, NumberStyles.HexNumber, CultureInfo.InvariantCulture);
					} catch (FormatException ex) {
						throw new ReaderException(Reader, false, PinReaderChanged(), "SimisReader failed to parse '" + token + "' as '" + rv.Type + "'.", ex);
					} catch (OverflowException ex) {
						throw new ReaderException(Reader, false, PinReaderChanged(), "SimisReader failed to parse '" + token + "' as '" + rv.Type + "'.", ex);
					}
					rv.Kind = SimisTokenKind.IntegerDWord;
					break;
				case "word":
					if (token.EndsWith(",", StringComparison.Ordinal)) token = token.Substring(0, token.Length - 1);
					if (token.Length != 4) throw new ReaderException(Reader, false, PinReaderChanged(), "SimisReader expected 4-digit hex number; got '" + token + "'.");
					try {
						rv.IntegerDWord = UInt16.Parse(token, NumberStyles.HexNumber, CultureInfo.InvariantCulture);
					} catch (FormatException ex) {
						throw new ReaderException(Reader, false, PinReaderChanged(), "SimisReader failed to parse '" + token + "' as '" + rv.Type + "'.", ex);
					} catch (OverflowException ex) {
						throw new ReaderException(Reader, false, PinReaderChanged(), "SimisReader failed to parse '" + token + "' as '" + rv.Type + "'.", ex);
					}
					rv.Kind = SimisTokenKind.IntegerDWord;
					break;
				case "byte":
					if (token.EndsWith(",", StringComparison.Ordinal)) token = token.Substring(0, token.Length - 1);
					if (token.Length != 2) throw new ReaderException(Reader, false, PinReaderChanged(), "SimisReader expected 2-digit hex number; got '" + token + "'.");
					try {
						rv.IntegerDWord = Byte.Parse(token, NumberStyles.HexNumber, CultureInfo.InvariantCulture);
					} catch (FormatException ex) {
						throw new ReaderException(Reader, false, PinReaderChanged(), "SimisReader failed to parse '" + token + "' as '" + rv.Type + "'.", ex);
					} catch (OverflowException ex) {
						throw new ReaderException(Reader, false, PinReaderChanged(), "SimisReader failed to parse '" + token + "' as '" + rv.Type + "'.", ex);
					}
					rv.Kind = SimisTokenKind.IntegerDWord;
					break;
				case "float":
					if (token.EndsWith(",", StringComparison.Ordinal)) token = token.Substring(0, token.Length - 1);
					try {
						rv.Float = float.Parse(token, CultureInfo.InvariantCulture);
					} catch (FormatException ex) {
						throw new ReaderException(Reader, false, PinReaderChanged(), "SimisReader failed to parse '" + token + "' as '" + rv.Type + "'.", ex);
					} catch (OverflowException ex) {
						throw new ReaderException(Reader, false, PinReaderChanged(), "SimisReader failed to parse '" + token + "' as '" + rv.Type + "'.", ex);
					}
					rv.Kind = SimisTokenKind.Float;
					break;
				case "string":
					rv.String = token;
					rv.Kind = SimisTokenKind.String;
					break;
				default:
					throw new ReaderException(Reader, false, PinReaderChanged(), "SimisReader found unexpected data type '" + rv.Type + "'.", new BnfStateException(BnfState, ""));
			}
			return rv;
		}

		string ReadTokenOrString() {
			string token = "";
			if ('"' == Reader.PeekChar()) {
				do {
					// Eat whitespace. (This is for the 2nd and further times through, to each whitespace after the "+".)
					if ('+' == Reader.PeekChar()) Reader.ReadChar();
					while ((Reader.BaseStream.Position < Reader.BaseStream.Length) && WhitespaceChars.Any(c => c == Reader.PeekChar())) {
						Reader.ReadChar();
					}
					// Consume string.
					if (Reader.BaseStream.Position >= Reader.BaseStream.Length) throw new ReaderException(Reader, false, 0, "SimisReader expected '\"'; got EOF.");
					Reader.ReadChar(); // "\""
					while ((Reader.BaseStream.Position < Reader.BaseStream.Length) && ('"' != Reader.PeekChar())) {
						if ('\\' == Reader.PeekChar()) {
							Reader.ReadChar();
							var ch = Reader.ReadChar();
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
									throw new ReaderException(Reader, false, PinReaderChanged(), "SimisReader found unknown escape in string: \\" + ch + ".");
							}
						} else {
							token += Reader.ReadChar();
						}
					}
					if (Reader.BaseStream.Position >= Reader.BaseStream.Length) throw new ReaderException(Reader, false, 0, "SimisReader expected '\"'; got EOF.");
					Reader.ReadChar(); // "\""
					// Eat whitespace. (This is for the 2nd and further times through, to each whitespace after the "+".)
					while ((Reader.BaseStream.Position < Reader.BaseStream.Length) && WhitespaceChars.Any(c => c == Reader.PeekChar())) {
						Reader.ReadChar();
					}
				} while ('+' == Reader.PeekChar());
			} else {
				// Consume all non-whitespace, non-special characters.
				while ((Reader.BaseStream.Position < Reader.BaseStream.Length) && !WhitespaceAndSpecialChars.Any(c => c == Reader.PeekChar())) {
					token += Reader.ReadChar();
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
				if (!validStates.All(s => s == dataType || !DataTypes.Contains(s))) throw new ReaderException(Reader, true, PinReaderChanged(), "SimisReader found inconsistent data types available.", new BnfStateException(BnfState, ""));

				rv.Type = dataType;
				switch (rv.Type) {
					case "uint":
						rv.IntegerUnsigned = Reader.ReadUInt32();
						rv.Kind = SimisTokenKind.IntegerUnsigned;
						break;
					case "sint":
						rv.IntegerSigned = Reader.ReadInt32();
						rv.Kind = SimisTokenKind.IntegerSigned;
						break;
					case "dword":
						rv.IntegerDWord = Reader.ReadUInt32();
						rv.Kind = SimisTokenKind.IntegerDWord;
						break;
					case "word":
						rv.IntegerDWord = Reader.ReadUInt16();
						rv.Kind = SimisTokenKind.IntegerWord;
						break;
					case "byte":
						rv.IntegerDWord = Reader.ReadByte();
						rv.Kind = SimisTokenKind.IntegerByte;
						break;
					case "float":
						rv.Float = Reader.ReadSingle();
						rv.Kind = SimisTokenKind.Float;
						break;
					case "string":
						var stringLength = Reader.ReadUInt16();
						if (stringLength > 10000) throw new ReaderException(Reader, true, PinReaderChanged(), "SimisReader found a string longer than 10,000 characters.", new BnfStateException(BnfState, ""));
						if (Reader.BaseStream.Position + stringLength * 2 > Reader.BaseStream.Length) throw new ReaderException(Reader, true, PinReaderChanged(), "SimisReader found a string extending beyond the end of the file.", new BnfStateException(BnfState, ""));
						for (var i = 0; i < stringLength; i++) {
							rv.String += (char)Reader.ReadUInt16();
						}
						rv.Kind = SimisTokenKind.String;
						break;
					case "buffer":
						var bufferLength = BlockEndOffsets.Peek() - Reader.BaseStream.Position;
						rv.String = String.Join("", Reader.ReadBytes((int)bufferLength).Select(b => b.ToString("X2", CultureInfo.InvariantCulture)).ToArray());
						rv.Kind = SimisTokenKind.String;
						break;
					default:
						throw new ReaderException(Reader, true, PinReaderChanged(), "SimisReader found unexpected data type '" + rv.Type + "'.", new BnfStateException(BnfState, ""));
				}
			} else {
				var tokenID = Reader.ReadUInt16();
				var tokenType = Reader.ReadUInt16();
				var token = ((uint)tokenType << 16) + tokenID;
				if (!SimisProvider.TokenNames.ContainsKey(token)) throw new ReaderException(Reader, true, PinReaderChanged(), String.Format(CultureInfo.CurrentCulture, "SimisReader got invalid block: id={0:X4}, type={1:X4}.", tokenID, tokenType), new BnfStateException(BnfState, ""));
				if ((tokenType != 0x0000) && (tokenType != 0x0004)) throw new ReaderException(Reader, true, PinReaderChanged(), String.Format(CultureInfo.CurrentCulture, "SimisReader got invalid block: id={0:X4}, type={1:X4}, name={2}.", tokenID, tokenType, SimisProvider.TokenNames[token]), new BnfStateException(BnfState, ""));

				rv.Type = SimisProvider.TokenNames[token];
				rv.Kind = SimisTokenKind.Block;

				if (BnfState == null) {
					try {
						BnfState = new BnfState(JinxStreamFormat.Bnf);
					} catch(UnknownSimisFormatException e) {
						throw new ReaderException(Reader, true, PinReaderChanged(), "", e);
					}
				}

				var contentsLength = Reader.ReadUInt32();
				if (contentsLength == 0) throw new ReaderException(Reader, true, PinReaderChanged(), "SimisReader got block with length of 0.", new BnfStateException(BnfState, ""));
				if (Reader.BaseStream.Position + contentsLength > StreamLength) throw new ReaderException(Reader, true, PinReaderChanged(), "SimisReader got block longer than stream.", new BnfStateException(BnfState, ""));

				BlockEndOffsets.Push((uint)Reader.BaseStream.Position + contentsLength);
				PendingTokens.Enqueue(new SimisToken() { Kind = SimisTokenKind.BlockBegin, String = BlockEndOffsets.Peek().ToString("X8", CultureInfo.CurrentCulture) });

				var nameLength = Reader.Read();
				if (nameLength > 0) {
					if (Reader.BaseStream.Position + nameLength * 2 > StreamLength) throw new ReaderException(Reader, true, PinReaderChanged(), "SimisReader got block with name longer than stream.", new BnfStateException(BnfState, ""));
					for (var i = 0; i < nameLength; i++) {
						rv.Name += (char)Reader.ReadUInt16();
					}
				}
			}

			return rv;
		}
	}
}
