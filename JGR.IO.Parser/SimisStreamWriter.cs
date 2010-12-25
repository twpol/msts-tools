//------------------------------------------------------------------------------
// Jgr.IO.Parser library, part of MSTS Editors & Tools (http://jgrmsts.codeplex.com/).
// License: New BSD License (BSD).
//------------------------------------------------------------------------------

using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Text;

namespace Jgr.IO.Parser {
	[Immutable]
	public class SimisStreamWriter : BinaryWriter {
		public readonly bool IsBinary;
		public readonly bool IsCompressed;
		readonly Stream RealStream;
		readonly long CompressedLengthPosition;

		SimisStreamWriter(Stream realStream, Stream output, bool isBinary, bool isCompressed, long compressedLengthPosition)
			: base(output, isBinary ? ByteEncoding.Encoding : Encoding.Unicode) {
			RealStream = realStream;
			IsBinary = isBinary;
			IsCompressed = isCompressed;
			CompressedLengthPosition = compressedLengthPosition;
		}

		public static SimisStreamWriter ToStream(Stream output, bool isBinary, bool isCompressed) {
			var compressedLengthPosition = 0L;
			var innerStream = output;
			if (isBinary) {
				using (var writer = new BinaryWriter(new UnclosableStream(output), ByteEncoding.Encoding)) {
					if (isCompressed) {
						writer.Write("SIMISA@F".ToCharArray());
						compressedLengthPosition = output.Position;
						writer.Write("\0\0\0\0@@@@\x78\x9C".ToCharArray());
						innerStream = new BufferedInMemoryStream(new DeflateStream(output, CompressionMode.Compress));
					} else {
						writer.Write("SIMISA@@@@@@@@@@".ToCharArray());
					}
				}
			} else {
				Debug.Assert(!isCompressed, "SimisStreamWriter does not support compressed text.");
				using (var writer = new BinaryWriter(new UnclosableStream(output), Encoding.Unicode)) {
					writer.Write(Encoding.Unicode.GetPreamble());
					writer.Write("SIMISA@@@@@@@@@@".ToCharArray());
				}
			}
			return new SimisStreamWriter(output, innerStream, isBinary, isCompressed, compressedLengthPosition);
		}

		protected override void Dispose(bool disposing) {
			if (disposing && IsCompressed) {
				((BufferedInMemoryStream)BaseStream).RealFlush();
				var position = RealStream.Position;
				RealStream.Position = CompressedLengthPosition;
				using (var writer = new BinaryWriter(new UnclosableStream(RealStream), ByteEncoding.Encoding)) {
					writer.Write((int)BaseStream.Position);
				}
				RealStream.Position = position;
			}
			base.Dispose(disposing);
		}
	}
}
