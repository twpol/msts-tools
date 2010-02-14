//------------------------------------------------------------------------------
// Jgr.Msts library, part of MSTS Editors & Tools (http://jgrmsts.codeplex.com/).
// License: Microsoft Public License (Ms-PL).
//------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Jgr.Msts {
	public class MSTSIGH {
		public readonly int Line;
		public readonly int Sample;
		public MSTSIGH(int line, int sample) {
			Line = line;
			Sample = sample;
		}
	}

	public class Tile {
		public static MSTSIGH Convert(string tileName) {
			var depthDown = new List<bool>();
			var depthRight = new List<bool>();
			foreach (var ch in tileName.Substring(1)) {
				switch (ch) {
					case '4':
					case '5':
					case '6':
					case '7':
					case '8':
					case '9':
					case 'a':
					case 'b':
						depthRight.Add(true);
						break;
					default:
						depthRight.Add(false);
						break;
				}
				switch (ch) {
					case '8':
					case '9':
					case 'a':
					case 'b':
					case 'c':
					case 'd':
					case 'e':
					case 'f':
						depthDown.Add(true);
						break;
					default:
						depthDown.Add(false);
						break;
				}
				switch (ch) {
					case '1':
					case '5':
					case '9':
					case 'd':
					case '2':
					case '6':
					case 'a':
					case 'e':
						depthRight.Add(true);
						break;
					default:
						depthRight.Add(false);
						break;
				}
				switch (ch) {
					case '2':
					case '6':
					case 'a':
					case 'e':
					case '3':
					case '7':
					case 'b':
					case 'f':
						depthDown.Add(true);
						break;
					default:
						depthDown.Add(false);
						break;
				}
			}
			if (tileName[0] == '-') {
				depthDown.RemoveAt(depthDown.Count - 1);
				depthRight.RemoveAt(depthRight.Count - 1);
			}

			int line = 0;
			int sample = 0;
			for (var i = 0; i < depthDown.Count; i++) {
				if (depthDown[i]) {
					line += (int)Math.Pow(2, 14 - i);
				}
				if (depthRight[i]) {
					sample += (int)Math.Pow(2, 14 - i);
				}
			}

			// We've calculated to top-left corner, but MSTS uses bottom-left.
			return new MSTSIGH(sample - 16384, 16384 - line - 1);
		}

		public static string Convert(MSTSIGH coordinates) {
			return "";
		}
	}
}
