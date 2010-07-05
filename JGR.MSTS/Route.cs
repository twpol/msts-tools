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
	public class Route : DataTreeNode<Route> {
		public SimisProvider SimisProvider { get; private set; }
		public string RoutePath { get; private set; }
		public FileFinder Files { get; private set; }
		public SimisFile TrackFile { get; private set; }
		//TrackService _TrackService;
		//RouteTrack _RouteTrack;

		internal Route(SimisProvider simisProvider, string routePath, FileFinder files, SimisFile trackFile) {
			SimisProvider = simisProvider;
			RoutePath = routePath;
			Files = files;
			TrackFile = trackFile;
		}

		public Route(string trackFile, SimisProvider simisProvider) {
			RoutePath = Path.GetDirectoryName(trackFile);
			SimisProvider = simisProvider;
			// We can find things relative to the following:
			// +-<msts>   <-- here
			//   +-Global   <-- here
			//   +-Routes
			//     +-<route>   <-- here
			//       +-Global   <-- here*
			// * Allowed for route-specific global files; this is a feature for Open Rails Train Simulator (ORTS).
			// Paths used to access files will usually contain 1 directory above, e.g. "activities\foo.act", to avoid
			// unexpected and undesired collisions between files in <msts>, <msts>\Global and <msts>\Routes\<route>.
			var mstsPath = Path.GetDirectoryName(Path.GetDirectoryName(RoutePath));
			Files = new FileFinder(new string[] { RoutePath, Path.Combine(RoutePath, "Global"), mstsPath, Path.Combine(mstsPath, "Global") });

			TrackFile = new SimisFile(trackFile, SimisProvider.GetForPath(trackFile));
		}

		protected override void SetArgument(string name, object value, ref Dictionary<string, object> arguments, ref DataTreeNode<Route>.TypeData typeData) {
			var trackFile = arguments.ContainsKey("TrackFile") ? (SimisFile)arguments["TrackFile"] : TrackFile;
			switch (name) {
				case "Name":
					arguments["TrackFile"] = trackFile.GetPath("Tree", "Tr_RouteFile", "Name", 0).Set(new SimisTreeNodeValueString("string", "", (string)value));
					break;
				case "Description":
					arguments["TrackFile"] = trackFile.GetPath("Tree", "Tr_RouteFile", "Description", 0).Set(new SimisTreeNodeValueString("string", "", ((string)value).Replace("\r\n", "\n")));
					break;
				case "SpeedLimit":
					arguments["TrackFile"] = trackFile.GetPath("Tree", "Tr_RouteFile", "SpeedLimit", 0).Set(new SimisTreeNodeValueFloat("float", "", (float)value));
					break;
				case "SpeedLimitRestricted":
					arguments["TrackFile"] = trackFile.GetPath("Tree", "Tr_RouteFile", "TempRestrictedSpeed", 0).Set(new SimisTreeNodeValueFloat("float", "", (float)value));
					break;
				case "TerrainErrorScale":
					arguments["TrackFile"] = trackFile.GetPath("Tree", "Tr_RouteFile", "TerrainErrorScale", 0).Set(new SimisTreeNodeValueFloat("float", "", (float)value));
					break;
				case "HasLowResolutionTerrain":
					arguments["TrackFile"] = trackFile.GetPath("Tree", "Tr_RouteFile", "Mountains", 0).Set(new SimisTreeNodeValueIntegerDWord("dword", "", (bool)value ? 1u : 0));
					break;
				case "GravityScale":
					if ((float)value == 1.0f) {
						if (TrackFile.Tree["Tr_RouteFile"].Contains("GravityScale")) {
							arguments["TrackFile"] = trackFile.GetPath("Tree", "Tr_RouteFile", "GravityScale").Remove();
						}
					} else if (TrackFile.Tree["Tr_RouteFile"].Contains("GravityScale")) {
						arguments["TrackFile"] = trackFile.GetPath("Tree", "Tr_RouteFile", "GravityScale", 0).Set(new SimisTreeNodeValueFloat("float", "", (float)value));
					} else {
						arguments["TrackFile"] = trackFile.GetPath("Tree", "Tr_RouteFile").Append(new SimisTreeNode("GravityScale", "", new[] { new SimisTreeNodeValueFloat("float", "", (float)value) }));
					}
					break;
				case "TimetableTollerance":
					if ((float)value == 0.0f) {
						if (TrackFile.Tree["Tr_RouteFile"].Contains("TimetableTollerance")) arguments["TrackFile"] = trackFile.GetPath("Tree", "Tr_RouteFile", "TimetableTollerance").Remove();
					} else if (TrackFile.Tree["Tr_RouteFile"].Contains("TimetableTollerance")) {
						arguments["TrackFile"] = trackFile.GetPath("Tree", "Tr_RouteFile", "TimetableTollerance", 0).Set(new SimisTreeNodeValueFloat("float", "", (float)value));
					} else {
						arguments["TrackFile"] = trackFile.GetPath("Tree", "Tr_RouteFile").Append(new SimisTreeNode("TimetableTollerance", "", new[] { new SimisTreeNodeValueFloat("float", "", (float)value) }));
					}
					break;
				case "HasUnitsMetric":
				case "HasUnitsImperial":
					if (name == "HasUnitsImperial") value = !(bool)value;
					if ((bool)value) {
						if (!TrackFile.Tree["Tr_RouteFile"].Contains("MilepostUnitsKilometers")) arguments["TrackFile"] = trackFile.GetPath("Tree", "Tr_RouteFile").Append(new SimisTreeNode("MilepostUnitsKilometers", ""));
						if (TrackFile.Tree["Tr_RouteFile"].Contains("MilepostUnitsMiles")) arguments["TrackFile"] = trackFile.GetPath("Tree", "Tr_RouteFile", "MilepostUnitsMiles").Remove();
					} else {
						if (TrackFile.Tree["Tr_RouteFile"].Contains("MilepostUnitsKilometers")) arguments["TrackFile"] = trackFile.GetPath("Tree", "Tr_RouteFile", "MilepostUnitsKilometers").Remove();
						if (!TrackFile.Tree["Tr_RouteFile"].Contains("MilepostUnitsMiles")) arguments["TrackFile"] = trackFile.GetPath("Tree", "Tr_RouteFile").Append(new SimisTreeNode("MilepostUnitsMiles", ""));
					}
					break;
				case "ElectrificationType":
					arguments["TrackFile"] = trackFile.GetPath("Tree", "Tr_RouteFile", "Electrified", 0).Set(new SimisTreeNodeValueIntegerDWord("dword", "", (uint)value));
					break;
				case "ElectrificationWireHeight":
					arguments["TrackFile"] = trackFile.GetPath("Tree", "Tr_RouteFile", "OverheadWireHeight", 0).Set(new SimisTreeNodeValueFloat("float", "", (float)value));
					break;
				case "ElectrificationMaxVoltage":
					if ((float)value == 0.0f) {
						if (TrackFile.Tree["Tr_RouteFile"].Contains("MaxLineVoltage")) arguments["TrackFile"] = trackFile.GetPath("Tree", "Tr_RouteFile", "MaxLineVoltage").Remove();
					} else if (TrackFile.Tree["Tr_RouteFile"].Contains("MaxLineVoltage")) {
						arguments["TrackFile"] = trackFile.GetPath("Tree", "Tr_RouteFile", "MaxLineVoltage", 0).Set(new SimisTreeNodeValueFloat("float", "", (float)value));
					} else {
						arguments["TrackFile"] = trackFile.GetPath("Tree", "Tr_RouteFile").Append(new SimisTreeNode("MaxLineVoltage", "", new[] { new SimisTreeNodeValueFloat("float", "", (float)value) }));
					}
					break;
				case "RouteStart":
					var routeStart = value as PreciseTileCoordinate;
					arguments["TrackFile"] = trackFile.GetPath("Tree", "Tr_RouteFile", "RouteStart").Set(new SimisTreeNode("RouteStart", "", new[] { new SimisTreeNodeValueFloat("int", "", routeStart.TileX), new SimisTreeNodeValueFloat("int", "", routeStart.TileZ), new SimisTreeNodeValueFloat("float", "", (float)(routeStart.X * 2048 - 1024)), new SimisTreeNodeValueFloat("float", "", (float)(routeStart.Z * 2048 - 1024)) }));
					break;
				default:
					base.SetArgument(name, value, ref arguments, ref typeData);
					break;
			}
		}

		public string FileName {
			get {
				return TrackFile.Tree["Tr_RouteFile"]["FileName"][0].ToValue<string>();
			}
		}

		public string Name {
			get {
				return TrackFile.Tree["Tr_RouteFile"]["Name"][0].ToValue<string>();
			}
		}

		public string Description {
			get {
				return TrackFile.Tree["Tr_RouteFile"]["Description"][0].ToValue<string>().Replace("\n", "\r\n");
			}
		}

		public float SpeedLimit {
			get {
				return TrackFile.Tree["Tr_RouteFile"]["SpeedLimit"][0].ToValue<float>();
			}
		}

		public float SpeedLimitRestricted {
			get {
				return TrackFile.Tree["Tr_RouteFile"]["TempRestrictedSpeed"][0].ToValue<float>();
			}
		}

		public float TerrainErrorScale {
			get {
				return TrackFile.Tree["Tr_RouteFile"]["TerrainErrorScale"][0].ToValue<float>();
			}
		}

		public bool HasLowResolutionTerrain {
			get {
				return TrackFile.Tree["Tr_RouteFile"]["Mountains"][0].ToValue<uint>() != 0;
			}
		}

		public float GravityScale {
			get {
				return TrackFile.Tree["Tr_RouteFile"].Contains("GravityScale") ? TrackFile.Tree["Tr_RouteFile"]["GravityScale"][0].ToValue<float>() : 1.0f;
			}
		}

		public float TimetableTollerance {
			get {
				return TrackFile.Tree["Tr_RouteFile"].Contains("TimetableTollerance") ? TrackFile.Tree["Tr_RouteFile"]["TimetableTollerance"][0].ToValue<float>() : 0.0f;
			}
		}

		public bool HasUnitsMetric {
			get {
				return TrackFile.Tree["Tr_RouteFile"].Contains("MilepostUnitsKilometers");
			}
		}

		public bool HasUnitsImperial {
			get {
				return !HasUnitsMetric;
			}
		}

		public uint ElectrificationType {
			get {
				return TrackFile.Tree["Tr_RouteFile"]["Electrified"][0].ToValue<uint>();
			}
		}

		public float ElectrificationWireHeight {
			get {
				return TrackFile.Tree["Tr_RouteFile"]["OverheadWireHeight"][0].ToValue<float>();
			}
		}

		public float ElectrificationMaxVoltage {
			get {
				return TrackFile.Tree["Tr_RouteFile"].Contains("MaxLineVoltage") ? TrackFile.Tree["Tr_RouteFile"]["MaxLineVoltage"][0].ToValue<float>() : 0.0f;
			}
		}

		public PreciseTileCoordinate RouteStart {
			get {
				var node = TrackFile.Tree["Tr_RouteFile"]["RouteStart"];
				return new PreciseTileCoordinate(node[0].ToValue<int>(), node[1].ToValue<int>(), (node[2].ToValue<float>() + 1024) / 2048, (node[3].ToValue<float>() + 1024) / 2048);
			}
		}

		//public TrackService TrackService {
		//    get {
		//        lock (this) {
		//            if (_TrackService != null) {
		//                return _TrackService;
		//            }
		//            _TrackService = new TrackService(Files, SimisProvider);
		//        }
		//        return _TrackService;
		//    }
		//}

		//public RouteTrack Track {
		//    get {
		//        lock (this) {
		//            if (_RouteTrack != null) {
		//                return _RouteTrack;
		//            }
		//            _RouteTrack = new RouteTrack(FileName, Files, SimisProvider);
		//        }
		//        return _RouteTrack;
		//    }
		//}

		public IEnumerable<string> Tiles {
			get {
				// TODO: Cache the tiles by name or something. We should not be creating a new set every time!
				return from tile in Directory.GetFiles(RoutePath + @"\Tiles", "*.t")
					   select Path.GetFileNameWithoutExtension(tile);
			}
		}
	}
}
