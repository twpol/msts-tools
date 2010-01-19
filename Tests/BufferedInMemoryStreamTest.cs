//------------------------------------------------------------------------------
// Tests, part of MSTS Editors & Tools (http://jgrmsts.codeplex.com/).
// License: Microsoft Public License (Ms-PL).
//------------------------------------------------------------------------------

using System;
using System.IO;
using Jgr.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Tests {
	/// <summary>
	///This is a test class for BufferedInMemoryStreamTest and is intended
	///to contain all BufferedInMemoryStreamTest Unit Tests
	///</summary>
	[TestClass]
	public class BufferedInMemoryStreamTest {
		private TestContext testContextInstance;

		/// <summary>
		///Gets or sets the test context which provides
		///information about and functionality for the current test run.
		///</summary>
		public TestContext TestContext {
			get {
				return testContextInstance;
			}
			set {
				testContextInstance = value;
			}
		}

		/// <summary>
		///A test for BufferedInMemoryStream Constructor
		///</summary>
		[TestMethod]
		public void _ctorTest() {
			Stream stream = new MemoryStream(new byte[1024]);
			BufferedInMemoryStream target = new BufferedInMemoryStream(stream);
			Assert.AreEqual(0, stream.Position, "Base stream must not be read from.");
			Assert.AreEqual(0, target.Position, "Must start from the beginning.");
		}

		/// <summary>
		///A test for CanRead
		///</summary>
		[TestMethod]
		public void CanReadTest() {
			Stream stream = new MemoryStream(new byte[1024]);
			BufferedInMemoryStream target = new BufferedInMemoryStream(stream);
			Assert.AreEqual(true, target.CanRead);
		}

		/// <summary>
		///A test for CanSeek
		///</summary>
		[TestMethod]
		public void CanSeekTest() {
			Stream stream = new MemoryStream(new byte[1024]);
			BufferedInMemoryStream target = new BufferedInMemoryStream(stream);
			Assert.AreEqual(true, target.CanSeek);
		}

		/// <summary>
		///A test for CanWrite
		///</summary>
		[TestMethod]
		public void CanWriteTest() {
			Stream stream = new MemoryStream(new byte[1024]);
			BufferedInMemoryStream target = new BufferedInMemoryStream(stream);
			Assert.AreEqual(true, target.CanWrite);
		}

		/// <summary>
		///A test for Close
		///</summary>
		[TestMethod]
		public void CloseTest() {
			Stream stream = new MemoryStream(new byte[1024]);
			BufferedInMemoryStream target = new BufferedInMemoryStream(stream);
			target.Close();
			Assert.Inconclusive("A method that does not return a value cannot be verified.");
		}

		/// <summary>
		///A test for Length
		///</summary>
		[TestMethod]
		public void LengthTest() {
			Stream stream = new MemoryStream(new byte[1024]);
			BufferedInMemoryStream target = new BufferedInMemoryStream(stream);
			Assert.AreEqual(1024, target.Length, "Should exposed length of seekable base stream.");
		}

		/// <summary>
		///A test for Flush
		///</summary>
		[TestMethod]
		public void FlushTest() {
			Stream stream = new MemoryStream(new byte[1024]);
			BufferedInMemoryStream target = new BufferedInMemoryStream(stream);
			target.WriteByte(0);
			target.Flush();
			Assert.AreEqual(0, stream.Position, "Must not advance base stream.");
		}

		/// <summary>
		///A test for Position
		///</summary>
		[TestMethod]
		public void PositionTest() {
			Stream stream = new MemoryStream(new byte[1024]);
			BufferedInMemoryStream target = new BufferedInMemoryStream(stream);
			target.WriteByte(0);
			Assert.AreEqual(1, target.Position, "Stream must advance for writes.");
		}

		/// <summary>
		///A test for Read
		///</summary>
		[TestMethod]
		public void ReadTest() {
			Stream stream = new MemoryStream(new byte[1024]);
			BufferedInMemoryStream target = new BufferedInMemoryStream(stream);
			byte[] buffer = new byte[10];
			int offset = 0;
			int count = 10;
			int actual = target.Read(buffer, offset, count);
			Assert.AreNotEqual(0, stream.Position, "Base stream must have advanced.");
			Assert.AreEqual(count, actual, "Must read 'count' bytes.");
			Assert.AreEqual(count, target.Position, "Must advanced 'count' bytes.");
			// FIXME: Verify 'buffer' has correct contents.
		}

		/// <summary>
		///A test for RealFlush
		///</summary>
		[TestMethod]
		public void RealFlushTest() {
			Stream stream = new MemoryStream(new byte[1024]);
			BufferedInMemoryStream target = new BufferedInMemoryStream(stream);
			target.WriteByte(1);
			target.RealFlush();
			Assert.AreNotEqual(0, stream.Position, "Must advanced base stream on ReadFlush() after Write().");
			// FIXME: Verify 'stream' has correct contents.
		}

		/// <summary>
		///A test for SetLength
		///</summary>
		[TestMethod]
		public void SetLengthTest() {
			Stream stream = new MemoryStream(new byte[1024]);
			BufferedInMemoryStream target = new BufferedInMemoryStream(stream);
			try {
				target.SetLength(0);
				Assert.Fail("Should have thrown NotImplementedException");
			} catch (NotImplementedException) {
			}
		}

		/// <summary>
		///A test for Seek
		///</summary>
		[TestMethod]
		public void SeekTest() {
			Stream stream = new MemoryStream(new byte[1024]);
			BufferedInMemoryStream target = new BufferedInMemoryStream(stream);
			long offset = 10;
			try {
				long actual = target.Seek(offset, SeekOrigin.Begin);
				Assert.Fail("Should have thrown ArgumentOutOfRangeException");
			} catch (ArgumentOutOfRangeException) {
			}
			// Causes base stream to be read.
			target.ReadByte();
			Assert.AreNotEqual(0, stream.Position, "Must advanced base stream.");
			{
				long actual = target.Seek(offset, SeekOrigin.Begin);
				Assert.AreEqual(offset, actual, "Must seek stream.");
			}
		}

		/// <summary>
		///A test for Write
		///</summary>
		[TestMethod]
		public void WriteTest() {
			Stream stream = new MemoryStream(new byte[1024]);
			BufferedInMemoryStream target = new BufferedInMemoryStream(stream); // TODO: Initialize to an appropriate value
			byte[] buffer = null; // TODO: Initialize to an appropriate value
			int offset = 0; // TODO: Initialize to an appropriate value
			int count = 0; // TODO: Initialize to an appropriate value
			target.Write(buffer, offset, count);
			Assert.Inconclusive("A method that does not return a value cannot be verified.");
		}
	}
}
