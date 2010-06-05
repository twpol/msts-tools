//------------------------------------------------------------------------------
// Jgr.Msts library, part of MSTS Editors & Tools (http://jgrmsts.codeplex.com/).
// License: New BSD License (BSD).
//------------------------------------------------------------------------------

using System.Collections.Generic;
using System.IO;
using Jgr.IO;
using Jgr.IO.Parser;

namespace Jgr.Msts {
	[Immutable]
	public class Route : DataTreeNode<Route> {
		readonly SimisProvider _simisProvider;
		readonly string _routePath;
		readonly FileFinder _files;
		readonly SimisFile _trackFile;
		//TrackService _TrackService;
		//RouteTrack _RouteTrack;

		public SimisProvider SimisProvider { get { return _simisProvider; } }
		public string RoutePath { get { return _routePath; } }
		public FileFinder Files { get { return _files; } }
		public SimisFile TrackFile { get { return _trackFile; } } 

		internal Route(SimisProvider simisProvider, string routePath, FileFinder files, SimisFile trackFile) {
			_simisProvider = simisProvider;
			_routePath = routePath;
			_files = files;
			_trackFile = trackFile;
		}

		public Route(string trackFile, SimisProvider simisProvider) {
			_routePath = Path.GetDirectoryName(trackFile);
			_simisProvider = simisProvider;
			// We can find things relative to the following:
			// +-<msts>   <-- here
			//   +-Global   <-- here
			//   +-Routes
			//     +-<route>   <-- here
			//       +-Global   <-- here*
			// * Allowed for route-specific global files; this is a feature for Open Rails Train Simulator (ORTS).
			// Paths used to access files will usually contain 1 directory above, e.g. "activities\foo.act", to avoid
			// unexpected and undesired collisions between files in <msts>, <msts>\Global and <msts>\Routes\<route>.
			var mstsPath = Path.GetDirectoryName(Path.GetDirectoryName(_routePath));
			_files = new FileFinder(new string[] { _routePath, Path.Combine(_routePath, "Global"), mstsPath, Path.Combine(mstsPath, "Global") });

			_trackFile = new SimisFile(trackFile, _simisProvider);
		}

		protected override void SetArgument(string name, object value, ref Dictionary<string, object> arguments, ref DataTreeNode<Route>.TypeData typeData) {
			var trackFile = arguments.ContainsKey("TrackFile") ? (SimisFile)arguments["TrackFile"] : _trackFile;
			switch (name) {
				case "Name":
					arguments["TrackFile"] = trackFile.GetPath("Tree", "Tr_RouteFile", "Name", 0).Set(new SimisTreeNodeValueString("string", "", (string)value));
					break;
				case "Description":
					arguments["TrackFile"] = trackFile.GetPath("Tree", "Tr_RouteFile", "Description", 0).Set(new SimisTreeNodeValueString("string", "", ((string)value).Replace("\r\n", "\n")));
					break;
				default:
					base.SetArgument(name, value, ref arguments, ref typeData);
					break;
			}
		}

		public string Name {
			get {
				return _trackFile.Tree["Tr_RouteFile"]["Name"][0].ToValue<string>();
			}
		}

		public string Description {
			get {
				return _trackFile.Tree["Tr_RouteFile"]["Description"][0].ToValue<string>().Replace("\n", "\r\n");
			}
		}

		public string FileName {
			get {
				return _trackFile.Tree["Tr_RouteFile"]["FileName"][0].ToValue<string>();
			}
		}

		//public Route Set(string name, string description, string fileName) {
		//    var trackFileTree = TrackFile.Tree;
		//    if (name != null) {
		//        trackFileTree = trackFileTree.Path("Tr_RouteFile", "Name", 0).Apply(n => new SimisTreeNodeValueString("string", "", name)).Root;
		//    }
		//    if (description != null) {
		//        trackFileTree = trackFileTree.Path("Tr_RouteFile", "Description", 0).Apply(n => new SimisTreeNodeValueString("string", "", description)).Root;
		//    }
		//    if (fileName != null) {
		//        trackFileTree = trackFileTree.Path("Tr_RouteFile", "FileName", 0).Apply(n => new SimisTreeNodeValueString("string", "", fileName)).Root;
		//    }
		//    return new Route(SimisProvider, RoutePath, Files, new SimisFile(TrackFile.FileName, TrackFile.SimisFormat, TrackFile.StreamFormat, TrackFile.StreamCompressed, trackFileTree, SimisProvider));
		//}

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

		//public IEnumerable<Tile> Tiles {
		//    get {
		//        // TODO: Cache the tiles by name or something. We should not be creating a new set every time!
		//        return from tile in Directory.GetFiles(RoutePath + @"\Tiles", "*.t")
		//               select new Tile(Path.GetFileNameWithoutExtension(tile), this, SimisProvider);
		//    }
		//}
	}
}
