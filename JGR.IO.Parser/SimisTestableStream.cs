//------------------------------------------------------------------------------
// Jgr.IO.Parser library, part of MSTS Editors & Tools (http://jgrmsts.codeplex.com/).
// License: New BSD License (BSD).
//------------------------------------------------------------------------------

using System;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;

namespace Jgr.IO.Parser
{
	public class SimisTestableStream : Stream
	{
		Stream UncompressedStream;

		public SimisTestableStream(Stream baseStream) {
			UncompressedStream = new UnclosableStream(new MemoryStream());
			baseStream = new UnclosableStream(baseStream);
			baseStream.Seek(0, SeekOrigin.Begin);

			var start = baseStream.Position;
			var streamCompressed = false;
			var binaryReader = new BinaryReader(baseStream, new ByteEncoding());
			var binaryWriter = new BinaryWriter(UncompressedStream, new ByteEncoding());
			{
				var sr = new StreamReader(baseStream, true);
				sr.ReadLine();
				if (!(sr.CurrentEncoding is UTF8Encoding)) {
					binaryReader.Close();
					binaryWriter.Close();
					binaryReader = new BinaryReader(baseStream, sr.CurrentEncoding);
					binaryWriter = new BinaryWriter(UncompressedStream, sr.CurrentEncoding);
					start += sr.CurrentEncoding.GetPreamble().Length;
					binaryWriter.Write(sr.CurrentEncoding.GetPreamble());
				}
			}
			baseStream.Position = start;

			{
				var signature = String.Join("", binaryReader.ReadChars(8).Select(c => c.ToString()).ToArray());
				if ((signature != "SIMISA@F") && (signature != "SIMISA@@")) {
					throw new InvalidDataException("Signature '" + signature + "' is invalid.");
				}
				streamCompressed = (signature == "SIMISA@F");
				binaryWriter.Write("SIMISA@@".ToCharArray());
			}

			if (streamCompressed) {
				// This is a compressed stream. Read in the uncompressed size and DEFLATE the rest.
				binaryReader.ReadUInt32();
				{
					var signature = String.Join("", binaryReader.ReadChars(4).Select(c => c.ToString()).ToArray());
					if (signature != "@@@@") {
						throw new InvalidDataException("Signature '" + signature + "' is invalid.");
					}
				}
				// The stream is technically ZLIB, but we assume the selected ZLIB compression is DEFLATE (though we verify that here just in case). The ZLIB
				// header for DEFLATE is 0x78 0x9C (apparently).
				{
					var zlibHeader = binaryReader.ReadBytes(2);
					if ((zlibHeader[0] != 0x78) || (zlibHeader[1] != 0x9C)) {
						throw new InvalidDataException("ZLIB signature is invalid.");
					}
				}

				// BinaryReader -> BufferedInMemoryStream -> DeflateStream -> BinaryReader -> BaseStream.
				// The BufferedInMemoryStream is needed because DeflateStream only supports reading forwards - no seeking - and we'll potentially be jumping around.
				binaryReader.Close();
				binaryReader = new BinaryReader(new BufferedInMemoryStream(new DeflateStream(baseStream, CompressionMode.Decompress)), new ByteEncoding());
			} else {
				var signature = String.Join("", binaryReader.ReadChars(8).Select(c => c.ToString()).ToArray());
				if (signature != "@@@@@@@@") {
					throw new InvalidDataException("Signature '" + signature + "' is invalid.");
				}
			}
			binaryWriter.Write("@@@@@@@@".ToCharArray());

			var isText = false;
			{
				var signature = String.Join("", binaryReader.ReadChars(16).Select(c => c.ToString()).ToArray());
				if (signature.Substring(0, 4) == "\x01\x00\x00\x00") {
					// Texture/ACE format.
					isText = false;
				} else {
					if (signature.Substring(0, 5) != "JINX0") {
						throw new InvalidDataException("Signature '" + signature + "' is invalid.");
					}
					if (signature.Substring(8, 8) != "______\r\n") {
						throw new InvalidDataException("Signature '" + signature + "' is invalid.");
					}
					isText = (signature[7] == 't');
				}
				binaryWriter.Write(signature.ToCharArray());
			}

			var inWhitespace = true;
			var inString = false;
			var stringBuffer = "";
			while (binaryReader.BaseStream.Position < binaryReader.BaseStream.Length) {
				if (isText) {
					var bite = binaryReader.ReadChar();
					if (inString || (bite == '"')) {
						inString = inString ^ (bite == '"');
						if (inString) {
							if (bite != '"') stringBuffer += bite;
						} else {
							while ((binaryReader.PeekChar() == '\t') || (binaryReader.PeekChar() == '\n') || (binaryReader.PeekChar() == '\r') || (binaryReader.PeekChar() == ' ')) {
								binaryReader.ReadChar();
							}
							if (binaryReader.PeekChar() == '+') {
								binaryReader.ReadChar();
								while ((binaryReader.PeekChar() == '\t') || (binaryReader.PeekChar() == '\n') || (binaryReader.PeekChar() == '\r') || (binaryReader.PeekChar() == ' ')) {
									binaryReader.ReadChar();
								}
								if (binaryReader.PeekChar() != '"') throw new InvalidDataException("Data is invalid.");
								binaryReader.ReadChar();
								inString = true;
							}
							if (!inString) {
								var stringChars = stringBuffer.ToCharArray();
								if (stringChars.All(c => SimisWriter.SafeTokenCharacters.Contains(c))) {
									binaryWriter.Write(stringChars);
								} else {
									binaryWriter.Write('"');
									binaryWriter.Write(stringChars);
									binaryWriter.Write('"');
								}
								stringBuffer = "";
								binaryWriter.Write(' ');
							}
							inWhitespace = !inString;
						}
					} else {
						if ("+-.0123456789aAbBcCdDeEfF".Contains(bite)) {
							var biteStart = bite;
							var numberStart = binaryReader.BaseStream.Position;
							var numberString = "";

							var isHex = true;
							var hasDP = false;
							var hasExp = false;

							if ((bite == '+') || (bite == '-')) {
								numberString += bite;
								isHex = false;
								bite = binaryReader.ReadChar();
							}

							var allowed = isHex ? ".0123456789aAbBcCdDeEfF" : ".0123456789eE";
							while (allowed.Contains(bite)) {
								numberString += bite;
								if (numberString.Length > 8) isHex = false;
								if (".".Contains(bite)) {
									isHex = false;
									hasDP = true;
									allowed = "0123456789eE";
								} else if ("eE".Contains(bite)) {
									hasExp = true;
									allowed = isHex ? "0123456789aAbBcCdDeEfF" : "0123456789";
								} else if ("aAbBcCdDeEfF".Contains(bite)) {
									allowed = "0123456789aAbBcCdDeEfF";
								}
								bite = binaryReader.ReadChar();
							}

							if (numberString.Length != 8) isHex = false;

							var value = 0d;
							if ("\t\n\r: )".Contains(bite)) {
								if (isHex) {
									var valueH = 0;
									if (!int.TryParse(numberString, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out valueH)) numberString = "";
									value = valueH;
								} else {
									if (!double.TryParse(numberString, out value)) numberString = "";
								}
							} else {
								numberString = "";
							}

							if (numberString.Length > 0) {
								if (isHex) {
									binaryWriter.Write(((long)value).ToString("X8", CultureInfo.InvariantCulture).ToCharArray());
								} else if (!hasDP && !hasExp) {
									binaryWriter.Write(value.ToString("F0", CultureInfo.InvariantCulture).ToCharArray());
								} else if (hasExp) {
									binaryWriter.Write(value.ToString("0.#####e000", CultureInfo.InvariantCulture).ToCharArray());
								} else {
									binaryWriter.Write(value.ToString("G6", CultureInfo.InvariantCulture).ToCharArray());
								}
								inWhitespace = false;
							} else {
								bite = biteStart;
								binaryReader.BaseStream.Position = numberStart;
							}
						}

						if ((bite == '(') || (bite == ')') || (bite == '+')) {
							if (!inWhitespace) binaryWriter.Write(' ');
						}
						var isWhitespace = (bite == '\t') || (bite == '\n') || (bite == '\r') || (bite == ':') || (bite == ' ');
						if (!isWhitespace || !inWhitespace) binaryWriter.Write(isWhitespace ? ' ' : bite);
						inWhitespace = isWhitespace;
						if ((bite == '(') || (bite == ')') || (bite == '+')) {
							binaryWriter.Write(' ');
							inWhitespace = true;
						}
					}
				} else {
					binaryWriter.Write(binaryReader.ReadChars(1024));
				}
			}

			binaryReader.Close();
			binaryWriter.Close();
			UncompressedStream.Seek(0, SeekOrigin.Begin);
		}

		public override void Close() {
			base.Close();
			UncompressedStream.Close();
		}

		public override bool CanRead {
			get {
				return true;
			}
		}

		public override bool CanSeek {
			get {
				return true;
			}
		}

		public override bool CanWrite {
			get {
				return false;
			}
		}

		public override void Flush() {
			throw new NotImplementedException();
		}

		public override long Length {
			get {
				return UncompressedStream.Length;
			}
		}

		public override long Position {
			get {
				return UncompressedStream.Position;
			}
			set {
				UncompressedStream.Position = value;
			}
		}

		public override int Read(byte[] buffer, int offset, int count) {
			return UncompressedStream.Read(buffer, offset, count);
		}

		public override long Seek(long offset, SeekOrigin origin) {
			return UncompressedStream.Seek(offset, origin);
		}

		public override void SetLength(long value) {
			throw new NotImplementedException();
		}

		public override void Write(byte[] buffer, int offset, int count) {
			throw new NotImplementedException();
		}
	}
}
