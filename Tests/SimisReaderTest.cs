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

		/// <summary>
		///A test for SimisReader on real files
		///</summary>
		[DeploymentItem("Tests\\MSTS files.csv"), DataSource("Microsoft.VisualStudio.TestTools.DataSource.CSV", "|DataDirectory|\\MSTS files.csv", "MSTS files#csv", DataAccessMethod.Sequential), TestMethod]
		public void TestRealFiles() {
			Assert.Fail((string)TestContext.DataRow["Filename"]);
		}
	}
}
