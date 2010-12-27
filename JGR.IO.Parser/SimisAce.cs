//------------------------------------------------------------------------------
// Jgr.IO.Parser library, part of MSTS Editors & Tools (http://jgrmsts.codeplex.com/).
// License: New BSD License (BSD).
//------------------------------------------------------------------------------

using System.Collections.Generic;
using System.Drawing;

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

	[Immutable]
	public class SimisAceImage : DataTreeNode<SimisAceImage> {
		public readonly Image ImageColor;
		public readonly Image ImageMask;

		public SimisAceImage(Image imageColor, Image imageMask) {
			ImageColor = imageColor;
			ImageMask = imageMask;
		}
	}

	//[Immutable]
	//public abstract class SimisAceImage : DataTreeNode<SimisAceImage> {
	//    public abstract int Width { get; }
	//    public abstract int Height { get; }
	//    public abstract Image Image { get; }
	//}

	//[Immutable]
	//public class SimisAceImageChannels : SimisAceImage {
	//    public override int Width { get { return Red == null ? Green == null ? Blue == null ? Alpha == null ? Mask == null ? 0 : Mask.Width : Alpha.Width : Blue.Width : Green.Width : Red.Width; } }
	//    public override int Height { get { return Red == null ? Green == null ? Blue == null ? Alpha == null ? Mask == null ? 0 : Mask.Height : Alpha.Height : Blue.Height : Green.Height : Red.Height; } }
	//    public readonly SimisAceImageChannel Red;
	//    public readonly SimisAceImageChannel Green;
	//    public readonly SimisAceImageChannel Blue;
	//    public readonly SimisAceImageChannel Alpha;
	//    public readonly SimisAceImageChannel Mask;

	//    SimisAceImageChannels(SimisAceImageChannel red, SimisAceImageChannel green, SimisAceImageChannel blue, SimisAceImageChannel alpha, SimisAceImageChannel mask) {
	//        Red = red;
	//        Green = green;
	//        Blue = blue;
	//        Alpha = alpha;
	//        Mask = mask;
	//        if ((Green != null) && (Green.Width != Width)) throw new ArgumentException("Green channel width is different.", "green");
	//        if ((Green != null) && (Green.Height != Height)) throw new ArgumentException("Green channel height is different.", "green");
	//        if ((Blue != null) && (Blue.Width != Width)) throw new ArgumentException("Blue channel width is different.", "blue");
	//        if ((Blue != null) && (Blue.Height != Height)) throw new ArgumentException("Blue channel height is different.", "blue");
	//        if ((Alpha != null) && (Alpha.Width != Width)) throw new ArgumentException("Alpha channel width is different.", "alpha");
	//        if ((Alpha != null) && (Alpha.Height != Height)) throw new ArgumentException("Alpha channel height is different.", "alpha");
	//        if ((Mask != null) && (Mask.Width != Width)) throw new ArgumentException("Mask channel width is different.", "mask");
	//        if ((Mask != null) && (Mask.Height != Height)) throw new ArgumentException("Mask channel height is different.", "mask");
	//    }

	//    public override Image Image {
	//        get {
	//            var image = new Bitmap(Width, Height, PixelFormat.Format32bppArgb);
	//            var data = image.LockBits(new Rectangle(Point.Empty, image.Size), ImageLockMode.WriteOnly, image.PixelFormat);
	//            var imageData = new byte[data.Stride * data.Height];
	//            for (var y = 0; y < Height; y++) {
	//                for (var x = 0; x < Width; x++) {
	//                    var imageDataOffset = (data.Stride * y + x) * 4;
	//                    var channelOffset = Width * y + x;
	//                    if (Red != null) {
	//                        imageData[imageDataOffset + 1] = Red.Data[channelOffset];
	//                    }
	//                    if (Green != null) {
	//                        imageData[imageDataOffset + 2] = Green.Data[channelOffset];
	//                    }
	//                    if (Blue != null) {
	//                        imageData[imageDataOffset + 3] = Blue.Data[channelOffset];
	//                    }
	//                    if (Alpha != null) {
	//                        imageData[imageDataOffset + 0] = Alpha.Data[channelOffset];
	//                    } else if (Mask != null) {
	//                        imageData[imageDataOffset + 0] = Mask.Data[channelOffset];
	//                    }
	//                }
	//            }
	//            Marshal.Copy(imageData, 0, data.Scan0, imageData.Length);
	//            image.UnlockBits(data);
	//            return image;
	//        }
	//    }
	//}

	//[Immutable]
	//public class SimisAceImageChannel : DataTreeNode<SimisAceImageChannel> {
	//    public readonly int Width;
	//    public readonly int Height;
	//    public IList<byte> Data { get { return DataArray; } }
	//    readonly byte[] DataArray;

	//    SimisAceImageChannel(int width, int height, byte[] dataArray) {
	//        Width = width;
	//        Height = height;
	//        DataArray = dataArray;
	//    }

	//    public Image Image {
	//        get {
	//            var image = new Bitmap(Width, Height, PixelFormat.Format8bppIndexed);
	//            for (var i = 0; i < 256; i++) {
	//                image.Palette.Entries[i] = Color.FromArgb(i, i, i);
	//            }
	//            var data = image.LockBits(new Rectangle(Point.Empty, image.Size), ImageLockMode.WriteOnly, image.PixelFormat);
	//            if (data.Stride == Width) {
	//                Marshal.Copy(DataArray, 0, data.Scan0, Width * Height);
	//            } else {
	//                for (var i = 0; i < Height; i++) {
	//                    Marshal.Copy(DataArray, Width * i, new IntPtr((int)data.Scan0 + data.Stride * i), Width);
	//                }
	//            }
	//            image.UnlockBits(data);
	//            return image;
	//        }
	//    }
	//}

	//[Immutable]
	//public class SimisAceImageDXT1 : SimisAceImage {
	//    public override int Width { get { throw new NotImplementedException(); } }
	//    public override int Height { get { throw new NotImplementedException(); } }
	//    public override Image Image { get { throw new NotImplementedException(); } }
	//}
}
