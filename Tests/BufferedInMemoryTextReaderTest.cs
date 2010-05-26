//------------------------------------------------------------------------------
// Tests, part of MSTS Editors & Tools (http://jgrmsts.codeplex.com/).
// License: New BSD License (BSD).
//------------------------------------------------------------------------------

using System.IO;
using Jgr.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Tests {
	/// <summary>
	///This is a test class for BufferedInMemoryTextReaderTest and is intended
	///to contain all BufferedInMemoryTextReaderTest Unit Tests
	///</summary>
	[TestClass]
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
		public void ConstructorTest() {
			BufferedInMemoryTextReader target = new BufferedInMemoryTextReader(new StringReader("abcdef"));
		}

		/// <summary>
		///A test for Length
		///</summary>
		[TestMethod]
		public void LengthTest() {
			BufferedInMemoryTextReader target = new BufferedInMemoryTextReader(new StringReader("abcdef"));
			Assert.AreEqual(6, target.Length, "Must pre-buffer reader.");
		}

		/// <summary>
		///A test for Peek
		///</summary>
		[TestMethod]
		public void PeekTest() {
			BufferedInMemoryTextReader target = new BufferedInMemoryTextReader(new StringReader("abcdef"));
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
			BufferedInMemoryTextReader target = new BufferedInMemoryTextReader(new StringReader("abcdef"));
			Assert.AreEqual(0, target.Position, "Must start from the beginning.");
			target.Read();
			Assert.AreEqual(1, target.Position, "Must advance 1 charatcer.");
		}

		/// <summary>
		///A test for Read
		///</summary>
		[TestMethod]
		public void ReadTest1() {
			BufferedInMemoryTextReader target = new BufferedInMemoryTextReader(new StringReader("abcdef"));
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
			BufferedInMemoryTextReader target = new BufferedInMemoryTextReader(new StringReader("abcdef"));
			char[] buffer = new char[] { '1', '2', '3', '4', '5', '6', '7', '8', '9', '0' };
			int actual = target.Read(buffer, 0, 4);
			Assert.AreEqual('a', buffer[0]);
			Assert.AreEqual('b', buffer[1]);
			Assert.AreEqual('c', buffer[2]);
			Assert.AreEqual('d', buffer[3]);
			Assert.AreEqual('5', buffer[4]);
			Assert.AreEqual('6', buffer[5]);
		}

		/// <summary>
		///A test for ReadBlock
		///</summary>
		[TestMethod]
		public void ReadBlockTest() {
			BufferedInMemoryTextReader target = new BufferedInMemoryTextReader(new StringReader("abcdef"));
			char[] buffer = new char[] { '1', '2', '3', '4', '5', '6', '7', '8', '9', '0' };
			int actual = target.ReadBlock(buffer, 0, 4);
			Assert.AreEqual('a', buffer[0]);
			Assert.AreEqual('b', buffer[1]);
			Assert.AreEqual('c', buffer[2]);
			Assert.AreEqual('d', buffer[3]);
			Assert.AreEqual('5', buffer[4]);
			Assert.AreEqual('6', buffer[5]);
		}

		/// <summary>
		///A test for ReadLine
		///</summary>
		[TestMethod]
		public void ReadLineTest() {
			BufferedInMemoryTextReader target = new BufferedInMemoryTextReader(new StringReader("Hello 1\nWorld 1!\n"));
			Assert.AreEqual("Hello 1", target.ReadLine());
			Assert.AreEqual("World 1!", target.ReadLine());
			Assert.AreEqual(null, target.ReadLine());
			target = new BufferedInMemoryTextReader(new StringReader("Hello 2\r\nWorld 2!\r\n"));
			Assert.AreEqual("Hello 2", target.ReadLine());
			Assert.AreEqual("World 2!", target.ReadLine());
			Assert.AreEqual(null, target.ReadLine());
			// FIXME: Is this right? Shouldn't we get the last line here?
			target = new BufferedInMemoryTextReader(new StringReader("Hello 3\nWorld 3!"));
			Assert.AreEqual("Hello 3", target.ReadLine());
			Assert.AreEqual(null, target.ReadLine());
		}

		/// <summary>
		///A test for ReadToEnd
		///</summary>
		[TestMethod]
		public void ReadToEndTest() {
			BufferedInMemoryTextReader target = new BufferedInMemoryTextReader(new StringReader("Hello 1\nWorld 1!\n"));
			Assert.AreEqual("Hello 1", target.ReadLine());
			Assert.AreEqual("World 1!\n", target.ReadToEnd());
			target = new BufferedInMemoryTextReader(new StringReader("Hello 1\nWorld 1!"));
			Assert.AreEqual("Hello 1", target.ReadLine());
			Assert.AreEqual("World 1!", target.ReadToEnd());
		}
	}
}
