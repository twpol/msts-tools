//------------------------------------------------------------------------------
// Jgr.IO library, part of MSTS Editors & Tools (http://jgrmsts.codeplex.com/).
// License: Microsoft Public License (Ms-PL).
//------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.IO;

namespace Jgr.IO {
	public class FileFinder {
		public readonly IEnumerable<string> Paths;

		public FileFinder(IEnumerable<string> paths) {
			Paths = paths;
		}

		public string this[string fileName] {
			get {
				foreach (var path in Paths) {
					if (File.Exists(Path.Combine(path, fileName))) {
						return Path.Combine(path, fileName);
					}
				}
				throw new ArgumentException("File '" + fileName + "' cannot be found in any path.", "fileName");
			}
		}
	}
}
