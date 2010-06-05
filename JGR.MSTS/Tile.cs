//------------------------------------------------------------------------------
// Jgr.Msts library, part of MSTS Editors & Tools (http://jgrmsts.codeplex.com/).
// License: New BSD License (BSD).
//------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using Jgr.IO.Parser;
using System.Globalization;

namespace Jgr.Msts {
	[Immutable]
	class TileObject {
		readonly double _x;
		readonly double _y;
		readonly double _z;

		public double X { get { return _x; } }
		public double Y { get { return _y; } }
		public double Z { get { return _z; } }

		protected TileObject(double x, double y, double z) {
			_x = x;
			_y = y;
			_z = z;
		}
	}

	[Immutable]
	class TileLabeledObject : TileObject {
		readonly string _label;

		public string Label { get { return _label; } }

		protected TileLabeledObject(double x, double y, double z, string label)
			: base(x, y, z) {
			_label = label;
		}
	}

	[Immutable]
	class TileTrackSection : TileLabeledObject {
		readonly double _dw;
		readonly double _dx;
		readonly double _dy;
		readonly double _dz;
		readonly TrackShape _track;

		public double DW { get { return _dw; } }
		public double DX { get { return _dx; } }
		public double DY { get { return _dy; } }
		public double DZ { get { return _dz; } }
		public TrackShape Track { get { return _track; } } 

		protected TileTrackSection(double x, double y, double z, string label, double dw, double dx, double dy, double dz, TrackShape track)
			: base(x, y, z, label) {
			_dw = dw;
			_dx = dx;
			_dy = dy;
			_dz = dz;
			_track = track;
		}
	}

	[Immutable]
	class TileTrackNode : TileLabeledObject {
		readonly bool _isRoad;
		readonly uint _id;
		readonly List<TileObject> _vectors;

		public bool IsRoad { get { return _isRoad; } }
		public uint ID { get { return _id; } }
		public List<TileObject> Vectors { get { return _vectors; } } 
		
		protected TileTrackNode(double x, double y, double z, string label, bool isRoad, uint id, List<TileObject> vectors)
			: base(x, y, z, label) {
			_isRoad = isRoad;
			_id = id;
			_vectors = vectors;
		}
	}

	[Immutable]
	class TilePlatform : TileLabeledObject {
		TilePlatform(double x, double y, double z, string label)
			: base(x, y, z, label) {
		}
	}

	[Immutable]
	class TileSiding : TileLabeledObject {
		TileSiding(double x, double y, double z, string label)
			: base(x, y, z, label) {
		}
	}

	[Immutable]
	class TileSignal : TileLabeledObject {
		// TODO: Need an orientation for a signal!
		TileSignal(double x, double y, double z, string label)
			: base(x, y, z, label) {
		}
	}

	[Immutable]
	class TileMarker : TileLabeledObject {
		internal TileMarker(double x, double y, double z, string label)
			: base(x, y, z, label) {
		}
	}

	public enum TileLayer {
		Tiles,
		Terrain,
		Roads,
		Track,
		Markers,
		Platforms,
		PlatformNames,
		Sidings,
		SidingNames,
		Signals,
		Mileposts,
		FuelPoints,
	}

	public class Tile {
		Route _route;
		readonly SimisProvider _simisProvider;
		readonly string _tileName;
		readonly TileCoordinate _tileCoordinate;
		Image _terrainImage;
		List<TileTrackSection> _trackSections;
		List<TileTrackNode> _trackNodes;
		List<TilePlatform> _platforms;
		List<TileSiding> _sidings;
		List<TileSignal> _signals;
		List<TileMarker> _markers;

		public Route Route { get { return _route; } set { _route = value; } }
		public SimisProvider SimisProvider { get { return _simisProvider; } }
		public string TileName { get { return _tileName; } }
		public TileCoordinate TileCoordinate { get { return _tileCoordinate; } } 

		public Tile(string tileName, Route route, SimisProvider simisProvider) {
			_tileName = tileName;
			_route = route;
			_simisProvider = simisProvider;
			_tileCoordinate = Coordinates.ConvertToTile(_tileName);
			_trackSections = new List<TileTrackSection>();
			_trackNodes = new List<TileTrackNode>();
			_platforms = new List<TilePlatform>();
			_sidings = new List<TileSiding>();
			_signals = new List<TileSignal>();
			_markers = new List<TileMarker>();
		}

		public void PrepareLayer(TileLayer layer) {
			switch (layer) {
				case TileLayer.Tiles:
					break;
				case TileLayer.Terrain:
					string tileElevation = String.Format(CultureInfo.InvariantCulture, @"{0}\Tiles\{1}_y.raw", _route.RoutePath, _tileName);
					string tileShadow = String.Format(CultureInfo.InvariantCulture, @"{0}\Tiles\{1}_n.raw", _route.RoutePath, _tileName);
					string tileUnknown = String.Format(CultureInfo.InvariantCulture, @"{0}\Tiles\{1}_e.raw", _route.RoutePath, _tileName);
					string tileFile = String.Format(CultureInfo.InvariantCulture, @"{0}\Tiles\{1}.t", _route.RoutePath, _tileName);

					var image = new Bitmap(256, 256);
					using (var g = Graphics.FromImage(image)) {
						var terrainWidth = 256;
						var terrainHeight = 256;
						var terrainFloor = 0f;
						var terrainScale = 1f;
						try {
							var tile = new UndoRedoSimisFile(tileFile, _simisProvider);
							tile.Read();
							terrainWidth = terrainHeight = (int)tile.Tree["terrain"]["terrain_samples"]["terrain_nsamples"][0].ToValue<uint>();
							Debug.Assert(terrainWidth == 256);
							Debug.Assert(terrainHeight == 256);
							if (tile.Tree["terrain"]["terrain_samples"].Contains("terrain_sample_rotation")) {
								Debug.Assert(tile.Tree["terrain"]["terrain_samples"]["terrain_sample_rotation"][0].ToValue<float>() == 0);
							}
							terrainFloor = tile.Tree["terrain"]["terrain_samples"]["terrain_sample_floor"][0].ToValue<float>();
							terrainScale = tile.Tree["terrain"]["terrain_samples"]["terrain_sample_scale"][0].ToValue<float>();
							var terrain_sample_size = tile.Tree["terrain"]["terrain_samples"]["terrain_sample_size"][0].ToValue<float>();
							Debug.Assert(terrain_sample_size == 8 || terrain_sample_size == 16 || terrain_sample_size == 32);
						} catch (Exception e) {
							g.DrawString(e.ToString(), SystemFonts.CaptionFont, Brushes.Black, 0, 0);
							_terrainImage = image;
							return;
						}

						var terrainData = new uint[terrainWidth * terrainHeight];
						using (var streamElevation = new FileStream(tileElevation, FileMode.Open, FileAccess.Read)) {
							var readerElevation = new BinaryReader(streamElevation);
							try {
								for (var i = 0; i < terrainWidth * terrainHeight; i++) {
									var value = (int)(1024 + 32 + (terrainFloor + (double)readerElevation.ReadUInt16() * terrainScale) / 4) % 512;
									terrainData[i] = (uint)(value < 256 ? value * 0x00000100 : (value - 256) * 0x00010001 + 0x0000FF00);
								}
							} catch (EndOfStreamException) {
							}
						}
						unsafe {
							fixed (uint* ptr = terrainData) {
								var terrainImage = new Bitmap(terrainWidth, terrainHeight, 4 * terrainWidth, PixelFormat.Format32bppRgb, new IntPtr(ptr));
								g.DrawImage(terrainImage, 0, 0, image.Width, image.Height);
							}
						}

						//g.DrawString(TileName, SystemFonts.CaptionFont, Brushes.White, 0, image.Height - 4 * 15);
						//g.DrawString(TileCoordinate.ToString(), SystemFonts.CaptionFont, Brushes.White, 0, image.Height - 3 * 15);
						//g.DrawString(Coordinates.ConvertToIgh(TileCoordinate, 0, 1).ToString(), SystemFonts.CaptionFont, Brushes.White, 0, image.Height - 2 * 15);
						//g.DrawString(Coordinates.ConvertToLatLon(Coordinates.ConvertToIgh(TileCoordinate, 0, 1)).ToString(), SystemFonts.CaptionFont, Brushes.White, 0, image.Height - 1 * 15);
					}

					_terrainImage = image;
					break;
				case TileLayer.Roads:
					// TODO: Roads and track use the same basic data, can we split anything out here?
					break;
				case TileLayer.Track:
					//foreach (var trackVectors in Route.Track.TrackVectors.Values) {
					//    if (trackVectors.Vectors.Any(v => v.TileX == TileCoordinate.X && v.TileZ == TileCoordinate.Z)) {
					//        var sections = new List<TileObject>(from vector in trackVectors.Vectors.Union(new RouteTrackVector[] { Route.Track.TrackNodes[trackVectors.PinEnd] })
					//                                                select new TileObject() { X = vector.X + 2048 * (vector.TileX - TileCoordinate.X), Y = vector.Y, Z = vector.Z + 2048 * (vector.TileZ - TileCoordinate.Z) });
					//        TrackNodes.Add(new TileTrackNode() {
					//            ID = trackVectors.ID, IsRoad = false, Label = "", Vectors = sections,
					//            X = sections[0].X, Y = sections[0].Y, Z = sections[0].Z
					//        });
					//    }
					//}
					break;
				case TileLayer.Markers:
					string markersFile = String.Format(CultureInfo.InvariantCulture, @"{0}\{1}.mkr", _route.RoutePath, _route.FileName);

					if (File.Exists(markersFile)) {
						var markers = new UndoRedoSimisFile(markersFile, _simisProvider);
						try {
							markers.Read();
						} catch (FileException) {
							return;
						}
						foreach (var marker in markers.Tree.Where(n => n.Type == "Marker")) {
							var tileX = 0d;
							var tileZ = 0d;
							var tileCoordinate = Coordinates.ConvertToTile(Coordinates.ConvertToIgh(new LatitudeLongitudeCoordinate(marker[1].ToValue<float>(), marker[0].ToValue<float>())), out tileX, out tileZ);
							if ((tileCoordinate.X == _tileCoordinate.X) && (tileCoordinate.Z == _tileCoordinate.Z)) {
								_markers.Add(new TileMarker(tileX, 0, tileZ, marker[2].ToValue<string>()));
							}
						}
					}
					break;
				case TileLayer.Platforms:
					break;
				case TileLayer.PlatformNames:
					break;
				case TileLayer.Sidings:
					break;
				case TileLayer.SidingNames:
					break;
				case TileLayer.Signals:
					break;
				case TileLayer.Mileposts:
					break;
				case TileLayer.FuelPoints:
					break;
			}
		}

		public void DrawLayer(TileLayer layer, Graphics graphics, float x, float y, float w, float h) {
			switch (layer) {
				case TileLayer.Tiles:
					graphics.DrawRectangle(Pens.LightGray, x, y, w, h);
					break;
				case TileLayer.Terrain:
					if (_terrainImage != null) {
						graphics.DrawImage(_terrainImage, x, y, w, h);
					}
					break;
				case TileLayer.Roads:
				case TileLayer.Track:
					foreach (var trackNode in _trackNodes) {
						if (layer != (trackNode.IsRoad ? TileLayer.Roads : TileLayer.Track)) {
							continue;
						}
						var brush = trackNode.IsRoad ? Brushes.Gray : Brushes.Black;
						//var col = (int)(64 * (trackNode.ID % 4));
						//var brush = new SolidBrush(Color.FromArgb(col, col, col));
						var pen = new Pen(brush);

						if (!String.IsNullOrEmpty(trackNode.Label)) {
							graphics.DrawString(trackNode.Label, SystemFonts.CaptionFont, brush,
								(int)(x + w * (0 + (trackNode.X + 1024) / 2048)),
								(int)(y + h * (1 - (trackNode.Z + 1024) / 2048)));
						}
						TileObject previousLocation = trackNode;
						//graphics.FillEllipse(brush,
						//    (float)(x + w * (1024 + trackNode.X) / 2048) - 1,
						//    (float)(y + h * (1024 - trackNode.Z) / 2048) - 1,
						//    2,
						//    2);

						foreach (var section in trackNode.Vectors) {
							graphics.DrawLine(pen,
								(float)(x + w * (1024 + previousLocation.X) / 2048),
								(float)(y + h * (1024 - previousLocation.Z) / 2048),
								(float)(x + w * (1024 + section.X) / 2048),
								(float)(y + h * (1024 - section.Z) / 2048));
							//graphics.FillEllipse(brush,
							//    (float)(x + w * (1024 + section.X) / 2048) - 1,
							//    (float)(y + h * (1024 - section.Z) / 2048) - 1,
							//    2,
							//    2);

							previousLocation = section;
						}
					}
					//foreach (var trackSection in TrackSections) {
					//    if (!String.IsNullOrEmpty(trackSection.Label)) {
					//        graphics.DrawString(trackSection.Label, SystemFonts.CaptionFont, Brushes.Black,
					//            (int)(x + w * (0 + (trackSection.X + 1024) / 2048)),
					//            (int)(y + h * (1 - (trackSection.Z + 1024) / 2048)));
					//    }
					//    if (trackSection.Track == null) {
					//        continue;
					//    }
					//    if (layer != (trackSection.Track.IsRoadShape ? TileLayer.Roads : TileLayer.Track)) {
					//        continue;
					//    }

					//    var needPoints = trackSection.Track.MainRoute > -1;
					//    foreach (var path in trackSection.Track.Paths) {
					//        // Rotation matrix for quaternion (a, b, c, d).
					//        //   [a^2+b^2-c^2-d^2  2bc-2ad          2bd+2ac        ]
					//        //   [2bc+2ad          a^2-b^2+c^2-d^2  2cd-2ab        ]
					//        //   [2bd-2ac          2cd+2ab          a^2-b^2-c^2+d^2]
					//        var rxx = trackSection.DX * trackSection.DX + trackSection.DY * trackSection.DY - trackSection.DZ * trackSection.DZ - trackSection.DW * trackSection.DW;
					//        var rxz = 2 * trackSection.DY * trackSection.DW - 2 * trackSection.DX * trackSection.DZ;
					//        var rzx = 2 * trackSection.DY * trackSection.DW + 2 * trackSection.DX * trackSection.DZ;
					//        var rzz = trackSection.DX * trackSection.DX - trackSection.DY * trackSection.DY - trackSection.DZ * trackSection.DZ + trackSection.DW * trackSection.DW;
					//        {
					//            // [ C  0  S] [xx yx zx] [ xxC+xzS  yxC+yzS  zxC+zzS]
					//            // [ 0  1  0].[xy yy zy]=[ xy       yy       zy     ]
					//            // [-S  0  C] [xz yz zz] [-xxS+xzC -yxS+yzC -zxS+zzC]
					//            var angle = path.Rotation * Math.PI / 180;
					//            var rxx2 = rxx * Math.Cos(angle) + rxz * Math.Sin(angle);
					//            var rxz2 = rxz * Math.Cos(angle) - rxx * Math.Sin(angle);
					//            var rzx2 = rzx * Math.Cos(angle) + rzz * Math.Sin(angle);
					//            var rzz2 = rzz * Math.Cos(angle) - rzx * Math.Sin(angle);
					//            rxx = rxx2; rxz = rxz2; rzx = rzx2; rzz = rzz2;
					//        }

					//        var startX = trackSection.X - rxx * path.X - rzx * path.Z;
					//        var startZ = trackSection.Z - rxz * path.X - rzz * path.Z;

					//        if (needPoints) {
					//            graphics.FillEllipse(Brushes.Black,
					//                (float)(x + w * (1024 + startX) / 2048) - 3,
					//                (float)(y + h * (1024 - startZ) / 2048) - 3,
					//                6,
					//                6);
					//            needPoints = false;
					//        }

					//        foreach (var section in path.Sections) {
					//            if (section.IsCurve) {
					//                var angle = section.Angle * Math.PI / 180;
					//                // Rotate 90 degrees left or right base ioon +ve or -ve angle (-ve angle = left).
					//                var curveCenterX = startX - rxx * section.Radius * Math.Sign(section.Angle);
					//                var curveCenterZ = startZ - rxz * section.Radius * Math.Sign(section.Angle);
					//                // Rotate the center->start vector by the curve's angle.
					//                var curveEndX = curveCenterX + (startX - curveCenterX) * Math.Cos(-angle) - (startZ - curveCenterZ) * Math.Sin(-angle);
					//                var curveEndZ = curveCenterZ + (startX - curveCenterX) * Math.Sin(-angle) + (startZ - curveCenterZ) * Math.Cos(-angle);
					//                // Work out the display angle.
					//                var angleStart = (float)(Math.Asin((curveCenterX - startX) / section.Radius) * 180 / Math.PI);

					//                graphics.DrawArc(trackSection.Track.IsRoadShape ? Pens.Gray : Pens.Black,
					//                    (float)(x + w * (1024 + curveCenterX - section.Radius) / 2048),
					//                    (float)(y + h * (1024 - curveCenterZ - section.Radius) / 2048),
					//                    (float)(w * section.Radius / 1024),
					//                    (float)(h * section.Radius / 1024),
					//                    (startZ < curveCenterZ ? 90 + angleStart : 270 - angleStart),
					//                    (float)section.Angle);

					//                // [ C  0  S] [xx yx zx] [ xxC+xzS  yxC+yzS  zxC+zzS]
					//                // [ 0  1  0].[xy yy zy]=[ xy       yy       zy     ]
					//                // [-S  0  C] [xz yz zz] [-xxS+xzC -yxS+yzC -zxS+zzC]
					//                var rxx2 = rxx * Math.Cos(angle) + rxz * Math.Sin(angle);
					//                var rxz2 = rxz * Math.Cos(angle) - rxx * Math.Sin(angle);
					//                var rzx2 = rzx * Math.Cos(angle) + rzz * Math.Sin(angle);
					//                var rzz2 = rzz * Math.Cos(angle) - rzx * Math.Sin(angle);
					//                rxx = rxx2; rxz = rxz2; rzx = rzx2; rzz = rzz2;
					//                startX = curveEndX;
					//                startZ = curveEndZ;
					//            } else {
					//                var straightEndX = startX - rzx * section.Length;
					//                var straightEndZ = startZ - rzz * section.Length;

					//                graphics.DrawLine(trackSection.Track.IsRoadShape ? Pens.Gray : Pens.Black,
					//                    (float)(x + w * (1024 + startX) / 2048),
					//                    (float)(y + h * (1024 - startZ) / 2048),
					//                    (float)(x + w * (1024 + straightEndX) / 2048),
					//                    (float)(y + h * (1024 - straightEndZ) / 2048));

					//                startX = straightEndX;
					//                startZ = straightEndZ;
					//            }
					//        }
					//    }
					//}
					break;
				case TileLayer.Markers:
					foreach (var marker in _markers) {
						var mx = (int)(x + w * marker.X);
						var my = (int)(y + h * marker.Z);
						var fm = graphics.MeasureString(marker.Label, SystemFonts.CaptionFont);
						graphics.FillEllipse(Brushes.DarkBlue, mx - 2, my - 2, 4, 4);
						graphics.DrawLine(Pens.DarkBlue, mx, my, mx, my - 4 * fm.Height);
						graphics.FillRectangle(Brushes.DarkBlue, mx - fm.Width / 2, my - 5 * fm.Height, fm.Width - 1, fm.Height - 1);
						graphics.DrawString(marker.Label, SystemFonts.CaptionFont, Brushes.White, mx - fm.Width / 2, my - 5 * fm.Height);
					}
					break;
				case TileLayer.Platforms:
					foreach (var platform in _platforms) {
						var mx = (int)(x + w * (1024 + platform.X) / 2048);
						var my = (int)(y + h * (1024 - platform.Z) / 2048);
						graphics.FillPolygon(Brushes.DarkBlue, new PointF[] { new PointF(mx + 6, my - 8), new PointF(mx + 8, my - 6), new PointF(mx - 6, my + 8), new PointF(mx - 8, my + 6) });
						graphics.DrawLine(Pens.Blue, mx - 8, my + 6, mx + 6, my - 8);
					}
					break;
				case TileLayer.PlatformNames:
					break;
				case TileLayer.Sidings:
					foreach (var siding in _sidings) {
						var mx = (int)(x + w * (1024 + siding.X) / 2048);
						var my = (int)(y + h * (1024 - siding.Z) / 2048);
						graphics.FillPolygon(Brushes.DarkGreen, new PointF[] { new PointF(mx + 6, my - 8), new PointF(mx + 8, my - 6), new PointF(mx - 6, my + 8), new PointF(mx - 8, my + 6) });
						graphics.DrawLine(Pens.Green, mx - 8, my + 6, mx + 6, my - 8);
					}
					break;
				case TileLayer.SidingNames:
					break;
				case TileLayer.Signals:
					foreach (var signal in _signals) {
						var mx = (int)(x + w * (1024 + signal.X) / 2048);
						var my = (int)(y + h * (1024 - signal.Z) / 2048);
						graphics.FillEllipse(Brushes.DarkRed, mx - 3, my - 3, 6, 6);
					}
					break;
				case TileLayer.Mileposts:
					break;
				case TileLayer.FuelPoints:
					break;
			}
		}
	}
}
