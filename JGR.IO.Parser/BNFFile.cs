//------------------------------------------------------------------------------
// Jgr.IO.Parser library, part of MSTS Editors & Tools (http://jgrmsts.codeplex.com/).
// License: Microsoft Public License (Ms-PL).
//------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using Jgr.Grammar;

namespace Jgr.IO.Parser
{
	public class InvalidBnfFormatException : FileException
	{
		static string FormatMessage(BufferedInMemoryTextReader reader, long alreadyRead, string message) {
			var beforeError = Math.Min(128, reader.Position - alreadyRead);
			var afterError = Math.Min(128, reader.Length - reader.Position);
			reader.Position -= alreadyRead;
			reader.Position -= beforeError;

			var sourceText = message + "\r\n\r\n";
			if (beforeError > 0) {
				sourceText += "From 0x" + reader.Position.ToString("X8", CultureInfo.CurrentCulture) + " - data preceding failure:\r\n";
				for (var i = 0; i < beforeError; i++) {
					sourceText += (char)reader.Read();
				}
				sourceText += "\r\n\r\n";
			}
			if (alreadyRead > 0) {
				sourceText += "From 0x" + reader.Position.ToString("X8", CultureInfo.CurrentCulture) + " - data at failure:\r\n";
				for (var i = 0; i < alreadyRead; i++) {
					sourceText += (char)reader.Read();
				}
				sourceText += "\r\n\r\n";
			}
			if (afterError > 0) {
				sourceText += "From 0x" + reader.Position.ToString("X8", CultureInfo.CurrentCulture) + " - data following failure:\r\n";
				for (var i = 0; i < afterError; i++) {
					sourceText += (char)reader.Read();
				}
				sourceText += "\r\n\r\n";
			}

			return sourceText;
		}

		public InvalidBnfFormatException(string fileName, BufferedInMemoryTextReader reader, long alreadyRead, string message)
			: base(fileName, FormatMessage(reader, alreadyRead, message))
		{
		}
	}

	public class BnfFile : BufferedMessageSource
	{
		public string FileName { get; private set; }
		public List<string> BnfFileRoots { get; private set; }
		public string BnfFileName { get; private set; }
		public string BnfFileExtension { get; private set; }
		public string BnfFileType { get; private set; }
		public int BnfFileTypeVersion { get; private set; }
		public Bnf Bnf { get; private set; }

		public BnfFile(string fileName) {
			FileName = fileName;
			BnfFileRoots = new List<string>();
			BnfFileName = "";
			BnfFileExtension = "";
			BnfFileType = "";
			BnfFileTypeVersion = -1;
			Bnf = new Bnf(FileName);
		}

		public override string MessageSourceName {
			get {
				return "BNF File";
			}
		}

		public void ReadFile() {
			MessageSend(LevelInformation, "Loading '" + FileName + "'...");
			using (var fileStream = File.OpenRead(FileName)) {
				using (var bufferedFileStream = new BufferedInMemoryStream(fileStream)) {
					using (var reader = new StreamReader(bufferedFileStream, true)) {
						var parser = new BnfParser(this, reader);
						while (true) {
							var rule = parser.NextRule();
							if (rule == null) break;
							MessageSend(LevelDebug, rule.ToString());

							if (rule is BnfDefinition) {
								if (rule.Symbol.Reference == "FILE") {
									Func<Operator, Func<Operator, IEnumerable<string>>, IEnumerable<string>> scan = null;
									scan = (op, finder) => {
										if (op is UnaryOperator) {
											return scan(((UnaryOperator)op).Right, finder);
										}
										if (op is LogicalOperator) {
											return scan(((LogicalOperator)op).Left, finder).Concat(scan(((LogicalOperator)op).Right, finder));
										}
										return finder(op);
									};

									BnfFileRoots = new List<string>(
										scan(rule.Expression, op => {
											if (op is ReferenceOperator) {
												return new string[] { ((ReferenceOperator)op).Reference };
											}
											return new string[] { };
										})
									);
								} else if ((rule.Symbol.Reference == "FILE_NAME") && (rule.Expression is StringOperator)) {
									BnfFileName = ((StringOperator)rule.Expression).Value;
								} else if ((rule.Symbol.Reference == "FILE_EXT") && (rule.Expression is StringOperator)) {
									BnfFileExtension = ((StringOperator)rule.Expression).Value;
								} else if ((rule.Symbol.Reference == "FILE_TYPE") && (rule.Expression is StringOperator)) {
									BnfFileType = ((StringOperator)rule.Expression).Value;
								} else if ((rule.Symbol.Reference == "FILE_TYPE_VER") && (rule.Expression is StringOperator)) {
									if (((StringOperator)rule.Expression).Value.Length > 0) {
										BnfFileTypeVersion = int.Parse(((StringOperator)rule.Expression).Value, CultureInfo.InvariantCulture);
									}
								}
								Bnf.Definitions.Add(rule.Symbol.Reference, (BnfDefinition)rule);
							}
							if (rule is BnfProduction) {
								Bnf.Productions.Add(rule.Symbol.Reference, (BnfProduction)rule);
							}
						}

						Debug.Assert(reader.BaseStream.Position >= reader.BaseStream.Length, "Parser " + parser.ToString() + " failed to consume all input for <" + FileName + ">.");
					}
				}
			}
			MessageSend(LevelInformation, "Done.");
			if (BnfFileName.Length == 0) throw new InvalidDataException("BNF File <" + FileName + "> does not specify a name.");
			if (BnfFileExtension.Length == 0) throw new InvalidDataException("BNF File <" + FileName + "> does not specify a file extension.");
			if (BnfFileType.Length == 0) throw new InvalidDataException("BNF File <" + FileName + "> does not specify a valid Simis Format.");
			if (BnfFileTypeVersion == -1) throw new InvalidDataException("BNF File <" + FileName + "> does not specify a valid Simis Format Version.");
		}
	}

	class BnfParser
	{
		static readonly char[] WhitespaceChars = InitWhitespaceChars();
		static readonly char[] SymbolFirstChars = InitSymbolFirstChars();
		static readonly char[] SymbolChars = InitSymbolChars();
		#region static init functions
		static char[] InitWhitespaceChars() {
			return " \t\r\n".ToCharArray();
		}

		static char[] InitSymbolFirstChars() {
			return "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ_".ToCharArray();
		}

		static char[] InitSymbolChars() {
			return SymbolFirstChars.Union<char>("0123456789".ToCharArray()).ToArray<char>();
		}
		#endregion

		protected BufferedInMemoryTextReader Reader { get; private set; }
		readonly BnfFile File;

		public BnfParser(BnfFile file, StreamReader reader) {
			File = file;
			Reader = new BufferedInMemoryTextReader(reader);
		}

		void EatWhitespace() {
			while ((Reader.Peek() != -1) && WhitespaceChars.Any<char>(c => c == Reader.Peek())) {
				Reader.Read();
			}
			if (EatComment()) {
				EatWhitespace();
			}
		}

		bool EatComment() {
			// Reader "/*" for comment start.
			if ((Reader.Peek() == -1) || ('/' != Reader.Peek())) {
				return false;
			}
			Reader.Read();
			if ((Reader.Peek() == -1) || ('*' != Reader.Peek())) {
				Reader.Position--;
				return false;
			}
			Reader.Read();

			// Read until "*/" for comment end.
			while (Reader.Peek() != -1) {
				while ((Reader.Peek() != -1) && ('*' != Reader.Peek())) {
					Reader.Read();
				}
				if (Reader.Peek() == -1) break;
				Reader.Read();
				if (Reader.Peek() == -1) break;
				if ('/' == Reader.Peek()) {
					Reader.Read();
					break;
				}
			}
			return true;
		}

		bool EatString(out string value) {
			value = "";
			if (Reader.Peek() == -1) return false;
			if ('"' != Reader.Peek()) return false;
			Reader.Read();
			while ((Reader.Peek() != -1) && ('"' != Reader.Peek())) {
				value += (char)Reader.Read();
			}
			if (Reader.Peek() == -1) return false;
			if ('"' != Reader.Peek()) return false;
			Reader.Read();
			return true;
		}

		bool EatSymbol(out string symbol) {
			symbol = "";
			if (Reader.Peek() == -1) return false;
			if (SymbolFirstChars.Any<char>(c => c == Reader.Peek())) {
				symbol += (char)Reader.Read();
			} else {
				return false;
			}
			while ((Reader.Peek() != -1) && SymbolChars.Any<char>(c => c == Reader.Peek())) {
				symbol += (char)Reader.Read();
			}
			return true;
		}

		public BnfRule NextRule() {
			var streamPosition = (long)0;
			var symbol = "";
			EatWhitespace();
			if (EatSymbol(out symbol)) {
				if (symbol == "EOF") {
					// This is the marker for the end of the BNF data. We don't care what else there is, eat it ALL.
					Reader.ReadToEnd();
					return null;
				}
				EatWhitespace();
				if (Reader.Peek() == -1) return null;
				if ('=' != Reader.Read()) throw new InvalidBnfFormatException(File.FileName, Reader, 1, "BNF parser expected '='.");
				var production = ('=' == Reader.Peek());
				if (production) {
					if ('=' != Reader.Read()) throw new InvalidBnfFormatException(File.FileName, Reader, 2, "BNF parser expected '==>'.");
					if ('>' != Reader.Read()) throw new InvalidBnfFormatException(File.FileName, Reader, 3, "BNF parser expected '==>'.");
				}

				var stackEx = new Stack<Operator>();
				var stackOp = new Stack<char>();
				Func<int> stackProcessOp = () => {
					var op = stackOp.Pop();
					switch (op) {
						case ']':
							if (stackEx.Count < 1) throw new InvalidBnfFormatException(File.FileName, Reader, 0, "BNF parser found unmatched '['.");
							var right = stackEx.Pop();
							stackEx.Push(new OptionalOperator(right));
							break;
						case '}':
							if (stackEx.Count < 1) throw new InvalidBnfFormatException(File.FileName, Reader, 0, "BNF parser found unmatched '{'.");
							right = stackEx.Pop();
							stackEx.Push(new RepeatOperator(right));
							break;
						case '|':
							if (stackEx.Count < 2) throw new InvalidBnfFormatException(File.FileName, Reader, 0, "BNF parser found unbalanced '|'.");
							right = stackEx.Pop();
							var left = stackEx.Pop();
							stackEx.Push(new LogicalOrOperator(left, right));
							break;
						case ' ':
							if (stackEx.Count < 2) throw new InvalidBnfFormatException(File.FileName, Reader, 0, "BNF parser found unbalanced ' '.");
							right = stackEx.Pop();
							left = stackEx.Pop();
							stackEx.Push(new LogicalAndOperator(left, right));
							break;
					}
					return 0;
				};

				while (true) {
					EatWhitespace();
					while (new char[] { '[', '{' }.Contains<char>((char)Reader.Peek())) {
						if ('[' == Reader.Peek()) {
							Reader.Read();
							stackOp.Push(']');
						} else if ('{' == Reader.Peek()) {
							Reader.Read();
							stackOp.Push('}');
						}
						EatWhitespace();
					}

					Operator ex = null;
					EatWhitespace();
					if ('"' == Reader.Peek()) {
						var value = "";
						streamPosition = Reader.Position;
						if (!EatString(out value)) throw new InvalidBnfFormatException(File.FileName, Reader, Reader.Position - streamPosition, "BNF parser expected string.");
						ex = new StringOperator(value);
					} else if ('.' == Reader.Peek()) {
						Reader.Read();
						if ((stackOp.Count > 0) && (' ' == stackOp.Peek())) stackOp.Pop();
						while (stackOp.Count > 0) stackProcessOp();
						if (stackEx.Count > 1) throw new InvalidBnfFormatException(File.FileName, Reader, 1, "BNF parser expected ']' and/or '}' but found '.'.");
						Reader.ReadLine();
						break;
					} else {
						if (':' != Reader.Read()) throw new InvalidBnfFormatException(File.FileName, Reader, 1, "BNF parser expected ':'.");
						var token = "";
						streamPosition = Reader.Position;
						if (!EatSymbol(out token)) throw new InvalidBnfFormatException(File.FileName, Reader, Reader.Position - streamPosition, "BNF parser expected token.");
						ex = new ReferenceOperator(token);

						EatWhitespace();
						if (',' == Reader.Peek()) {
							Reader.Read();

							var tokenName = "";
							EatWhitespace();
							streamPosition = Reader.Position;
							if (!EatSymbol(out tokenName)) throw new InvalidBnfFormatException(File.FileName, Reader, Reader.Position - streamPosition, "BNF parser expected token name.");
							ex = new NamedReferenceOperator(tokenName, token);
						}
					}
					Debug.Assert(ex != null, "BNF parsed expression is null!");
					stackEx.Push(ex);

					EatWhitespace();
					while (new char[]{ ']', '}' }.Contains<char>((char)Reader.Peek())) {
						var endOp = (char)Reader.Read();
						while ((stackOp.Count > 0) && (stackOp.Peek() != endOp)) stackProcessOp();
						if (stackOp.Count == 0) throw new InvalidBnfFormatException(File.FileName, Reader, 0, "BNF parser found unmatched '" + endOp + "'.");
						stackProcessOp();
						EatWhitespace();
					}

					EatWhitespace();
					if ('|' == Reader.Peek()) {
						Reader.Read();
						stackOp.Push('|');
					} else {
						while ((stackOp.Count > 0) && ('|' == stackOp.Peek())) stackProcessOp();
						stackOp.Push(' ');
					}
				}
				if (stackEx.Count == 0) stackEx.Push(null);
				BnfRule rule = null;
				if (production) {
					rule = new BnfProduction(File.Bnf, new ReferenceOperator(symbol), stackEx.Pop());
				} else {
					rule = new BnfDefinition(File.Bnf, new ReferenceOperator(symbol), stackEx.Pop());
				}
				return rule;
			}
			return null;
		}
	}
}
