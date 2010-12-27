//------------------------------------------------------------------------------
// Jgr.IO.Parser library, part of MSTS Editors & Tools (http://jgrmsts.codeplex.com/).
// License: New BSD License (BSD).
//------------------------------------------------------------------------------

using System;
using System.IO;

namespace Jgr.IO.Parser {
	[Immutable]
	public class SimisReader : IDisposable {
		public bool StreamIsBinary { get { return Reader.StreamIsBinary; } }
		public bool StreamIsCompressed { get { return Reader.StreamIsCompressed; } }

		protected readonly SimisStreamReader Reader;

		protected SimisReader(SimisStreamReader reader) {
			Reader = reader;
		}

		public static SimisReader FromStream(Stream stream, SimisProvider simisProvider) {
			return FromStream(stream, simisProvider, null);
		}

		public static SimisReader FromStream(Stream stream, SimisProvider simisProvider, SimisJinxFormat jinxStreamFormat) {
			if (!stream.CanRead) throw new ArgumentException("Stream must support reading.", "stream");
			if (!stream.CanSeek) throw new ArgumentException("Stream must support seeking.", "stream");

			var reader = SimisStreamReader.FromStream(stream);
			var position = reader.BaseStream.Position;
			var signature = new String(reader.ReadChars(4));
			switch (signature) {
				case "JINX":
					return new SimisJinxReader(reader, simisProvider, jinxStreamFormat);
				case "\x01\x00\x00\x00":
					return new SimisAceReader(reader);
				default:
					throw new ReaderException(reader, reader.StreamIsBinary, (int)(reader.BaseStream.Position - position), "Signature '" + signature + "' is invalid.");
			}
		}

		~SimisReader() {
			Dispose(false);
		}

		#region IDisposable Members

		public void Dispose() {
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		#endregion

		protected virtual void Dispose(bool disposing) {
			if (disposing) {
				Reader.Close();
			}
		}

		#region PinReader code

		long PinReaderPosition;
		protected void PinReader() {
			PinReaderPosition = Reader.BaseStream.Position;
		}

		protected int PinReaderChanged() {
			return (int)(Reader.BaseStream.Position - PinReaderPosition);
		}

		protected void PinReaderReset() {
			Reader.BaseStream.Position = PinReaderPosition;
		}

		#endregion
	}
}
