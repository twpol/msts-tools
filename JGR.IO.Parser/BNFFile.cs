//------------------------------------------------------------------------------
// JGR.IO.Parser library, part of MSTS Editors & Tools (http://jgrmsts.codeplex.com/).
// License: Microsoft Public License (Ms-PL).
//------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using JGR;
using JGR.Grammar;
using JGR.IO;

namespace JGR.IO.Parser
{
	public class InvalidBNFFormatException : FileException
	{
		private static string FormatMessage(BufferedInMemoryTextReader reader, long alreadyRead, string message) {
			var beforeError = Math.Min(128, reader.Position - alreadyRead);
			var afterError = Math.Min(128, reader.Length - reader.Position);
			reader.Position -= alreadyRead;
			reader.Position -= beforeError;

			var sourceText = message + "\n\n";
			if (beforeError > 0) {
				sourceText += "From 0x" + reader.Position.ToString("X8") + " - data preceding failure:\n";
				for (var i = 0; i < beforeError; i++) {
					sourceText += (char)reader.Read();
				}
				sourceText += "\n\n";
			}
			if (alreadyRead > 0) {
				sourceText += "From 0x" + reader.Position.ToString("X8") + " - data at failure:\n";
				for (var i = 0; i < alreadyRead; i++) {
					sourceText += (char)reader.Read();
				}
				sourceText += "\n\n";
			}
			if (afterError > 0) {
				sourceText += "From 0x" + reader.Position.ToString("X8") + " - data following failure:\n";
				for (var i = 0; i < afterError; i++) {
					sourceText += (char)reader.Read();
				}
				sourceText += "\n\n";
			}

			return sourceText;
		}

		public InvalidBNFFormatException(string filename, BufferedInMemoryTextReader reader, long alreadyRead, string message)
			: base(filename, FormatMessage(reader, alreadyRead, message))
		{
		}
	}

	public class BNFFile : BufferedMessageSource
	{
		public string Filename { get; private set; }
		public string BNFFileName { get; private set; }
		public string BNFFileExt { get; private set; }
		public string BNFFileType { get; private set; }
		public int BNFFileTypeVer { get; private set; }
		public BNF BNF { get; private set; }

		public BNFFile(string filename) {
			Filename = filename;
			BNFFileName = "";
			BNFFileExt = "";
			BNFFileType = "";
			BNFFileTypeVer = 0;
			BNF = new BNF(Filename);
		}

		public override string GetMessageSourceName() {
			return "BNF File";
		}

		public void ReadFile() {
			MessageSend(LEVEL_INFORMATION, "Loading '" + Filename + "'...");
			using (var fileStream = File.OpenRead(Filename)) {
				using (var bufferedFileStream = new BufferedInMemoryStream(fileStream)) {
					using (var reader = new StreamReader(bufferedFileStream, true)) {
						var parser = new BNFParser(this, reader);
						while (true) {
							var rule = parser.NextRule();
							if (rule == null) break;
							MessageSend(LEVEL_DEBUG, rule.ToString());

							if (rule is BNFDefinition) {
								if ((rule.Symbol.Reference == "FILE_NAME") && (rule.Expression is StringOperator)) {
									BNFFileName = ((StringOperator)rule.Expression).Value;
								} else if ((rule.Symbol.Reference == "FILE_EXT") && (rule.Expression is StringOperator)) {
									BNFFileExt = ((StringOperator)rule.Expression).Value;
								} else if ((rule.Symbol.Reference == "FILE_TYPE") && (rule.Expression is StringOperator)) {
									BNFFileType = ((StringOperator)rule.Expression).Value;
								} else if ((rule.Symbol.Reference == "FILE_TYPE_VER") && (rule.Expression is StringOperator)) {
									BNFFileTypeVer = int.Parse(((StringOperator)rule.Expression).Value);
								}
								BNF.Definitions.Add(rule.Symbol.Reference, (BNFDefinition)rule);
							}
							if (rule is BNFProduction) {
								BNF.Productions.Add(rule.Symbol.Reference, (BNFProduction)rule);
							}
						}

						Debug.Assert(reader.BaseStream.Position >= reader.BaseStream.Length, "Parser " + parser.ToString() + " failed to consume all input for <" + Filename + ">.");
					}
				}
			}
			MessageSend(LEVEL_INFORMATION, "Done.");
		}
	}

	class BNFParser
	{
		private readonly char[] whitespaceChars;
		private readonly char[] symbolFirstChars;
		private readonly char[] symbolChars;

		private readonly BNFFile File;
		protected BufferedInMemoryTextReader Reader { get; private set; }

		public BNFParser(BNFFile file, StreamReader reader) {
			whitespaceChars = " \t\r\n".ToCharArray();
			symbolFirstChars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ_".ToCharArray();
			symbolChars = symbolFirstChars.Union<char>("0123456789".ToCharArray()).ToArray<char>();

			File = file;
			Reader = new BufferedInMemoryTextReader(reader);
		}

		private void EatWhitespace() {
			while ((Reader.Peek() != -1) && whitespaceChars.Any<char>(c => c == Reader.Peek())) {
				Reader.Read();
			}
			if (EatComment()) {
				EatWhitespace();
			}
		}

		private bool EatComment() {
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

		private bool EatString(out string value) {
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

		private bool EatSymbol(out string symbol) {
			symbol = "";
			if (Reader.Peek() == -1) return false;
			if (symbolFirstChars.Any<char>(c => c == Reader.Peek())) {
				symbol += (char)Reader.Read();
			} else {
				return false;
			}
			while ((Reader.Peek() != -1) && symbolChars.Any<char>(c => c == Reader.Peek())) {
				symbol += (char)Reader.Read();
			}
			return true;
		}

		public BNFRule NextRule() {
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
				if ('=' != Reader.Read()) throw new InvalidBNFFormatException(File.Filename, Reader, 1, "BNF parser expected '='.");
				var production = ('=' == Reader.Peek());
				if (production) {
					if ('=' != Reader.Read()) throw new InvalidBNFFormatException(File.Filename, Reader, 2, "BNF parser expected '==>'.");
					if ('>' != Reader.Read()) throw new InvalidBNFFormatException(File.Filename, Reader, 3, "BNF parser expected '==>'.");
				}

				var stackEx = new Stack<Operator>();
				var stackOp = new Stack<char>();
				Func<int> stackProcessOp = () => {
					var op = stackOp.Pop();
					switch (op) {
						case ']':
							if (stackEx.Count < 1) throw new InvalidBNFFormatException(File.Filename, Reader, 0, "BNF parser found unmatched '['.");
							var right = stackEx.Pop();
							stackEx.Push(new OptionalOperator(right));
							break;
						case '}':
							if (stackEx.Count < 1) throw new InvalidBNFFormatException(File.Filename, Reader, 0, "BNF parser found unmatched '{'.");
							right = stackEx.Pop();
							stackEx.Push(new RepeatOperator(right));
							break;
						case '|':
							if (stackEx.Count < 2) throw new InvalidBNFFormatException(File.Filename, Reader, 0, "BNF parser found unbalanced '|'.");
							right = stackEx.Pop();
							var left = stackEx.Pop();
							stackEx.Push(new LogicalOrOperator(left, right));
							break;
						case ' ':
							if (stackEx.Count < 2) throw new InvalidBNFFormatException(File.Filename, Reader, 0, "BNF parser found unbalanced ' '.");
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
						if (!EatString(out value)) throw new InvalidBNFFormatException(File.Filename, Reader, Reader.Position - streamPosition, "BNF parser expected string.");
						ex = new StringOperator(value);
					} else if ('.' == Reader.Peek()) {
						Reader.Read();
						if ((stackOp.Count > 0) && (' ' == stackOp.Peek())) stackOp.Pop();
						while (stackOp.Count > 0) stackProcessOp();
						if (stackEx.Count > 1) throw new InvalidBNFFormatException(File.Filename, Reader, 1, "BNF parser expected ']' and/or '}' but found '.'.");
						Reader.ReadLine();
						break;
					} else {
						if (':' != Reader.Read()) throw new InvalidBNFFormatException(File.Filename, Reader, 1, "BNF parser expected ':'.");
						var token = "";
						streamPosition = Reader.Position;
						if (!EatSymbol(out token)) throw new InvalidBNFFormatException(File.Filename, Reader, Reader.Position - streamPosition, "BNF parser expected token.");
						ex = new ReferenceOperator(token);

						EatWhitespace();
						if (',' == Reader.Peek()) {
							Reader.Read();

							var tokenName = "";
							EatWhitespace();
							streamPosition = Reader.Position;
							if (!EatSymbol(out tokenName)) throw new InvalidBNFFormatException(File.Filename, Reader, Reader.Position - streamPosition, "BNF parser expected token name.");
							ex = new NamedReferenceOperator(tokenName, token);
						}
					}
					Debug.Assert(ex != null, "BNF parsed expression is null!");
					stackEx.Push(ex);

					EatWhitespace();
					while (new char[]{ ']', '}' }.Contains<char>((char)Reader.Peek())) {
						var endOp = (char)Reader.Read();
						while ((stackOp.Count > 0) && (stackOp.Peek() != endOp)) stackProcessOp();
						if (stackOp.Count == 0) throw new InvalidBNFFormatException(File.Filename, Reader, 0, "BNF parser found unmatched '" + endOp + "'.");
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
				BNFRule rule = null;
				if (production) {
					rule = new BNFProduction(File.BNF, new ReferenceOperator(symbol), stackEx.Pop());
				} else {
					rule = new BNFDefinition(File.BNF, new ReferenceOperator(symbol), stackEx.Pop());
				}
				return rule;
			}
			return null;
		}
	}
}
