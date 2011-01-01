//------------------------------------------------------------------------------
// Jgr.IO.Parser library, part of MSTS Editors & Tools (http://jgrmsts.codeplex.com/).
// License: New BSD License (BSD).
//------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Diagnostics;

namespace Jgr.IO.Parser {
	public class SimisAceReader : SimisReader {
		public SimisAceReader(SimisStreamReader reader)
			: base(reader) {
		}

		public SimisAce Read() {
			// ACE format flags:
			//   01 = has mipmap images.
			//   02 = ?
			//   04 = ?
			//   08 = ?
			//   10 = is DXT compressed.
			var format = Reader.ReadInt32();
			if (!new[] { 0x00, 0x01, 0x04, 0x05, 0x10, 0x11, 0x14, 0x15 }.Contains(format)) {
				throw new ReaderException(Reader, true, 4, "ACE format 0x" + format.ToString("X") + " is not supported.");
			}
			var width = Reader.ReadInt32();
			var height = Reader.ReadInt32();
			// TODO: Could this be the DirectX surface format?
			//   0E = D3DFMT_R5G6B5
			//   10 = D3DFMT_A1R5G5B5
			//   11 = D3DFMT_A4R4G4B4
			//   12 = D3DFMT_DXT1
			//   14 = D3DFMT_DXT3
			//   16 = D3DFMT_DXT5
			var unknown4 = Reader.ReadInt32();
			var channelCount = Reader.ReadInt32();
			// TODO: Work out what this int is for. Tends to be zero.
			var unknown6 = Reader.ReadInt32();
			var unknown7 = new String(Reader.ReadChars(16));
			if (unknown7.Contains('\0')) {
				unknown7 = unknown7.Substring(0, unknown7.IndexOf('\0'));
			}
			var creator = new String(Reader.ReadChars(64));
			if (creator.Contains('\0')) {
				creator = creator.Substring(0, creator.IndexOf('\0'));
			}

			// TODO: Work out what these 44 bytes are for; they are sometimes all zero, sometimes not.
			var unknown9 = Reader.ReadBytes(44);

			var channels = new List<SimisAceChannel>();
			for (var channel = 0; channel < channelCount; channel++) {
				var size = (byte)Reader.ReadUInt64();
				if ((size != 1) && (size != 8)) {
					throw new ReaderException(Reader, true, 8, "ACE color channel size " + size + " is not supported.");
				}
				var type = Reader.ReadUInt64();
				if ((type < 2) || (type > 6)) {
					throw new ReaderException(Reader, true, 8, "ACE color channel type " + type + " is unknown.");
				}
				channels.Add(new SimisAceChannel(size, (SimisAceChannelId)type));
			}

			var images = new List<SimisAceImage>();
			var imageCount = 1 + (int)((format & 0x01) == 0x01 ? Math.Log(width) / Math.Log(2) : 0);
			if ((format & 0x10) == 0x10) {
				// DXT format.
				// TODO: Check width/height are a power-of-2 dimension.
				switch (unknown4) {
					case 0x12:
						// DXT1
						if (channels.Any(c => c.Type == SimisAceChannelId.Alpha)) throw new InvalidDataException("Alpha channel not supported with DXT1 ACE files (use a mask instead).");

						// Jump table: offsets to start of each image.
						Reader.ReadBytes(imageCount * 4);

						for (var imageIndex = 0; imageIndex < imageCount; imageIndex++) {
							var imageWidth = width / (int)Math.Pow(2, imageIndex);
							var imageHeight = height / (int)Math.Pow(2, imageIndex);
							var imageColorData = new int[imageWidth * imageHeight];
							var imageMaskData = new int[imageWidth * imageHeight];

							if ((imageWidth >= 4) && (imageHeight >= 4)) {
								// DXT compressed blocks have a length header (32bits).
								Reader.ReadUInt32();
								ReadDXT1ImageData(imageWidth, imageHeight, channels, imageColorData, imageMaskData);
							} else {
								// For <4 pixels the images are in RGB format.
								ReadARGBImageData(imageWidth, imageHeight, channels, imageColorData, imageMaskData);
							}

							var imageColor = CreateBitmapFromData(imageWidth, imageHeight, imageColorData, PixelFormat.Format32bppArgb);
							var imageMask = CreateBitmapFromData(imageWidth, imageHeight, imageMaskData, PixelFormat.Format32bppRgb);
							images.Add(new SimisAceImage(imageColor, imageMask));
						}
						break;
					default:
						throw new NotSupportedException("ACE DXT format unknown4=" + unknown4 + " is not supported.");
				}
			} else {
				// RGB format.

				// Jump table: offsets to start of each scanline of each image.
				for (var imageIndex = 0; imageIndex < imageCount; imageIndex++) {
					var imageHeight = height / (int)Math.Pow(2, imageIndex);
					Reader.ReadBytes(imageHeight * 4);
				}

				for (var imageIndex = 0; imageIndex < imageCount; imageIndex++) {
					var imageWidth = width / (int)Math.Pow(2, imageIndex);
					var imageHeight = height / (int)Math.Pow(2, imageIndex);
					var imageColorData = new int[imageWidth * imageHeight];
					var imageMaskData = new int[imageWidth * imageHeight];

					ReadARGBImageData(imageWidth, imageHeight, channels, imageColorData, imageMaskData);

					var imageColor = CreateBitmapFromData(imageWidth, imageHeight, imageColorData, PixelFormat.Format32bppArgb);
					var imageMask = CreateBitmapFromData(imageWidth, imageHeight, imageMaskData, PixelFormat.Format32bppRgb);
					images.Add(new SimisAceImage(imageColor, imageMask));
				}
			}

			var unknownTrail1 = new byte[0];
			var unknownTrail2 = Reader.ReadBytes(0x10000);

			return new SimisAce(format, width, height, unknown4, unknown6, unknown7, creator, unknown9, channels.ToArray(), images.ToArray(), unknownTrail1, unknownTrail2);
		}

		void ReadARGBImageData(int width, int height, IEnumerable<SimisAceChannel> channels, int[] colorData, int[] maskData) {
			for (var y = 0; y < height; y++) {
				var imageChannels = new byte[8][];
				foreach (var channel in channels) {
					switch (channel.Size) {
						case 1:
							// 1bpp channels start with the MSB and work down to LSB and then the next byte.
							var bytes = Reader.ReadBytes((int)Math.Ceiling((double)channel.Size * width / 8));
							imageChannels[(int)channel.Type] = new byte[width];
							for (var x = 0; x < width; x++) {
								imageChannels[(int)channel.Type][x] = (byte)(((bytes[x / 8] >> (7 - (x % 8))) & 1) * 0xFF);
							}
							break;
						case 8:
							// 8bpp are simple.
							imageChannels[(int)channel.Type] = Reader.ReadBytes(width);
							break;
					}
				}

				for (var x = 0; x < width; x++) {
					colorData[width * y + x] = (imageChannels[(int)SimisAceChannelId.Red][x] << 16) + (imageChannels[(int)SimisAceChannelId.Green][x] << 8) + imageChannels[(int)SimisAceChannelId.Blue][x];
					if (imageChannels[(int)SimisAceChannelId.Alpha] != null) {
						colorData[width * y + x] += (imageChannels[(int)SimisAceChannelId.Alpha][x] << 24);
					} else {
						colorData[width * y + x] += (0xFF << 24);
					}

					if (imageChannels[(int)SimisAceChannelId.Mask] != null) {
						maskData[width * y + x] = imageChannels[(int)SimisAceChannelId.Mask][x] * 0x00010101;
					}
				}
			}
		}

		void ReadDXT1ImageData(int width, int height, IEnumerable<SimisAceChannel> channels, int[] colorData, int[] maskData) {
			var size = (int)Math.Ceiling(height / 4f);
			for (var y = 0; y < size; y++) {
				for (var x = 0; x < size; x++) {
					var c = new byte[4, 4];
					var ci = new uint[2];
					for (var i = 0; i < 2; i++) {
						ci[i] = Reader.ReadUInt16();
						c[i, 0] = 0xFF;
						c[i, 1] = (byte)((ci[i] & 0xF800) >> 8);
						c[i, 2] = (byte)((ci[i] & 0x07E0) >> 3);
						c[i, 3] = (byte)((ci[i] & 0x001F) << 3);
					}
					if (ci[0] > ci[1]) {
						for (var i = 0; i < 4; i++) {
							c[2, i] = (byte)(2 * c[0, i] / 3 + 1 * c[1, i] / 3);
							c[3, i] = (byte)(1 * c[0, i] / 3 + 2 * c[1, i] / 3);
						}
					} else {
						for (var i = 0; i < 4; i++) {
							c[2, i] = (byte)(c[0, i] / 2 + c[1, i] / 2);
						}
					}
					var lookup = Reader.ReadUInt32();
					for (var x2 = 0; x2 < 4; x2++) {
						for (var y2 = 0; y2 < 4; y2++) {
							var index = (lookup >> (x2 * 2 + y2 * 8)) & 0x3;
							if ((x * 4 + x2 < width) && (y * 4 + y2 < height)) {
								colorData[width * (y * 4 + y2) + (x * 4 + x2)] = (0xFF << 24) + (c[index, 1] << 16) + (c[index, 2] << 8) + c[index, 3];
								maskData[width * (y * 4 + y2) + (x * 4 + x2)] = c[index, 0] * 0x00010101;
							}
						}
					}
				}
			}
		}

		static Bitmap CreateBitmapFromData(int width, int height, int[] data, PixelFormat format) {
			var image = new Bitmap(width, height, format);
			var imageBits = image.LockBits(new Rectangle(Point.Empty, image.Size), ImageLockMode.WriteOnly, image.PixelFormat);
			Debug.Assert(imageBits.Stride == 4 * imageBits.Width, "Cannot copy data to bitmap with Stride != Width.");
			Marshal.Copy(data, 0, imageBits.Scan0, data.Length);
			image.UnlockBits(imageBits);
			return image;
		}
	}
}
