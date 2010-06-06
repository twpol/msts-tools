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
				default:
					base.SetArgument(name, value, ref arguments, ref typeData);
					break;
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

		public string FileName {
			get {
				return TrackFile.Tree["Tr_RouteFile"]["FileName"][0].ToValue<string>();
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
