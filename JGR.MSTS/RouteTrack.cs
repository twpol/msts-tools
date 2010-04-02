//------------------------------------------------------------------------------
// Jgr.Msts library, part of MSTS Editors & Tools (http://jgrmsts.codeplex.com/).
// License: Microsoft Public License (Ms-PL).
//------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Jgr.IO;
using Jgr.IO.Parser;
using System.IO;

namespace Jgr.Msts {
	public class RouteTrack {
		public readonly SimisProvider SimisProvider;
		public readonly FileFinder Files;
		public readonly string FileName;
		readonly SimisFile TrackDB;
		public readonly Dictionary<uint, RouteTrackNode> TrackNodes;
		public readonly Dictionary<uint, RouteTrackVectors> TrackVectors;

		public RouteTrack(string fileName, FileFinder files, SimisProvider simisProvider) {
			FileName = fileName;
			Files = files;
			SimisProvider = simisProvider;

			TrackNodes = new Dictionary<uint, RouteTrackNode>();
			TrackVectors = new Dictionary<uint, RouteTrackVectors>();

			TrackDB = new SimisFile(Files[FileName + ".tdb"], SimisProvider);
			TrackDB.ReadFile();

			foreach (var node in TrackDB.Tree["TrackDB"]["TrackNodes"].Where(n => n.Type == "TrackNode")) {
				if (node.Contains("TrJunctionNode") || node.Contains("TrEndNode")) {
					var uid = node["UiD"];
					var tileX = uid[4].ToValue<int>();
					var tileZ = uid[5].ToValue<uint>();
					var x = uid[6].ToValue<float>();
					var y = uid[7].ToValue<float>();
					var z = uid[8].ToValue<float>();
					var rtNode = new RouteTrackNode(node[0].ToValue<uint>(), tileX, tileZ, x, y, z);
					TrackNodes.Add(rtNode.ID, rtNode);
				} else if (node.Contains("TrVectorNode")) {
					var sections = node["TrVectorNode"]["TrVectorSections"];
					var sectionCount = sections[0].ToValue<uint>();
					var vectors = new List<RouteTrackVector>();
					for (var i = 0; i < sectionCount; i++) {
						var tileX = sections[i * 16 + 9].ToValue<int>();
						var tileZ = sections[i * 16 + 10].ToValue<uint>();
						var x = sections[i * 16 + 11].ToValue<float>();
						var y = sections[i * 16 + 12].ToValue<float>();
						var z = sections[i * 16 + 13].ToValue<float>();
						vectors.Add(new RouteTrackVector(tileX, tileZ, x, y, z));
					}
					if (node["TrPins"].Count != 4) throw new InvalidDataException("Track DB node does not have exactly 2 pins.");
					var rtVectors = new RouteTrackVectors(node[0].ToValue<uint>(), vectors, node["TrPins"][2][0].ToValue<uint>(), node["TrPins"][3][0].ToValue<uint>());
					TrackVectors.Add(rtVectors.ID, rtVectors);
				} else {
					throw new InvalidDataException("Track DB contains track node with no obvious type.");
				}
			}
		}
	}

	[Immutable]
	public class RouteTrackVector {
		public readonly int TileX;
		public readonly uint TileZ;
		public readonly double X;
		public readonly double Y;
		public readonly double Z;

		public RouteTrackVector(int tileX, uint tileZ, double x, double y, double z) {
			TileX = tileX;
			TileZ = tileZ;
			X = x;
			Y = y;
			Z = z;
		}
	}

	[Immutable]
	public class RouteTrackNode : RouteTrackVector {
		public readonly uint ID;

		public RouteTrackNode(uint id, int tileX, uint tileZ, double x, double y, double z)
			: base(tileX, tileZ, x, y, z) {
			ID = id;
		}
	}

	[Immutable]
	public class RouteTrackVectors {
		public readonly uint ID;
		public readonly IEnumerable<RouteTrackVector> Vectors;
		public readonly uint PinStart;
		public readonly uint PinEnd;

		public RouteTrackVectors(uint id, IEnumerable<RouteTrackVector> vectors, uint pinStart, uint pinEnd) {
			ID = id;
			Vectors = vectors;
			PinStart = pinStart;
			PinEnd = pinEnd;
		}
	}
}
