//------------------------------------------------------------------------------
// Jgr.IO.Parser library, part of MSTS Editors & Tools (http://jgrmsts.codeplex.com/).
// License: New BSD License (BSD).
//------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace Jgr.IO.Parser {
	public class SimisWriter : IDisposable {
		public bool StreamIsBinary { get { return Writer.StreamIsBinary; } }
		public bool StreamIsCompressed { get { return Writer.StreamIsCompressed; } }

		protected readonly SimisStreamWriter Writer;

		protected SimisWriter(SimisStreamWriter writer) {
			Writer = writer;
		}

		//public static SimisAceWriter ToAceStream(Stream stream, bool streamIsBinary, bool streamIsCompressed) {
		//    if (!stream.CanWrite) throw new ArgumentException("Stream must support writing.", "stream");
		//    if (!stream.CanSeek) throw new ArgumentException("Stream must support seeking.", "stream");
		//    throw new NotImplementedException();
		//}

		public static SimisJinxWriter ToJinxStream(Stream stream, bool streamIsBinary, bool streamIsCompressed, SimisProvider simisProvider, bool jinxStreamIsBinary, SimisJinxFormat jinxStreamFormat) {
			if (!stream.CanWrite) throw new ArgumentException("Stream must support writing.", "stream");
			if (!stream.CanSeek) throw new ArgumentException("Stream must support seeking.", "stream");
			return new SimisJinxWriter(SimisStreamWriter.ToStream(stream, streamIsBinary, streamIsCompressed), simisProvider, jinxStreamIsBinary, jinxStreamFormat);
		}

		~SimisWriter() {
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
				Writer.Close();
			}
		}
	}
}
