using System.IO;
using Jgr.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Tests {
	/// <summary>
	///This is a test class for BufferedInMemoryTextReaderTest and is intended
	///to contain all BufferedInMemoryTextReaderTest Unit Tests
	///</summary>
	[TestClass()]
	public class BufferedInMemoryTextReaderTest {
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
		///A test for BufferedInMemoryTextReader Constructor
		///</summary>
		[TestMethod]
		public void _ctorTest() {
			TextReader reader = new StringReader("abcdef");
			BufferedInMemoryTextReader target = new BufferedInMemoryTextReader(reader);
		}

		/// <summary>
		///A test for Length
		///</summary>
		[TestMethod]
		public void LengthTest() {
			TextReader reader = new StringReader("abcdef");
			BufferedInMemoryTextReader target = new BufferedInMemoryTextReader(reader);
			Assert.AreEqual(6, target.Length, "Must pre-buffer reader.");
		}

		/// <summary>
		///A test for Peek
		///</summary>
		[TestMethod]
		public void PeekTest() {
			TextReader reader = new StringReader("abcdef");
			BufferedInMemoryTextReader target = new BufferedInMemoryTextReader(reader);
			Assert.AreEqual('a', target.Peek());
			Assert.AreEqual('a', target.Peek());
			Assert.AreEqual('a', target.Read());
			Assert.AreEqual('b', target.Peek());
			Assert.AreEqual('b', target.Peek());
		}

		/// <summary>
		///A test for Position
		///</summary>
		[TestMethod]
		public void PositionTest() {
			TextReader reader = new StringReader("abcdef");
			BufferedInMemoryTextReader target = new BufferedInMemoryTextReader(reader);
			Assert.AreEqual(0, target.Position, "Must start from the beginning.");
			target.Read();
			Assert.AreEqual(1, target.Position, "Must advance 1 charatcer.");
		}

		/// <summary>
		///A test for Read
		///</summary>
		[TestMethod]
		public void ReadTest1() {
			TextReader reader = new StringReader("abcdef");
			BufferedInMemoryTextReader target = new BufferedInMemoryTextReader(reader);
			Assert.AreEqual('a', target.Read());
			Assert.AreEqual('b', target.Read());
			Assert.AreEqual('c', target.Read());
			Assert.AreEqual('d', target.Read());
			Assert.AreEqual('e', target.Read());
			Assert.AreEqual('f', target.Read());
			Assert.AreEqual(-1, target.Read());
			Assert.AreEqual(-1, target.Read());
		}

		/// <summary>
		///A test for Read
		///</summary>
		[TestMethod]
		public void ReadTest() {
			TextReader reader = null; // TODO: Initialize to an appropriate value
			BufferedInMemoryTextReader target = new BufferedInMemoryTextReader(reader); // TODO: Initialize to an appropriate value
			char[] buffer = null; // TODO: Initialize to an appropriate value
			int index = 0; // TODO: Initialize to an appropriate value
			int count = 0; // TODO: Initialize to an appropriate value
			int expected = 0; // TODO: Initialize to an appropriate value
			int actual;
			actual = target.Read(buffer, index, count);
			Assert.AreEqual(expected, actual);
			Assert.Inconclusive("Verify the correctness of this test method.");
		}

		/// <summary>
		///A test for ReadBlock
		///</summary>
		[TestMethod]
		public void ReadBlockTest() {
			TextReader reader = null; // TODO: Initialize to an appropriate value
			BufferedInMemoryTextReader target = new BufferedInMemoryTextReader(reader); // TODO: Initialize to an appropriate value
			char[] buffer = null; // TODO: Initialize to an appropriate value
			int index = 0; // TODO: Initialize to an appropriate value
			int count = 0; // TODO: Initialize to an appropriate value
			int expected = 0; // TODO: Initialize to an appropriate value
			int actual;
			actual = target.ReadBlock(buffer, index, count);
			Assert.AreEqual(expected, actual);
			Assert.Inconclusive("Verify the correctness of this test method.");
		}

		/// <summary>
		///A test for ReadLine
		///</summary>
		[TestMethod]
		public void ReadLineTest() {
			TextReader reader = new StringReader("Hello 1\nWorld 1!\n");
			BufferedInMemoryTextReader target = new BufferedInMemoryTextReader(reader);
			Assert.AreEqual("Hello 1", target.ReadLine());
			Assert.AreEqual("World 1!", target.ReadLine());
			Assert.AreEqual(null, target.ReadLine());
			reader = new StringReader("Hello 2\r\nWorld 2!\r\n");
			target = new BufferedInMemoryTextReader(reader);
			Assert.AreEqual("Hello 2", target.ReadLine());
			Assert.AreEqual("World 2!", target.ReadLine());
			Assert.AreEqual(null, target.ReadLine());
			// FIXME: Is this right? Shouldn't we get the last line here?
			reader = new StringReader("Hello 3\nWorld 3!");
			target = new BufferedInMemoryTextReader(reader);
			Assert.AreEqual("Hello 3", target.ReadLine());
			Assert.AreEqual(null, target.ReadLine());
		}

		/// <summary>
		///A test for ReadToEnd
		///</summary>
		[TestMethod]
		public void ReadToEndTest() {
			TextReader reader = new StringReader("Hello 1\nWorld 1!\n");
			BufferedInMemoryTextReader target = new BufferedInMemoryTextReader(reader);
			Assert.AreEqual("Hello 1", target.ReadLine());
			Assert.AreEqual("World 1!\n", target.ReadToEnd());
			reader = new StringReader("Hello 1\nWorld 1!");
			target = new BufferedInMemoryTextReader(reader);
			Assert.AreEqual("Hello 1", target.ReadLine());
			Assert.AreEqual("World 1!", target.ReadToEnd());
		}
	}
}
