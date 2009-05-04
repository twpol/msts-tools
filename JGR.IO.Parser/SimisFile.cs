using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using JGR;
using JGR.Grammar;

namespace JGR.IO.Parser
{
	public class UnknownSimisFormatException : FileException
	{
		public readonly string Format;

		public UnknownSimisFormatException(string filename, string format, IEnumerable<string> formats)
			: base(filename, "Simis format '" + format + "' is not in the list of BNF parsers provided. "
			+ "Only Simis formats for which there is a BNF parser associated can be loaded. "
			+ "Formats currently provided by BNFs: " + String.Join(", ", formats.Select<string, string>(s => "'" + s + "'").ToArray<string>()) + ". "
			+ "\n\n")
		{
			Format = format;
		}
	}

	public class InvalidSimisFormatException : FileException
	{
		private static string ReadAndFormatData(BinaryReader reader, long length) {
			var formattedText = "";
			var rows = (int)(length + 15) / 16;
			var lastColumn = 16 - (int)(16 * rows - length);
			for (var i = 0; i < rows; i++) {
				var bytes = reader.ReadBytes((i < rows - 1 ? 16 : lastColumn));
				for (var j = 0; j < bytes.Length; j++) {
					formattedText += bytes[j].ToString("X2") + (j % 4 == 3 ? "  " : " ");
				}
				for (var j = 0; j < bytes.Length; j++) {
					formattedText += (bytes[j] < 32 ? '.' : (char)bytes[j]);
				}
				if (i < rows - 1) formattedText += "\n";
			}
			return formattedText;
		}

		private static string FormatMessage(BinaryReader reader, long alreadyRead, BNFState state, string message) {
			var beforeError = Math.Min(128, reader.BaseStream.Position - alreadyRead);
			var afterError = Math.Min(128, reader.BaseStream.Length - reader.BaseStream.Position);
			reader.BaseStream.Seek(-alreadyRead, SeekOrigin.Current);
			reader.BaseStream.Seek(-beforeError, SeekOrigin.Current);

			var sourceText = message + "\n\n" + (state == null ? "" : state + "\n\n");
			if (beforeError > 0) {
				sourceText += "From 0x" + reader.BaseStream.Position.ToString("X8") + " - data preceding failure:\n";
				sourceText += ReadAndFormatData(reader, beforeError);
				sourceText += "\n\n";
			}
			if (alreadyRead > 0) {
				sourceText += "From 0x" + reader.BaseStream.Position.ToString("X8") + " - data at failure:\n";
				sourceText += ReadAndFormatData(reader, alreadyRead);
				sourceText += "\n\n";
			}
			if (afterError > 0) {
				sourceText += "From 0x" + reader.BaseStream.Position.ToString("X8") + " - data following failure:\n";
				sourceText += ReadAndFormatData(reader, afterError);
				sourceText += "\n\n";
			}

			return sourceText;
		}

		public InvalidSimisFormatException(string filename, BinaryReader reader, long alreadyRead, string message)
			: this(filename, reader, alreadyRead, null, message) {
		}

		public InvalidSimisFormatException(string filename, BinaryReader reader, long alreadyRead, string message, Exception innerException)
			: this(filename, reader, alreadyRead, null, message, innerException) {
		}

		public InvalidSimisFormatException(string filename, BinaryReader reader, long alreadyRead, BNFState state, string message)
			: base(filename, FormatMessage(reader, alreadyRead, state, message)) {
		}

		public InvalidSimisFormatException(string filename, BinaryReader reader, long alreadyRead, BNFState state, string message, Exception innerException)
			: base(filename, FormatMessage(reader, alreadyRead, state, message), innerException) {
		}
	}

	public class SimisFile : BufferedMessageSource
	{
		public string Filename { get; private set; }
		public SimisBlock Root;
		protected Dictionary<string, BNF> BNFs;

		public SimisFile(string filename, Dictionary<string, BNF> bnfs) {
			Filename = filename;
			Root = null;
			BNFs = bnfs;
		}

		public override string GetMessageSourceName() {
			return "Simis File";
		}

		public void ReadFile() {
			MessageSend(LEVEL_INFORMATION, "Loading '" + Filename + "'...");
			using (var fileStream = File.OpenRead(Filename)) {
				var reader = new BinaryReader(fileStream, Encoding.Default);
				var format = "";

				// Let's find out what we've got in today's file.
				ISimisParser parser;
				//var utf16LE = false;
				{
					// INDEX: 0  SIZE: 2
					var signature = String.Join("", reader.ReadChars(2).Select<char, string>(c => c.ToString()).ToArray<string>());
					if (signature == "\xFF\xFE") {
						//utf16LE = true;
						reader = new BinaryReader(reader.BaseStream, Encoding.Unicode);
					} else if (signature != "SI") {
						throw new InvalidSimisFormatException(Filename, reader, 2, "Signature (bytes 0-1) is invalid: " + signature);
					} else {
						reader.BaseStream.Seek(0, SeekOrigin.Begin);
					}
				}

				// INDEX: 0  SIZE: 8
				var signature1 = String.Join("", reader.ReadChars(8).Select<char, string>(c => c.ToString()).ToArray<string>());
				if (signature1 == "SIMISA@F") {
					// Compressed Binary.

					// INDEX: 8  SIZE: 4
					var uncompressedSize = reader.ReadUInt32();
					// INDEX: 12  SIZE: 4
					var signature2 = String.Join("", reader.ReadChars(4).Select<char, string>(c => c.ToString()).ToArray<string>());
					if (signature2 != "@@@@") {
						throw new InvalidSimisFormatException(Filename, reader, 4, "Signature (bytes 12-15) is invalid: " + signature2);
					}
					// INDEX: 16  SIZE: 4
					// The stream is technically ZLIB, but we assume the selected ZLIB compression is DEFLATE (though we verify that here just in case). The ZLIB
					// header for DEFLATE is 0x78 0x9C (apparently).
					var zlibHeader = reader.ReadBytes(2);
					if ((zlibHeader[0] != 0x78) || (zlibHeader[1] != 0x9C)) {
						throw new InvalidSimisFormatException(Filename, reader, 2, "ZLIB compression header says it isn't using the expected DEFLATE configuration. Expected 0x78 0x9C, got " + String.Join(" ", zlibHeader.Select<byte, string>(b => "0x" + b.ToString("X2")).ToArray<string>()) + ".");
					}
					// INDEX: 18
					var deflate = new DeflateStream(reader.BaseStream, CompressionMode.Decompress, false);
					reader = new BinaryReader(new BufferedInMemoryStream(deflate), Encoding.Default);
				} else if (signature1 == "SIMISA@@") {
					// Uncompressed binary or text.
					// INDEX: 8  SIZE: 8
					var signature2 = String.Join("", reader.ReadChars(8).Select<char, string>(c => c.ToString()).ToArray<string>());
					if (signature2 != "@@@@@@@@") {
						throw new InvalidSimisFormatException(Filename, reader, 8, "Signature (bytes 8-15) is invalid: " + signature2);
					}
				} else {
					throw new InvalidSimisFormatException(Filename, reader, 8, "Signature (bytes 0-7) is invalid: " + signature1);
				}

				{
					// For uncompressed binary or test, we start from index 16. For compressed binary, we start from index 0 inside the compressed stream.
					// INDEX: 16  SIZE: 4
					var signature2 = String.Join("", reader.ReadChars(4).Select<char, string>(c => c.ToString()).ToArray<string>());
					if (signature2 != "JINX") {
						throw new InvalidSimisFormatException(Filename, reader, 4, "Signature (bytes 16-19) is invalid: " + signature2);
					}
					// INDEX: 20  SIZE: 4
					var signature3 = String.Join("", reader.ReadChars(4).Select<char, string>(c => c.ToString()).ToArray<string>());
					format = signature3.Substring(1, 2);
					if (!BNFs.ContainsKey(format)) {
						throw new UnknownSimisFormatException(Filename, format, BNFs.Keys);
						//throw new InvalidDataException("Signature (bytes 20-22) is invalid (unknown format): " + signature3);
					}
					if ((signature3[3] != 'b') && (signature3[3] != 't')) {
						throw new InvalidSimisFormatException(Filename, reader, 4, "Signature (byte 23) is invalid (not binary or text mode): " + signature3);
					}
					// INDEX: 24  SIZE: 8
					var signature4 = String.Join("", reader.ReadChars(8).Select<char, string>(c => c.ToString()).ToArray<string>());
					if (signature4 != "______\r\n") {
						throw new InvalidSimisFormatException(Filename, reader, 8, "Signature (bytes 24-31) is invalid: " + signature4);
					}
					// INDEX: 32
					if (signature3[3] == 'b') {
						parser = new SimisParserBinary(this, reader);
					} else {
						parser = new SimisParserText(this, reader);
					}
				}

				if (BNFs.ContainsKey(format)) {
					parser.BNF = BNFs[format];
				}

				//using (var writer = new StreamWriter(Filename + ".txt", false, Encoding.UTF8)) {
					var blockStack = new Stack<SimisBlock>();
					var indent = "";
					//var inline = false;
					//writer.WriteLine("SIMISA@@@@@@@@@@JINX0" + format + "t______");
					//writer.WriteLine();
					for (var token = parser.NextToken(); token != SimisParserToken.None; token = parser.NextToken()) {
						switch (token) {
							case SimisParserToken.Block:
								MessageSend(LEVEL_DEBUG, "Got token BLOCK (" + parser.TokenText + ").");
								var block = new SimisBlock(this, parser.TokenText);
								if (Root == null) Root = block;
								if (blockStack.Count > 0) blockStack.Peek().Nodes.Add(block);
								blockStack.Push(block);
								//if (inline) writer.WriteLine();
								//writer.Write(indent + parser.TokenText + " (");
								//inline = true;
								break;
							case SimisParserToken.BlockBegin:
								MessageSend(LEVEL_DEBUG, "Got token BLOCK-BEGIN.");
								indent += "\t";
								break;
							case SimisParserToken.BlockEnd:
								MessageSend(LEVEL_DEBUG, "Got token BLOCK-END.");
								blockStack.Pop();
								indent = indent.Substring(1);
								//if (inline) {
								//	writer.WriteLine(" )");
								//} else {
								//	writer.WriteLine(indent + ")");
								//}
								//inline = false;
								break;
							case SimisParserToken.Double:
								MessageSend(LEVEL_DEBUG, "Got number: " + parser.TokenNumber);
								if ((parser.BNFState != null) && (parser.BNFState.State != null) && (parser.BNFState.State.Op is NamedReferenceOperator)) {
									blockStack.Peek().Nodes.Add(new SimisBlockValueDouble(this, ((NamedReferenceOperator)parser.BNFState.State.Op).Name, parser.TokenNumber));
								} else {
									blockStack.Peek().Nodes.Add(new SimisBlockValueDouble(this, "unnamed_" + parser.TokenText, parser.TokenNumber));
								}
								break;
							case SimisParserToken.Integer:
								MessageSend(LEVEL_DEBUG, "Got number: " + parser.TokenNumber);
								if ((parser.BNFState != null) && (parser.BNFState.State != null) && (parser.BNFState.State.Op is NamedReferenceOperator)) {
									blockStack.Peek().Nodes.Add(new SimisBlockValueInteger(this, ((NamedReferenceOperator)parser.BNFState.State.Op).Name, (long)parser.TokenNumber));
								} else {
									blockStack.Peek().Nodes.Add(new SimisBlockValueInteger(this, "unnamed_" + parser.TokenText, (long)parser.TokenNumber));
								}
								/*if ((parser.BNFState != null) && (parser.BNFState.State != null) && (parser.BNFState.State.Op is NamedReferenceOperator)) {
									var nrop = (NamedReferenceOperator)parser.BNFState.State.Op;
									writer.WriteLine();
									writer.WriteLine(indent + nrop.Name + " " + parser.TokenNumber.ToString("G6"));
									inline = false;
								} else*/
								//if (inline) {
								//	writer.Write(" " + parser.TokenNumber.ToString("G6"));
								//} else {
								//	writer.WriteLine(indent + parser.TokenNumber.ToString("G6"));
								//}
								break;
							case SimisParserToken.Text:
								if ((parser.BNFState != null) && (parser.BNFState.State != null) && (parser.BNFState.State.Op is NamedReferenceOperator)) {
									blockStack.Peek().Nodes.Add(new SimisBlockValueString(this, ((NamedReferenceOperator)parser.BNFState.State.Op).Name, parser.TokenText));
								} else {
									blockStack.Peek().Nodes.Add(new SimisBlockValueString(this, "unnamed_string", parser.TokenText));
								}
								/*if ((parser.BNFState != null) && (parser.BNFState.State != null) && (parser.BNFState.State.Op is NamedReferenceOperator)) {
									var nrop = (NamedReferenceOperator)parser.BNFState.State.Op;
									writer.WriteLine();
									writer.WriteLine(indent + nrop.Name + " " + parser.TokenText);
									inline = false;
								} else*/
								var formattedString = parser.TokenText.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\n", "\\n");
								MessageSend(LEVEL_DEBUG, "Got text: " + formattedString);
								//if (inline) {
								//	writer.Write(" \"" + formattedString + "\"");
								//} else {
								//	writer.WriteLine(indent + "\"" + formattedString + "\"");
								//}
								break;
							default:
								MessageSend(LEVEL_ERROR, "Unknown parser token: " + token);
								//writer.WriteLine(indent + token + (token == SimisParserToken.Block ? " " + parser.TokenText : token == SimisParserToken.Text ? " \"" + parser.TokenText + "\"" : token == SimisParserToken.Number ? " " + parser.TokenNumber.ToString() : ""));
								break;
						}
					}
				//}

				Debug.Assert(reader.BaseStream.Position >= reader.BaseStream.Length, "Parser " + parser.ToString() + " failed to consume all input for <" + Filename + ">.");

				//if (reader.BaseStream is BufferedInMemoryStream) {
				//    var buf = (BufferedInMemoryStream)reader.BaseStream;
				//    using (var writer = new BinaryWriter(File.OpenWrite(Filename + ".bin"), Encoding.Default)) {
				//        writer.Write("SIMISA@@@@@@@@@@".ToCharArray());
				//        buf.Seek(0, SeekOrigin.Begin);
				//        while (buf.Position < buf.Length) {
				//            writer.Write((byte)buf.ReadByte());
				//        }
				//    }
				//}
			}
			MessageSend(LEVEL_INFORMATION, "Done.");
		}
	}

	public class SimisBlock
	{
		public readonly SimisFile File;
		public string Token;
		public string Name;
		public readonly List<SimisBlock> Nodes;

		public SimisBlock(SimisFile file)
			: this(file, "", "") {
		}

		public SimisBlock(SimisFile file, string token)
			: this(file, token, "") {
		}

		public SimisBlock(SimisFile file, string token, string name) {
			File = file;
			Token = token;
			Name = name;
			Nodes = new List<SimisBlock>();
		}
	}

	public class SimisBlockValue : SimisBlock
	{
		protected object _value;
		protected SimisBlockValue(SimisFile file, string name, object value)
			: base(file, "", name) {
			_value = value;
		}
	}

	public class SimisBlockValueInteger : SimisBlockValue
	{
		public SimisBlockValueInteger(SimisFile file, string name, long value)
			: base(file, name, value) {
		}

		public override string ToString() {
			return ((long)_value).ToString();
		}
	}

	public class SimisBlockValueDouble : SimisBlockValue
	{
		public SimisBlockValueDouble(SimisFile file, string name, double value)
			: base(file, name, value) {
		}

		public override string ToString() {
			return ((double)_value).ToString("G6");
		}
	}

	public class SimisBlockValueString : SimisBlockValue
	{
		public SimisBlockValueString(SimisFile file, string name, string value)
			: base(file, name, value) {
		}

		public override string ToString() {
			return (string)_value;
		}
	}

	enum SimisParserToken
	{
		None,
		Block,
		Text,
		Integer,
		Double,
		BlockBegin,
		BlockEnd
	}

	interface ISimisParser
	{
		SimisParserToken NextToken();
		string TokenText { get; }
		double TokenNumber { get; }
		BNF BNF { get; set; }
		BNFState BNFState { get; }
	}

	internal abstract class SimisParser : ISimisParser
	{
		protected readonly SimisFile File;
		protected BinaryReader Reader { get; private set; }
		public BNF BNF { get; set; }
		public BNFState BNFState { get; protected set; }

		protected SimisParser(SimisFile file, BinaryReader reader) {
			File = file;
			Reader = reader;
			ResetToken();
		}

		protected void ResetToken() {
			TokenText = "";
			TokenNumber = 0.0;
		}


		#region ISimisParser Members
		public virtual SimisParserToken NextToken() { return SimisParserToken.None; }
		public string TokenText { get; protected set; }
		public double TokenNumber { get; protected set; }
		#endregion
	}

	internal class SimisParserText : SimisParser
	{
		private readonly char[] whitespaceChars;
		private readonly char[] whitespaceAndSpecialChars;
		private readonly char[] numberChars;
		private readonly char[] nonhexNumberChars;
		private readonly List<string> dataTypes;

		public SimisParserText(SimisFile file, BinaryReader reader) : base(file, reader) {
			whitespaceChars = new char[] { ' ', '\t', '\r', '\n' };
			whitespaceAndSpecialChars = new char[] { ' ', '\t', '\r', '\n', '(', ')', '"' };
			numberChars = new char[] { '-', '0', '1', '2', '3', '4', '5', '6', '7', '8', '9', '.' };
			nonhexNumberChars = new char[] { '-', '.' };
			dataTypes = new List<string>(new string[] { "string", "uint", "sint", "dword", "float" });
		}

		public override SimisParserToken NextToken() {
			ResetToken();

			// First, chomp whitespace.
			while ((Reader.BaseStream.Position < Reader.BaseStream.Length) && whitespaceChars.Any<char>(c => c == Reader.PeekChar())) {
				Reader.ReadChar();
			}
			if (Reader.BaseStream.Position >= Reader.BaseStream.Length) {
				if (!BNFState.IsEmpty) throw new InvalidSimisFormatException(File.Filename, Reader, 0, BNFState, "Simis parser ran out of data with unfinished BNF rules left. See below for BNF state.");
				return SimisParserToken.None;
			}

			var isString = false;
			string token = "";
			if ('(' == Reader.PeekChar()) {
				token += Reader.ReadChar();
			} else if (')' == Reader.PeekChar()) {
				token += Reader.ReadChar();
			} else if ('"' == Reader.PeekChar()) {
				isString = true;
				do {
					// Eat whitespace. (This is for the 2nd and further times through, to each whitespace after the "+".)
					if ('+' == Reader.PeekChar()) Reader.ReadChar();
					while ((Reader.BaseStream.Position < Reader.BaseStream.Length) && whitespaceChars.Any<char>(c => c == Reader.PeekChar())) {
						Reader.ReadChar();
					}
					// Consume string.
					Reader.ReadChar(); // "\""
					while ((Reader.BaseStream.Position < Reader.BaseStream.Length) && ('"' != Reader.PeekChar())) {
						if ('\\' == Reader.PeekChar()) {
							Reader.ReadChar();
							switch ((char)Reader.PeekChar()) {
								case '\\':
									token += "\\";
									break;
								case 'n':
									token += "\n";
									break;
								case '"':
									token += "\"";
									break;
								default:
									throw new InvalidSimisFormatException(File.Filename, Reader, 0, BNFState, "Simis parser found unknown escape in string: \\" + (char)Reader.PeekChar() + ".");
							}
							Reader.ReadChar();
						} else {
							token += Reader.ReadChar();
						}
					}
					Reader.ReadChar(); // "\""
					// Eat whitespace. (This is for the 2nd and further times through, to each whitespace after the "+".)
					while ((Reader.BaseStream.Position < Reader.BaseStream.Length) && whitespaceChars.Any<char>(c => c == Reader.PeekChar())) {
						Reader.ReadChar();
					}
				} while ('+' == Reader.PeekChar());
			} else {
				while ((Reader.BaseStream.Position < Reader.BaseStream.Length) && whitespaceAndSpecialChars.All<char>(c => c != Reader.PeekChar())) {
					token += Reader.ReadChar();
				}
			}

			if ((BNF != null) && (BNFState == null)) {
				// First token in the file must match the production FILE.
				if ((token == "(") || (token == ")") || token.ToCharArray().All<char>(t => numberChars.Any<char>(c => t == c))) throw new InvalidSimisFormatException(File.Filename, Reader, 0, BNFState, "Simis parser expected token; got '" + token + "'.");
				if (!BNF.Definitions.ContainsKey("FILE")) throw new InvalidSimisFormatException(File.Filename, Reader, 0, BNFState, "Simis parser cannot find FILE definition in BNF.");
				if (BNF.Definitions["FILE"].Expression.Op != OperatorType.Reference) throw new InvalidSimisFormatException(File.Filename, Reader, 0, BNFState, "Simis parser found BFN definition for FILE doesn't contain just a single reference.");
				var rootOp = (ReferenceOperator)BNF.Definitions["FILE"].Expression;
				if (!BNF.Productions.ContainsKey(rootOp.Reference)) throw new InvalidSimisFormatException(File.Filename, Reader, 0, BNFState, "Simis parser found BNF symbol " + rootOp.Reference + " is not a production.");
				if (token != rootOp.Reference) throw new InvalidSimisFormatException(File.Filename, Reader, 0, BNFState, "Simis parser expected '" + rootOp.Reference + "'; got '" + token + "'.");
				BNFState = new BNFState(BNF);
				BNFState.RegisterMessageSink(File);
			}

			if (token == "(") {
				if (BNF != null) {
					try {
						BNFState.EnterBlock();
					} catch (FileException ex) {
						throw new InvalidSimisFormatException(File.Filename, Reader, 0, "BNF state failed begin-block; see below for BNF state.", ex);
					}
				}
				return SimisParserToken.BlockBegin;
			}

			if (token == ")") {
				if (BNF != null) {
					try {
						BNFState.LeaveBlock();
					} catch (FileException ex) {
						throw new InvalidSimisFormatException(File.Filename, Reader, 0, "BNF state failed end-block; see below for BNF state.", ex);
					}
					if (BNFState.IsEmpty) {
						while (Reader.BaseStream.Position < Reader.BaseStream.Length) {
							Reader.ReadChar();
						}
					}
				}
				return SimisParserToken.BlockEnd;
			}

			if (token.ToLower() == "skip") {
				while ((Reader.BaseStream.Position < Reader.BaseStream.Length) && (')' != Reader.PeekChar())) {
					token += Reader.ReadChar();
				}
				token += Reader.ReadChar();
				return SimisParserToken.Text;
			}

			if (BNF != null) {
				if (BNFState.IsEnterBlockTime) {
					// This is (probably?) a block name, which fits in between the block tag and the "(".
					TokenText = token;
					return SimisParserToken.Text;
				}

				// Get all valid new states, except the special <begin-block> and <end-block>.
				var validReferences = BNFState.GetValidStates().Where<string>(s => !s.StartsWith("<")).ToArray<string>();

				var dataType = "";
				if (validReferences.Any<string>(s => s == token)) {
					// Token matches reference. Go!
					dataType = token;
				} else {
					// Check all the datatypes match.
					var validDataTypes = validReferences.Where<string>(s => dataTypes.Contains(s));
					if (validDataTypes.Any<string>(s => s != validDataTypes.First<string>())) throw new InvalidSimisFormatException(File.Filename, Reader, 0, BNFState, "Simis parser got token '" + token + "' but BNF has multiple datatype states to move to; see below for BNF state.");
					// Look for first datatype.
					dataType = validReferences.FirstOrDefault<string>(s => dataTypes.Contains(s));
					if (dataType == null) throw new InvalidSimisFormatException(File.Filename, Reader, 0, BNFState, "Simis parser got token '" + token + "' but BNF doesn't include this state or a datatype to parse it with; see below for BNF state.");
				}

				if ((dataType == "uint") || (dataType == "sint") || (dataType == "dword") || (dataType == "float")) {
					TokenText = dataType;
					try {
						if (dataType == "float") {
							TokenNumber = double.Parse(token);
						} else if ((token.Length == 8) && !nonhexNumberChars.Any<char>(c => token.Contains(c))) {
							TokenNumber = long.Parse(token, NumberStyles.HexNumber);
						} else {
							TokenNumber = long.Parse(token);
						}
					} catch (Exception ex) {
						throw new InvalidSimisFormatException(File.Filename, Reader, 0, BNFState, "Simis parser failed to parse '" + token + "' as '" + dataType + "'.", ex);
					}
					try {
						BNFState.MoveTo(dataType);
					} catch (FileException ex) {
						throw new InvalidSimisFormatException(File.Filename, Reader, 0, "Simis parser got token '" + token + "' but BNF state failed move to '" + dataType + "'; see below for BNF state.", ex);
					}
					return (dataType == "float" ? SimisParserToken.Double : SimisParserToken.Integer);
				}
				TokenText = token;
				try {
					BNFState.MoveTo(dataType);
				} catch (FileException ex) {
					throw new InvalidSimisFormatException(File.Filename, Reader, 0, "Simis parser got token '" + token + "' but BNF state failed move to '" + dataType + "'; see below for BNF state.", ex);
				}
				return (dataType == "string" ? SimisParserToken.Text : SimisParserToken.Block);
			}

			if (token.ToCharArray().All<char>(t => numberChars.Any<char>(c => t == c))) {
				if (token.Contains('.')) {
					TokenNumber = double.Parse(token);
					if (BNF != null) {
						try {
							BNFState.MoveTo("float");
						} catch (FileException ex) {
							throw new InvalidSimisFormatException(File.Filename, Reader, 0, "BNF state failed move to 'float'; see below for BNF state.", ex);
						}
					}
					return SimisParserToken.Double;
				}
				if ((token.Length == 8) && !nonhexNumberChars.Any<char>(c => token.Contains(c))) {
					TokenNumber = long.Parse(token, NumberStyles.HexNumber);
					if (BNF != null) {
						try {
							BNFState.MoveTo("dword");
						} catch (FileException ex) {
							throw new InvalidSimisFormatException(File.Filename, Reader, 0, "BNF state failed move to 'dword'; see below for BNF state.", ex);
						}
					}
					return SimisParserToken.Integer;
				}
				TokenNumber = long.Parse(token);
				if (BNF != null) {
					try {
						BNFState.MoveTo("uint");
					} catch (FileException ex) {
						throw new InvalidSimisFormatException(File.Filename, Reader, 0, "BNF state failed move to 'uint'; see below for BNF state.", ex);
					}
				}
				return SimisParserToken.Integer;
			}
			{
				TokenText = token;
				return isString ? SimisParserToken.Text : SimisParserToken.Block;
			}
		}
	}

	internal class SimisParserBinary : SimisParser
	{
		private Dictionary<uint, string> TokenNames;
		private Stack<uint> BlockEnds;
		private Queue<SimisParserToken> TokenQueue;
		private readonly List<string> dataTypes;

		public SimisParserBinary(SimisFile file, BinaryReader reader) : base(file, reader) {
			TokenNames = new Dictionary<uint, string>();
			BlockEnds = new Stack<uint>();
			TokenQueue = new Queue<SimisParserToken>();
			dataTypes = new List<string>(new string[] { "string", "byte", "uint", "sint", "dword", "float", "buffer" });

			var app = Environment.GetCommandLineArgs()[0];
			app = app.Substring(0, app.LastIndexOf('\\'));
			app = @"E:\Users\James\Documents\Visual Studio 2008\Projects\JGR MSTS Editor\MSTS Route Editor";
			var ffReader = new StreamReader(System.IO.File.OpenRead(app + @"\\ffedit.txt"), Encoding.ASCII);
			var tokenType = (ushort)0x0000;
			var tokenIndex = (ushort)0x0000;
			for (var ffLine = ffReader.ReadLine(); !ffReader.EndOfStream; ffLine = ffReader.ReadLine()) {
				if (ffLine.StartsWith("SID_DEFINE_FIRST_ID(")) {
					var type = ffLine.Substring(ffLine.IndexOf('(') + 1, ffLine.LastIndexOf(')') - ffLine.IndexOf('(') - 1);
					tokenType = ushort.Parse(type.Substring(2), NumberStyles.HexNumber);
					tokenIndex = 0x0000;
				} else if (ffLine.StartsWith("SIDDEF(")) {
					var name = ffLine.Substring(ffLine.IndexOf('"') + 1, ffLine.LastIndexOf('"') - ffLine.IndexOf('"') - 1);
					TokenNames.Add(((uint)tokenType << 16) + ++tokenIndex, name);
				}
			}
		}

		public override SimisParserToken NextToken() {
			ResetToken();

			while ((BlockEnds.Count > 0) && (Reader.BaseStream.Position >= BlockEnds.Peek())) {
				TokenQueue.Enqueue(SimisParserToken.BlockEnd);
				BlockEnds.Pop();
			}

			if (TokenQueue.Count > 0) {
				if (BNF != null) {
					if (TokenQueue.Peek() == SimisParserToken.BlockBegin) {
						BNFState.EnterBlock();
					} else if (TokenQueue.Peek() == SimisParserToken.BlockEnd) {
						BNFState.LeaveBlock();
					}
				}
				return TokenQueue.Dequeue();
			}

			if (Reader.BaseStream.Position >= Reader.BaseStream.Length) {
				return SimisParserToken.None;
			}

			if ((BNF != null) && (BNFState == null)) {
				BNFState = new BNFState(BNF);
				BNFState.RegisterMessageSink(File);
			}

			if (BNF != null) {
				// Get all valid new states, except the special <begin-block> and <end-block>.
				var validReferences = BNFState.GetValidStates().Where<string>(s => !s.StartsWith("<")).ToArray<string>();
				if (validReferences.Length == 0) throw new InvalidSimisFormatException(File.Filename, Reader, 0, BNFState, "BNF has no valid states to move to.");
				if (validReferences.Length == 0) {
					BNF = null;
				} else {
					if (validReferences.All<string>(r => dataTypes.Contains(r))) {
						if (validReferences.Any<string>(r => r != validReferences[0])) throw new InvalidSimisFormatException(File.Filename, Reader, 0, BNFState, "BNF has multiple datatype states to move to. We can't decide where to go from here: " + String.Join(", ", validReferences.Select<string, string>(s => "'" + s + "'").ToArray()));
						BNFState.MoveTo(validReferences[0]);
						TokenText = validReferences[0];
						switch (validReferences[0]) {
							case "string":
								var stringLength = Reader.ReadUInt16();
								for (var i = 0; i < stringLength; i++) {
									TokenText += (char)Reader.ReadUInt16();
								}
								return SimisParserToken.Text;
							case "byte":
								TokenNumber = Reader.ReadByte();
								return SimisParserToken.Integer;
							case "uint":
								TokenNumber = Reader.ReadUInt32();
								return SimisParserToken.Integer;
							case "sint":
								TokenNumber = Reader.ReadInt32();
								return SimisParserToken.Integer;
							case "dword":
								TokenNumber = Reader.ReadUInt32();
								return SimisParserToken.Integer;
							case "float":
								TokenNumber = Reader.ReadSingle();
								return SimisParserToken.Double;
							case "buffer":
								var bufferLength = BlockEnds.Peek() - Reader.BaseStream.Position;
								TokenText = String.Join("", Reader.ReadBytes((int)bufferLength).Select<byte, string>(b => b.ToString("X2")).ToArray<string>());
								return SimisParserToken.Text;
							default:
								throw new InvalidSimisFormatException(File.Filename, Reader, 0, BNFState, "Unexpected BNF data type '" + validReferences[0] + "'.");
						}
					} else {
						var tokenID = Reader.ReadUInt16();
						var tokenType = Reader.ReadUInt16();
						var token = ((uint)tokenType << 16) + tokenID;
						if (((tokenType == 0x0000) || (tokenType == 0x0004)) && TokenNames.ContainsKey(token)) {
							TokenText = TokenNames[token];
							if (!validReferences.Contains(TokenText)) throw new InvalidSimisFormatException(File.Filename, Reader, 4, BNFState, "Simis parser expected token from " + String.Join(", ", validReferences.Select<string, string>(s => "'" + s + "'").ToArray()) + "; got '" + TokenText + "'.");
							BNFState.MoveTo(TokenText);

							var contentsLength = Reader.ReadUInt32();
							if (contentsLength > 0) {
								BlockEnds.Push((uint)Reader.BaseStream.Position + contentsLength);
								TokenQueue.Enqueue(SimisParserToken.BlockBegin);

								var nameLength = Reader.Read();
								if (nameLength > 0) {
									TokenText += " \"";
									for (var i = 0; i < nameLength; i++) {
										TokenText += (char)Reader.ReadUInt16();
									}
									TokenText += "\"";
								}

								while ((BlockEnds.Count > 0) && (Reader.BaseStream.Position == BlockEnds.Peek())) {
									TokenQueue.Enqueue(SimisParserToken.BlockEnd);
									BlockEnds.Pop();
								}
								return SimisParserToken.Block;
							}
							throw new InvalidSimisFormatException(File.Filename, Reader, 8, BNFState, "Simis parser got empty or malformed block '" + TokenText + "'.");
							//TokenText += " [read " + contentsLength + " bytes of non-nested data]";
							//Reader.ReadBytes((int)contentsLength);
							//if ((BlockEnds.Count > 0) && (Reader.BaseStream.Position == BlockEnds.Peek())) {
							//	TokenQueue.Enqueue(SimisParserToken.BlockEnd);
							//	BlockEnds.Pop();
							//}
							//return SimisParserToken.Text;
						}
						throw new InvalidSimisFormatException(File.Filename, Reader, 4, BNFState, "Simis parser got unknown token block 0x" + token.ToString("X8") + ".");
					}
				}
			}

			// Read in the 2 + 2 bytes for the token (ID + type).
			if ((Reader.BaseStream.Position + 4 <= Reader.BaseStream.Length) && ((BlockEnds.Count == 0) || (Reader.BaseStream.Position + 8 <= BlockEnds.Peek()))) {
				var tokenID = Reader.ReadUInt16();
				var tokenType = Reader.ReadUInt16();
				var token = ((uint)tokenType << 16) + tokenID;
				if (((tokenType == 0x0000) || (tokenType == 0x0004)) && TokenNames.ContainsKey(token)) {
					// Read in the 4 bytes for contents length.
					if (Reader.BaseStream.Position + 4 > Reader.BaseStream.Length) {
						return SimisParserToken.None;
					}
					var contentsLength = Reader.ReadUInt32();

					TokenText = TokenNames[token];

					if ((contentsLength > 0) && (Reader.PeekChar() == '\x00')) {
						BlockEnds.Push((uint)Reader.BaseStream.Position + contentsLength);
						Reader.ReadByte();
						TokenQueue.Enqueue(SimisParserToken.BlockBegin);
						while ((BlockEnds.Count > 0) && (Reader.BaseStream.Position == BlockEnds.Peek())) {
							TokenQueue.Enqueue(SimisParserToken.BlockEnd);
							BlockEnds.Pop();
						}
						TokenText += " [" + contentsLength + " bytes]";
						return SimisParserToken.Block;
					}

					if (Reader.BaseStream.Position + contentsLength > Reader.BaseStream.Length) throw new InvalidSimisFormatException(File.Filename, Reader, 8, BNFState, "Simis parser got block with rediculous size: 0x" + contentsLength.ToString("X8") + ".");

					TokenText += " [read " + contentsLength + " bytes of non-nested data]";
					Reader.ReadBytes((int)contentsLength);
					if ((BlockEnds.Count > 0) && (Reader.BaseStream.Position == BlockEnds.Peek())) {
						TokenQueue.Enqueue(SimisParserToken.BlockEnd);
						BlockEnds.Pop();
					}
					return SimisParserToken.Text;
				}
				Reader.BaseStream.Seek(-4, SeekOrigin.Current);
			}

			var by = Reader.ReadByte();
			TokenText = "0x" + by.ToString("X2");
			while ((BlockEnds.Count > 0) && (Reader.BaseStream.Position >= BlockEnds.Peek())) {
				TokenQueue.Enqueue(SimisParserToken.BlockEnd);
				BlockEnds.Pop();
			}
			return SimisParserToken.Text;
		}
	}
}
