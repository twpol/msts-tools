//------------------------------------------------------------------------------
// JGR.IO.Parser library, part of MSTS Editors & Tools (http://jgrmsts.codeplex.com/).
// License: Microsoft Public License (Ms-PL).
//------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using Jgr.Grammar;

namespace Jgr.IO.Parser
{
	class SimisWriter
	{
		public SimisFormat SimisFormat { get; private set; }
		public SimisStreamFormat StreamFormat { get; private set; }
		public bool StreamCompressed { get; private set; }
		UnclosableStream BaseStream;
		SimisProvider SimisProvider;
		BinaryWriter BinaryWriter;
		bool DoneHeader;
		int TextIndent;
		bool TextBlocked;
		bool TextBlockEmpty;
		Stack<long> BlockStarts;
		BnfState BnfState;
		public static readonly string SafeTokenCharacters = "._!/-abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";

		public SimisWriter(Stream stream, SimisProvider provider, SimisFormat simisFormat, SimisStreamFormat format, bool compressed) {
			if (!stream.CanWrite) throw new InvalidDataException("Stream must support writing.");
			if (!stream.CanSeek) throw new InvalidDataException("Stream must support seeking.");
			if (simisFormat == null) throw new ArgumentException("Cannot save a stream without a simisFormat.", "simisFormat");
			if (format == SimisStreamFormat.AutoDetect) throw new ArgumentException("Cannot save a stream in Autodetect format.", "format");

			BaseStream = new UnclosableStream(stream);
			BinaryWriter = new BinaryWriter(BaseStream, new ByteEncoding());
			SimisProvider = provider;
			SimisFormat = simisFormat;
			StreamFormat = format;
			StreamCompressed = compressed;
			DoneHeader = false;
			TextIndent = 0;
			TextBlocked = true;
			TextBlockEmpty = false;
			BlockStarts = new Stack<long>();
		}

		public void WriteToken(SimisToken token) {
			if (!DoneHeader) WriteHeader();

			if (StreamFormat == SimisStreamFormat.Text) {
				switch (token.Kind) {
					case SimisTokenKind.Block:
						if (BnfState == null) {
							BnfState = new BnfState(SimisFormat.Bnf);
						}
						BnfState.MoveTo(token.Type);
						if (!TextBlocked) {
							BinaryWriter.Write("\r\n".ToCharArray());
						}
						for (var i = 0; i < TextIndent; i++) BinaryWriter.Write('\t');
						BinaryWriter.Write(token.Type.ToCharArray());
						if (token.String.Length > 0) {
							BinaryWriter.Write((" " + token.String).ToCharArray());
						}
						TextBlocked = true;
						break;
					case SimisTokenKind.BlockBegin:
						BnfState.EnterBlock();
						BinaryWriter.Write(" (".ToCharArray());
						TextIndent++;
						TextBlocked = false;
						TextBlockEmpty = true;
						break;
					case SimisTokenKind.BlockEnd:
						BnfState.LeaveBlock();
						if (BnfState.IsEmpty) {
							BnfState = null;
						}
						TextIndent--;
						if (TextBlocked) {
							for (var i = 0; i < TextIndent; i++) BinaryWriter.Write('\t');
						} else if (!TextBlockEmpty) {
							BinaryWriter.Write(' ');
						}
						BinaryWriter.Write(")\r\n".ToCharArray());
						TextBlocked = true;
						break;
					case SimisTokenKind.IntegerUnsigned:
						BnfState.MoveTo("uint");
						if (TextBlocked) {
							for (var i = 0; i < TextIndent; i++) BinaryWriter.Write('\t');
						} else {
							BinaryWriter.Write(' ');
						}
						BinaryWriter.Write(token.IntegerUnsigned.ToString().ToCharArray());
						if (TextBlocked) {
							BinaryWriter.Write("\r\n".ToCharArray());
						}
						TextBlockEmpty = false;
						break;
					case SimisTokenKind.IntegerSigned:
						BnfState.MoveTo("sint");
						if (TextBlocked) {
							for (var i = 0; i < TextIndent; i++) BinaryWriter.Write('\t');
						} else {
							BinaryWriter.Write(' ');
						}
						BinaryWriter.Write(token.IntegerSigned.ToString().ToCharArray());
						if (TextBlocked) {
							BinaryWriter.Write("\r\n".ToCharArray());
						}
						TextBlockEmpty = false;
						break;
					case SimisTokenKind.IntegerDWord:
						BnfState.MoveTo("dword");
						if (TextBlocked) {
							for (var i = 0; i < TextIndent; i++) BinaryWriter.Write('\t');
						} else {
							BinaryWriter.Write(' ');
						}
						BinaryWriter.Write(token.IntegerDWord.ToString("X8").ToCharArray());
						if (TextBlocked) {
							BinaryWriter.Write("\r\n".ToCharArray());
						}
						TextBlockEmpty = false;
						break;
					case SimisTokenKind.Float:
						BnfState.MoveTo("float");
						if (TextBlocked) {
							for (var i = 0; i < TextIndent; i++) BinaryWriter.Write('\t');
						} else {
							BinaryWriter.Write(' ');
						}
						if (token.Float.ToString("G6").IndexOf("E") >= 0) {
							BinaryWriter.Write(token.Float.ToString("0.#####e000").ToCharArray());
						} else {
							BinaryWriter.Write(token.Float.ToString("G6").ToCharArray());
						}
						if (TextBlocked) {
							BinaryWriter.Write("\r\n".ToCharArray());
						}
						TextBlockEmpty = false;
						break;
					case SimisTokenKind.String:
						// Special-case Skip(...) blocks which are not parsed.
						//if (!token.String.StartsWith("Skip", StringComparison.InvariantCultureIgnoreCase) && !token.String.Replace(" ", "").StartsWith("Skip(", StringComparison.InvariantCultureIgnoreCase)) {
							var stringDatatypes = BnfState.ValidStates.Where(s => s == "string" || s == "buffer");
							BnfState.MoveTo(stringDatatypes.First());
						//}
						if (TextBlocked) {
							for (var i = 0; i < TextIndent; i++) BinaryWriter.Write('\t');
						} else {
							BinaryWriter.Write(' ');
						}
						if ((token.String.Length > 0) && token.String.ToCharArray().All(c => SafeTokenCharacters.Contains(c))) {
							BinaryWriter.Write(token.String.ToCharArray());
						} else {
							var wrap = "\"+\r\n";
							for (var i = 0; i < TextIndent; i++) wrap += '\t';
							wrap += " \"";

							BinaryWriter.Write(('"' + token.String.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\t", "\\t").Replace("\n\n", "\\n\n").Replace("\n", "\\n" + wrap) + '"').Replace(wrap + '"', "\"" + wrap.Substring(2, wrap.Length - 4)).ToCharArray());
						}
						if (TextBlocked) {
							BinaryWriter.Write("\r\n".ToCharArray());
						}
						TextBlockEmpty = false;
						break;
					default:
						throw new InvalidDataException("SimisToken.Kind is invalid: " + token.Kind);
				}
			} else {
				switch (token.Kind) {
					case SimisTokenKind.Block:
						var id = SimisProvider.TokenIds[token.Type];
						BinaryWriter.Write((uint)id);
						BinaryWriter.Write((uint)0x7F8F7F8F); // Length, set to sentinel value for now.
						BlockStarts.Push(BinaryWriter.BaseStream.Position);
						BinaryWriter.Write((byte)token.String.Length);
						foreach (var ch in token.String.ToCharArray()) {
							BinaryWriter.Write((ushort)ch);
						}
						break;
					case SimisTokenKind.BlockBegin:
						break;
					case SimisTokenKind.BlockEnd:
						var start = BlockStarts.Pop();
						var length = (uint)(BinaryWriter.BaseStream.Position - start);
						BinaryWriter.BaseStream.Seek(start - 4, SeekOrigin.Begin);
						BinaryWriter.Write(length);
						BinaryWriter.BaseStream.Seek(0, SeekOrigin.End);
						break;
					case SimisTokenKind.IntegerUnsigned:
						BinaryWriter.Write(token.IntegerUnsigned);
						break;
					case SimisTokenKind.IntegerSigned:
						BinaryWriter.Write(token.IntegerSigned);
						break;
					case SimisTokenKind.IntegerDWord:
						BinaryWriter.Write(token.IntegerDWord);
						break;
					case SimisTokenKind.Float:
						BinaryWriter.Write((float)token.Float);
						break;
					case SimisTokenKind.String:
						BinaryWriter.Write((ushort)token.String.Length);
						foreach (var ch in token.String.ToCharArray()) {
							BinaryWriter.Write((ushort)ch);
						}
						break;
					default:
						throw new InvalidDataException("SimisToken.Kind is invalid: " + token.Kind);
				}
			}
		}

		public void WriteEnd() {
			if (StreamFormat == SimisStreamFormat.Text) {
				BinaryWriter.Write("\r\n".ToCharArray());
			}
			if (StreamCompressed) {
				var uncompressedSize = BinaryWriter.BaseStream.Position;
				((BufferedInMemoryStream)BinaryWriter.BaseStream).RealFlush();
				BinaryWriter.Close();
				if (StreamFormat == SimisStreamFormat.Text) {
					BinaryWriter = new BinaryWriter(BaseStream, Encoding.Unicode);
					BinaryWriter.Seek(8 + Encoding.Unicode.GetPreamble().Length, SeekOrigin.Begin);
				} else {
					BinaryWriter = new BinaryWriter(BaseStream, new ByteEncoding());
					BinaryWriter.Seek(8, SeekOrigin.Begin);
				}
				BinaryWriter.Write((uint)uncompressedSize);
			}
			BinaryWriter.Close();
		}

		void WriteHeader() {
			// We support:
			//   Text (uncompressed)   ==> UTF16LE text
			//   Binary (uncompressed) ==> binary
			//   Binary (compressed)   ==> Deflate binary

			if (StreamFormat == SimisStreamFormat.Text) {
				BinaryWriter.Write(Encoding.Unicode.GetPreamble());
				BinaryWriter.Close();
				BinaryWriter = new BinaryWriter(BaseStream, Encoding.Unicode);
			}

			if (StreamCompressed) {
				BinaryWriter.Write("SIMISA@F".ToCharArray());
			} else {
				BinaryWriter.Write("SIMISA@@@@@@@@@@".ToCharArray());
			}

			if (StreamCompressed) {
				BinaryWriter.Write((uint)0x7F8F7F8F);
				BinaryWriter.Write("@@@@".ToCharArray());
				BinaryWriter.Write((byte)0x78);
				BinaryWriter.Write((byte)0x9C);
				BinaryWriter.Close();
				BinaryWriter = new BinaryWriter(new BufferedInMemoryStream(new DeflateStream(BaseStream, CompressionMode.Compress)), new ByteEncoding());
			}

			if (StreamFormat == SimisStreamFormat.Text) {
				BinaryWriter.Write(("JINX0" + SimisFormat.Format + "t______\r\n\r\n").ToCharArray());
			} else {
				BinaryWriter.Write(("JINX0" + SimisFormat.Format + "b______\r\n").ToCharArray());
			}

			DoneHeader = true;
		}
	}
}
