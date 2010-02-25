//------------------------------------------------------------------------------
// Jgr.Msts library, part of MSTS Editors & Tools (http://jgrmsts.codeplex.com/).
// License: Microsoft Public License (Ms-PL).
//------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Jgr.IO.Parser;
using Jgr.IO;

namespace Jgr.Msts {
	public class TrackService {
		public readonly string RoutePath;
		public readonly string GlobalPath;
		public readonly SimisProvider SimisProvider;
		public readonly FileFinder Files;
		readonly SimisFile TSection;

		public TrackService(FileFinder files, SimisProvider simisProvider) {
			Files = files;
			SimisProvider = simisProvider;

			TSection = new SimisFile(Files[@"Global\tsection.dat"], SimisProvider);
			TSection.ReadFile();
		}
	}
}
