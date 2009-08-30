//------------------------------------------------------------------------------
// JGR.IO library, part of MSTS Editors & Tools (http://jgrmsts.codeplex.com/).
// License: Microsoft Public License (Ms-PL).
//------------------------------------------------------------------------------

using System.IO;

namespace JGR.IO
{
	public class UnclosableStream : Stream
	{
		Stream BaseStream;

		public UnclosableStream(Stream baseStream) {
			BaseStream = baseStream;
		}

		public override void Close() {
			/* NOTHING AT ALL! */
		}

		public override bool CanRead {
			get {
				return BaseStream.CanRead;
			}
		}

		public override bool CanSeek {
			get {
				return BaseStream.CanSeek;
			}
		}

		public override bool CanWrite {
			get {
				return BaseStream.CanWrite;
			}
		}

		public override void Flush() {
			BaseStream.Flush();
		}

		public override long Length {
			get {
				return BaseStream.Length;
			}
		}

		public override long Position {
			get {
				return BaseStream.Position;
			}
			set {
				BaseStream.Position = value;
			}
		}

		public override int Read(byte[] buffer, int offset, int count) {
			return BaseStream.Read(buffer, offset, count);
		}

		public override long Seek(long offset, SeekOrigin origin) {
			return BaseStream.Seek(offset, origin);
		}

		public override void SetLength(long value) {
			BaseStream.SetLength(value);
		}

		public override void Write(byte[] buffer, int offset, int count) {
			BaseStream.Write(buffer, offset, count);
		}
	}
}
