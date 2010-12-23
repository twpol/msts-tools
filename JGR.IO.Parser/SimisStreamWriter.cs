//------------------------------------------------------------------------------
// Jgr.IO.Parser library, part of MSTS Editors & Tools (http://jgrmsts.codeplex.com/).
// License: New BSD License (BSD).
//------------------------------------------------------------------------------

using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Text;

namespace Jgr.IO.Parser {
	public class SimisStreamWriter : BinaryWriter {
		public readonly bool IsBinary;
		public readonly bool IsCompressed;
		readonly long CompressedLengthPosition;

		SimisStreamWriter(Stream output, bool isBinary, bool isCompressed, long compressedLengthPosition)
			: base(output, isBinary ? (Encoding)new ByteEncoding() : new UnicodeEncoding()) {
			IsBinary = isBinary;
			IsCompressed = isCompressed;
			CompressedLengthPosition = compressedLengthPosition;
		}

		public static SimisStreamWriter ToStream(Stream output, bool isBinary, bool isCompressed) {
			var compressedLengthPosition = 0L;
			if (isBinary) {
				using (var writer = new BinaryWriter(new UnclosableStream(output), new ByteEncoding())) {
					if (isCompressed) {
						writer.Write("SIMISA@F");
						//writer.Write(new byte[] { 0x53, 0x49, 0x4D, 0x49, 0x53, 0x41, 0x40, 0x46 }, 0, 8);
						compressedLengthPosition = output.Position;
						writer.Write("\0\0\0\0@@@@\x78\x9C");
						//writer.Write(new byte[] { 0x00, 0x00, 0x00, 0x00, 0x40, 0x40, 0x40, 0x40, 0x78, 0x9C }, 0, 10);
					} else {
						writer.Write("SIMISA@@@@@@@@@@");
						//writer.Write(new byte[] { 0x53, 0x49, 0x4D, 0x49, 0x53, 0x41, 0x40, 0x40, 0x40, 0x40, 0x40, 0x40, 0x40, 0x40, 0x40, 0x40 }, 0, 16);
					}
				}
				return new SimisStreamWriter(new BufferedInMemoryStream(new DeflateStream(output, CompressionMode.Compress)), isBinary, isCompressed, compressedLengthPosition);
			} else {
				Debug.Assert(!isCompressed, "SimisStreamWriter does not support compressed text.");
				using (var writer = new BinaryWriter(new UnclosableStream(output), new UnicodeEncoding())) {
					writer.Write("SIMISA@@@@@@@@@@");
				}
				return new SimisStreamWriter(output, isBinary, isCompressed, compressedLengthPosition);
			}
		}

		public override void Flush() {
			if (IsCompressed) {
				var position = BaseStream.Position;
				BaseStream.Position = CompressedLengthPosition;
				using (var writer = new BinaryWriter(new UnclosableStream(BaseStream), new ByteEncoding())) {
					writer.Write((int)0);
				}
				BaseStream.Position = position;
			}
			base.Flush();
		}
	}
}
