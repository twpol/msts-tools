//------------------------------------------------------------------------------
// Jgr.Msts library, part of MSTS Editors & Tools (http://jgrmsts.codeplex.com/).
// License: New BSD License (BSD).
//------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;

namespace Jgr.Msts {
	[Immutable]
	public class TileCoordinate {
		public readonly int X;
		public readonly int Z;
		public readonly int Size;
		public TileCoordinate(int x, int z)
			: this(x, z, 1) {
		}
		public TileCoordinate(int x, int z, int size) {
			X = x;
			Z = z;
			Size = size;
		}
		public override string ToString() {
			return "TILE{" + X + " " + Z + " " + Size + "}";
		}
	}

	[Immutable]
	public class IghCoordinate {
		public readonly double Line;
		public readonly double Sample;
		public IghCoordinate(double line, double sample) {
			Line = line;
			Sample = sample;
		}
		public override string ToString() {
			return "IGH{" + Line + " " + Sample + "}";
		}
	}

	[Immutable]
	public class LatitudeLongitudeCoordinate {
		public readonly double Latitude;
		public readonly double Longitude;
		public LatitudeLongitudeCoordinate(double latitude, double longitude) {
			Latitude = latitude;
			Longitude = longitude;
		}
		public override string ToString() {
			return "LL{" + Latitude + " " + Longitude + "}";
		}
	}

	public static class Coordinates {
		// Tile Name -> MSTS Tile.
		public static TileCoordinate ConvertToTile(string tileName) {
			var depthRight = new List<bool>();
			var depthDown = new List<bool>();
			// 0 1   4 5
			// 3 2   7 6
			//
			// c d   8 9
			// f e   b a
			foreach (var ch in tileName.Substring(1)) {
				var value = int.Parse(ch.ToString(), NumberStyles.HexNumber, CultureInfo.InvariantCulture);
				depthRight.Add((value % 12) >= 4);
				depthDown.Add(value >= 8);
				depthRight.Add(((value % 4) % 3) >= 1);
				depthDown.Add((value % 4) >= 2);
			}
			if (tileName[0] == '-') {
				depthRight.RemoveAt(depthRight.Count - 1);
				depthDown.RemoveAt(depthDown.Count - 1);
			}

			int sample = 0;
			int line = 0;
			for (var i = 0; i < depthDown.Count; i++) {
				if (depthRight[i]) {
					sample += (int)Math.Pow(2, 14 - i);
				}
				if (depthDown[i]) {
					line += (int)Math.Pow(2, 14 - i);
				}
			}

			return new TileCoordinate(sample - 16384, 16384 - line - 1, (int)Math.Pow(2, 15 - depthDown.Count));
		}

		// MSTS Tile -> Tile Name.
		public static string ConvertToTileName(TileCoordinate coordinates) {
			var sample = coordinates.X + 16384;
			var line = 16384 - coordinates.Z - 1;
			var depthRight = new List<bool>();
			var depthDown = new List<bool>();
			for (var i = 14; (i >= 0) && (Math.Pow(2, i) >= coordinates.Size); i--) {
				depthRight.Add(sample >= Math.Pow(2, i));
				depthDown.Add(line >= Math.Pow(2, i));
				sample %= (int)Math.Pow(2, i);
				line %= (int)Math.Pow(2, i);
			}

			var tileName = depthDown.Count % 2 == 0 ? "_" : "-";
			if (depthDown.Count % 2 != 0) {
				depthRight.Add(false);
				depthDown.Add(false);
			}
			for (var i = 0; i < depthDown.Count; i += 2) {
				var c = (depthRight[i] ? depthDown[i] ? 8 : 4 : depthDown[i] ? 12 : 0) + (depthRight[i + 1] ? depthDown[i + 1] ? 2 : 1 : depthDown[i + 1] ? 3 : 0);
				tileName += c.ToString("x", CultureInfo.InvariantCulture);
			}

			return tileName;
		}

		// MSTS Tile -> IGH
		public static IghCoordinate ConvertToIgh(TileCoordinate coordinates, double tileX, double tileZ) {
			Debug.Assert(tileZ >= 0, "tileZ is off the top");
			Debug.Assert(tileZ <= 1, "tileZ is off the bottom");
			Debug.Assert(tileX >= 0, "tileX is off the left");
			Debug.Assert(tileX <= 1, "tileX is off the right");
			return new IghCoordinate(2048 * (16384 - coordinates.Z - 1 + tileZ), 2048 * (coordinates.X + 16384 + tileX));
		}

		// IGH -> MSTS Tile
		public static TileCoordinate ConvertToTile(IghCoordinate coordinates, out double tileX, out double tileZ) {
			var x = coordinates.Sample / 2048;
			var z = coordinates.Line / 2048;
			tileX = x - Math.Floor(x);
			tileZ = z - Math.Floor(z);
			Debug.Assert(tileZ >= 0, "tileZ is off the top");
			Debug.Assert(tileZ <= 1, "tileZ is off the bottom");
			Debug.Assert(tileX >= 0, "tileX is off the left");
			Debug.Assert(tileX <= 1, "tileX is off the right");
			return new TileCoordinate((int)Math.Floor(x) - 16384, 16384 - (int)Math.Floor(z) - 1);
		}

		public static TileCoordinate ConvertToTile(IghCoordinate coordinates) {
			double tileX;
			double tileZ;
			return ConvertToTile(coordinates, out tileX, out tileZ);
		}

		// Original calculations:
		//   r =   6370997
		//   l = -20015500   w = 40031000
		//   t =   8673500   h = 17347000
		const double IghRadius = 6370997;
		const double IghImageLeft = -20015000;
		const double IghImageTop = 8673000;
		const double IghImageWidth = -2 * IghImageLeft;
		const double IghImageHeight = 2 * IghImageTop;
		static readonly double[] IghLongitudeCenter = new double[] {
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
		const double IghParallel41 = (40d + (44d / 60) + (11.8d / 3600)) / 180 * Math.PI; // 40deg 44' 11.8"
		const double IghMeridian20 = 20d / 180 * Math.PI;  // 20deg
		const double IghMeridian40 = 40d / 180 * Math.PI;  // 40deg
		const double IghMeridian80 = 80d / 180 * Math.PI;  // 80deg
		const double IghMeridian100 = 100d / 180 * Math.PI; // 100deg

		// IGH -> Lat/Lon
		public static LatitudeLongitudeCoordinate ConvertToLatLon(IghCoordinate coordinates) {
			// Line/Sample -> Latitude/Longitude Algorithm
			// Based on C code provided by the USGS, available at ftp://edcftp.cr.usgs.gov/pub/software/misc/gihll2ls.c.
			// By D. Steinwand, HSTX/EROS Data Center, June, 1993.

			Debug.Assert(coordinates.Line >= 0, "line is off the top");
			Debug.Assert(coordinates.Line <= IghImageHeight, "line is off the bottom");
			Debug.Assert(coordinates.Sample >= 0, "line is off the left");
			Debug.Assert(coordinates.Sample <= IghImageWidth, "line is off the right");

			double y = (IghImageTop - coordinates.Line) / IghRadius;
			double x = (IghImageLeft + coordinates.Sample) / IghRadius;

			Debug.Assert(y >= -Math.PI / 2, "y is off the bottom");
			Debug.Assert(y <= +Math.PI / 2, "y is off the top");
			Debug.Assert(x >= -Math.PI, "x is off the left");
			Debug.Assert(x <= +Math.PI, "x is off the right");

			int region = -1;
			if (y >= IghParallel41) {                 /* If on or above 40 44' 11.8" */
				if (x <= -IghMeridian40) {            /* If to the left of -40 */
					region = 0;
				} else {
					region = 2;
				}
			} else if (y >= 0.0) {                 /* Between 0.0 and 40 44' 11.8" */
				if (x <= -IghMeridian40) {            /* If to the left of -40 */
					region = 1;
				} else {
					region = 3;
				}
			} else if (y >= -IghParallel41) {         /* Between 0.0 & -40 44' 11.8" */
				if (x <= -IghMeridian100) {           /* If between -180 and -100 */
					region = 4;
				} else if (x <= -IghMeridian20) {     /* If between -100 and -20 */
					region = 5;
				} else if (x <= IghMeridian80) {      /* If between -20 and 80 */
					region = 8;
				} else {                           /* If between 80 and 180 */
					region = 9;
				}
			} else {                               /* Below -40 44' 11.8" */
				if (x <= -IghMeridian100) {           /* If between -180 and -100 */
					region = 6;
				} else if (x <= -IghMeridian20) {     /* If between -100 and -20 */
					region = 7;
				} else if (x <= IghMeridian80) {      /* If between -20 and 80 */
					region = 10;
				} else {                           /* If between 80 and 180 */
					region = 11;
				}
			}
			x = x - IghLongitudeCenter[region];

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
					temp = IghLongitudeCenter[region] + x / Math.Cos(lat);
					lon = adjust_lon(temp);
				} else {
					lon = IghLongitudeCenter[region];
				}
			} else {
				double arg = (y + 0.0528035274542 * Math.Sign(y)) / 1.4142135623731;
				if (Math.Abs(arg) > 1.0) {
					return null;
				}
				double theta = Math.Asin(arg);
				lon = IghLongitudeCenter[region] + (x / (0.900316316158 * Math.Cos(theta)));
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

			return new LatitudeLongitudeCoordinate(lat * 180 / Math.PI, lon * 180 / Math.PI);
		}

		// Lat/Lon -> MSTS IGH
		public static IghCoordinate ConvertToIgh(LatitudeLongitudeCoordinate coordinates) {
			// Latitude/Longitude -> Line/Sample Algorithm
			// Based on C code provided by the USGS, available at ftp://edcftp.cr.usgs.gov/pub/software/misc/gihll2ls.c.
			// By D. Steinwand, HSTX/EROS Data Center, June, 1993.

			Debug.Assert(coordinates.Latitude >= -90, "latitude is off the bottom");
			Debug.Assert(coordinates.Latitude <= 90, "latitude is off the top");
			Debug.Assert(coordinates.Longitude >= -180, "longitude is off the left");
			Debug.Assert(coordinates.Longitude <= 180, "longitude is off the right");

			double lat = coordinates.Latitude * Math.PI / 180;
			double lon = coordinates.Longitude * Math.PI / 180;

			int region = -1;
			if (lat >= IghParallel41) {               /* If on or above 40 44' 11.8" */
				if (lon <= -IghMeridian40) {          /* If to the left of -40 */
					region = 0;
				} else {
					region = 2;
				}
			} else if (lat >= 0.0) {               /* Between 0.0 and 40 44' 11.8" */
				if (lon <= -IghMeridian40) {          /* If to the left of -40 */
					region = 1;
				} else {
					region = 3;
				}
			} else if (lat >= -IghParallel41) {       /* Between 0.0 & -40 44' 11.8" */
				if (lon <= -IghMeridian100) {         /* If between -180 and -100 */
					region = 4;
				} else if (lon <= -IghMeridian20) {   /* If between -100 and -20 */
					region = 5;
				} else if (lon <= IghMeridian80) {    /* If between -20 and 80 */
					region = 8;
				} else {                           /* If between 80 and 180 */
					region = 9;
				}
			} else {                               /* Below -40 44' 11.8" */
				if (lon <= -IghMeridian100) {         /* If between -180 and -100 */
					region = 6;
				} else if (lon <= -IghMeridian20) {   /* If between -100 and -20 */
					region = 7;
				} else if (lon <= IghMeridian80) {    /* If between -20 and 80 */
					region = 10;
				} else {                           /* If between 80 and 180 */
					region = 11;
				}
			}

			double y = 0;
			double x = 0;
			if ((region == 1) || (region == 3) || (region == 4) || (region == 5) || (region == 8) || (region == 9)) {
				var delta_lon = adjust_lon(lon - IghLongitudeCenter[region]);
				y = lat;
				x = IghLongitudeCenter[region] + delta_lon * Math.Cos(lat);
			} else {
				var delta_lon = adjust_lon(lon - IghLongitudeCenter[region]);
				var theta = lat;
				var constant = Math.PI * Math.Sin(lat);

				/* Iterate using the Newton-Raphson method to find theta
				  -----------------------------------------------------*/
				for (var i = 0; ; i++) {
					var delta_theta = -(theta + Math.Sin(theta) - constant) / (1.0 + Math.Cos(theta));
					theta += delta_theta;
					if (Math.Abs(delta_theta) < 0.00000000001) break;
					if (i >= 30) {
						//giherror("Iteration failed to converge", "Goode-forward");
						return null;
					}
				}
				theta /= 2.0;
				y = 1.4142135623731 * Math.Sin(theta) - 0.0528035274542 * Math.Sign(lat);
				x = IghLongitudeCenter[region] + 0.900316316158 * delta_lon * Math.Cos(theta);
			}

			Debug.Assert(y >= -Math.PI / 2, "y is off the bottom");
			Debug.Assert(y <= +Math.PI / 2, "y is off the top");
			Debug.Assert(x >= -Math.PI, "x is off the left");
			Debug.Assert(x <= +Math.PI, "x is off the right");

			var igh = new IghCoordinate(IghImageTop - y * IghRadius, x * IghRadius - IghImageLeft);

			Debug.Assert(igh.Line >= 0, "line is off the top");
			Debug.Assert(igh.Line <= IghImageHeight, "line is off the bottom");
			Debug.Assert(igh.Sample >= 0, "line is off the left");
			Debug.Assert(igh.Sample <= IghImageWidth, "line is off the right");

			return igh;
		}

		private static double adjust_lon(double temp) {
			if (Math.Abs(temp) >= Math.PI) {
				return temp - Math.Sign(temp) * 2 * Math.PI;
			}
			return temp;
		}
	}
}
