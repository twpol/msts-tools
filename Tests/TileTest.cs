//------------------------------------------------------------------------------
// Tests, part of MSTS Editors & Tools (http://jgrmsts.codeplex.com/).
// License: Microsoft Public License (Ms-PL).
//------------------------------------------------------------------------------

using Jgr.Msts;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Tests {
	/// <summary>
	///This is a test class for TileTest and is intended
	///to contain all TileTest Unit Tests
	///</summary>
	[TestClass]
	public class TileTest {
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
		///A test for Convert
		///</summary>
		[DataSource("Microsoft.VisualStudio.TestTools.DataSource.CSV", "|DataDirectory|\\Tiles.csv", "Tiles#csv", DataAccessMethod.Sequential), DeploymentItem("Tests\\Tiles.csv"), TestMethod]
		public void ConvertTileToMSTSIGHTest() {
			var tileName = testContextInstance.DataRow["Tile"].ToString();
			var line = int.Parse(testContextInstance.DataRow["Line"].ToString());
			var sample = int.Parse(testContextInstance.DataRow["Sample"].ToString());
			var expected = new MSTSIGH(line, sample);
			var actual = Tile.Convert(tileName);
			Assert.AreEqual(expected.Line, actual.Line);
			Assert.AreEqual(expected.Sample, actual.Sample);
		}

		/// <summary>
		///A test for Convert
		///</summary>
		[DataSource("Microsoft.VisualStudio.TestTools.DataSource.CSV", "|DataDirectory|\\Tiles.csv", "Tiles#csv", DataAccessMethod.Sequential), DeploymentItem("Tests\\Tiles.csv"), TestMethod]
		public void ConvertMSTSIGHToTileTest() {
			var tileName = testContextInstance.DataRow["Tile"].ToString();
			var line = int.Parse(testContextInstance.DataRow["Line"].ToString());
			var sample = int.Parse(testContextInstance.DataRow["Sample"].ToString());
			var expected = tileName;
			var actual = Tile.Convert(new MSTSIGH(line, sample));
			Assert.AreEqual(expected, actual);
		}
	}
}
