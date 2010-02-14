//------------------------------------------------------------------------------
// Tests, part of MSTS Editors & Tools (http://jgrmsts.codeplex.com/).
// License: Microsoft Public License (Ms-PL).
//------------------------------------------------------------------------------

using Jgr.Msts;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Diagnostics;

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
		public void ConvertTileNameToMstsTileTest() {
			var tileName = testContextInstance.DataRow["Tile"].ToString();
			var x = int.Parse(testContextInstance.DataRow["X"].ToString());
			var y = int.Parse(testContextInstance.DataRow["Y"].ToString());
			var expected = new MstsTile(x, y);
			var actual = Tile.ConvertToMstsTile(tileName);
			Assert.AreEqual(expected.X, actual.X);
			Assert.AreEqual(expected.Y, actual.Y);
		}

		/// <summary>
		///A test for Convert
		///</summary>
		[DataSource("Microsoft.VisualStudio.TestTools.DataSource.CSV", "|DataDirectory|\\Tiles.csv", "Tiles#csv", DataAccessMethod.Sequential), DeploymentItem("Tests\\Tiles.csv"), TestMethod]
		public void ConvertMstsTileToTileNameTest() {
			var tileName = testContextInstance.DataRow["Tile"].ToString();
			var x = int.Parse(testContextInstance.DataRow["X"].ToString());
			var y = int.Parse(testContextInstance.DataRow["Y"].ToString());
			var expected = tileName;
			var actual = Tile.ConvertToTileName(new MstsTile(x, y));
			Assert.AreEqual(expected, actual);
		}

		/// <summary>
		///A test for Convert
		///</summary>
		[DataSource("Microsoft.VisualStudio.TestTools.DataSource.CSV", "|DataDirectory|\\Tiles.csv", "Tiles#csv", DataAccessMethod.Sequential), DeploymentItem("Tests\\Tiles.csv"), TestMethod]
		public void ConvertMstsTileToLatLonTest() {
			var x = int.Parse(testContextInstance.DataRow["X"].ToString());
			var y = int.Parse(testContextInstance.DataRow["Y"].ToString());
			var lat = double.Parse(testContextInstance.DataRow["Lat"].ToString());
			var lon = double.Parse(testContextInstance.DataRow["Lon"].ToString());
			var expected = new LatLon(lat, lon);
			var actual = Tile.ConvertToLatLon(Tile.ConvertToIgh(new MstsTile(x, y), 0.5, 0.5));
			Assert.AreEqual(expected.Lat, actual.Lat, 0.01);
			Assert.AreEqual(expected.Lon, actual.Lon, 0.01);
		}

		/// <summary>
		///A test for Convert
		///</summary>
		[DataSource("Microsoft.VisualStudio.TestTools.DataSource.CSV", "|DataDirectory|\\Tiles.csv", "Tiles#csv", DataAccessMethod.Sequential), DeploymentItem("Tests\\Tiles.csv"), TestMethod]
		public void ConvertLatLonToMstsTileTest() {
			var x = int.Parse(testContextInstance.DataRow["X"].ToString());
			var y = int.Parse(testContextInstance.DataRow["Y"].ToString());
			var lat = double.Parse(testContextInstance.DataRow["Lat"].ToString());
			var lon = double.Parse(testContextInstance.DataRow["Lon"].ToString());
			var expected = new MstsTile(x, y);
			var actual = Tile.ConvertToMstsTile(Tile.ConvertToIgh(new LatLon(lat, lon)));
			Assert.AreEqual(expected.X, actual.X);
			Assert.AreEqual(expected.Y, actual.Y);
			Assert.Inconclusive();
		}
	}
}
