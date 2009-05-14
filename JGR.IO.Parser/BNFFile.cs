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
				Operator expression = null;
				var or = false;
				while (true) {
					var optional = false;
					var repeat = false;
					Operator op = null;
					EatWhitespace();
					if ('[' == Reader.Peek()) {
						Reader.Read();
						optional = true;
					}
					EatWhitespace();
					if ('{' == Reader.Peek()) {
						Reader.Read();
						repeat = true;
					}
					EatWhitespace();
					if ('"' == Reader.Peek()) {
						var value = "";
						streamPosition = Reader.Position;
						if (!EatString(out value)) throw new InvalidBNFFormatException(File.Filename, Reader, Reader.Position - streamPosition, "BNF parser expected string.");
						Debug.Assert(expression == null);
						op = new StringOperator(value);
					} else if ('.' == Reader.Peek()) {
						Reader.ReadLine();
						break;
					} else {
						if (':' == Reader.Peek()) {
							Reader.Read();
						}
						var token = "";
						streamPosition = Reader.Position;
						if (!EatSymbol(out token)) throw new InvalidBNFFormatException(File.Filename, Reader, Reader.Position - streamPosition, "BNF parser expected token.");
						op = new ReferenceOperator(token);
						if (',' == Reader.Peek()) {
							Reader.Read();
							var tokenName = "";
							streamPosition = Reader.Position;
							if (!EatSymbol(out tokenName)) throw new InvalidBNFFormatException(File.Filename, Reader, Reader.Position - streamPosition, "BNF parser expected token name.");
							op = new NamedReferenceOperator(tokenName, token);
						}
					}
					EatWhitespace();
					if (repeat) {
						op = new RepeatOperator(op);
						if ('}' != Reader.Read()) throw new InvalidBNFFormatException(File.Filename, Reader, 1, "BNF parser expected '}'.");
					}
					EatWhitespace();
					if (optional) {
						op = new OptionalOperator(op);
						if (']' != Reader.Read()) throw new InvalidBNFFormatException(File.Filename, Reader, 1, "BNF parser expected ']'.");
					}

					if (expression == null) {
						expression = op;
					} else if (or) {
						expression = new LogicalOrOperator(expression, op);
					} else {
						expression = new LogicalAndOperator(expression, op);
					}

					EatWhitespace();
					if ('|' == Reader.Peek()) {
						Reader.Read();
						or = true;
					} else {
						or = false;
					}
				}
				BNFRule rule = null;
				if (production) {
					rule = new BNFProduction(File.BNF, new ReferenceOperator(symbol), expression);
				} else {
					rule = new BNFDefinition(File.BNF, new ReferenceOperator(symbol), expression);
				}
				return rule;
			}
			return null;
		}
	}
}
