//------------------------------------------------------------------------------
// Tests, part of MSTS Editors & Tools (http://jgrmsts.codeplex.com/).
// License: New BSD License (BSD).
//------------------------------------------------------------------------------

using System.Globalization;
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
		///A test for ConvertToTile
		///</summary>
		[DataSource("Microsoft.VisualStudio.TestTools.DataSource.CSV", "|DataDirectory|\\Tiles.csv", "Tiles#csv", DataAccessMethod.Sequential), DeploymentItem("Tests\\Tiles.csv"), TestMethod]
		public void ConvertTileNameToTileTest() {
			var tileName = testContextInstance.DataRow["Tile"].ToString();
			var x = int.Parse(testContextInstance.DataRow["X"].ToString(), CultureInfo.InvariantCulture);
			var y = int.Parse(testContextInstance.DataRow["Y"].ToString(), CultureInfo.InvariantCulture);
			var expected = new TileCoordinate(x, y);
			var actual = Coordinates.ConvertToTile(tileName);
			Assert.AreEqual(expected.TileX, actual.TileX);
			Assert.AreEqual(expected.TileZ, actual.TileZ);
		}

		/// <summary>
		///A test for ConvertToTileName
		///</summary>
		[DataSource("Microsoft.VisualStudio.TestTools.DataSource.CSV", "|DataDirectory|\\Tiles.csv", "Tiles#csv", DataAccessMethod.Sequential), DeploymentItem("Tests\\Tiles.csv"), TestMethod]
		public void ConvertTileToTileNameTest() {
			var tileName = testContextInstance.DataRow["Tile"].ToString();
			var x = int.Parse(testContextInstance.DataRow["X"].ToString(), CultureInfo.InvariantCulture);
			var y = int.Parse(testContextInstance.DataRow["Y"].ToString(), CultureInfo.InvariantCulture);
			var expected = tileName;
			var actual = Coordinates.ConvertToTileName(new TileCoordinate(x, y));
			Assert.AreEqual(expected, actual);
		}

		/// <summary>
		///A test for ConvertToTile and ConvertToIgh
		///</summary>
		[DataSource("Microsoft.VisualStudio.TestTools.DataSource.CSV", "|DataDirectory|\\Tiles.csv", "Tiles#csv", DataAccessMethod.Sequential), DeploymentItem("Tests\\Tiles.csv"), TestMethod]
		public void ConvertTileToIghRoundTripTest() {
			var x = int.Parse(testContextInstance.DataRow["X"].ToString(), CultureInfo.InvariantCulture);
			var y = int.Parse(testContextInstance.DataRow["Y"].ToString(), CultureInfo.InvariantCulture);
			var expected = new TileCoordinate(x, y);
			var actual = Coordinates.ConvertToTile(Coordinates.ConvertToIgh(new TileCoordinate(x, y), 0, 0));
			Assert.AreEqual(expected.TileX, actual.TileX);
			Assert.AreEqual(expected.TileZ, actual.TileZ);
		}

		/// <summary>
		///A test for ConvertToIgh and ConvertToLatLon
		///</summary>
		[DataSource("Microsoft.VisualStudio.TestTools.DataSource.CSV", "|DataDirectory|\\Tiles.csv", "Tiles#csv", DataAccessMethod.Sequential), DeploymentItem("Tests\\Tiles.csv"), TestMethod]
		public void ConvertTileToLatLonTest() {
			var x = int.Parse(testContextInstance.DataRow["X"].ToString(), CultureInfo.InvariantCulture);
			var y = int.Parse(testContextInstance.DataRow["Y"].ToString(), CultureInfo.InvariantCulture);
			var lat = double.Parse(testContextInstance.DataRow["Lat"].ToString(), CultureInfo.InvariantCulture);
			var lon = double.Parse(testContextInstance.DataRow["Lon"].ToString(), CultureInfo.InvariantCulture);
			var expected = new LatitudeLongitudeCoordinate(lat, lon);
			var actual = Coordinates.ConvertToLatLon(Coordinates.ConvertToIgh(new TileCoordinate(x, y), 0.5, 0.5));
			Assert.AreEqual(expected.Latitude, actual.Latitude, 0.01);
			Assert.AreEqual(expected.Longitude, actual.Longitude, 0.01);
		}

		/// <summary>
		///A test for ConvertToIgh and ConvertToTile
		///</summary>
		[DataSource("Microsoft.VisualStudio.TestTools.DataSource.CSV", "|DataDirectory|\\Tiles.csv", "Tiles#csv", DataAccessMethod.Sequential), DeploymentItem("Tests\\Tiles.csv"), TestMethod]
		public void ConvertLatLonToTileTest() {
			var x = int.Parse(testContextInstance.DataRow["X"].ToString(), CultureInfo.InvariantCulture);
			var y = int.Parse(testContextInstance.DataRow["Y"].ToString(), CultureInfo.InvariantCulture);
			var lat = double.Parse(testContextInstance.DataRow["Lat"].ToString(), CultureInfo.InvariantCulture);
			var lon = double.Parse(testContextInstance.DataRow["Lon"].ToString(), CultureInfo.InvariantCulture);
			var expected = new TileCoordinate(x, y);
			var actual = Coordinates.ConvertToTile(Coordinates.ConvertToIgh(new LatitudeLongitudeCoordinate(lat, lon)));
			Assert.AreEqual(expected.TileX, actual.TileX);
			Assert.AreEqual(expected.TileZ, actual.TileZ);
		}
	}
}
