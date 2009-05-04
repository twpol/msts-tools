using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace JGR.IO
{
	public class BufferedInMemoryStream : Stream
	{
		private MemoryStream Memory;
		private Stream Incomming;
		private const int ChunkSize = 1024;

		public BufferedInMemoryStream(Stream stream) {
			Memory = new MemoryStream();
			Incomming = stream;
			ReadChunk(ChunkSize);
		}

		private void ReadChunk(int chunk) {
			var buffer = new byte[chunk];
			var bytes = Incomming.Read(buffer, 0, chunk);
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
			get { return false; }
		}

		public override void Flush() {
			throw new NotImplementedException();
		}

		public override long Length {
			get {
				if (Memory.Position + ChunkSize >= Memory.Length) ReadChunk(ChunkSize);
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
			throw new NotImplementedException();
		}
	}
}
