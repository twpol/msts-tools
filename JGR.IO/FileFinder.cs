//------------------------------------------------------------------------------
// Jgr.IO library, part of MSTS Editors & Tools (http://jgrmsts.codeplex.com/).
// License: New BSD License (BSD).
//------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.IO;

namespace Jgr.IO {
	/// <summary>
	/// Stores a list of file paths within which files are to be found.
	/// </summary>
	[Immutable]
	public class FileFinder {
		public IEnumerable<string> Paths { get; private set; }

		/// <summary>
		/// Constructs the <see cref="FileFinder"/> with a set of <paramref name="paths"/> to search.
		/// </summary>
		/// <param name="paths">The sequence of paths to search for files in.</param>
		public FileFinder(IEnumerable<string> paths) {
			Paths = paths;
		}

		/// <summary>
		/// Returns the first path found containing the <paramref name="fileName"/>.
		/// </summary>
		/// <param name="fileName">The file to find in the paths. May contain directory elements, including up-level.</param>
		/// <returns>The file path of the found file.</returns>
		/// <exception cref="ArgumentException">Thrown if the file is not found in any of the paths.</exception>
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
