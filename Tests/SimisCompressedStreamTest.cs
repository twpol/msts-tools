//------------------------------------------------------------------------------
// Tests, part of MSTS Editors & Tools (http://jgrmsts.codeplex.com/).
// License: New BSD License (BSD).
//------------------------------------------------------------------------------

using System;
using System.Linq;
using System.IO;
using System.IO.Compression;
using Jgr.IO.Parser;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Tests {
	/// <summary>
	///This is a test class for SimisCompressedStreamTest and is intended
	///to contain all SimisCompressedStreamTest Unit Tests
	///</summary>
	[TestClass]
	public class SimisCompressedStreamTest {
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
		///A test for Position
		///</summary>
		[TestMethod]
		[ExpectedException(typeof(NotImplementedException))]
		public void PositionCompressTest() {
			var stream = new MemoryStream(new byte[1024], true);
			var target = new SimisCompressedStream(stream, CompressionMode.Compress, true);
			var position = target.Position;
		}

		/// <summary>
		///A test for Position
		///</summary>
		[TestMethod]
		[ExpectedException(typeof(NotImplementedException))]
		public void PositionDecompressTest() {
			var stream = new MemoryStream(SimisCompressedStream.Preamble, false);
			var target = new SimisCompressedStream(stream, CompressionMode.Decompress, true);
			var position = target.Position;
		}

		/// <summary>
		///A test for Position
		///</summary>
		[TestMethod]
		[ExpectedException(typeof(NotImplementedException))]
		public void PositionSetCompressTest() {
			var stream = new MemoryStream(new byte[1024], true);
			var target = new SimisCompressedStream(stream, CompressionMode.Compress, true);
			target.Position = 0;
		}

		/// <summary>
		///A test for Position
		///</summary>
		[TestMethod]
		[ExpectedException(typeof(NotImplementedException))]
		public void PositionSetDecompressTest() {
			var stream = new MemoryStream(SimisCompressedStream.Preamble, false);
			var target = new SimisCompressedStream(stream, CompressionMode.Decompress, true);
			target.Position = 0;
		}

		/// <summary>
		///A test for Length
		///</summary>
		[TestMethod]
		[ExpectedException(typeof(NotImplementedException))]
		public void LengthCompressTest() {
			var stream = new MemoryStream(new byte[1024], true);
			var target = new SimisCompressedStream(stream, CompressionMode.Compress, true);
			var length = target.Length;
		}

		/// <summary>
		///A test for Length
		///</summary>
		[TestMethod]
		[ExpectedException(typeof(NotImplementedException))]
		public void LengthDecompressTest() {
			var stream = new MemoryStream(SimisCompressedStream.Preamble, false);
			var target = new SimisCompressedStream(stream, CompressionMode.Decompress, true);
			var length = target.Length;
		}

		/// <summary>
		///A test for CanWrite
		///</summary>
		[TestMethod]
		public void CanWriteTest() {
			var stream = new MemoryStream(new byte[1024], true);
			var target = new SimisCompressedStream(stream, CompressionMode.Compress, true);
			Assert.AreEqual(true, target.CanWrite, "Must be able to write to a compressing stream.");
			stream = new MemoryStream(SimisCompressedStream.Preamble, false);
			target = new SimisCompressedStream(stream, CompressionMode.Decompress, true);
			Assert.AreEqual(false, target.CanWrite, "Must not be able to write to a decompressing stream.");
		}

		/// <summary>
		///A test for CanSeek
		///</summary>
		[TestMethod]
		public void CanSeekTest() {
			var stream = new MemoryStream(new byte[1024], true);
			var target = new SimisCompressedStream(stream, CompressionMode.Compress, true);
			Assert.AreEqual(false, target.CanSeek, "Should not be able to seek a compressed stream.");
			stream = new MemoryStream(SimisCompressedStream.Preamble, false);
			target = new SimisCompressedStream(stream, CompressionMode.Decompress, true);
			Assert.AreEqual(false, target.CanSeek, "Should not be able to seek a compressed stream.");
		}

		/// <summary>
		///A test for CanRead
		///</summary>
		[TestMethod]
		public void CanReadTest() {
			var stream = new MemoryStream(new byte[1024], true);
			var target = new SimisCompressedStream(stream, CompressionMode.Compress, true);
			Assert.AreEqual(false, target.CanRead, "Must not be able to read to a compressing stream.");
			stream = new MemoryStream(SimisCompressedStream.Preamble, false);
			target = new SimisCompressedStream(stream, CompressionMode.Decompress, true);
			Assert.AreEqual(true, target.CanRead, "Must be able to read to a decompressing stream.");
		}

		/// <summary>
		///A test for Write
		///</summary>
		[TestMethod]
		public void WriteTest() {
			var text = "Hello World!";
			var buffer = new byte[1024];
			var stream = new MemoryStream(buffer, true);
			var target = new SimisCompressedStream(stream, CompressionMode.Compress, true);
			target.Write(text.ToCharArray().Select(c => (byte)c).ToArray(), 0, text.Length);
			target.Close();
			Assert.IsTrue(stream.Length > SimisCompressedStream.Preamble.Length, "Must write out preamble and contents.");
			for (var i = 0; i < SimisCompressedStream.Preamble.Length; i++) {
				Assert.IsTrue((SimisCompressedStream.Preamble[i] == 0) || (SimisCompressedStream.Preamble[i] == buffer[i]), "Must write out correct preamble.");
			}
			Assert.AreEqual(12, buffer[8], "Must write out length in preamble.");
			Assert.AreEqual(0, buffer[9], "Must write out length in preamble.");
			Assert.AreEqual(0, buffer[10], "Must write out length in preamble.");
			Assert.AreEqual(0, buffer[11], "Must write out length in preamble.");
		}

		/// <summary>
		///A test for SetLength
		///</summary>
		[TestMethod]
		[ExpectedException(typeof(NotImplementedException))]
		public void SetLengthCompressTest() {
			var stream = new MemoryStream(new byte[1024], true);
			var target = new SimisCompressedStream(stream, CompressionMode.Compress, true);
			target.SetLength(0);
		}

		/// <summary>
		///A test for SetLength
		///</summary>
		[TestMethod]
		[ExpectedException(typeof(NotImplementedException))]
		public void SetLengthDecompressTest() {
			var stream = new MemoryStream(SimisCompressedStream.Preamble, false);
			var target = new SimisCompressedStream(stream, CompressionMode.Decompress, true);
			target.SetLength(0);
		}

		/// <summary>
		///A test for Seek
		///</summary>
		[TestMethod]
		[ExpectedException(typeof(NotImplementedException))]
		public void SeekCompressTest() {
			var stream = new MemoryStream(new byte[1024], true);
			var target = new SimisCompressedStream(stream, CompressionMode.Compress, true);
			target.Seek(0, SeekOrigin.Begin);
		}

		/// <summary>
		///A test for Seek
		///</summary>
		[TestMethod]
		[ExpectedException(typeof(NotImplementedException))]
		public void SeekDecompressTest() {
			var stream = new MemoryStream(SimisCompressedStream.Preamble, false);
			var target = new SimisCompressedStream(stream, CompressionMode.Decompress, true);
			target.Seek(0, SeekOrigin.Begin);
		}

		/// <summary>
		///A test for Read
		///</summary>
		[TestMethod]
		public void ReadTest() {
			var buffer = new byte[] {
				0x53, 0x49, 0x4D, 0x49, 0x53, 0x41, 0x40, 0x46, 0x0C, 0x00, 0x00, 0x00, 0x40, 0x40, 0x40, 0x40,
				0x78, 0x9C, 0xED, 0xBD, 0x07, 0x60, 0x1C, 0x49, 0x96, 0x25, 0x26, 0x2F, 0x6D, 0xCA, 0x7B, 0x7F,
				0x4A, 0xF5, 0x4A, 0xD7, 0xE0, 0x74, 0xA1, 0x08, 0x80, 0x60, 0x13, 0x24, 0xD8, 0x90, 0x40, 0x10,
				0xEC, 0xC1, 0x88, 0xCD, 0xE6, 0x92, 0xEC, 0x1D, 0x69, 0x47, 0x23, 0x29, 0xAB, 0x2A, 0x81, 0xCA,
				0x65, 0x56, 0x65, 0x5D, 0x66, 0x16, 0x40, 0xCC, 0xED, 0x9D, 0xBC, 0xF7, 0xDE, 0x7B, 0xEF, 0xBD,
				0xF7, 0xDE, 0x7B, 0xEF, 0xBD, 0xF7, 0xBA, 0x3B, 0x9D, 0x4E, 0x27, 0xF7, 0xDF, 0xFF, 0x3F, 0x5C,
				0x66, 0x64, 0x01, 0x6C, 0xF6, 0xCE, 0x4A, 0xDA, 0xC9, 0x9E, 0x21, 0x80, 0xAA, 0xC8, 0x1F, 0x3F,
				0x7E, 0x7C, 0x1F, 0x3F, 0x22, 0xBE, 0x9D, 0x97, 0x65, 0x95, 0x7E, 0xB7, 0xAA, 0xCB, 0xD9, 0xEF,
				0xFA, 0xFF, 0x00
			};
			var stream = new MemoryStream(buffer, false);
			var target = new SimisCompressedStream(stream, CompressionMode.Decompress, true);
			var output = new byte[32];
			var outputLength = target.Read(output, 0, output.Length);
			Assert.AreEqual(12, outputLength, "Must read only the data included.");
			Assert.AreEqual("Hello World!", new String(output.Take(outputLength).Select(b => (char)b).ToArray()), "Must read and decompress correctly.");
		}

		/// <summary>
		///A test for Flush
		///</summary>
		[TestMethod]
		public void FlushTest() {
			var stream = new MemoryStream(new byte[1024], true);
			var target = new SimisCompressedStream(stream, CompressionMode.Compress, true);
			target.Flush();
			Assert.Inconclusive("A method that does not return a value cannot be verified.");
		}

		/// <summary>
		///A test for SimisCompressedStream Constructor
		///</summary>
		[TestMethod]
		public void SimisCompressedStreamConstructorTest() {
			var stream = new MemoryStream(new byte[1024], true);
			var target = new SimisCompressedStream(stream, CompressionMode.Compress, true);
			stream = new MemoryStream(SimisCompressedStream.Preamble, false);
			target = new SimisCompressedStream(stream, CompressionMode.Decompress, true);
		}
	}
}
