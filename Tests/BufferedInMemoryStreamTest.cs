//------------------------------------------------------------------------------
// Tests, part of MSTS Editors & Tools (http://jgrmsts.codeplex.com/).
// License: New BSD License (BSD).
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
		public void ConstructorTest() {
			var stream = new MemoryStream(new byte[1024]);
			var target = new BufferedInMemoryStream(stream);
			Assert.AreEqual(0, stream.Position, "Must not read from base stream.");
			Assert.AreEqual(0, target.Position, "Must start from the beginning.");
		}

		/// <summary>
		///A test for CanRead
		///</summary>
		[TestMethod]
		public void CanReadTest() {
			var stream = new MemoryStream(new byte[1024]);
			var target = new BufferedInMemoryStream(stream);
			Assert.AreEqual(true, target.CanRead);
		}

		/// <summary>
		///A test for CanSeek
		///</summary>
		[TestMethod]
		public void CanSeekTest() {
			var stream = new MemoryStream(new byte[1024]);
			var target = new BufferedInMemoryStream(stream);
			Assert.AreEqual(true, target.CanSeek);
		}

		/// <summary>
		///A test for CanWrite
		///</summary>
		[TestMethod]
		public void CanWriteTest() {
			var stream = new MemoryStream(new byte[1024]);
			var target = new BufferedInMemoryStream(stream);
			Assert.AreEqual(true, target.CanWrite);
		}

		/// <summary>
		///A test for Close
		///</summary>
		[TestMethod]
		public void CloseTest() {
			var stream = new MemoryStream(new byte[1024]);
			var target = new BufferedInMemoryStream(stream);
			var i = stream.Position;
			target.Close();
			try {
				i = stream.Position;
				Assert.Fail("Must close base stream.");
			} catch (ObjectDisposedException) {
			}
		}

		/// <summary>
		///A test for Length
		///</summary>
		[TestMethod]
		public void LengthTest() {
			var stream = new MemoryStream(new byte[1024]);
			var target = new BufferedInMemoryStream(stream);
			Assert.AreEqual(1024, target.Length, "Must exposed length of seekable base stream.");
		}

		/// <summary>
		///A test for Flush
		///</summary>
		[TestMethod]
		public void FlushTest() {
			var stream = new MemoryStream(new byte[1024]);
			var target = new BufferedInMemoryStream(stream);
			target.WriteByte(0);
			target.Flush();
			Assert.AreEqual(0, stream.Position, "Must not advance base stream.");
		}

		/// <summary>
		///A test for Position
		///</summary>
		[TestMethod]
		public void PositionTest() {
			var stream = new MemoryStream(new byte[1024]);
			var target = new BufferedInMemoryStream(stream);
			target.WriteByte(0);
			Assert.AreEqual(1, target.Position, "Must advance stream for writes.");
		}

		/// <summary>
		///A test for Read
		///</summary>
		[TestMethod]
		public void ReadTest() {
			var stream = new MemoryStream(new byte[1024]);
			var target = new BufferedInMemoryStream(stream);
			var buffer = new byte[10];
			var offset = 0;
			var count = 10;
			var actual = target.Read(buffer, offset, count);
			Assert.AreNotEqual(0, stream.Position, "Base stream must have advanced.");
			Assert.AreEqual(count, actual, "Must read 'count' bytes.");
			Assert.AreEqual(count, target.Position, "Must advanced 'count' bytes.");
			Assert.Inconclusive("Verify 'buffer' has correct contents.");
		}

		/// <summary>
		///A test for RealFlush
		///</summary>
		[TestMethod]
		public void RealFlushTest() {
			var stream = new MemoryStream(new byte[1024]);
			var target = new BufferedInMemoryStream(stream);
			target.WriteByte(1);
			target.RealFlush();
			Assert.AreNotEqual(0, stream.Position, "Must advanced base stream on ReadFlush() after Write().");
			Assert.Inconclusive("Verify 'stream' has correct contents.");
		}

		/// <summary>
		///A test for SetLength
		///</summary>
		[TestMethod]
		public void SetLengthTest() {
			var stream = new MemoryStream(new byte[1024]);
			var target = new BufferedInMemoryStream(stream);
			try {
				target.SetLength(0);
				Assert.Fail("Must throw NotImplementedException.");
			} catch (NotImplementedException) {
			}
		}

		/// <summary>
		///A test for Seek
		///</summary>
		[TestMethod]
		public void SeekTest() {
			var stream = new MemoryStream(new byte[1024]);
			var target = new BufferedInMemoryStream(stream);
			var offset = 10;
			target.Seek(offset, SeekOrigin.Begin);
			Assert.AreEqual(0, stream.Position, "Must not advanced base stream.");
			target.ReadByte();
			Assert.AreNotEqual(0, stream.Position, "Must advanced base stream.");
			{
				var actual = target.Seek(offset, SeekOrigin.Begin);
				Assert.AreEqual(offset, actual, "Must seek stream.");
			}
		}

		/// <summary>
		///A test for Write
		///</summary>
		[TestMethod]
		public void WriteTest() {
			var stream = new MemoryStream(new byte[1024]);
			var target = new BufferedInMemoryStream(stream); // TODO: Initialize to an appropriate value
			var buffer = new byte[0]; // TODO: Initialize to an appropriate value
			var offset = 0; // TODO: Initialize to an appropriate value
			var count = 0; // TODO: Initialize to an appropriate value
			target.Write(buffer, offset, count);
			Assert.Inconclusive("A method that does not return a value cannot be verified.");
		}
	}
}
