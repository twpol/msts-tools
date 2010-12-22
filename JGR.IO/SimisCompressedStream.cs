//------------------------------------------------------------------------------
// Jgr.IO library, part of MSTS Editors & Tools (http://jgrmsts.codeplex.com/).
// License: New BSD License (BSD).
//------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.IO.Compression;

namespace Jgr.IO.Parser {
	public class SimisCompressedStream : Stream {
		public static TraceSwitch TraceSwitch = new TraceSwitch("jgr.io.simiscompressedstream", "Trace SimisCompressedStream");

		// 00000000 53 49 4D 49 53 41 40 46 .. .. .. .. 40 40 40 40 SIMISA@F....@@@@
		// 00000010 78 9C                                           x?
		public static readonly byte[] Preamble = new byte[] { 0x53, 0x49, 0x4D, 0x49, 0x53, 0x41, 0x40, 0x46, 0, 0, 0, 0, 0x40, 0x40, 0x40, 0x40, 0x78, 0x9C };

		readonly Stream BaseStream;
		readonly long BasePosition;
		readonly CompressionMode Mode;
		readonly bool LeaveOpen;
		readonly DeflateStream Stream;
		long StreamLength;

		public SimisCompressedStream(Stream stream, CompressionMode mode, bool leaveOpen) {
			if (stream == null) throw new ArgumentNullException("stream");
			if ((mode != CompressionMode.Compress) && (mode != CompressionMode.Decompress)) throw new ArgumentException("Invalid CompressionMode.", "mode");
			if ((mode == CompressionMode.Compress) && !stream.CanSeek) throw new ArgumentException("Cannot compress when CanSeek is false.");
			if ((mode == CompressionMode.Compress) && !stream.CanWrite) throw new ArgumentException("Cannot compress when CanWrite is false.");
			if ((mode == CompressionMode.Decompress) && !stream.CanRead) throw new ArgumentException("Cannot decompress when CanRead is false.");

			BaseStream = stream;
			BasePosition = stream.Position;
			Mode = mode;
			LeaveOpen = leaveOpen;

			if (mode == CompressionMode.Compress) {
				BaseStream.Write(Preamble, 0, Preamble.Length);
				StreamLength = 0;
			} else {
				var preamble = new byte[Preamble.Length];
				BaseStream.Read(preamble, 0, Preamble.Length);
				for (var i = 0; i < Preamble.Length; i++) {
					if ((Preamble[i] != 0) && (Preamble[i] != preamble[i])) throw new InvalidDataException("Preamble at byte " + i + ", expected " + Preamble[i].ToString("X2") + "; got " + preamble[i].ToString("X2") + ".");
				}
				StreamLength = preamble[8] + preamble[9] << 8 + preamble[10] << 16 + preamble[11] << 24;
			}

			Stream = new DeflateStream(BaseStream, mode, true);
		}

		protected override void Dispose(bool disposing) {
			if (disposing) {
				Stream.Close();
				if (Mode == CompressionMode.Compress) {
					var position = BaseStream.Position;
					BaseStream.Seek(BasePosition + 8, SeekOrigin.Begin);
					BaseStream.Write(new[] { (byte)StreamLength, (byte)(StreamLength >> 8), (byte)(StreamLength >> 16), (byte)(StreamLength >> 24) }, 0, 4);
					BaseStream.Position = position;
				}
				if (!LeaveOpen) {
					BaseStream.Close();
				}
			}
			base.Dispose(disposing);
		}

		#region Stream implementation

		public override bool CanRead {
			get {
				return Mode == CompressionMode.Decompress;
			}
		}

		public override bool CanSeek {
			get {
				return false;
			}
		}

		public override bool CanWrite {
			get {
				return Mode == CompressionMode.Compress;
			}
		}

		public override void Flush() {
			Stream.Flush();
		}

		public override long Length {
			get { throw new NotImplementedException(); }
		}

		public override long Position {
			get {
				throw new NotImplementedException();
			}
			set {
				throw new NotImplementedException();
			}
		}

		public override int Read(byte[] buffer, int offset, int count) {
			return Stream.Read(buffer, offset, count);
		}

		public override long Seek(long offset, SeekOrigin origin) {
			throw new NotImplementedException();
		}

		public override void SetLength(long value) {
			throw new NotImplementedException();
		}

		public override void Write(byte[] buffer, int offset, int count) {
			Stream.Write(buffer, offset, count);
			StreamLength += count;
		}

		#endregion
	}
}
