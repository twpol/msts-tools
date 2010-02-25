//------------------------------------------------------------------------------
// Jgr.Msts library, part of MSTS Editors & Tools (http://jgrmsts.codeplex.com/).
// License: Microsoft Public License (Ms-PL).
//------------------------------------------------------------------------------

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Jgr.IO.Parser;
using Jgr.IO;

namespace Jgr.Msts {
	public class Route {
		public readonly string RoutePath;
		public readonly SimisProvider SimisProvider;
		public readonly FileFinder Files;
		public readonly TrackService TrackService;
		readonly SimisFile TrackFile;

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
			TrackService = new TrackService(Files, SimisProvider);

			TrackFile = new SimisFile(trackFile, SimisProvider);
			TrackFile.ReadFile();
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

		public IEnumerable<string> Tiles {
			get {
				return from t in Directory.GetFiles(RoutePath + @"\Tiles", "*.t")
					   select t.Substring(t.LastIndexOf('\\') + 1).TrimEnd('.', 't');
			}
		}
	}
}
