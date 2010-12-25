//------------------------------------------------------------------------------
// Jgr.IO.Parser library, part of MSTS Editors & Tools (http://jgrmsts.codeplex.com/).
// License: New BSD License (BSD).
//------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

namespace Jgr.IO.Parser {
	[Immutable]
	public class SimisAce : DataTreeNode<SimisAce> {
		public int Width { get { return Red == null ? Green == null ? Blue == null ? Alpha == null ? Mask == null ? 0 : Mask.Width : Alpha.Width : Blue.Width : Green.Width : Red.Width; } }
		public int Height { get { return Red == null ? Green == null ? Blue == null ? Alpha == null ? Mask == null ? 0 : Mask.Height : Alpha.Height : Blue.Height : Green.Height : Red.Height; } }
		public readonly SimisAceChannel Red;
		public readonly SimisAceChannel Green;
		public readonly SimisAceChannel Blue;
		public readonly SimisAceChannel Alpha;
		public readonly SimisAceChannel Mask;

		SimisAce(SimisAceChannel red, SimisAceChannel green, SimisAceChannel blue, SimisAceChannel alpha, SimisAceChannel mask) {
			Red = red;
			Green = green;
			Blue = blue;
			Alpha = alpha;
			Mask = mask;
			if ((Green != null) && (Green.Width != Width)) throw new ArgumentException("Green channel width is different.", "green");
			if ((Green != null) && (Green.Height != Height)) throw new ArgumentException("Green channel height is different.", "green");
			if ((Blue != null) && (Blue.Width != Width)) throw new ArgumentException("Blue channel width is different.", "blue");
			if ((Blue != null) && (Blue.Height != Height)) throw new ArgumentException("Blue channel height is different.", "blue");
			if ((Alpha != null) && (Alpha.Width != Width)) throw new ArgumentException("Alpha channel width is different.", "alpha");
			if ((Alpha != null) && (Alpha.Height != Height)) throw new ArgumentException("Alpha channel height is different.", "alpha");
			if ((Mask != null) && (Mask.Width != Width)) throw new ArgumentException("Mask channel width is different.", "mask");
			if ((Mask != null) && (Mask.Height != Height)) throw new ArgumentException("Mask channel height is different.", "mask");
		}

		public Image Image {
			get {
				var image = new Bitmap(Width, Height, PixelFormat.Format32bppArgb);
				var data = image.LockBits(new Rectangle(Point.Empty, image.Size), ImageLockMode.WriteOnly, image.PixelFormat);
				var imageData = new byte[data.Stride * data.Height];
				for (var y = 0; y < Height; y++) {
					for (var x = 0; x < Width; x++) {
						var imageDataOffset = (data.Stride * y + x) * 4;
						var channelOffset = Width * y + x;
						if (Red != null) {
							imageData[imageDataOffset + 1] = Red.Data[channelOffset];
						}
						if (Green != null) {
							imageData[imageDataOffset + 2] = Green.Data[channelOffset];
						}
						if (Blue != null) {
							imageData[imageDataOffset + 3] = Blue.Data[channelOffset];
						}
						if (Alpha != null) {
							imageData[imageDataOffset + 0] = Alpha.Data[channelOffset];
						} else if (Mask != null) {
							imageData[imageDataOffset + 0] = Mask.Data[channelOffset];
						}
					}
				}
				Marshal.Copy(imageData, 0, data.Scan0, imageData.Length);
				image.UnlockBits(data);
				return image;
			}
		}
	}

	[Immutable]
	public class SimisAceChannel : DataTreeNode<SimisAceChannel> {
		public readonly int Width;
		public readonly int Height;
		public IList<byte> Data { get { return DataArray; } }
		readonly byte[] DataArray;

		SimisAceChannel(int width, int height, byte[] dataArray) {
			Width = width;
			Height = height;
			DataArray = dataArray;
		}

		public Image Image {
			get {
				var image = new Bitmap(Width, Height, PixelFormat.Format8bppIndexed);
				for (var i = 0; i < 256; i++) {
					image.Palette.Entries[i] = Color.FromArgb(i, i, i);
				}
				var data = image.LockBits(new Rectangle(Point.Empty, image.Size), ImageLockMode.WriteOnly, image.PixelFormat);
				if (data.Stride == Width) {
					Marshal.Copy(DataArray, 0, data.Scan0, Width * Height);
				} else {
					for (var i = 0; i < Height; i++) {
						Marshal.Copy(DataArray, Width * i, new IntPtr((int)data.Scan0 + data.Stride * i), Width);
					}
				}
				image.UnlockBits(data);
				return image;
			}
		}
	}
}
