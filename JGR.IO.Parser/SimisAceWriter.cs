//------------------------------------------------------------------------------
// Jgr.IO.Parser library, part of MSTS Editors & Tools (http://jgrmsts.codeplex.com/).
// License: New BSD License (BSD).
//------------------------------------------------------------------------------

using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Runtime.InteropServices;

namespace Jgr.IO.Parser {
	public class SimisAceWriter : SimisWriter {
		public SimisAceWriter(SimisStreamWriter writer)
			: base(writer) {
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
				throw new NotSupportedException();
			} else {
				// RGB format.

				// Jump table: offsets to start of each scanline of each image.
				var dataSize = 152 + 16 * ace.ChannelCount;
				foreach (var image in ace.Image) {
					dataSize += image.Height * 4;
				}
				foreach (var image in ace.Image) {
					var stride = ace.Channel.Sum(c => (int)Math.Ceiling((double)c.Size * image.Width / 8));
					for (var y = 0; y < image.Height; y++) {
						Writer.Write(dataSize);
						dataSize += stride;
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
