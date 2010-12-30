//------------------------------------------------------------------------------
// Jgr.IO.Parser library, part of MSTS Editors & Tools (http://jgrmsts.codeplex.com/).
// License: New BSD License (BSD).
//------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;

namespace Jgr.IO.Parser {
	public class SimisAceWriter : SimisWriter {
		public SimisAceWriter(SimisStreamWriter writer)
			: base(writer) {
		}

		/// <summary>
		/// Calculates the size of a single data block for a given image; this is the same as the difference in offsets in the header.
		/// </summary>
		/// <param name="channels">Collection of color, alpha and mask channels to include.</param>
		/// <param name="dxt">Specifies whether this is a DXT-compressed file and as such will return entire-image sizes rather than scanline/stride sizes.</param>
		/// <param name="image">Image to calculate data size of.</param>
		/// <returns>The number of bytes for the whole image (<paramref name="dxt"/> is <c>true</c>) or a single scanline (<paramref name="dxt"/> is <c>false</c>).</returns>
		int GetImageDataBlockSize(IEnumerable<SimisAceChannel> channels, bool dxt, SimisAceImage image) {
			if (dxt) {
				if ((image.Width >= 4) && (image.Height >= 4)) {
					return 4 + (int)Math.Ceiling((double)image.Width / 4) * (int)Math.Ceiling((double)image.Height / 4) * 8;
				}
				return channels.Sum(c => (int)Math.Ceiling((double)c.Size * image.Width / 8)) * image.Height;
			}
			return channels.Sum(c => (int)Math.Ceiling((double)c.Size * image.Width / 8));
		}

		public void Write(SimisAce ace) {
			Writer.Write((int)1);
			Writer.Write(ace.Format);
			Writer.Write(ace.Width);
			Writer.Write(ace.Height);
			Writer.Write(ace.Unknown4);
			Writer.Write(ace.ChannelCount);
			Writer.Write(ace.Unknown6);
			Writer.Write(ace.Unknown7.PadRight(16, '\0').Substring(0, 16).ToCharArray());
			Writer.Write(ace.Creator.PadRight(64, '\0').Substring(0, 64).ToCharArray());
			Writer.Write(ace.Unknown9);

			foreach (var channel in ace.Channel) {
				Writer.Write((long)channel.Size);
				Writer.Write((long)channel.Type);
			}

			if ((ace.Format & 0x10) == 0x10) {
				// DXT format.

				switch (ace.Unknown4) {
					case 0x12:
						// DXT1
						if (ace.Channel.Any(c => c.Type == SimisAceChannelId.Alpha)) {
							throw new InvalidDataException("Alpha channel not supported with DXT1 ACE files (use a mask instead).");
						}

						// Jump table: offsets to start of each image.
						var dataSize = 152 + 16 * ace.ChannelCount + 4 * ace.Image.Count;
						foreach (var image in ace.Image) {
							Writer.Write(dataSize);
							dataSize += GetImageDataBlockSize(ace.Channel, true, image);
						}

						foreach (var image in ace.Image) {
							var strideH = (int)Math.Ceiling((double)image.Width / 4) * 4;
							var strideV = (int)Math.Ceiling((double)image.Height / 4) * 4;
							Writer.Write(new byte[strideH * strideV * 8]);
						}

						break;
					default:
						throw new NotSupportedException("ACE DXT format unknown4=" + ace.Unknown4 + " is not supported.");
				}
			} else {
				// RGB format.

				// Jump table: offsets to start of each scanline of each image.
				var dataSize = 152 + 16 * ace.ChannelCount + 4 * ace.Image.Sum(i => i.Height);
				foreach (var image in ace.Image) {
					for (var y = 0; y < image.Height; y++) {
						Writer.Write(dataSize);
						dataSize += GetImageDataBlockSize(ace.Channel, false, image);
					}
				}

				foreach (var image in ace.Image) {
					var colorData = new byte[image.Width * image.Height * 4];
					var maskData = new byte[image.Width * image.Height * 4];
					if (image.ImageColor != null) {
						var imageColorBits = image.ImageColor.LockBits(new Rectangle(Point.Empty, image.ImageColor.Size), ImageLockMode.ReadOnly, image.ImageColor.PixelFormat);
						Debug.Assert(imageColorBits.Stride == 4 * imageColorBits.Width, "Cannot copy data to bitmap with Stride != Width.");
						Marshal.Copy(imageColorBits.Scan0, colorData, 0, colorData.Length);
						image.ImageColor.UnlockBits(imageColorBits);
					}
					if (image.ImageMask != null) {
						var imageMaskBits = image.ImageMask.LockBits(new Rectangle(Point.Empty, image.ImageMask.Size), ImageLockMode.ReadOnly, image.ImageMask.PixelFormat);
						Debug.Assert(imageMaskBits.Stride == 4 * imageMaskBits.Width, "Cannot copy data to bitmap with Stride != Width.");
						Marshal.Copy(imageMaskBits.Scan0, maskData, 0, maskData.Length);
						image.ImageMask.UnlockBits(imageMaskBits);
					}
					for (var y = 0; y < image.Height; y++) {
						foreach (var channel in ace.Channel) {
							var data = channel.Type == SimisAceChannelId.Mask ? maskData : colorData;
							var dataOffset = new[] { 0, 2, 1, 0, 3 }[(int)channel.Type - 2];
							var bits = new byte[(int)Math.Ceiling((double)channel.Size * image.Width / 8)];
							switch (channel.Size) {
								case 1:
									for (var x = 0; x < image.Width; x += 8) {
										for (var i = 0; i < 8; i++) {
											if (x + i < image.Width) {
												bits[x / 8] += (byte)((data[(image.Width * y + x + i) * 4 + dataOffset] >= 0x80 ? 1 : 0) << (7 - i));
											}
										}
									}
									break;
								case 8:
									for (var x = 0; x < image.Width; x++) {
										bits[x] = data[(image.Width * y + x) * 4 + dataOffset];
									}
									break;
							}
							Writer.Write(bits);
						}
					}
				}
			}
			Writer.Write(ace.UnknownTrail1);
			Writer.Write(ace.UnknownTrail2);
		}
	}
}
