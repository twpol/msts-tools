//------------------------------------------------------------------------------
// Jgr.Msts library, part of MSTS Editors & Tools (http://jgrmsts.codeplex.com/).
// License: Microsoft Public License (Ms-PL).
//------------------------------------------------------------------------------

using System.Collections.Generic;
using System.IO;
using System.Linq;
using Jgr.IO;
using Jgr.IO.Parser;

namespace Jgr.Msts {
	public class TrackService {
		public readonly SimisProvider SimisProvider;
		public readonly FileFinder Files;
		readonly SimisFile TSection;
		public readonly Dictionary<uint, TrackShape> TrackShapes;
		public readonly Dictionary<string, TrackShape> TrackShapesByFileName;
		public readonly Dictionary<uint, TrackSection> TrackSections;

		public TrackService(FileFinder files, SimisProvider simisProvider) {
			Files = files;
			SimisProvider = simisProvider;

			TrackShapes = new Dictionary<uint, TrackShape>();
			TrackShapesByFileName = new Dictionary<string, TrackShape>();
			TrackSections = new Dictionary<uint, TrackSection>();

			TSection = new SimisFile(Files[@"Global\tsection.dat"], SimisProvider);
			TSection.ReadFile();

			foreach (var section in TSection.Tree["TrackSections"].Where(n => n.Type == "TrackSection")) {
				if (section.Contains("SectionSize")) {
					var sectionSize = section["SectionSize"];
					if (section.Contains("SectionCurve")) {
						var sectionCurve = section["SectionCurve"];
						var ts = new TrackSection(section[0].ToValue<uint>(), sectionSize[0].ToValue<float>(), sectionSize[1].ToValue<float>(), true, sectionCurve[0].ToValue<float>(), sectionCurve[1].ToValue<float>());
						TrackSections.Add(ts.ID, ts);
					} else {
						var ts = new TrackSection(section[0].ToValue<uint>(), sectionSize[0].ToValue<float>(), sectionSize[1].ToValue<float>());
						TrackSections.Add(ts.ID, ts);
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
				TrackShapes.Add(ts.ID, ts);
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
		public readonly uint ID;
		public readonly string FileName;
		public readonly double ClearanceDistance;
		public readonly bool IsTunnelShape;
		public readonly bool IsRoadShape;
		public readonly IEnumerable<TrackPath> Paths;
		public readonly int MainRoute;

		public TrackShape(uint id, string fileName, double clearanceDistance, bool isTunnelShape, bool isRoadShape, IEnumerable<TrackPath> paths, int mainRoute) {
			ID = id;
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
		public readonly double X;
		public readonly double Y;
		public readonly double Z;
		public readonly double Rotation; // About Y (vertical?)
		public readonly IEnumerable<TrackSection> Sections;

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
		public readonly uint ID;
		public readonly double Guage;
		public readonly double Length;
		public readonly bool IsCurve;
		public readonly double Radius;
		public readonly double Angle;

		public TrackSection(uint id, double guage, double length)
			: this(id, guage, length, false, 0, 0) {
		}

		public TrackSection(uint id, double guage, double length, bool isCurve, double radius, double angle) {
			ID = id;
			Guage = guage;
			Length = length;
			IsCurve = isCurve;
			Radius = radius;
			Angle = angle;
		}
	}
}
