//------------------------------------------------------------------------------
// Jgr.Msts library, part of MSTS Editors & Tools (http://jgrmsts.codeplex.com/).
// License: New BSD License (BSD).
//------------------------------------------------------------------------------

using System.Collections.Generic;
using System.IO;
using System.Linq;
using Jgr.IO;
using Jgr.IO.Parser;

namespace Jgr.Msts {
	[Immutable]
	public class TrackService {
		public SimisProvider SimisProvider { get; private set; }
		public FileFinder Files { get; private set; }
		public IDictionary<uint, TrackShape> TrackShapes { get; private set; }
		public IDictionary<string, TrackShape> TrackShapesByFileName { get; private set; }
		public IDictionary<uint, TrackSection> TrackSections { get; private set; }
		UndoRedoSimisFile TSection { get; set; }

		public TrackService(FileFinder files, SimisProvider simisProvider) {
			Files = files;
			SimisProvider = simisProvider;

			TrackShapes = new Dictionary<uint, TrackShape>();
			TrackShapesByFileName = new Dictionary<string, TrackShape>();
			TrackSections = new Dictionary<uint, TrackSection>();

			TSection = new UndoRedoSimisFile(Files[@"Global\tsection.dat"], SimisProvider);
			TSection.Read();

			foreach (var section in TSection.Tree["TrackSections"].Where(n => n.Type == "TrackSection")) {
				if (section.Contains("SectionSize")) {
					var sectionSize = section["SectionSize"];
					if (section.Contains("SectionCurve")) {
						var sectionCurve = section["SectionCurve"];
						var ts = new TrackSection(section[0].ToValue<uint>(), sectionSize[0].ToValue<float>(), sectionSize[1].ToValue<float>(), true, sectionCurve[0].ToValue<float>(), sectionCurve[1].ToValue<float>());
						TrackSections.Add(ts.Id, ts);
					} else {
						var ts = new TrackSection(section[0].ToValue<uint>(), sectionSize[0].ToValue<float>(), sectionSize[1].ToValue<float>());
						TrackSections.Add(ts.Id, ts);
					}
				}
			}

			foreach (var shape in TSection.Tree["TrackShapes"].Where(n => n.Type == "TrackShape")) {
				var tpaths = new List<TrackPath>();
				foreach (var path in shape.Where(n => n.Type == "SectionIdx")) {
					var count = path[0].ToValue<uint>();
					var tsections = new List<TrackSection>();
					for (var i = 0; i < count; i++) {
						tsections.Add(TrackSections[path[5 + i].ToValue<uint>()]);
					}
					tpaths.Add(new TrackPath(path[1].ToValue<float>(), path[2].ToValue<float>(), path[3].ToValue<float>(), path[4].ToValue<float>(), tsections));
				}
				var ts = new TrackShape(shape[0].ToValue<uint>(), shape["FileName"][0].ToValue<string>().ToLowerInvariant(), shape.Contains("ClearanceDist") ? shape["ClearanceDist"][0].ToValue<float>() : 0, shape.Contains("TunnelShape"), shape.Contains("RoadShape"), tpaths, shape.Contains("MainRoute") ? (int)shape["MainRoute"][0].ToValue<uint>() : -1);
				TrackShapes.Add(ts.Id, ts);
				if (TrackShapesByFileName.ContainsKey(ts.FileName)) {
					TrackShapesByFileName[ts.FileName] = ts;
				} else {
					TrackShapesByFileName.Add(ts.FileName, ts);
				}
			}
		}
	}

	[Immutable]
	public class TrackShape {
		readonly uint _id;
		readonly string _fileName;
		readonly double _clearanceDistance;
		readonly bool _isTunnelShape;
		readonly bool _isRoadShape;
		readonly IEnumerable<TrackPath> _paths;
		readonly int _mainRoute;

		public uint Id { get { return _id; } }
		public string FileName { get { return _fileName; } }
		public double ClearanceDistance { get { return _clearanceDistance; } }
		public bool IsTunnelShape { get { return _isTunnelShape; } }
		public bool IsRoadShape { get { return _isRoadShape; } }
		public IEnumerable<TrackPath> Paths { get { return _paths; } }
		public int MainRoute { get { return _mainRoute; } }

		public TrackShape(uint id, string fileName, double clearanceDistance, bool isTunnelShape, bool isRoadShape, IEnumerable<TrackPath> paths, int mainRoute) {
			_id = id;
			_fileName = fileName;
			_clearanceDistance = clearanceDistance;
			_isTunnelShape = isTunnelShape;
			_isRoadShape = isRoadShape;
			_paths = paths;
			_mainRoute = mainRoute;
		}
	}

	[Immutable]
	public class TrackPath {
		readonly double _x;
		readonly double _y;
		readonly double _z;
		readonly double _rotation; // About Y (vertical?)
		readonly IEnumerable<TrackSection> _sections;

		public double X { get { return _x; } }
		public double Y { get { return _y; } }
		public double Z { get { return _z; } }
		public double Rotation { get { return _rotation; } }
		public IEnumerable<TrackSection> Sections { get { return _sections; } }

		public TrackPath(double x, double y, double z, double rotation, IEnumerable<TrackSection> sections) {
			_x = x;
			_y = y;
			_z = z;
			_rotation = rotation;
			_sections = sections;
		}
	}

	[Immutable]
	public class TrackSection {
		readonly uint _id;
		readonly double _gauge;
		readonly double _length;
		readonly bool _isCurve;
		readonly double _radius;
		readonly double _angle;

		public uint Id { get { return _id; } }
		public double Gauge { get { return _gauge; } }
		public double Length { get { return _length; } }
		public bool IsCurve { get { return _isCurve; } }
		public double Radius { get { return _radius; } }
		public double Angle { get { return _angle; } }

		public TrackSection(uint id, double gauge, double length)
			: this(id, gauge, length, false, 0, 0) {
		}

		public TrackSection(uint id, double gauge, double length, bool isCurve, double radius, double angle) {
			_id = id;
			_gauge = gauge;
			_length = length;
			_isCurve = isCurve;
			_radius = radius;
			_angle = angle;
		}
	}
}
