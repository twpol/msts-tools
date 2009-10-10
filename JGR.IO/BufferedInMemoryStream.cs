//------------------------------------------------------------------------------
// JGR.IO library, part of MSTS Editors & Tools (http://jgrmsts.codeplex.com/).
// License: Microsoft Public License (Ms-PL).
//------------------------------------------------------------------------------

using System;
using System.IO;

namespace Jgr.IO
{
	/// <summary>
	/// A <see cref="Stream"/> which buffers both reads and writes in memory (courtesy of <see cref="MemoryStream"/>).
	/// <para>When reading, the length may be variable as it represents the amount buffered if the underlying stream does not report a length itself.
	/// Otherwise, it should act normally; each read will only read from the underlying stream if necessary, all data is buffered sequentially, and seeking
	/// will work within the already buffered length.</para>
	/// <para>When writing, <see cref="Flush"/> is a no-op and <see cref="SetLength"/> is not implemented; otherwise, it should act normally; each write and
	/// seek operate on the internal buffer. To actually flush the write out, you must call <see cref="RealFlush"/>; you can continue using the stream after
	/// this, but bear in mind that <see cref="RealFlush"/> only ever writes out any given byte once: if you seek back and change it after a
	/// <see cref="RealFlush"/>, the new values will never be written out.</para>
	/// </summary>
	public class BufferedInMemoryStream : Stream
	{
		MemoryStream Memory;
		Stream Base;
		long WritePosition;
		const int ChunkSize = 1024;

		public BufferedInMemoryStream(Stream stream) {
			Memory = new MemoryStream();
			Base = stream;
			if (Base.CanRead) ReadChunk(ChunkSize);
		}

		public override void Close() {
			base.Close();
			Base.Close();
		}

		void ReadChunk(int chunk) {
			var buffer = new byte[chunk];
			var bytes = Base.Read(buffer, 0, chunk);
			var oldPosition = Memory.Position;
			Memory.Seek(0, SeekOrigin.End);
			Memory.Write(buffer, 0, bytes);
			Memory.Seek(oldPosition, SeekOrigin.Begin);
		}

		public override bool CanRead {
			get { return true;  }
		}

		public override bool CanSeek {
			get { return true; }
		}

		public override bool CanWrite {
			get { return Base.CanWrite; }
		}

		public override void Flush() {
		}

		public void RealFlush() {
			if (Memory.Position > WritePosition) {
				var currentPosition = Memory.Position;
				Memory.Seek(WritePosition, SeekOrigin.Begin);
				var buffer = new byte[currentPosition - WritePosition];
				Memory.Read(buffer, 0, buffer.Length);
				Base.Write(buffer, 0, buffer.Length);
				WritePosition = currentPosition;
				Memory.Position = currentPosition;
			}

			Base.Flush();
		}

		public override long Length {
			get {
				if (Base.CanSeek) return Base.Length;
				if (Base.CanRead && (Memory.Position + ChunkSize >= Memory.Length)) ReadChunk(ChunkSize);
				return Memory.Length;
			}
		}

		public override long Position {
			get {
				return Memory.Position;
			}
			set {
				Memory.Position = value;
			}
		}

		public override int Read(byte[] buffer, int offset, int count) {
			if (Memory.Position + count > Memory.Length) ReadChunk(count);
			return Memory.Read(buffer, offset, count);
		}

		public override long Seek(long offset, SeekOrigin origin) {
			return Memory.Seek(offset, origin);
		}

		public override void SetLength(long value) {
			throw new NotImplementedException();
		}

		public override void Write(byte[] buffer, int offset, int count) {
			Memory.Write(buffer, offset, count);
		}
	}
}
