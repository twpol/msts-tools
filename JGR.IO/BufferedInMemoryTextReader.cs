//------------------------------------------------------------------------------
// Jgr.IO library, part of MSTS Editors & Tools (http://jgrmsts.codeplex.com/).
// License: Microsoft Public License (Ms-PL).
//------------------------------------------------------------------------------

using System;
using System.IO;
using System.Linq;

namespace Jgr.IO
{
	/// <summary>
	/// A <see cref="TextReader"/> which buffers all the data in memory (as a <see cref="String"/>).
	/// </summary>
	/// <remarks>
	/// <para>This class should behave in all ways like a normal <see cref="TextReader"/>, but with speed improvements in cases where the same data is read
	/// multiple times (e.g. calling <see cref="Peek"/> a lot without advancing the stream).</para>
	/// <para>An additional advantage is that reads to the underlying reader are done in large chunks (currently 1K), which can improve read speed for file
	/// accesses.</para>
	/// </remarks>
	public class BufferedInMemoryTextReader : TextReader
	{
		string Memory;
		long MemoryPosition;
		TextReader Incomming;
		const int ChunkSize = 1024;

		public BufferedInMemoryTextReader(TextReader reader) {
			Memory = "";
			Incomming = reader;
			ReadChunk(ChunkSize);
		}

		void ReadChunk(int chunk) {
			var buffer = new char[chunk];
			var length = Incomming.Read(buffer, 0, chunk);
			Memory += new String(buffer, 0, length);
		}

		public long Length {
			get {
				if (MemoryPosition + ChunkSize >= Memory.Length) ReadChunk(ChunkSize);
				return Memory.Length;
			}
		}

		public long Position {
			get {
				return MemoryPosition;
			}
			set {
				MemoryPosition = value;
			}
		}

		public override int Peek() {
			if (MemoryPosition >= Memory.Length) return -1;
			return Memory[(int)MemoryPosition];
		}

		public override int Read() {
			if (MemoryPosition >= Memory.Length) return -1;
			return Memory[(int)MemoryPosition++];
		}

		public override int Read(char[] buffer, int index, int count) {
			if (buffer == null) throw new ArgumentNullException("buffer", "Buffer cannot be null.");
			if (buffer.Length < index + count) throw new ArgumentException("Offset and length were out of bounds for the array or count is greater than the number of elements from index to the end of the source collection.");
			if (index < 0) throw new ArgumentOutOfRangeException("index", "Non-negative number required.");
			if (count < 0) throw new ArgumentOutOfRangeException("count", "Non-negative number required.");

			if (MemoryPosition + count > Memory.Length) ReadChunk(count);
			if (MemoryPosition + count > Memory.Length) count = Memory.Length - (int)MemoryPosition;

			if (count <= 0) return 0;
			for (var i = 0; i < count; i++) {
				buffer[index + i] = Memory[(int)MemoryPosition + i];
			}
			MemoryPosition += count;
			return count;
		}

		public override int ReadBlock(char[] buffer, int index, int count) {
			return Read(buffer, index, count);
		}

		public override string ReadLine() {
			var poses = new int[] {};
			while (!poses.Any(p => p >= 0)) {
				ReadChunk(ChunkSize);
				var posR = Memory.IndexOf('\r', (int)MemoryPosition);
				var posN = Memory.IndexOf('\n', (int)MemoryPosition);
				var posNL = Memory.IndexOf(Environment.NewLine, (int)MemoryPosition, StringComparison.Ordinal);
				poses = new int[] { posR, posN, posNL };
				// If the incomming TextReader has run out, we have to bail no matter what.
				if (Incomming.Peek() == -1) break;
			}
			if (poses.Any(p => p >= 0)) {
				var first = poses.Where(p => p >= 0).OrderBy(p => p).First();
				var rv = Memory.Substring((int)MemoryPosition, first - (int)MemoryPosition);
				if ((first + Environment.NewLine.Length <= Memory.Length) && (Memory.Substring(first, Environment.NewLine.Length) == Environment.NewLine)) {
					MemoryPosition = first + Environment.NewLine.Length;
					return rv;
				}
				if (Memory[first] == '\n') {
					MemoryPosition = first + 1;
					return rv;
				}
				if (Memory[first] == '\r') {
					MemoryPosition = first + 1;
					if ((MemoryPosition + 1 < Memory.Length) && (Memory[(int)MemoryPosition + 1] == '\n')) {
						MemoryPosition++;
					}
					return rv;
				}
				throw new InvalidOperationException("We should never get here, WAAA!");
			}
			return null;
		}

		public override string ReadToEnd() {
			while (true) {
				ReadChunk(ChunkSize);
				// If the incomming TextReader has run out, we're done reading.
				if (Incomming.Peek() == -1) break;
			}
			var rv = Memory.Substring((int)MemoryPosition);
			MemoryPosition += rv.Length;
			return rv;
		}
	}
}
