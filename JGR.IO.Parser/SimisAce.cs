//------------------------------------------------------------------------------
// Jgr.IO.Parser library, part of MSTS Editors & Tools (http://jgrmsts.codeplex.com/).
// License: New BSD License (BSD).
//------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

namespace Jgr.IO.Parser {
	[Immutable]
	public class SimisAce : DataTreeNode<SimisAce> {
		public readonly int Format;
		public readonly int Width;
		public readonly int Height;
		public readonly int Unknown4;
		public readonly int ChannelCount;
		public readonly int Unknown6;
		public readonly string Unknown7;
		public readonly string Creator;
		public readonly byte[] Unknown9;
		public IList<SimisAceChannel> Channel { get { return Channels; } }
		readonly SimisAceChannel[] Channels;
		public IList<SimisAceImage> Image { get { return Images; } }
		readonly SimisAceImage[] Images;

		public SimisAce(int format, int width, int height, int unknown4, int channelCount, int unknown6, string unknown7, string creator, byte[] unknown9, SimisAceChannel[] channels, SimisAceImage[] images) {
			Format = format;
			Width = width;
			Height = height;
			Unknown4 = unknown4;
			ChannelCount = channelCount;
			Unknown6 = unknown6;
			Unknown7 = unknown7;
			Creator = creator;
			Unknown9 = unknown9;
			Channels = channels;
			Images = images;
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
		public readonly Bitmap ImageColor;
		public readonly Bitmap ImageMask;

		public SimisAceImage(Bitmap imageColor, Bitmap imageMask) {
			if ((imageColor != null) && (imageColor.PixelFormat != PixelFormat.Format32bppArgb)) throw new ArgumentException("Argument must use PixelFormat.Format32bppArgb.", "imageColor");
			if ((imageMask != null) && (imageMask.PixelFormat != PixelFormat.Format32bppRgb)) throw new ArgumentException("Argument must use PixelFormat.Format32bppRgb.", "imageMask");
			ImageColor = imageColor;
			ImageMask = imageMask;
		}

		public Image GetImage(SimisAceImageType type) {
			switch (type) {
				case SimisAceImageType.ColorOnly:
					if (ImageColor == null) throw new InvalidOperationException("Cannot produce color-only image when ImageColor is null.");
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
					if (ImageColor == null) throw new InvalidOperationException("Cannot produce alpha-only image when ImageColor is null.");
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
					if (ImageMask == null) throw new InvalidOperationException("Cannot produce mask-only image when ImageMask is null.");
					return ImageMask;
				case SimisAceImageType.ColorAndAlpha:
					if (ImageColor == null) throw new InvalidOperationException("Cannot produce color-and-alpha image when ImageColor is null.");
					return ImageColor;
				case SimisAceImageType.ColorAndMask:
					if (ImageColor == null) throw new InvalidOperationException("Cannot produce color-and-mask image when ImageColor is null.");
					if (ImageMask == null) throw new InvalidOperationException("Cannot produce color-and-mask image when ImageMask is null.");
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
