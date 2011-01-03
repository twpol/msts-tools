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
	public static class ImageComparison {
		public static double GetRootMeanSquareError(Bitmap image1, Bitmap image2, int scoreWidth, int scoreHeight) {
			if (image1 == null) throw new ArgumentNullException("image1");
			if (image2 == null) throw new ArgumentNullException("image2");
			if (image1.Width != image2.Width) throw new ArgumentException("Images must have the same width.");
			if (image1.Height != image2.Height) throw new ArgumentException("Images must have the same height.");
			if (!new[] { PixelFormat.Format32bppArgb, PixelFormat.Format32bppPArgb, PixelFormat.Format32bppRgb }.Contains(image1.PixelFormat)) throw new ArgumentException("Images must have a 32bpp pixel format.", "image1");
			if (!new[] { PixelFormat.Format32bppArgb, PixelFormat.Format32bppPArgb, PixelFormat.Format32bppRgb }.Contains(image2.PixelFormat)) throw new ArgumentException("Images must have a 32bpp pixel format.", "image2");
			if (image1.PixelFormat != image2.PixelFormat) throw new ArgumentException("Images must have the same PixelFormat.");

			var hasAlpha = image1.PixelFormat != PixelFormat.Format32bppRgb;
			var width = image1.Width;
			var height = image1.Height;
			var size = new Size(width, height);

			var image1Bits = image1.LockBits(new Rectangle(Point.Empty, size), ImageLockMode.ReadOnly, image1.PixelFormat);
			var image2Bits = image2.LockBits(new Rectangle(Point.Empty, size), ImageLockMode.ReadOnly, image2.PixelFormat);

			Debug.Assert(image1Bits.Stride == width * 4);
			Debug.Assert(image2Bits.Stride == width * 4);

			var image1Data = new byte[width * height * 4];
			var image2Data = new byte[width * height * 4];

			Marshal.Copy(image1Bits.Scan0, image1Data, 0, image1Data.Length);
			Marshal.Copy(image2Bits.Scan0, image2Data, 0, image2Data.Length);

			image1.UnlockBits(image1Bits);
			image2.UnlockBits(image2Bits);

			var indexes = hasAlpha ? new[] { 0, 1, 2, 3 } : new[] { 1, 2, 3 };
			long error = 0;
			for (var y = 0; y < height; y++) {
				for (var x = 0; x < width; x++) {
					var baseIndex = (width * y + x) * 4;
					var image1Color = image1Data[width * y + x];
					var image2Color = image2Data[width * y + x];

					foreach (var i in indexes) {
						error += (int)Math.Pow(image1Data[baseIndex + i] - image2Data[baseIndex + i], 2);
					}
				}
			}

			var pixelCount = scoreWidth * scoreHeight * (hasAlpha ? 4 : 3);

			return Math.Sqrt((double)error / pixelCount);
		}
	}
}
