//------------------------------------------------------------------------------
// Jgr.Msts library, part of MSTS Editors & Tools (http://jgrmsts.codeplex.com/).
// License: Microsoft Public License (Ms-PL).
//------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace Jgr.Msts {
	public class MstsTile {
		public readonly int X;
		public readonly int Y;
		public MstsTile(int x, int y) {
			X = x;
			Y = y;
		}
		public override string ToString() {
			return "TILE{" + X + " " + Y + "}";
		}
	}

	public class Igh {
		public readonly double Line;
		public readonly double Sample;
		public Igh(double line, double sample) {
			Line = line;
			Sample = sample;
		}
		public override string ToString() {
			return "IGH{" + Line + " " + Sample + "}";
		}
	}

	public class LatLon {
		public readonly double Lat;
		public readonly double Lon;
		public LatLon(double lat, double lon) {
			Lat = lat;
			Lon = lon;
		}
		public override string ToString() {
			return "LL{" + Lat + " " + Lon + "}";
		}
	}

	public class Tile {
		// Tile Name -> MSTS Tile.
		public static MstsTile ConvertToMstsTile(string tileName) {
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

			return new MstsTile(sample - 16384, 16384 - line);
		}

		// MSTS Tile -> Tile Name.
		public static string ConvertToTileName(MstsTile coordinates) {
			return "";
		}

		// MSTS Tile -> IGH
		public static Igh ConvertToIgh(MstsTile coordinates, double tileX, double tileY) {
			Debug.Assert(tileY >= 0, "tileY is off the top");
			Debug.Assert(tileY <= 1, "tileY is off the bottom");
			Debug.Assert(tileX >= 0, "tileX is off the left");
			Debug.Assert(tileX <= 1, "tileX is off the right");
			return new Igh(2048 * (16384 - coordinates.Y - 1 + tileY), 2048 * (coordinates.X + 16384 + tileX));
		}

		// IGH -> MSTS Tile
		public static MstsTile ConvertToMstsTile(Igh coordinates) {
			return new MstsTile(0, 0);
		}

		// IGH -> Lat/Lon
		public static LatLon ConvertToLatLon(Igh coordinates) {
			// Line/Sample -> Latitude/Longitude Algorithm
			// Based on C code provided by the USGS, available at ftp://edcftp.cr.usgs.gov/pub/software/misc/gihll2ls.c.
			// By D. Steinwand, HSTX/EROS Data Center, June, 1993.

			const double radius = 6370997;
			const double imageLeft = -20015499.5;
			const double imageTop = 8673455.5;
			const double imageWidth = 40031000;
			const double imageHeight = 17347000;

			var lon_center = new double[] {
				-(double)100 / 180 * Math.PI, /* -100.0 degrees */
				-(double)100 / 180 * Math.PI, /* -100.0 degrees */
				+(double) 30 / 180 * Math.PI, /*   30.0 degrees */
				+(double) 30 / 180 * Math.PI, /*   30.0 degrees */
				-(double)160 / 180 * Math.PI, /* -160.0 degrees */
				-(double) 60 / 180 * Math.PI, /*  -60.0 degrees */
				-(double)160 / 180 * Math.PI, /* -160.0 degrees */
				-(double) 60 / 180 * Math.PI, /*  -60.0 degrees */
				+(double) 20 / 180 * Math.PI, /*   20.0 degrees */
				+(double)140 / 180 * Math.PI, /*  140.0 degrees */
				+(double) 20 / 180 * Math.PI, /*   20.0 degrees */
				+(double)140 / 180 * Math.PI, /*  140.0 degrees */
			};

			Debug.Assert(coordinates.Line >= 0, "line is off the top");
			Debug.Assert(coordinates.Line <= imageHeight, "line is off the bottom");
			Debug.Assert(coordinates.Sample >= 0, "line is off the left");
			Debug.Assert(coordinates.Sample <= imageWidth, "line is off the right");

			double y = (imageTop - coordinates.Line) / radius;
			double x = (imageLeft + coordinates.Sample) / radius;

			Debug.Assert(y >= -Math.PI / 2, "y is off the bottom");
			Debug.Assert(y <= +Math.PI / 2, "y is off the top");
			Debug.Assert(x >= -Math.PI, "x is off the left");
			Debug.Assert(x <= +Math.PI, "x is off the right");

			const double parallel41 = ((double)40 + (44 / 60) + (11.8 / 3600)) / 180 * Math.PI; // 40deg 44' 11.8"
			const double meridian20 = (double)20 / 180 * Math.PI;  // 20deg
			const double meridian40 = (double)40 / 180 * Math.PI;  // 40deg
			const double meridian80 = (double)80 / 180 * Math.PI;  // 80deg
			const double meridian100 = (double)100 / 180 * Math.PI; // 100deg
			int region = -1;
			if (y >= parallel41) {                 /* If on or above 40 44' 11.8" */
				if (x <= -meridian40) {            /* If to the left of -40 */
					region = 0;
				} else {
					region = 2;
				}
			} else if (y >= 0.0) {                 /* Between 0.0 and 40 44' 11.8" */
				if (x <= -meridian40) {            /* If to the left of -40 */
					region = 1;
				} else {
					region = 3;
				}
			} else if (y >= -parallel41) {         /* Between 0.0 & -40 44' 11.8" */
				if (x <= -meridian100) {           /* If between -180 and -100 */
					region = 4;
				} else if (x <= -meridian20) {     /* If between -100 and -20 */
					region = 5;
				} else if (x <= meridian80) {      /* If between -20 and 80 */
					region = 8;
				} else {                           /* If between 80 and 180 */
					region = 9;
				}
			} else {                               /* Below -40 44' 11.8" */
				if (x <= -meridian100) {           /* If between -180 and -100 */
					region = 6;
				} else if (x <= -meridian20) {     /* If between -100 and -20 */
					region = 7;
				} else if (x <= meridian80) {      /* If between -20 and 80 */
					region = 10;
				} else {                           /* If between 80 and 180 */
					region = 11;
				}
			}
			x = x - lon_center[region];

			double lat = 0;
			double lon = 0;
			if ((region == 1) || (region == 3) || (region == 4) || (region == 5) || (region == 8) || (region == 9)) {
				lat = y;
				if (Math.Abs(lat) > Math.PI / 2) {
					// giherror("Input data error", "Goode-inverse");
					return null;
				}
				double temp = Math.Abs(lat) - Math.PI / 2;
				if (Math.Abs(temp) > double.Epsilon) {
					temp = lon_center[region] + x / Math.Cos(lat);
					if (Math.Abs(temp) >= Math.PI) {
						temp = temp - Math.Sign(temp) * 2 * Math.PI;
					}
					lon = temp;
				} else {
					lon = lon_center[region];
				}
			} else {
				double arg = (y + 0.0528035274542 * Math.Sign(y)) / 1.4142135623731;
				if (Math.Abs(arg) > 1.0) {
					return null;
				}
				double theta = Math.Asin(arg);
				lon = lon_center[region] + (x / (0.900316316158 * Math.Cos(theta)));
				if (lon < -Math.PI) {
					return null;
				}
				arg = (2.0 * theta + Math.Sin(2.0 * theta)) / Math.PI;
				if (Math.Abs(arg) > 1.0) {
					return null;
				}
				lat = Math.Asin(arg);
			}

			///////////////////////////////////////////////////////////////////

			return new LatLon(lat * 180 / Math.PI, lon * 180 / Math.PI);
		}

		// Lat/Lon -> MSTS IGH
		public static Igh ConvertToIgh(LatLon coordinates) {
			return new Igh(0, 0);
		}
	}
}
