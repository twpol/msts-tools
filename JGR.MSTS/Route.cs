﻿//------------------------------------------------------------------------------
// Jgr.Msts library, part of MSTS Editors & Tools (http://jgrmsts.codeplex.com/).
// License: Microsoft Public License (Ms-PL).
//------------------------------------------------------------------------------

using System.Collections.Generic;
using System.IO;
using System.Linq;
using Jgr.IO;
using Jgr.IO.Parser;

namespace Jgr.Msts {
	public class Route {
		public readonly SimisProvider SimisProvider;
		public readonly string RoutePath;
		public readonly FileFinder Files;
		TrackService _TrackService;
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

		public string FileName {
			get {
				return TrackFile.Tree["Tr_RouteFile"]["FileName"][0].ToValue<string>();
			}
		}

		public TrackService TrackService {
			get {
				if (_TrackService != null) {
					return _TrackService;
				}
				_TrackService = new TrackService(Files, SimisProvider);
				return _TrackService;
			}
		}

		public IEnumerable<Tile> Tiles {
			get {
				// TODO: Cache the tiles by name or something. We should not be creating a new set every time!
				return from tile in Directory.GetFiles(RoutePath + @"\Tiles", "*.t")
					   select new Tile(Path.GetFileNameWithoutExtension(tile), this, SimisProvider);
			}
		}
	}
}
