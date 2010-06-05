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
	public class TrackService {
		readonly SimisProvider _simisProvider;
		readonly FileFinder _files;
		readonly Dictionary<uint, TrackShape> _trackShapes;
		readonly Dictionary<string, TrackShape> _trackShapesByFileName;
		readonly Dictionary<uint, TrackSection> _trackSections;
		readonly UndoRedoSimisFile _tSection;

		public SimisProvider SimisProvider { get { return _simisProvider; } }
		public FileFinder Files { get { return _files; } }
		public Dictionary<uint, TrackShape> TrackShapes { get { return _trackShapes; } }
		public Dictionary<string, TrackShape> TrackShapesByFileName { get { return _trackShapesByFileName; } }
		public Dictionary<uint, TrackSection> TrackSections { get { return _trackSections; } }

		public TrackService(FileFinder files, SimisProvider simisProvider) {
			_files = files;
			_simisProvider = simisProvider;

			_trackShapes = new Dictionary<uint, TrackShape>();
			_trackShapesByFileName = new Dictionary<string, TrackShape>();
			_trackSections = new Dictionary<uint, TrackSection>();

			_tSection = new UndoRedoSimisFile(_files[@"Global\tsection.dat"], _simisProvider);
			_tSection.Read();

			foreach (var section in _tSection.Tree["TrackSections"].Where(n => n.Type == "TrackSection")) {
				if (section.Contains("SectionSize")) {
					var sectionSize = section["SectionSize"];
					if (section.Contains("SectionCurve")) {
						var sectionCurve = section["SectionCurve"];
						var ts = new TrackSection(section[0].ToValue<uint>(), sectionSize[0].ToValue<float>(), sectionSize[1].ToValue<float>(), true, sectionCurve[0].ToValue<float>(), sectionCurve[1].ToValue<float>());
						_trackSections.Add(ts.ID, ts);
					} else {
						var ts = new TrackSection(section[0].ToValue<uint>(), sectionSize[0].ToValue<float>(), sectionSize[1].ToValue<float>());
						_trackSections.Add(ts.ID, ts);
					}
				}
			}

			foreach (var shape in _tSection.Tree["TrackShapes"].Where(n => n.Type == "TrackShape")) {
				var tpaths = new List<TrackPath>();
				foreach (var path in shape.Where(n => n.Type == "SectionIdx")) {
					var count = path[0].ToValue<uint>();
					var tsections = new List<TrackSection>();
					for (var i = 0; i < count; i++) {
						tsections.Add(_trackSections[path[5 + i].ToValue<uint>()]);
					}
					tpaths.Add(new TrackPath(path[1].ToValue<float>(), path[2].ToValue<float>(), path[3].ToValue<float>(), path[4].ToValue<float>(), tsections));
				}
				var ts = new TrackShape(shape[0].ToValue<uint>(), shape["FileName"][0].ToValue<string>().ToLowerInvariant(), shape.Contains("ClearanceDist") ? shape["ClearanceDist"][0].ToValue<float>() : 0, shape.Contains("TunnelShape"), shape.Contains("RoadShape"), tpaths, shape.Contains("MainRoute") ? (int)shape["MainRoute"][0].ToValue<uint>() : -1);
				_trackShapes.Add(ts.ID, ts);
				if (_trackShapesByFileName.ContainsKey(ts.FileName)) {
					_trackShapesByFileName[ts.FileName] = ts;
				} else {
					_trackShapesByFileName.Add(ts.FileName, ts);
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

		public uint ID { get { return _id; } }
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
		readonly double _guage;
		readonly double _length;
		readonly bool _isCurve;
		readonly double _radius;
		readonly double _angle;

		public uint ID { get { return _id; } }
		public double Guage { get { return _guage; } }
		public double Length { get { return _length; } }
		public bool IsCurve { get { return _isCurve; } }
		public double Radius { get { return _radius; } }
		public double Angle { get { return _angle; } }

		public TrackSection(uint id, double guage, double length)
			: this(id, guage, length, false, 0, 0) {
		}

		public TrackSection(uint id, double guage, double length, bool isCurve, double radius, double angle) {
			_id = id;
			_guage = guage;
			_length = length;
			_isCurve = isCurve;
			_radius = radius;
			_angle = angle;
		}
	}
}
