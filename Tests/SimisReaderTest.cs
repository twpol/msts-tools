using System;
using System.IO;
using Jgr;
using Jgr.IO;
using Jgr.IO.Parser;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Tests {
	/// <summary>
	/// Summary description for SimisReaderTest
	/// </summary>
	[TestClass]
	public class SimisReaderTest {
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

		private SimisProvider Provider;

		[TestInitialize]
		public void TestInitialize() {
			var resourcesDirectory = TestContext.TestDir + @"\..\..\Resources";
			Provider = new SimisProvider(resourcesDirectory);
			try {
				Provider.Join();
			} catch (FileException ex) {
				Console.WriteLine(ex.ToString());
				return;
			}
		}

		/// <summary>
		///A test for SimisReader on real files
		///</summary>
		[DeploymentItem("Tests\\MSTS files.csv"), DataSource("Microsoft.VisualStudio.TestTools.DataSource.CSV", "|DataDirectory|\\MSTS files.csv", "MSTS files#csv", DataAccessMethod.Sequential), TestMethod]
		public void TestRealFiles() {
			var file = (string)TestContext.DataRow["Filename"];
			var extension = Path.GetExtension(file).ToUpperInvariant();
			var success = true;
			var newFile = new SimisFile(file, Provider);
			newFile.SimisFormat = Provider.GetForPath(file);
			Stream readStream = new BufferedInMemoryStream(File.OpenRead(file));
			Stream saveStream = new MemoryStream();

			// First, read the file in.
			if (success) {
				try {
					try {
						newFile.ReadStream(readStream);
					} catch (Exception e) {
						throw new FileException(file, e);
					}
				} catch (FileException ex) {
					Assert.Fail("Read: " + ex + "\n");
				}
			}
		}
	}
}
