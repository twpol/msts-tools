//------------------------------------------------------------------------------
// Jgr.Msts library, part of MSTS Editors & Tools (http://jgrmsts.codeplex.com/).
// License: New BSD License (BSD).
//------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Jgr.IO;
using Jgr.IO.Parser;
using System.IO;

namespace Jgr.Msts {
	[Immutable]
	public class RouteTrack {
		public SimisProvider SimisProvider { get; private set; }
		public FileFinder Files { get; private set; }
		public string FileName { get; private set; }
		public IDictionary<uint, RouteTrackNode> TrackNodes { get; private set; }
		public IDictionary<uint, RouteTrackVectors> TrackVectors { get; private set; }
		UndoRedoSimisFile TrackDB { get; set; }

		public RouteTrack(string fileName, FileFinder files, SimisProvider simisProvider) {
			FileName = fileName;
			Files = files;
			SimisProvider = simisProvider;

			TrackNodes = new Dictionary<uint, RouteTrackNode>();
			TrackVectors = new Dictionary<uint, RouteTrackVectors>();

			TrackDB = new UndoRedoSimisFile(Files[FileName + ".tdb"], SimisProvider);
			TrackDB.Read();

			foreach (var node in TrackDB.Tree["TrackDB"]["TrackNodes"].Where(n => n.Type == "TrackNode")) {
				if (node.Contains("TrJunctionNode") || node.Contains("TrEndNode")) {
					var uid = node["UiD"];
					var tileX = uid[4].ToValue<int>();
					var tileZ = uid[5].ToValue<uint>();
					var x = uid[6].ToValue<float>();
					var y = uid[7].ToValue<float>();
					var z = uid[8].ToValue<float>();
					var rtNode = new RouteTrackNode(node[0].ToValue<uint>(), tileX, tileZ, x, y, z);
					TrackNodes.Add(rtNode.Id, rtNode);
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
					TrackVectors.Add(rtVectors.Id, rtVectors);
				} else {
					throw new InvalidDataException("Track DB contains track node with no obvious type.");
				}
			}
		}
	}

	[Immutable]
	public class RouteTrackVector {
		public int TileX { get; private set; }
		public uint TileZ { get; private set; }
		public double X { get; private set; }
		public double Y { get; private set; }
		public double Z { get; private set; }

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
		public uint Id { get; private set; }

		public RouteTrackNode(uint id, int tileX, uint tileZ, double x, double y, double z)
			: base(tileX, tileZ, x, y, z) {
			Id = id;
		}
	}

	[Immutable]
	public class RouteTrackVectors {
		public uint Id { get; private set; }
		public IEnumerable<RouteTrackVector> Vectors { get; private set; }
		public uint PinStart { get; private set; }
		public uint PinEnd { get; private set; }

		public RouteTrackVectors(uint id, IEnumerable<RouteTrackVector> vectors, uint pinStart, uint pinEnd) {
			Id = id;
			Vectors = vectors;
			PinStart = pinStart;
			PinEnd = pinEnd;
		}
	}
}
