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
		public SimisFile TSection { get; private set; }

		public TrackService(FileFinder files, SimisProvider simisProvider) {
			Files = files;
			SimisProvider = simisProvider;

			TrackShapes = new Dictionary<uint, TrackShape>();
			TrackShapesByFileName = new Dictionary<string, TrackShape>();
			TrackSections = new Dictionary<uint, TrackSection>();

			TSection = new SimisFile(Files[@"Global\tsection.dat"], SimisProvider);

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
		public uint Id { get; private set; }
		public string FileName { get; private set; }
		public double ClearanceDistance { get; private set; }
		public bool IsTunnelShape { get; private set; }
		public bool IsRoadShape { get; private set; }
		public IEnumerable<TrackPath> Paths { get; private set; }
		public int MainRoute { get; private set; }

		public TrackShape(uint id, string fileName, double clearanceDistance, bool isTunnelShape, bool isRoadShape, IEnumerable<TrackPath> paths, int mainRoute) {
			Id = id;
			FileName = fileName;
			ClearanceDistance = clearanceDistance;
			IsTunnelShape = isTunnelShape;
			IsRoadShape = isRoadShape;
			Paths = paths;
			MainRoute = mainRoute;
		}
	}

	[Immutable]
	public class TrackPath {
		public double X { get; private set; }
		public double Y { get; private set; }
		public double Z { get; private set; }
		public double Rotation { get; private set; }
		public IEnumerable<TrackSection> Sections { get; private set; }

		public TrackPath(double x, double y, double z, double rotation, IEnumerable<TrackSection> sections) {
			X = x;
			Y = y;
			Z = z;
			Rotation = rotation;
			Sections = sections;
		}
	}

	[Immutable]
	public class TrackSection {
		public uint Id { get; private set; }
		public double Gauge { get; private set; }
		public double Length { get; private set; }
		public bool IsCurve { get; private set; }
		public double Radius { get; private set; }
		public double Angle { get; private set; }

		public TrackSection(uint id, double gauge, double length)
			: this(id, gauge, length, false, 0, 0) {
		}

		public TrackSection(uint id, double gauge, double length, bool isCurve, double radius, double angle) {
			Id = id;
			Gauge = gauge;
			Length = length;
			IsCurve = isCurve;
			Radius = radius;
			Angle = angle;
		}
	}
}
