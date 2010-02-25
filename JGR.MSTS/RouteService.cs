//------------------------------------------------------------------------------
// Jgr.Msts library, part of MSTS Editors & Tools (http://jgrmsts.codeplex.com/).
// License: Microsoft Public License (Ms-PL).
//------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Jgr.IO.Parser;

namespace Jgr.Msts {
	public class RouteService {
		public readonly string BasePath;
		public readonly SimisProvider SimisProvider;

		public RouteService(string basePath, SimisProvider simisProvider) {
			BasePath = basePath;
			SimisProvider = simisProvider;
		}

		public IEnumerable<Route> Routes {
			get {
				var filesLevel1 = Directory.GetFiles(BasePath, "*.trk", SearchOption.TopDirectoryOnly).Where(name => name.EndsWith(".trk", StringComparison.InvariantCultureIgnoreCase));
				if (filesLevel1.Count() == 1) {
					Route route = null;
					try {
						route = new Route(filesLevel1.First(), SimisProvider);
					} catch (FileException) {
					}
					if (route != null) {
						yield return route;
						yield break;
					}
				}
				foreach (var directory in Directory.GetDirectories(BasePath)) {
					var filesLevel2 = Directory.GetFiles(Path.Combine(BasePath, directory), "*.trk", SearchOption.TopDirectoryOnly).Where(name => name.EndsWith(".trk", StringComparison.InvariantCultureIgnoreCase));
					if (filesLevel2.Count() == 1) {
						Route route = null;
						try {
							route = new Route(filesLevel2.First(), SimisProvider);
						} catch (FileException) {
						}
						if (route != null) {
							yield return route;
						}
					}
				}
				foreach (var trackFile in Directory.GetFiles(BasePath, "*.trk", SearchOption.AllDirectories).Where(name => name.EndsWith(".trk", StringComparison.InvariantCultureIgnoreCase))) {
					Route route = null;
					try {
						route = new Route(trackFile, SimisProvider);
					} catch (FileException) {
					}
					if (route != null) {
						yield return route;
					}
				}
			}
		}
	}
}
