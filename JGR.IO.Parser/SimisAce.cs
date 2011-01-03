//------------------------------------------------------------------------------
// Jgr.IO.Parser library, part of MSTS Editors & Tools (http://jgrmsts.codeplex.com/).
// License: New BSD License (BSD).
//------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Runtime.InteropServices;

namespace Jgr.IO.Parser {
	[Immutable]
	public class SimisAce : DataTreeNode<SimisAce> {
		public readonly int Format;
		public readonly int Width;
		public readonly int Height;
		public readonly int Unknown4;
		public readonly int Unknown6;
		public readonly string Unknown7;
		public readonly string Creator;
		public readonly byte[] Unknown9;
		public IList<SimisAceChannel> Channel { get { return Channels; } }
		readonly SimisAceChannel[] Channels;
		public IList<SimisAceImage> Image { get { return Images; } }
		readonly SimisAceImage[] Images;
		public readonly byte[] UnknownTrail1;
		public readonly byte[] UnknownTrail2;
		public readonly bool HasAlpha;
		public readonly bool HasMask;

		SimisAce(int format, int width, int height, int unknown4, int unknown6, string unknown7, string creator, byte[] unknown9, SimisAceChannel[] channels, SimisAceImage[] images, byte[] unknownTrail1, byte[] unknownTrail2, bool hasAlpha, bool hasMask) {
			Format = format;
			Width = width;
			Height = height;
			Unknown4 = unknown4;
			Unknown6 = unknown6;
			Unknown7 = unknown7;
			Creator = creator;
			Unknown9 = unknown9;
			Channels = channels;
			Images = images;
			UnknownTrail1 = unknownTrail1;
			UnknownTrail2 = unknownTrail2;
			HasAlpha = hasAlpha;
			HasMask = hasMask;
		}

		public SimisAce(int format, int width, int height, int unknown4, int unknown6, string unknown7, string creator, byte[] unknown9, SimisAceChannel[] channels, SimisAceImage[] images, byte[] unknownTrail1, byte[] unknownTrail2)
			: this(format, width, height, unknown4, unknown6, unknown7, creator, unknown9, channels, images, unknownTrail1, unknownTrail2, channels.Any(c => c.Type == SimisAceChannelId.Alpha), channels.Any(c => c.Type == SimisAceChannelId.Mask)) {
		}
	}

	public enum SimisAceChannelId {
		Mask = 2,
		Red = 3,
		Green = 4,
		Blue = 5,
		Alpha = 6,
	}

	[Immutable]
	public class SimisAceChannel : DataTreeNode<SimisAceChannel> {
		public readonly int Size;
		public readonly SimisAceChannelId Type;

		public SimisAceChannel(int size, SimisAceChannelId type) {
			Size = size;
			Type = type;
		}
	}

	public enum SimisAceImageType {
		ColorOnly,
		AlphaOnly,
		MaskOnly,
		ColorAndAlpha,
		ColorAndMask,
	}

	[Immutable]
	public class SimisAceImage : DataTreeNode<SimisAceImage> {
		public readonly int Width;
		public readonly int Height;
		public readonly Bitmap ImageColor;
		public readonly Bitmap ImageMask;

		SimisAceImage(int width, int height, Bitmap imageColor, Bitmap imageMask) {
			if (imageColor == null) throw new ArgumentNullException("imageColor");
			if (imageMask == null) throw new ArgumentNullException("imageMask");
			if (imageColor.PixelFormat != PixelFormat.Format32bppArgb) throw new ArgumentException("Argument must use PixelFormat.Format32bppArgb.", "imageColor");
			if (imageMask.PixelFormat != PixelFormat.Format32bppRgb) throw new ArgumentException("Argument must use PixelFormat.Format32bppRgb.", "imageMask");
			if (imageColor.Size != imageMask.Size) throw new ArgumentException("Color and mask images must be the same dimensions.");
			Width = width;
			Height = height;
			ImageColor = imageColor;
			ImageMask = imageMask;
		}

		public SimisAceImage(Bitmap imageColor, Bitmap imageMask)
			: this(
				imageColor != null ? imageColor.Width : 0,
				imageColor != null ? imageColor.Height : 0,
				imageColor,
				imageMask) {
		}

		public Image GetImage(SimisAceImageType type) {
			switch (type) {
				case SimisAceImageType.ColorOnly:
					var imageColorBits = ImageColor.LockBits(new Rectangle(Point.Empty, ImageColor.Size), ImageLockMode.ReadOnly, ImageColor.PixelFormat);
					var image = new Bitmap(ImageColor.Width, ImageColor.Height, PixelFormat.Format32bppRgb);
					var imageBits = image.LockBits(new Rectangle(Point.Empty, ImageColor.Size), ImageLockMode.WriteOnly, ImageColor.PixelFormat);
					var buffer = new int[imageBits.Width * imageBits.Height];
					Debug.Assert(imageBits.Stride == 4 * imageBits.Width);
					Debug.Assert(imageBits.Stride == imageColorBits.Stride);
					Marshal.Copy(imageColorBits.Scan0, buffer, 0, buffer.Length);
					for (var i = 0; i < buffer.Length; i++) {
						buffer[i] = buffer[i] & 0x00FFFFFF;
					}
					Marshal.Copy(buffer, 0, imageBits.Scan0, buffer.Length);
					image.UnlockBits(imageBits);
					ImageColor.UnlockBits(imageColorBits);
					return image;
				case SimisAceImageType.AlphaOnly:
					imageColorBits = ImageColor.LockBits(new Rectangle(Point.Empty, ImageColor.Size), ImageLockMode.ReadOnly, ImageColor.PixelFormat);
					image = new Bitmap(ImageColor.Width, ImageColor.Height, PixelFormat.Format32bppRgb);
					imageBits = image.LockBits(new Rectangle(Point.Empty, ImageColor.Size), ImageLockMode.WriteOnly, ImageColor.PixelFormat);
					buffer = new int[imageBits.Width * imageBits.Height];
					Debug.Assert(imageBits.Stride == 4 * imageBits.Width);
					Debug.Assert(imageBits.Stride == imageColorBits.Stride);
					Marshal.Copy(imageColorBits.Scan0, buffer, 0, buffer.Length);
					for (var i = 0; i < buffer.Length; i++) {
						buffer[i] = (buffer[i] >> 24) * 0x00010101;
					}
					Marshal.Copy(buffer, 0, imageBits.Scan0, buffer.Length);
					image.UnlockBits(imageBits);
					ImageColor.UnlockBits(imageColorBits);
					return image;
				case SimisAceImageType.MaskOnly:
					return ImageMask;
				case SimisAceImageType.ColorAndAlpha:
					return ImageColor;
				case SimisAceImageType.ColorAndMask:
					imageColorBits = ImageColor.LockBits(new Rectangle(Point.Empty, ImageColor.Size), ImageLockMode.ReadOnly, ImageColor.PixelFormat);
					var imageMaskBits = ImageMask.LockBits(new Rectangle(Point.Empty, ImageColor.Size), ImageLockMode.ReadOnly, ImageMask.PixelFormat);
					image = new Bitmap(ImageColor.Width, ImageColor.Height, PixelFormat.Format32bppArgb);
					imageBits = image.LockBits(new Rectangle(Point.Empty, ImageColor.Size), ImageLockMode.WriteOnly, ImageColor.PixelFormat);
					buffer = new int[imageBits.Width * imageBits.Height];
					var bufferMask = new int[imageBits.Width * imageBits.Height];
					Debug.Assert(imageBits.Stride == 4 * imageBits.Width);
					Debug.Assert(imageBits.Stride == imageColorBits.Stride);
					Debug.Assert(imageBits.Stride == imageMaskBits.Stride);
					Marshal.Copy(imageColorBits.Scan0, buffer, 0, buffer.Length);
					Marshal.Copy(imageMaskBits.Scan0, bufferMask, 0, bufferMask.Length);
					for (var i = 0; i < buffer.Length; i++) {
						buffer[i] = (buffer[i] & 0x00FFFFFF) + ((bufferMask[i] & 0x000000FF) << 24);
					}
					Marshal.Copy(buffer, 0, imageBits.Scan0, buffer.Length);
					image.UnlockBits(imageBits);
					ImageColor.UnlockBits(imageColorBits);
					ImageMask.UnlockBits(imageMaskBits);
					return image;
				default:
					throw new ArgumentException("Unknown image type: " + type, "type");
			}
		}
	}
}
