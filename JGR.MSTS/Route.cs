//------------------------------------------------------------------------------
// Jgr.IO.Parser library, part of MSTS Editors & Tools (http://jgrmsts.codeplex.com/).
// License: Microsoft Public License (Ms-PL).
//------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Jgr.IO.Parser;
using System.IO;

namespace Jgr.Msts {
	public class Route {
		public string RootPath { get; private set; }
		SimisProvider SimisProvider;
		SimisFile TrackFile;

		public Route(string path, SimisProvider simisProvider) {
			RootPath = path;
			SimisProvider = simisProvider;

			var tracks = Directory.GetFiles(path, "*.trk", SearchOption.TopDirectoryOnly).Where(n => n.EndsWith(".trk", StringComparison.OrdinalIgnoreCase));
			if (tracks.Count() != 1) throw new ArgumentException("Path contains " + tracks.Count() + " .trk files; must be exactly 1.", "path");
			TrackFile = new SimisFile(tracks.First(), SimisProvider);
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
	}
}
