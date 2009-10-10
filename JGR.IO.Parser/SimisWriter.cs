//------------------------------------------------------------------------------
// JGR.IO.Parser library, part of MSTS Editors & Tools (http://jgrmsts.codeplex.com/).
// License: Microsoft Public License (Ms-PL).
//------------------------------------------------------------------------------

using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.IO.Compression;

namespace Jgr.IO.Parser
{
	class SimisWriter
	{
		public SimisStreamFormat StreamFormat { get; private set; }
		public bool StreamCompressed { get; private set; }
		public string SimisFormat { get; private set; }
		UnclosableStream BaseStream;
		SimisProvider SimisProvider;
		BinaryWriter BinaryWriter;
		bool DoneHeader;
		int TextIndent;
		bool TextBlocked;
		bool TextBlockEmpty;
		Stack<long> BlockStarts;
		static readonly string SafeTokenCharacters = "._!abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";

		public SimisWriter(Stream stream, SimisProvider provider, SimisStreamFormat format, bool compressed, string simisFormat) {
			if (!stream.CanWrite) throw new InvalidDataException("Stream must support writing.");
			if (!stream.CanSeek) throw new InvalidDataException("Stream must support seeking.");
			BaseStream = new UnclosableStream(stream);
			SimisProvider = provider;
			BinaryWriter = new BinaryWriter(BaseStream, new ByteEncoding());
			StreamFormat = format;
			StreamCompressed = compressed;
			SimisFormat = simisFormat;
			DoneHeader = false;
			TextIndent = 0;
			TextBlocked = true;
			TextBlockEmpty = false;
			BlockStarts = new Stack<long>();
			Debug.Assert(StreamFormat != SimisStreamFormat.AutoDetect, "Cannot save a stream in Autodetect format - must be Binary or Text.");
		}

		public void WriteToken(SimisToken token) {
			if (!DoneHeader) WriteHeader();

			if (StreamFormat == SimisStreamFormat.Text) {
				switch (token.Kind) {
					case SimisTokenKind.Block:
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
						BinaryWriter.Write(" (".ToCharArray());
						TextIndent++;
						TextBlocked = false;
						TextBlockEmpty = true;
						break;
					case SimisTokenKind.BlockEnd:
						TextIndent--;
						if (TextBlocked) {
							for (var i = 0; i < TextIndent; i++) BinaryWriter.Write('\t');
						} else if (!TextBlockEmpty) {
							BinaryWriter.Write(' ');
						}
						BinaryWriter.Write(")\r\n".ToCharArray());
						TextBlocked = true;
						break;
					case SimisTokenKind.Integer:
						if (TextBlocked) {
							for (var i = 0; i < TextIndent; i++) BinaryWriter.Write('\t');
						} else {
							BinaryWriter.Write(' ');
						}
						BinaryWriter.Write(token.Integer.ToString().ToCharArray());
						if (TextBlocked) {
							BinaryWriter.Write("\r\n".ToCharArray());
						}
						TextBlockEmpty = false;
						break;
					case SimisTokenKind.Float:
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
						if (TextBlocked) {
							for (var i = 0; i < TextIndent; i++) BinaryWriter.Write('\t');
						} else {
							BinaryWriter.Write(' ');
						}
						if (token.String.ToCharArray().All<char>(c => SafeTokenCharacters.Contains(c))) {
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
						var id = SimisProvider.TokenNames.FirstOrDefault<KeyValuePair<uint, string>>(kvp => kvp.Value == token.Type).Key;
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
					case SimisTokenKind.Integer:
						if (token.Integer > int.MaxValue) {
							BinaryWriter.Write((uint)token.Integer);
						} else {
							BinaryWriter.Write((int)token.Integer);
						}
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
				BinaryWriter.Write(("JINX0" + SimisFormat + "t______\r\n\r\n").ToCharArray());
			} else {
				BinaryWriter.Write(("JINX0" + SimisFormat + "b______\r\n").ToCharArray());
			}

			DoneHeader = true;
		}
	}
}
