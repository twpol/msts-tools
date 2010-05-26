//------------------------------------------------------------------------------
// Tests, part of MSTS Editors & Tools (http://jgrmsts.codeplex.com/).
// License: New BSD License (BSD).
//------------------------------------------------------------------------------

using Jgr.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Tests {
	/// <summary>
	///This is a test class for ByteEncodingTest and is intended
	///to contain all ByteEncodingTest Unit Tests
	///</summary>
	[TestClass]
	public class ByteEncodingTest {
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
		///A test for ByteEncoding Constructor
		///</summary>
		[TestMethod]
		public void ConstructorTest() {
			ByteEncoding target = new ByteEncoding();
		}

		/// <summary>
		///A test for GetByteCount
		///</summary>
		[TestMethod]
		public void GetByteCountTest() {
			ByteEncoding target = new ByteEncoding();
			var chars = "abcdef".ToCharArray();
			Assert.AreEqual(chars.Length, target.GetByteCount(chars, 0, chars.Length));
		}

		/// <summary>
		///A test for GetBytes
		///</summary>
		[TestMethod]
		public void GetBytesTest() {
			ByteEncoding target = new ByteEncoding();
			var chars = "abcdef".ToCharArray();
			var actual = new byte[chars.Length];
			Assert.AreEqual(chars.Length, target.GetBytes(chars, 0, chars.Length, actual, 0), "Must return same number of bytes as characters.");
			for (var i = 0; i < chars.Length; i++) {
				Assert.AreEqual((int)chars[i], actual[i], "Index " + i + " must match.");
			}
		}

		/// <summary>
		///A test for GetCharCount
		///</summary>
		[TestMethod]
		public void GetCharCountTest() {
			ByteEncoding target = new ByteEncoding();
			var bytes = new byte[] { 97, 98, 99, 100, 101, 102 };
			Assert.AreEqual(bytes.Length, target.GetCharCount(bytes, 0, bytes.Length));
		}

		/// <summary>
		///A test for GetChars
		///</summary>
		[TestMethod]
		public void GetCharsTest() {
			ByteEncoding target = new ByteEncoding();
			var bytes = new byte[] { 97, 98, 99, 100, 101, 102 };
			var actual = new char[bytes.Length];
			Assert.AreEqual(bytes.Length, target.GetChars(bytes, 0, bytes.Length, actual, 0), "Must return same number of characters as bytes.");
			for (var i = 0; i < bytes.Length; i++) {
				Assert.AreEqual((char)bytes[i], actual[i], "Index " + i + " must match.");
			}
		}

		/// <summary>
		///A test for GetMaxByteCount
		///</summary>
		[TestMethod]
		public void GetMaxByteCountTest() {
			ByteEncoding target = new ByteEncoding();
			Assert.AreEqual(10, target.GetMaxByteCount(10));
		}

		/// <summary>
		///A test for GetMaxCharCount
		///</summary>
		[TestMethod]
		public void GetMaxCharCountTest() {
			ByteEncoding target = new ByteEncoding();
			Assert.AreEqual(10, target.GetMaxCharCount(10));
		}
	}
}
