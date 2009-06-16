﻿//------------------------------------------------------------------------------
// JGR.IO.Parser library, part of MSTS Editors & Tools (http://jgrmsts.codeplex.com/).
// License: Microsoft Public License (Ms-PL).
//------------------------------------------------------------------------------

using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Text;

namespace JGR.IO.Parser
{
	class SimisWriter
	{
		protected Stream BaseStream { get; set; }
		protected SimisProvider SimisProvider { get; set; }
		protected BinaryWriter BinaryWriter { get; set; }
		public SimisStreamFormat StreamFormat { get; protected set; }
		public bool StreamCompressed { get; protected set; }
		public string SimisFormat { get; protected set; }
		protected bool DoneHeader { get; set; }
		protected int TextIndent { get; set; }
		protected bool TextBlocked { get; set; }

		protected readonly string SafeTokenCharacters;

		public SimisWriter(Stream stream, SimisProvider provider, SimisStreamFormat format, bool compressed, string simisFormat) {
			if (!stream.CanWrite) throw new InvalidDataException("Stream must support writing.");
			if (!stream.CanSeek) throw new InvalidDataException("Stream must support seeking.");
			BaseStream = stream;
			SimisProvider = provider;
			BinaryWriter = new BinaryWriter(BaseStream, new ByteEncoding());
			StreamFormat = format;
			StreamCompressed = compressed;
			SimisFormat = simisFormat;
			DoneHeader = false;
			TextIndent = 0;
			TextBlocked = true;
			Debug.Assert(StreamFormat != SimisStreamFormat.Autodetect, "Cannot save a stream in Autodetect format - must be Binary or Text.");
			Debug.Assert(StreamCompressed == false, "Compressed streams are not currently supported.");

			SafeTokenCharacters = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
		}

		public void WriteToken(SimisToken token) {
			if (!DoneHeader) WriteHeader();

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
					break;
				case SimisTokenKind.BlockEnd:
					TextIndent--;
					if (TextBlocked) {
						for (var i = 0; i < TextIndent; i++) BinaryWriter.Write('\t');
					} else {
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
					break;
				case SimisTokenKind.Float:
					if (TextBlocked) {
						for (var i = 0; i < TextIndent; i++) BinaryWriter.Write('\t');
					} else {
						BinaryWriter.Write(' ');
					}
					BinaryWriter.Write(token.Float.ToString("G6").ToCharArray());
					if (TextBlocked) {
						BinaryWriter.Write("\r\n".ToCharArray());
					}
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
					break;
				default:
					throw new InvalidDataException("SimisToken.Kind is invalid: " + token.Kind);
			}
		}

		private void WriteHeader() {
			// We support:
			//   Text (uncompressed)   ==> UTF16LE text
			//   Binary (uncompressed) ==> binary
			//   Binary (compressed)   ==> Deflate binary

			if (StreamFormat == SimisStreamFormat.Text) {
				BinaryWriter.Write(Encoding.Unicode.GetPreamble());
				BinaryWriter = new BinaryWriter(BaseStream, Encoding.Unicode);
			} else if (StreamCompressed) {
				// FIXME: Put DeflatStream stuff in here when we support it.
			}

			if (StreamCompressed) {
				BinaryWriter.Write("SIMISA@F".ToCharArray());
			} else {
				BinaryWriter.Write("SIMISA@@@@@@@@@@".ToCharArray());
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
