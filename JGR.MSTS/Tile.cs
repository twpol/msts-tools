//------------------------------------------------------------------------------
// Jgr.Msts library, part of MSTS Editors & Tools (http://jgrmsts.codeplex.com/).
// License: Microsoft Public License (Ms-PL).
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

namespace Jgr.Msts {
	class TileTrackSection {
		public string Label;
		public double X;
		public double Z;
		public double DX;
		public double DY;
		public double DZ;
		public double DW;
		public TrackShape Track;
	}

	public class Tile {
		public Route Route;
		public readonly SimisProvider SimisProvider;
		public readonly string TileName;
		public readonly TileCoordinate TileCoordinate;
		Image TerrainImage;
		Image ObjectImage;
		List<TileTrackSection> TrackSections;

		public Tile(string tileName, Route route, SimisProvider simisProvider) {
			TileName = tileName;
			Route = route;
			SimisProvider = simisProvider;
			TileCoordinate = Coordinates.ConvertToTile(TileName);
			TrackSections = new List<TileTrackSection>();
		}

		public void RenderTerrainImage() {
			string tileElevation = String.Format(@"{0}\Tiles\{1}_y.raw", Route.RoutePath, TileName);
			string tileShadow = String.Format(@"{0}\Tiles\{1}_n.raw", Route.RoutePath, TileName);
			string tileUnknown = String.Format(@"{0}\Tiles\{1}_e.raw", Route.RoutePath, TileName);
			string tileFile = String.Format(@"{0}\Tiles\{1}.t", Route.RoutePath, TileName);

			var image = new Bitmap(256, 256);
			using (var g = Graphics.FromImage(image)) {
				var terrainWidth = 256;
				var terrainHeight = 256;
				var terrainFloor = 0f;
				var terrainScale = 1f;
				try {
					var tile = new SimisFile(tileFile, SimisProvider);
					tile.ReadFile();
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
					TerrainImage = image;
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

				//g.DrawString(File, SystemFonts.CaptionFont, Brushes.White, 0, Image.Height - 3 * 15);
				//g.DrawString(Location.ToString(), SystemFonts.CaptionFont, Brushes.White, 0, Image.Height - 2 * 15);
				//g.DrawString(Coordinates.ConvertToIgh(Location, 0, 1).ToString(), SystemFonts.CaptionFont, Brushes.White, 0, Image.Height - 1 * 15);
				//g.DrawString(Coordinates.ConvertToLatLon(Coordinates.ConvertToIgh(Location, 0, 1)).ToString(), SystemFonts.CaptionFont, Brushes.White, 0, Image.Height - 0 * 15);
			}

			TerrainImage = image;
			return;
		}

		public void RenderObjectImage() {
			string worldFile = String.Format(@"{0}\World\w{1,6:+000000;-000000}{2,6:+000000;-000000}.w", Route.RoutePath, TileCoordinate.X, TileCoordinate.Y);

			var image = new Bitmap(256, 256);
			using (var g = Graphics.FromImage(image)) {
				if (File.Exists(worldFile)) {
					try {
						var world = new SimisFile(worldFile, SimisProvider);
						world.ReadFile();
						foreach (var item in world.Tree["Tr_Worldfile"]) {
							if ((item.Type != "TrackObj") && (item.Type != "Dyntrack")) {
								try {
									var position = item["Position"];
									g.FillRectangle(Brushes.Yellow, image.Width * (position[0].ToValue<float>() + 1024) / 2048, image.Height * (-position[2].ToValue<float>() + 1024) / 2048, 1, 1);
								} catch (ArgumentException) {
								}
							}
						}
					} catch (Exception e) {
						g.DrawString(e.ToString(), SystemFonts.CaptionFont, Brushes.White, 0, 0);
					}
				}
			}

			ObjectImage = image;
			return;
		}

		public void RenderTrackImage() {
			string worldFile = String.Format(@"{0}\World\w{1,6:+000000;-000000}{2,6:+000000;-000000}.w", Route.RoutePath, TileCoordinate.X, TileCoordinate.Y);

			if (File.Exists(worldFile)) {
				try {
					var world = new SimisFile(worldFile, SimisProvider);
					world.ReadFile();
					foreach (var item in world.Tree["Tr_Worldfile"]) {
						try {
							if ((item.Type == "TrackObj") || (item.Type == "Dyntrack")) {
								var position = item["Position"];
								var direction = item["QDirection"];
								var tts = new TileTrackSection() {
									X = position[0].ToValue<float>(),
									Z = position[2].ToValue<float>(),
									DX = direction[0].ToValue<float>(),
									DY = direction[1].ToValue<float>(),
									DZ = direction[2].ToValue<float>(),
									DW = direction[3].ToValue<float>(),
								};
								if (item.Type == "TrackObj") {
									var filename = item["FileName"][0].ToValue<string>().ToLowerInvariant();
									if (Route.TrackService.TrackShapesByFileName.ContainsKey(filename)) {
										tts.Track = Route.TrackService.TrackShapesByFileName[filename];
									} else {
										tts.Label = filename;
									}
								} else {
									var tpaths = new List<TrackPath>();
									var tsections = new List<TrackSection>();
									foreach (var section in item["TrackSections"].Where(n => n.Type == "TrackSection")) {
										if (section[1].ToValue<int>() > 0) {
											if (section["SectionCurve"][0].ToValue<uint>() != 0) {
												// Curve.
												tsections.Add(new TrackSection(0, 0, 0, true, section[3].ToValue<float>(), section[2].ToValue<float>()));
											} else {
												// Straight.
												tsections.Add(new TrackSection(0, 0, section[2].ToValue<int>()));
											}
										}
									}
									tpaths.Add(new TrackPath(0, 0, 0, 0, tsections));
									tts.Track = new TrackShape(0, "", 0, false, false, tpaths);
								}
								TrackSections.Add(tts);
							}
						} catch (ArgumentException) {
						}
					}
				} catch (Exception) {
				}
			}

			return;
		}

		public void Render(Graphics g, int x, int y, int w, int h) {
			if (TerrainImage != null) {
				g.DrawImage(TerrainImage, x, y, w, h);
			} else {
				g.DrawLine(Pens.Black, x, y, x + w, y + h);
				g.DrawLine(Pens.Black, x + w, y, x, y + h);
			}
			if (ObjectImage != null) {
				g.DrawImage(ObjectImage, x, y, w, h);
			}
			foreach (var trackSection in TrackSections) {
				if (!String.IsNullOrEmpty(trackSection.Label)) {
					g.DrawString(trackSection.Label, SystemFonts.CaptionFont, Brushes.Black,
						(int)(x + w * (0 + (trackSection.X + 1024) / 2048)),
						(int)(y + h * (1 - (trackSection.Z + 1024) / 2048)));
				}
				if (trackSection.Track != null) {
					// Rotation matrix for quaternion (a, b, c, d).
					//   [a^2+b^2-c^2-d^2  2bc-2ad          2bd+2ac        ]
					//   [2bc+2ad          a^2-b^2+c^2-d^2  2cd-2ab        ]
					//   [2bd-2ac          2cd+2ab          a^2-b^2-c^2+d^2]
					// Rotation of 90 degrees right about the vector (0, 1, 0).
					//   [0                0                -1             ]
					//   [0                1                0              ]
					//   [1                0                0              ]
					var rx = 2 * trackSection.DY * trackSection.DW - 2 * trackSection.DX * trackSection.DZ;
					var rz = trackSection.DX * trackSection.DX + trackSection.DY * trackSection.DY - trackSection.DZ * trackSection.DZ - trackSection.DW * trackSection.DW;

					//g.DrawLine(Pens.Yellow,
					//    (float)(x + w * (1024 + trackSection.X) / 2048),
					//    (float)(y + h * (1024 - trackSection.Z) / 2048),
					//    x,
					//    y);

					foreach (var path in trackSection.Track.Paths) {
						var startX = trackSection.X - rx * path.X;
						var startZ = trackSection.Z - rz * path.Z;

						foreach (var section in path.Sections) {
							if (section.IsCurve) {
								// Rotate 90 degrees left or right base ioon +ve or -ve angle (-ve angle = left).
								var curveCenterX = startX - rz * section.Radius * Math.Sign(section.Angle);
								var curveCenterZ = startZ + rx * section.Radius * Math.Sign(section.Angle);
								// Rotate the center->start vector by the curve's angle.
								var curveEndX = curveCenterX + (startX - curveCenterX) * Math.Cos(-section.Angle * Math.PI / 180) - (startZ - curveCenterZ) * Math.Sin(-section.Angle * Math.PI / 180);
								var curveEndZ = curveCenterZ + (startX - curveCenterX) * Math.Sin(-section.Angle * Math.PI / 180) + (startZ - curveCenterZ) * Math.Cos(-section.Angle * Math.PI / 180);
								// Work out the display angle.
								var angleStart = (float)(Math.Asin((curveCenterX - startX) / section.Radius) * 180 / Math.PI);
								//g.DrawLine(Pens.LightGray,
								//    (float)(x + w * (1024 + startX) / 2048),
								//    (float)(y + h * (1024 - startZ) / 2048),
								//    (float)(x + w * (1024 + curveCenterX) / 2048),
								//    (float)(y + h * (1024 - curveCenterZ) / 2048));
								//g.DrawLine(Pens.LightGray,
								//    (float)(x + w * (1024 + curveEndX) / 2048),
								//    (float)(y + h * (1024 - curveEndZ) / 2048),
								//    (float)(x + w * (1024 + curveCenterX) / 2048),
								//    (float)(y + h * (1024 - curveCenterZ) / 2048));
								//g.DrawLine(Pens.Blue,
								//    (float)(x + w * (1024 + startX) / 2048),
								//    (float)(y + h * (1024 - startZ) / 2048),
								//    (float)(x + w * (1024 + curveEndX) / 2048),
								//    (float)(y + h * (1024 - curveEndZ) / 2048));
								g.DrawArc(Pens.Black,
									(float)(x + w * (1024 + curveCenterX - section.Radius) / 2048),
									(float)(y + h * (1024 - curveCenterZ - section.Radius) / 2048),
									(float)(w * section.Radius / 1024),
									(float)(h * section.Radius / 1024),
									(startZ < curveCenterZ ? 90 + angleStart : 270 - angleStart),
									(float)section.Angle);
								startX = curveEndX;
								startZ = curveEndZ;
							} else {
								var straightEndX = startX - rx * section.Length;
								var straightEndZ = startZ - rz * section.Length;
								//g.DrawLine(Pens.LightGray,
								//    (float)(x + w * (1024 + startX) / 2048),
								//    (float)(y + h * (1024 - startZ) / 2048),
								//    x,
								//    y);
								//g.DrawLine(Pens.LightGray,
								//    (float)(x + w * (1024 + straightEndX) / 2048),
								//    (float)(y + h * (1024 - straightEndZ) / 2048),
								//    x,
								//    y);
								g.DrawLine(Pens.Black,
									(float)(x + w * (1024 + startX) / 2048),
									(float)(y + h * (1024 - startZ) / 2048),
									(float)(x + w * (1024 + straightEndX) / 2048),
									(float)(y + h * (1024 - straightEndZ) / 2048));
								startX = straightEndX;
								startZ = straightEndZ;
							}
						}
					}
				}
			}
		}
	}
}
