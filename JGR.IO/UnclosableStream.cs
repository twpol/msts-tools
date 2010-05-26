//------------------------------------------------------------------------------
// Jgr.IO library, part of MSTS Editors & Tools (http://jgrmsts.codeplex.com/).
// License: New BSD License (BSD).
//------------------------------------------------------------------------------

using System.IO;

namespace Jgr.IO
{
	/// <summary>
	/// A <see cref="Stream"/> which ignores calls to <see cref="Close"/> for stuations where a consumer of a stream assumes control which is undesired.
	/// </summary>
	/// <remarks>
	/// <para>All methods and properties pass straight through to the underlying <see cref="Stream"/>, with just one exception: <see cref="Close"/>. This
	/// method will do absolutely nothing. <see cref="UnclosableStream"/> should be used when a target accepting a <see cref="Stream"/> will close it but
	/// this isn't desired; a common example is a <c>Reader</c>, e.g. <see cref="BinaryReader"/>, which often close the underlying <see cref="Stream"/>
	/// when they are closed.</para>
	/// </remarks>
	public class UnclosableStream : Stream
	{
		Stream BaseStream;

		/// <summary>
		/// Initializes a new instance of the <see cref="UnclosableStream"/> class with a given underlying <see cref="Stream"/>.
		/// </summary>
		/// <param name="baseStream">The underlying <see cref="Stream"/> to wrap.</param>
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
