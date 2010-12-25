//------------------------------------------------------------------------------
// Jgr.IO.Parser library, part of MSTS Editors & Tools (http://jgrmsts.codeplex.com/).
// License: New BSD License (BSD).
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
	public class SimisJinxWriter : SimisWriter
	{
		public readonly bool JinxStreamIsBinary;
		public readonly SimisJinxFormat JinxStreamFormat;

		readonly SimisProvider SimisProvider;

		int TextIndent;
		bool TextBlocked;
		bool TextBlockEmpty;
		Stack<long> BlockStarts;
		BnfState BnfState;

		public const string SafeTokenCharacters = "._!/+-abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";

		public SimisJinxWriter(SimisStreamWriter writer, SimisProvider simisProvider, bool jinxStreamIsBinary, SimisJinxFormat jinxStreamFormat)
			: base(writer) {
			SimisProvider = simisProvider;
			JinxStreamIsBinary = jinxStreamIsBinary;
			JinxStreamFormat = jinxStreamFormat;

			TextBlocked = true;
			BlockStarts = new Stack<long>();

			if (JinxStreamIsBinary) {
				Writer.Write(("JINX0" + JinxStreamFormat.Format + "b______\r\n").ToCharArray());
			} else {
				Writer.Write(("JINX0" + JinxStreamFormat.Format + "t______\r\n\r\n").ToCharArray());
			}
		}

		public void WriteToken(SimisToken token) {
			if (JinxStreamIsBinary) {
				switch (token.Kind) {
					case SimisTokenKind.Block:
						var id = SimisProvider.TokenIds[token.Type];
						Writer.Write((uint)id);
						Writer.Write((uint)0x7F8F7F8F); // Length, set to sentinel value for now.
						BlockStarts.Push(Writer.BaseStream.Position);
						Writer.Write((byte)token.Name.Length);
						foreach (var ch in token.Name.ToCharArray()) {
							Writer.Write((ushort)ch);
						}
						break;
					case SimisTokenKind.BlockBegin:
						break;
					case SimisTokenKind.BlockEnd:
						var start = BlockStarts.Pop();
						var length = (uint)(Writer.BaseStream.Position - start);
						Writer.BaseStream.Seek(start - 4, SeekOrigin.Begin);
						Writer.Write(length);
						Writer.BaseStream.Seek(0, SeekOrigin.End);
						break;
					case SimisTokenKind.IntegerUnsigned:
						Writer.Write(token.IntegerUnsigned);
						break;
					case SimisTokenKind.IntegerSigned:
						Writer.Write(token.IntegerSigned);
						break;
					case SimisTokenKind.IntegerDWord:
						Writer.Write(token.IntegerDWord);
						break;
					case SimisTokenKind.IntegerWord:
						Writer.Write((ushort)token.IntegerDWord);
						break;
					case SimisTokenKind.IntegerByte:
						Writer.Write((byte)token.IntegerDWord);
						break;
					case SimisTokenKind.Float:
						Writer.Write((float)token.Float);
						break;
					case SimisTokenKind.String:
						Writer.Write((ushort)token.String.Length);
						foreach (var ch in token.String.ToCharArray()) {
							Writer.Write((ushort)ch);
						}
						break;
					default:
						throw new InvalidDataException("SimisToken.Kind is invalid: " + token.Kind);
				}
			} else {
				switch (token.Kind) {
					case SimisTokenKind.Block:
						if (BnfState == null) {
							BnfState = new BnfState(JinxStreamFormat.Bnf);
						}
						if (token.Type.Length > 0) {
							BnfState.MoveTo(token.Type);
						}
						if (!TextBlocked) {
							Writer.Write("\r\n".ToCharArray());
						}
						for (var i = 0; i < TextIndent; i++) Writer.Write('\t');
						Writer.Write(token.Type.ToCharArray());
						if (token.Name.Length > 0) {
							Writer.Write((" " + token.Name).ToCharArray());
						}
						TextBlocked = true;
						break;
					case SimisTokenKind.BlockBegin:
						BnfState.EnterBlock();
						Writer.Write(" (".ToCharArray());
						TextIndent++;
						TextBlocked = false;
						TextBlockEmpty = true;
						break;
					case SimisTokenKind.BlockEnd:
						BnfState.LeaveBlock();
						if (BnfState.IsCompleted) {
							BnfState = null;
						}
						TextIndent--;
						if (TextBlocked) {
							for (var i = 0; i < TextIndent; i++) Writer.Write('\t');
						} else if (!TextBlockEmpty) {
							Writer.Write(' ');
						}
						Writer.Write(")\r\n".ToCharArray());
						TextBlocked = true;
						break;
					case SimisTokenKind.IntegerUnsigned:
						BnfState.MoveTo(token.Type);
						if (TextBlocked) {
							for (var i = 0; i < TextIndent; i++) Writer.Write('\t');
						} else {
							Writer.Write(' ');
						}
						Writer.Write(token.IntegerUnsigned.ToString(CultureInfo.InvariantCulture).ToCharArray());
						if (TextBlocked) {
							Writer.Write("\r\n".ToCharArray());
						}
						TextBlockEmpty = false;
						break;
					case SimisTokenKind.IntegerSigned:
						BnfState.MoveTo(token.Type);
						if (TextBlocked) {
							for (var i = 0; i < TextIndent; i++) Writer.Write('\t');
						} else {
							Writer.Write(' ');
						}
						Writer.Write(token.IntegerSigned.ToString(CultureInfo.InvariantCulture).ToCharArray());
						if (TextBlocked) {
							Writer.Write("\r\n".ToCharArray());
						}
						TextBlockEmpty = false;
						break;
					case SimisTokenKind.IntegerDWord:
					case SimisTokenKind.IntegerWord:
					case SimisTokenKind.IntegerByte:
						BnfState.MoveTo(token.Type);
						if (TextBlocked) {
							for (var i = 0; i < TextIndent; i++) Writer.Write('\t');
						} else {
							Writer.Write(' ');
						}
						Writer.Write(token.IntegerDWord.ToString(token.Kind == SimisTokenKind.IntegerDWord ? "X8" : token.Kind == SimisTokenKind.IntegerWord ? "X4" : "X2", CultureInfo.InvariantCulture).ToCharArray());
						if (TextBlocked) {
							Writer.Write("\r\n".ToCharArray());
						}
						TextBlockEmpty = false;
						break;
					case SimisTokenKind.Float:
						BnfState.MoveTo(token.Type);
						if (TextBlocked) {
							for (var i = 0; i < TextIndent; i++) Writer.Write('\t');
						} else {
							Writer.Write(' ');
						}
						if (token.Float.ToString("G6", CultureInfo.InvariantCulture).IndexOf("E", StringComparison.OrdinalIgnoreCase) >= 0) {
							Writer.Write(token.Float.ToString("0.#####e000", CultureInfo.InvariantCulture).ToCharArray());
						} else {
							Writer.Write(token.Float.ToString("G6", CultureInfo.InvariantCulture).ToCharArray());
						}
						if (TextBlocked) {
							Writer.Write("\r\n".ToCharArray());
						}
						TextBlockEmpty = false;
						break;
					case SimisTokenKind.String:
						// If the token has no type, it was a specially-skipped input (comment, SKIP(...) block etc.).
						if (token.Type.Length == 0) {
							if (!TextBlocked) {
								Writer.Write("\r\n".ToCharArray());
							}
							for (var i = 0; i < TextIndent; i++) Writer.Write('\t');
							Writer.Write(token.String.ToCharArray());
							Writer.Write("\r\n".ToCharArray());
							TextBlocked = true;
							TextBlockEmpty = true;
						} else {
							BnfState.MoveTo(token.Type);
							if (TextBlocked) {
								for (var i = 0; i < TextIndent; i++) Writer.Write('\t');
							} else {
								Writer.Write(' ');
							}
							if ((token.String.Length > 0) && token.String.ToCharArray().All(c => SafeTokenCharacters.Contains(c))) {
								Writer.Write(token.String.ToCharArray());
							} else {
								var wrap = "\"+\r\n";
								for (var i = 0; i < TextIndent; i++) wrap += '\t';
								wrap += " \"";

								Writer.Write(('"' + token.String.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\t", "\\t").Replace("\n\n", "\\n\n").Replace("\n", "\\n" + wrap) + '"').Replace(wrap + '"', "\"" + wrap.Substring(2, wrap.Length - 4)).ToCharArray());
							}
							if (TextBlocked) {
								Writer.Write("\r\n".ToCharArray());
							}
							TextBlockEmpty = false;
						}
						break;
					default:
						throw new InvalidDataException("SimisToken.Kind is invalid: " + token.Kind);
				}
			}
		}

		public void WriteEnd() {
			if (!JinxStreamIsBinary) {
				Writer.Write("\r\n".ToCharArray());
			}
		}
	}
}
