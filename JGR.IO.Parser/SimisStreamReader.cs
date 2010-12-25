//------------------------------------------------------------------------------
// Jgr.IO.Parser library, part of MSTS Editors & Tools (http://jgrmsts.codeplex.com/).
// License: New BSD License (BSD).
//------------------------------------------------------------------------------

using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;

namespace Jgr.IO.Parser {
	[Immutable]
	public class SimisStreamReader : BinaryReader {
		public readonly bool IsBinary;
		public readonly bool IsCompressed;
		public readonly long UncompressedLength;

		SimisStreamReader(Stream input, bool isBinary, bool isCompressed, long uncompressedLength)
			: base(input, isBinary ? ByteEncoding.Encoding : Encoding.Unicode) {
			IsBinary = isBinary;
			IsCompressed = isCompressed;
			UncompressedLength = uncompressedLength;
		}

		public static SimisStreamReader FromStream(Stream input) {
			var isBinary = true;

			var position = input.Position;
			input.Position = 0;
			using (var reader = new BinaryReader(new UnclosableStream(input), ByteEncoding.Encoding)) {
				using (var sr = new StreamReader(new UnclosableStream(input), true)) {
					sr.ReadLine();
					input.Position = position;
					switch (sr.CurrentEncoding.BodyName) {
						case "utf-16":
							isBinary = false;
							input.Position += Encoding.Unicode.GetPreamble().Length;
							break;
						case "utf-8":
							break;
						default:
							throw new ReaderException(reader, true, (int)input.Position, "Unexpected stream encoding: " + sr.CurrentEncoding.EncodingName);
					}
				}
			}

			position = input.Position;
			using (var reader = new BinaryReader(new UnclosableStream(input), isBinary ? ByteEncoding.Encoding : Encoding.Unicode)) {
				var signature = new String(reader.ReadChars(8));
				switch (signature) {
					case "SIMISA@F":
						// Compressed header has the uncompressed size embedded in the @-padding.
						var uncompressedLength = reader.ReadUInt32();
						position = input.Position;
						signature = new String(reader.ReadChars(4));
						if (signature != "@@@@") {
							throw new ReaderException(reader, isBinary, (int)(input.Position - position), "Signature '" + signature + "' is invalid.");
						}

						// The stream is technically ZLIB, but we assume the selected ZLIB compression is DEFLATE (though we verify that here just in case). The ZLIB
						// header for DEFLATE is 0x78 0x9C (apparently).
						position = input.Position;
						var zlibHeader = reader.ReadBytes(2);
						if ((zlibHeader[0] != 0x78) || (zlibHeader[1] != 0x9C)) {
							throw new ReaderException(reader, isBinary, (int)(input.Position - position), "ZLIB signature is invalid.");
						}

						// The BufferedInMemoryStream is needed because DeflateStream only supports reading forwards - no seeking.
						return new SimisStreamReader(new BufferedInMemoryStream(new DeflateStream(input, CompressionMode.Decompress)), isBinary, true, position + uncompressedLength);
					case "SIMISA@@":
						// Uncompressed header is all @-padding.
						position = input.Position;
						signature = new String(reader.ReadChars(8));
						if (signature != "@@@@@@@@") {
							throw new ReaderException(reader, isBinary, (int)(input.Position - position), "Signature '" + signature + "' is invalid.");
						}
						return new SimisStreamReader(input, isBinary, false, input.Length);
					default:
						throw new ReaderException(reader, isBinary, (int)(input.Position - position), "Signature '" + signature + "' is invalid.");
				}
			}
		}
	}
}
