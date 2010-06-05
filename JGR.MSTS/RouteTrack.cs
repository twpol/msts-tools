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
	public class RouteTrack {
		readonly SimisProvider _simisProvider;
		readonly FileFinder _files;
		readonly string _fileName;
		readonly Dictionary<uint, RouteTrackNode> _trackNodes;
		readonly Dictionary<uint, RouteTrackVectors> _trackVectors;
		readonly UndoRedoSimisFile _trackDB;

		public SimisProvider SimisProvider { get { return _simisProvider; } }
		public FileFinder Files { get { return _files; } }
		public string FileName { get { return _fileName; } }
		public Dictionary<uint, RouteTrackNode> TrackNodes { get { return _trackNodes; } }
		public Dictionary<uint, RouteTrackVectors> TrackVectors { get { return _trackVectors; } }

		public RouteTrack(string fileName, FileFinder files, SimisProvider simisProvider) {
			_fileName = fileName;
			_files = files;
			_simisProvider = simisProvider;

			_trackNodes = new Dictionary<uint, RouteTrackNode>();
			_trackVectors = new Dictionary<uint, RouteTrackVectors>();

			_trackDB = new UndoRedoSimisFile(_files[_fileName + ".tdb"], _simisProvider);
			_trackDB.Read();

			foreach (var node in _trackDB.Tree["TrackDB"]["TrackNodes"].Where(n => n.Type == "TrackNode")) {
				if (node.Contains("TrJunctionNode") || node.Contains("TrEndNode")) {
					var uid = node["UiD"];
					var tileX = uid[4].ToValue<int>();
					var tileZ = uid[5].ToValue<uint>();
					var x = uid[6].ToValue<float>();
					var y = uid[7].ToValue<float>();
					var z = uid[8].ToValue<float>();
					var rtNode = new RouteTrackNode(node[0].ToValue<uint>(), tileX, tileZ, x, y, z);
					_trackNodes.Add(rtNode.ID, rtNode);
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
					_trackVectors.Add(rtVectors.ID, rtVectors);
				} else {
					throw new InvalidDataException("Track DB contains track node with no obvious type.");
				}
			}
		}
	}

	[Immutable]
	public class RouteTrackVector {
		readonly int _tileX;
		readonly uint _tileZ;
		readonly double _x;
		readonly double _y;
		readonly double _z;

		public int TileX { get { return _tileX; } }
		public uint TileZ { get { return _tileZ; } }
		public double X { get { return _x; } }
		public double Y { get { return _y; } }
		public double Z { get { return _z; } }

		public RouteTrackVector(int tileX, uint tileZ, double x, double y, double z) {
			_tileX = tileX;
			_tileZ = tileZ;
			_x = x;
			_y = y;
			_z = z;
		}
	}

	[Immutable]
	public class RouteTrackNode : RouteTrackVector {
		readonly uint _id;

		public uint ID { get { return _id; } }

		public RouteTrackNode(uint id, int tileX, uint tileZ, double x, double y, double z)
			: base(tileX, tileZ, x, y, z) {
			_id = id;
		}
	}

	[Immutable]
	public class RouteTrackVectors {
		readonly uint _id;
		readonly IEnumerable<RouteTrackVector> _vectors;
		readonly uint _pinStart;
		readonly uint _pinEnd;

		public uint ID { get { return _id; } }
		public IEnumerable<RouteTrackVector> Vectors { get { return _vectors; } }
		public uint PinStart { get { return _pinStart; } }
		public uint PinEnd { get { return _pinEnd; } }

		public RouteTrackVectors(uint id, IEnumerable<RouteTrackVector> vectors, uint pinStart, uint pinEnd) {
			_id = id;
			_vectors = vectors;
			_pinStart = pinStart;
			_pinEnd = pinEnd;
		}
	}
}
