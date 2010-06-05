//------------------------------------------------------------------------------
// Jgr.Msts library, part of MSTS Editors & Tools (http://jgrmsts.codeplex.com/).
// License: New BSD License (BSD).
//------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Jgr.IO.Parser;

namespace Jgr.Msts {
	public class RouteService {
		readonly string _basePath;
		readonly SimisProvider _simisProvider;

		public string BasePath { get { return _basePath; } }
		public SimisProvider SimisProvider { get { return _simisProvider; } }

		public RouteService(string basePath, SimisProvider simisProvider) {
			_basePath = basePath;
			_simisProvider = simisProvider;
		}

		public IEnumerable<Route> Routes {
			get {
				var filesLevel1 = Directory.GetFiles(_basePath, "*.trk", SearchOption.TopDirectoryOnly).Where(name => name.EndsWith(".trk", StringComparison.InvariantCultureIgnoreCase));
				if (filesLevel1.Count() == 1) {
					Route route = null;
					try {
						route = new Route(filesLevel1.First(), _simisProvider);
					} catch (FileException) {
					}
					if (route != null) {
						yield return route;
						yield break;
					}
				}
				var path = _basePath;
				if (Directory.Exists(Path.Combine(path, "Routes"))) {
					path = Path.Combine(path, "Routes");
				}
				var found = false;
				foreach (var directory in Directory.GetDirectories(path)) {
					var filesLevel2 = Directory.GetFiles(directory, "*.trk", SearchOption.TopDirectoryOnly).Where(name => name.EndsWith(".trk", StringComparison.InvariantCultureIgnoreCase));
					if (filesLevel2.Count() == 1) {
						Route route = null;
						try {
							route = new Route(filesLevel2.First(), _simisProvider);
						} catch (FileException) {
						}
						if (route != null) {
							found = true;
							yield return route;
						}
					}
				}
				if (found) {
					yield break;
				}
				foreach (var trackFile in Directory.GetFiles(_basePath, "*.trk", SearchOption.AllDirectories).Where(name => name.EndsWith(".trk", StringComparison.InvariantCultureIgnoreCase))) {
					Route route = null;
					try {
						route = new Route(trackFile, _simisProvider);
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
