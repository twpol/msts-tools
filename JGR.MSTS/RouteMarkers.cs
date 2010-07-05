//------------------------------------------------------------------------------
// Jgr.Msts library, part of MSTS Editors & Tools (http://jgrmsts.codeplex.com/).
// License: New BSD License (BSD).
//------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using Jgr.IO.Parser;

namespace Jgr.Msts {
	[Immutable]
	public class RouteMarker {
		public LatitudeLongitudeCoordinate Location { get; private set; }
		public TileCoordinate Tile { get; private set; }
		public double TileX { get; private set; }
		public double TileZ { get; private set; }
		public string Label { get; private set; }

		public RouteMarker(LatitudeLongitudeCoordinate location, string label) {
			Location = location;
			double tileX;
			double tileZ;
			Tile = Coordinates.ConvertToTile(Coordinates.ConvertToIgh(location), out tileX, out tileZ);
			TileX = tileX;
			TileZ = tileZ;
			Label = label;
		}
	}

	[Immutable]
	public class RouteMarkers {
		public Route Route { get; private set; }
		public SimisProvider SimisProvider { get; private set; }
		public IEnumerable<RouteMarker> Markers { get; private set; }

		public RouteMarkers(Route route, SimisProvider simisProvider) {
			Route = route;
			SimisProvider = simisProvider;
			Markers = LoadMarkers();
		}

		IEnumerable<RouteMarker> LoadMarkers() {
			var markerList = new List<RouteMarker>();
			string markersFile = String.Format(CultureInfo.InvariantCulture, @"{0}\{1}.mkr", Route.RoutePath, Route.FileName);

			if (File.Exists(markersFile)) {
				var markers = new SimisFile(markersFile, SimisProvider);
				foreach (var marker in markers.Tree.Where(n => n.Type == "Marker")) {
					markerList.Add(new RouteMarker(new LatitudeLongitudeCoordinate(marker[1].ToValue<float>(), marker[0].ToValue<float>()), marker[2].ToValue<string>()));
				}
			}

			return markerList;
		}
	}
}
