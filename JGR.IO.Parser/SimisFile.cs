//------------------------------------------------------------------------------
// JGR.IO.Parser library, part of MSTS Editors & Tools (http://jgrmsts.codeplex.com/).
// License: Microsoft Public License (Ms-PL).
//------------------------------------------------------------------------------

using System.Collections.Generic;
using System.IO;
using JGR.Grammar;

namespace JGR.IO.Parser
{
	public class SimisFile
	{
		public string Filename { get; set; }
		public List<SimisBlock> Roots;
		public SimisStreamFormat StreamFormat;
		public bool StreamCompressed;
		public string SimisFormat;
		protected SimisProvider SimisProvider;

		public SimisFile(string filename, SimisProvider provider) {
			Filename = filename;
			Roots = new List<SimisBlock>();
			SimisProvider = provider;
		}

		public void ReadFile() {
			try {
				using (var fileStream = File.OpenRead(Filename)) {
					var reader = new SimisReader(new BufferedInMemoryStream(fileStream), SimisProvider);

					var blockStack = new Stack<SimisBlock>();
					while (!reader.EndOfStream) {
						var token = reader.ReadToken();
						var key = (blockStack.Count > 0 ? blockStack.Peek().Key + "." : "");
						var name = "";
						if ((reader.BNFState != null) && (reader.BNFState.State != null) && (reader.BNFState.State.Op is NamedReferenceOperator)) {
							name = ((NamedReferenceOperator)reader.BNFState.State.Op).Name;
						}
						key += (token.Kind == SimisTokenKind.Block ? token.Type : name);

						switch (token.Kind) {
							case SimisTokenKind.Block:
								var block = new SimisBlock(this, key, token.Type, token.String);
								if (blockStack.Count == 0) {
									Roots.Add(block);
								} else {
									blockStack.Peek().Nodes.Add(block);
								}
								blockStack.Push(block);
								break;
							case SimisTokenKind.BlockBegin:
								break;
							case SimisTokenKind.BlockEnd:
								blockStack.Pop();
								break;
							case SimisTokenKind.String:
								blockStack.Peek().Nodes.Add(new SimisBlockValueString(this, key, token.Type, name, token.String));
								break;
							case SimisTokenKind.Integer:
								blockStack.Peek().Nodes.Add(new SimisBlockValueInteger(this, key, token.Type, name, token.Integer));
								break;
							case SimisTokenKind.Float:
								blockStack.Peek().Nodes.Add(new SimisBlockValueFloat(this, key, token.Type, name, token.Float));
								break;
						}
					}

					// We need to do this AFTER reading the file, otherwise they'll not be correct.
					StreamFormat = reader.StreamFormat;
					StreamCompressed = reader.StreamCompressed;
					SimisFormat = reader.SimisFormat;
				}
			} catch (ReaderException e) {
				throw new FileException(Filename, e);
			}
		}

		public void WriteFile() {
			try {
				using (var fileStream = File.Create(Filename)) {
					var writer = new SimisWriter(fileStream, SimisProvider, StreamFormat, StreamCompressed, SimisFormat);
					foreach (var root in Roots) {
						WriteBlock(writer, root);
					}
				}
			} catch (ReaderException e) {
				throw new FileException(Filename, e);
			}
		}

		private void WriteBlock(SimisWriter writer, SimisBlock block) {
			writer.WriteToken(new SimisToken() { Kind = SimisTokenKind.Block, Type = block.Type, String = block.Name });
			writer.WriteToken(new SimisToken() { Kind = SimisTokenKind.BlockBegin });
			foreach (var child in block.Nodes) {
				if (child is SimisBlockValueInteger) {
					writer.WriteToken(new SimisToken() { Kind = SimisTokenKind.Integer, Integer = (long)((SimisBlockValueInteger)child).Value });
				} else if (child is SimisBlockValueFloat) {
					writer.WriteToken(new SimisToken() { Kind = SimisTokenKind.Float, Float = (double)((SimisBlockValueFloat)child).Value });
				} else if (child is SimisBlockValueString) {
					writer.WriteToken(new SimisToken() { Kind = SimisTokenKind.String, String = (string)((SimisBlockValueString)child).Value });
				} else {
					WriteBlock(writer, child);
				}
			}
			writer.WriteToken(new SimisToken() { Kind = SimisTokenKind.BlockEnd });
		}
	}

	public class SimisBlock
	{
		public readonly SimisFile File;
		public readonly string Key;
		public string Type;
		public string Name;
		public readonly List<SimisBlock> Nodes;

		public SimisBlock(SimisFile file, string key, string type, string name) {
			File = file;
			Key = key;
			Type = type;
			Name = name;
			Nodes = new List<SimisBlock>();
		}
	}

	public class SimisBlockValue : SimisBlock
	{
		protected object _value;
		protected SimisBlockValue(SimisFile file, string key, string type, string name, object value)
			: base(file, key, type, name) {
			_value = value;
		}

		public object Value {
			get {
				return _value;
			}
			set {
				_value = value;
			}
		}
	}

	public class SimisBlockValueInteger : SimisBlockValue
	{
		public SimisBlockValueInteger(SimisFile file, string key, string type, string name, long value)
			: base(file, key, type, name, value) {
		}

		public override string ToString() {
			return ((long)_value).ToString();
		}
	}

	public class SimisBlockValueFloat : SimisBlockValue
	{
		public SimisBlockValueFloat(SimisFile file, string key, string type, string name, double value)
			: base(file, key, type, name, value) {
		}

		public override string ToString() {
			return ((double)_value).ToString("G6");
		}
	}

	public class SimisBlockValueString : SimisBlockValue
	{
		public SimisBlockValueString(SimisFile file, string key, string type, string name, string value)
			: base(file, key, type, name, value) {
		}

		public override string ToString() {
			return (string)_value;
		}
	}

	//enum SimisParserToken
	//{
	//    None,
	//    Block,
	//    Text,
	//    Integer,
	//    Double,
	//    BlockBegin,
	//    BlockEnd
	//}

	//interface ISimisParser
	//{
	//    SimisParserToken NextToken();
	//    string TokenType { get; }
	//    string TokenText { get; }
	//    double TokenNumber { get; }
	//    BNF BNF { get; set; }
	//    Dictionary<uint, string> TokenNames { get; set; }
	//    BNFState BNFState { get; }
	//}

	//internal abstract class SimisParser : ISimisParser
	//{
	//    protected readonly SimisFile File;
	//    protected BinaryReader Reader { get; private set; }
	//    public BNF BNF { get; set; }
	//    public Dictionary<uint, string> TokenNames { get; set; }
	//    public BNFState BNFState { get; protected set; }

	//    protected SimisParser(SimisFile file, BinaryReader reader) {
	//        File = file;
	//        Reader = reader;
	//        TokenNames = new Dictionary<uint, string>();
	//        ResetToken();
	//    }

	//    protected void ResetToken() {
	//        TokenType = "";
	//        TokenText = "";
	//        TokenNumber = 0.0;
	//    }


	//    #region ISimisParser Members
	//    public virtual SimisParserToken NextToken() { return SimisParserToken.None; }
	//    public string TokenType { get; protected set; }
	//    public string TokenText { get; protected set; }
	//    public double TokenNumber { get; protected set; }
	//    #endregion
	//}

	//internal class SimisParserText : SimisParser
	//{
	//    private readonly char[] whitespaceChars;
	//    private readonly char[] whitespaceAndSpecialChars;
	//    private readonly char[] numberChars;
	//    private readonly char[] nonhexNumberChars;
	//    private readonly List<string> dataTypes;

	//    public SimisParserText(SimisFile file, BinaryReader reader) : base(file, reader) {
	//        whitespaceChars = new char[] { ' ', '\t', '\r', '\n' };
	//        whitespaceAndSpecialChars = new char[] { ' ', '\t', '\r', '\n', '(', ')', '"' };
	//        numberChars = new char[] { '-', '0', '1', '2', '3', '4', '5', '6', '7', '8', '9', '.' };
	//        nonhexNumberChars = new char[] { '-', '.' };
	//        dataTypes = new List<string>(new string[] { "string", "uint", "sint", "dword", "float" });
	//    }

	//    public override SimisParserToken NextToken() {
	//        ResetToken();

	//        // First, chomp whitespace.
	//        while ((Reader.BaseStream.Position < Reader.BaseStream.Length) && whitespaceChars.Any<char>(c => c == Reader.PeekChar())) {
	//            Reader.ReadChar();
	//        }
	//        if (Reader.BaseStream.Position >= Reader.BaseStream.Length) {
	//            if (!BNFState.IsEmpty) throw new XInvalidSimisFormatException(File.Filename, Reader, 0, BNFState, "Simis parser ran out of data with unfinished BNF rules left. See below for BNF state.");
	//            return SimisParserToken.None;
	//        }

	//        string token = "";
	//        if ('(' == Reader.PeekChar()) {
	//            token += Reader.ReadChar();
	//        } else if (')' == Reader.PeekChar()) {
	//            token += Reader.ReadChar();
	//        } else if ('"' == Reader.PeekChar()) {
	//            do {
	//                // Eat whitespace. (This is for the 2nd and further times through, to each whitespace after the "+".)
	//                if ('+' == Reader.PeekChar()) Reader.ReadChar();
	//                while ((Reader.BaseStream.Position < Reader.BaseStream.Length) && whitespaceChars.Any<char>(c => c == Reader.PeekChar())) {
	//                    Reader.ReadChar();
	//                }
	//                // Consume string.
	//                Reader.ReadChar(); // "\""
	//                while ((Reader.BaseStream.Position < Reader.BaseStream.Length) && ('"' != Reader.PeekChar())) {
	//                    if ('\\' == Reader.PeekChar()) {
	//                        Reader.ReadChar();
	//                        switch ((char)Reader.PeekChar()) {
	//                            case '\\':
	//                                token += "\\";
	//                                break;
	//                            case '"':
	//                                token += "\"";
	//                                break;
	//                            case 't':
	//                                token += "\t";
	//                                break;
	//                            case 'n':
	//                                token += "\n";
	//                                break;
	//                            default:
	//                                throw new XInvalidSimisFormatException(File.Filename, Reader, 0, BNFState, "Simis parser found unknown escape in string: \\" + (char)Reader.PeekChar() + ".");
	//                        }
	//                        Reader.ReadChar();
	//                    } else {
	//                        token += Reader.ReadChar();
	//                    }
	//                }
	//                Reader.ReadChar(); // "\""
	//                // Eat whitespace. (This is for the 2nd and further times through, to each whitespace after the "+".)
	//                while ((Reader.BaseStream.Position < Reader.BaseStream.Length) && whitespaceChars.Any<char>(c => c == Reader.PeekChar())) {
	//                    Reader.ReadChar();
	//                }
	//            } while ('+' == Reader.PeekChar());
	//        } else {
	//            while ((Reader.BaseStream.Position < Reader.BaseStream.Length) && whitespaceAndSpecialChars.All<char>(c => c != Reader.PeekChar())) {
	//                token += Reader.ReadChar();
	//            }
	//        }

	//        if (BNFState == null) {
	//            // First token in the file must match the production FILE.
	//            if ((token == "(") || (token == ")") || token.ToCharArray().All<char>(t => numberChars.Any<char>(c => t == c))) throw new XInvalidSimisFormatException(File.Filename, Reader, 0, BNFState, "Simis parser expected token; got '" + token + "'.");
	//            if (!BNF.Definitions.ContainsKey("FILE")) throw new XInvalidSimisFormatException(File.Filename, Reader, 0, BNFState, "Simis parser cannot find FILE definition in BNF.");
	//            if (BNF.Definitions["FILE"].Expression == null) throw new XInvalidSimisFormatException(File.Filename, Reader, 0, BNFState, "Simis parser found BFN definition for FILE which is empty.");
	//            //if (BNF.Definitions["FILE"].Expression.Op != OperatorType.Reference) throw new InvalidSimisFormatException(File.Filename, Reader, 0, BNFState, "Simis parser found BFN definition for FILE doesn't contain just a single reference.");
	//            //var rootOp = (ReferenceOperator)BNF.Definitions["FILE"].Expression;
	//            //if (!BNF.Productions.ContainsKey(rootOp.Reference)) throw new InvalidSimisFormatException(File.Filename, Reader, 0, BNFState, "Simis parser found BNF symbol " + rootOp.Reference + " is not a production.");
	//            //if (token != rootOp.Reference) throw new InvalidSimisFormatException(File.Filename, Reader, 0, BNFState, "Simis parser expected '" + rootOp.Reference + "'; got '" + token + "'.");
	//            BNFState = new BNFState(BNF);
	//            BNFState.RegisterMessageSink(File);
	//        }

	//        if (token == "(") {
	//            try {
	//                BNFState.EnterBlock();
	//            } catch (FileException ex) {
	//                throw new XInvalidSimisFormatException(File.Filename, Reader, 0, "BNF state failed begin-block; see below for BNF state.", ex);
	//            }
	//            return SimisParserToken.BlockBegin;
	//        }

	//        if (token == ")") {
	//            try {
	//                BNFState.LeaveBlock();
	//            } catch (FileException ex) {
	//                throw new XInvalidSimisFormatException(File.Filename, Reader, 0, "BNF state failed end-block; see below for BNF state.", ex);
	//            }
	//            //if (BNFState.IsEmpty) {
	//            //	while (Reader.BaseStream.Position < Reader.BaseStream.Length) {
	//            //		Reader.ReadChar();
	//            //	}
	//            //}
	//            return SimisParserToken.BlockEnd;
	//        }

	//        if (token.ToLower() == "skip") {
	//            while ((Reader.BaseStream.Position < Reader.BaseStream.Length) && (')' != Reader.PeekChar())) {
	//                token += Reader.ReadChar();
	//            }
	//            token += Reader.ReadChar();
	//            return SimisParserToken.Text;
	//        }

	//        if (BNFState.IsEnterBlockTime) {
	//            // This is (probably?) a block name, which fits in between the block tag and the "(".
	//            TokenType = "block-string";
	//            TokenText = token;
	//            return SimisParserToken.Text;
	//        }

	//        // Get all valid new states, except the special <begin-block> and <end-block>.
	//        var validReferences = BNFState.GetValidStates().Where<string>(s => !s.StartsWith("<")).ToArray<string>();

	//        if (validReferences.Any<string>(s => s == token)) {
	//            // Token matches reference. Go!
	//            TokenType = token;
	//        } else {
	//            // Check all the datatypes match.
	//            var validDataTypes = validReferences.Where<string>(s => dataTypes.Contains(s));
	//            if (validDataTypes.Any<string>(s => s != validDataTypes.First<string>())) throw new XInvalidSimisFormatException(File.Filename, Reader, 0, BNFState, "Simis parser got token '" + token + "' but BNF has multiple datatype states to move to; see below for BNF state.");
	//            // Look for first datatype.
	//            TokenType = validReferences.FirstOrDefault<string>(s => dataTypes.Contains(s));
	//            if (TokenType == null) throw new XInvalidSimisFormatException(File.Filename, Reader, 0, BNFState, "Simis parser got token '" + token + "' but BNF doesn't include this state or a datatype to parse it with; see below for BNF state.");
	//        }

	//        var rv = SimisParserToken.Block;
	//        switch (TokenType) {
	//            case "uint":
	//                if (token.EndsWith(",")) token = token.Substring(0, token.Length - 1);
	//                try {
	//                    TokenNumber = UInt32.Parse(token);
	//                } catch (Exception ex) {
	//                    throw new XInvalidSimisFormatException(File.Filename, Reader, 0, BNFState, "Simis parser failed to parse '" + token + "' as '" + TokenType + "'.", ex);
	//                }
	//                rv = SimisParserToken.Integer;
	//                break;
	//            case "sint":
	//                if (token.EndsWith(",")) token = token.Substring(0, token.Length - 1);
	//                try {
	//                    TokenNumber = Int32.Parse(token);
	//                } catch (Exception ex) {
	//                    throw new XInvalidSimisFormatException(File.Filename, Reader, 0, BNFState, "Simis parser failed to parse '" + token + "' as '" + TokenType + "'.", ex);
	//                }
	//                rv = SimisParserToken.Integer;
	//                break;
	//            case "dword":
	//                if (token.EndsWith(",")) token = token.Substring(0, token.Length - 1);
	//                if ((token.Length != 8) || nonhexNumberChars.Any<char>(c => token.Contains(c))) throw new XInvalidSimisFormatException(File.Filename, Reader, 0, "");
	//                try {
	//                    TokenNumber = UInt32.Parse(token, NumberStyles.HexNumber);
	//                } catch (Exception ex) {
	//                    throw new XInvalidSimisFormatException(File.Filename, Reader, 0, BNFState, "Simis parser failed to parse '" + token + "' as '" + TokenType + "'.", ex);
	//                }
	//                rv = SimisParserToken.Integer;
	//                break;
	//            case "float":
	//                if (token.EndsWith(",")) token = token.Substring(0, token.Length - 1);
	//                try {
	//                    TokenNumber = Double.Parse(token);
	//                } catch (Exception ex) {
	//                    throw new XInvalidSimisFormatException(File.Filename, Reader, 0, BNFState, "Simis parser failed to parse '" + token + "' as '" + TokenType + "'.", ex);
	//                }
	//                rv = SimisParserToken.Double;
	//                break;
	//            case "string":
	//                TokenText = token;
	//                rv = SimisParserToken.Text;
	//                break;
	//        }
	//        try {
	//            BNFState.MoveTo(TokenType);
	//        } catch (FileException ex) {
	//            throw new XInvalidSimisFormatException(File.Filename, Reader, 0, "Simis parser got token '" + token + "' but BNF state failed move to '" + TokenType + "'; see below for BNF state.", ex);
	//        }
	//        return rv;
	//    }
	//}

	//internal class SimisParserBinary : SimisParser
	//{
	//    private Stack<uint> BlockEnds;
	//    private Queue<SimisParserToken> TokenQueue;
	//    private readonly List<string> dataTypes;

	//    public SimisParserBinary(SimisFile file, BinaryReader reader) : base(file, reader) {
	//        BlockEnds = new Stack<uint>();
	//        TokenQueue = new Queue<SimisParserToken>();
	//        dataTypes = new List<string>(new string[] { "string", "byte", "uint", "sint", "dword", "float", "buffer" });
	//    }

	//    public override SimisParserToken NextToken() {
	//        ResetToken();

	//        while ((BlockEnds.Count > 0) && (Reader.BaseStream.Position >= BlockEnds.Peek())) {
	//            TokenQueue.Enqueue(SimisParserToken.BlockEnd);
	//            BlockEnds.Pop();
	//        }

	//        if (TokenQueue.Count > 0) {
	//            if (TokenQueue.Peek() == SimisParserToken.BlockBegin) {
	//                BNFState.EnterBlock();
	//            } else if (TokenQueue.Peek() == SimisParserToken.BlockEnd) {
	//                BNFState.LeaveBlock();
	//            }
	//            return TokenQueue.Dequeue();
	//        }

	//        if (Reader.BaseStream.Position >= Reader.BaseStream.Length) {
	//            return SimisParserToken.None;
	//        }

	//        if (BNFState == null) {
	//            BNFState = new BNFState(BNF);
	//            BNFState.RegisterMessageSink(File);
	//        }

	//        // Get all valid new states, except the special <begin-block> and <end-block>.
	//        var validReferences = BNFState.GetValidStates().Where<string>(s => !s.StartsWith("<")).ToArray<string>();
	//        if (validReferences.Length == 0) throw new XInvalidSimisFormatException(File.Filename, Reader, 0, BNFState, "BNF has no valid states to move to.");
	//        if (validReferences.All<string>(r => dataTypes.Contains(r))) {
	//            if (validReferences.Any<string>(r => r != validReferences[0])) throw new XInvalidSimisFormatException(File.Filename, Reader, 0, BNFState, "BNF has multiple datatype states to move to. We can't decide where to go from here: " + String.Join(", ", validReferences.Select<string, string>(s => "'" + s + "'").ToArray()));
	//            TokenType = validReferences[0];
	//            BNFState.MoveTo(TokenType);
	//            switch (TokenType) {
	//                case "string":
	//                    var stringLength = Reader.ReadUInt16();
	//                    for (var i = 0; i < stringLength; i++) {
	//                        TokenText += (char)Reader.ReadUInt16();
	//                    }
	//                    return SimisParserToken.Text;
	//                case "byte":
	//                    TokenNumber = Reader.ReadByte();
	//                    return SimisParserToken.Integer;
	//                case "uint":
	//                    TokenNumber = Reader.ReadUInt32();
	//                    return SimisParserToken.Integer;
	//                case "sint":
	//                    TokenNumber = Reader.ReadInt32();
	//                    return SimisParserToken.Integer;
	//                case "dword":
	//                    TokenNumber = Reader.ReadUInt32();
	//                    return SimisParserToken.Integer;
	//                case "float":
	//                    TokenNumber = Reader.ReadSingle();
	//                    return SimisParserToken.Double;
	//                case "buffer":
	//                    var bufferLength = BlockEnds.Peek() - Reader.BaseStream.Position;
	//                    TokenText = String.Join("", Reader.ReadBytes((int)bufferLength).Select<byte, string>(b => b.ToString("X2")).ToArray<string>());
	//                    return SimisParserToken.Text;
	//                default:
	//                    throw new XInvalidSimisFormatException(File.Filename, Reader, 0, BNFState, "Unexpected BNF data type '" + validReferences[0] + "'.");
	//            }
	//        } else {
	//            var tokenID = Reader.ReadUInt16();
	//            var tokenType = Reader.ReadUInt16();
	//            var token = ((uint)tokenType << 16) + tokenID;
	//            if (((tokenType == 0x0000) || (tokenType == 0x0004)) && TokenNames.ContainsKey(token)) {
	//                TokenType = TokenNames[token];
	//                if (!validReferences.Contains(TokenType)) throw new XInvalidSimisFormatException(File.Filename, Reader, 4, BNFState, "Simis parser expected token from " + String.Join(", ", validReferences.Select<string, string>(s => "'" + s + "'").ToArray()) + "; got '" + TokenType + "'.");
	//                BNFState.MoveTo(TokenType);

	//                var contentsLength = Reader.ReadUInt32();
	//                if (contentsLength > 0) {
	//                    BlockEnds.Push((uint)Reader.BaseStream.Position + contentsLength);
	//                    TokenQueue.Enqueue(SimisParserToken.BlockBegin);

	//                    var nameLength = Reader.Read();
	//                    if (nameLength > 0) {
	//                        for (var i = 0; i < nameLength; i++) {
	//                            TokenText += (char)Reader.ReadUInt16();
	//                        }
	//                    }

	//                    while ((BlockEnds.Count > 0) && (Reader.BaseStream.Position == BlockEnds.Peek())) {
	//                        TokenQueue.Enqueue(SimisParserToken.BlockEnd);
	//                        BlockEnds.Pop();
	//                    }
	//                    return SimisParserToken.Block;
	//                }
	//                throw new XInvalidSimisFormatException(File.Filename, Reader, 8, BNFState, "Simis parser got empty or malformed block '" + TokenText + "'.");
	//                //TokenText += " [read " + contentsLength + " bytes of non-nested data]";
	//                //Reader.ReadBytes((int)contentsLength);
	//                //if ((BlockEnds.Count > 0) && (Reader.BaseStream.Position == BlockEnds.Peek())) {
	//                //	TokenQueue.Enqueue(SimisParserToken.BlockEnd);
	//                //	BlockEnds.Pop();
	//                //}
	//                //return SimisParserToken.Text;
	//            }
	//            throw new XInvalidSimisFormatException(File.Filename, Reader, 4, BNFState, "Simis parser got unknown token block 0x" + token.ToString("X8") + ".");
	//        }
	//    }
	//}
}
