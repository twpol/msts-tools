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
using System.Text;

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
			Writer.Write(ace.Channel.Count);
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
				// TODO: Check width/height are a power-of-2 dimension.
				switch (ace.Unknown4) {
					case 0x12:
						// DXT1
						if (ace.Channel.Any(c => c.Type == SimisAceChannelId.Alpha)) {
							throw new InvalidDataException("Alpha channel not supported with DXT1 ACE files (use a mask instead).");
						}

						// Jump table: offsets to start of each image.
						var dataSize = 152 + 16 * ace.Channel.Count + 4 * ace.Image.Count;
						foreach (var image in ace.Image) {
							Writer.Write(dataSize);
							dataSize += GetImageDataBlockSize(ace.Channel, true, image);
						}

						foreach (var image in ace.Image) {
							if ((image.Width >= 4) && (image.Height >= 4)) {
								Writer.Write(GetImageDataBlockSize(ace.Channel, true, image));

								var imageBits = image.ImageColor.LockBits(new Rectangle(Point.Empty, image.ImageColor.Size), ImageLockMode.ReadOnly, image.ImageColor.PixelFormat);
								Debug.Assert(imageBits.Stride == image.Width * 4);
								var imageData = new int[image.Width * image.Height];
								Marshal.Copy(imageBits.Scan0, imageData, 0, imageData.Length);
								image.ImageColor.UnlockBits(imageBits);

								for (var y = 0; y < image.Height; y += 4) {
									for (var x = 0; x < image.Width; x += 4) {
										var colors = new Matrix(16, 4);
										var componentMeans = new Matrix(1, 4);
										var hasAlpha = false;
										for (var x2 = 0; x2 < 4; x2++) {
											for (var y2 = 0; y2 < 4; y2++) {
												var color = imageData[image.Width * (y + y2) + x + x2];
												for (var component = 0; component < 4; component++) {
													var colorComponent = (color >> (24 - 8 * component)) & 0xFF;
													colors.Values[y2 * 4 + x2, component] = colorComponent;
													componentMeans.Values[0, component] += (double)colorComponent / 16;
												}
												hasAlpha |= (color >> 24) > 0;
											}
										}

										var colorsCentered = new Matrix(16, 4);
										for (var color = 0; color < 16; color++) {
											for (var component = 0; component < 4; component++) {
												colorsCentered.Values[color, component] = colors.Values[color, component] - componentMeans.Values[0, component];
											}
										}

										// Covariance.
										Func<int, int, double> cov = (componentA, componentB) => {
											var sum = 0d;
											for (var color = 0; color < 16; color++) {
												sum += colorsCentered.Values[color, componentA] * colorsCentered.Values[color, componentB];
											}
											return sum / (16 - 1);
										};

										var A = new Matrix(3, 3,
											cov(1, 1), cov(1, 2), cov(1, 3),
											cov(2, 1), cov(2, 2), cov(2, 3),
											cov(3, 1), cov(3, 2), cov(3, 3)
										);
										var R = new Matrix(3, 3);
										var Q = new Matrix(3, 3);
										var QROkay = false;

										// QR decomposition via Householder transformation:
										// If |u1| or |u2| is zero, we will get NaNs in the decomposition.
										var a1 = A.GetColumn(0);
										var u1 = a1 - a1.GetEuclideanNorm() * new Matrix(3, 1, 1, 0, 0);
										if (u1.GetEuclideanNorm() > double.Epsilon) {
											var v1 = u1 / u1.GetEuclideanNorm();
											var Q1 = Matrix.Identity(3) - 2 * v1 * v1.GetTranspose();

											Q = Q1.GetTranspose();
											R = Q1 * A;
											QROkay = true;

											var a2 = R.GetMinor(1, 1).GetColumn(0);
											var u2 = a2 - a2.GetEuclideanNorm() * new Matrix(2, 1, 1, 0);
											if (u2.GetEuclideanNorm() > double.Epsilon) {
												var v2 = u2 / u2.GetEuclideanNorm();
												var Q2 = Matrix.Identity(2) - 2 * v2 * v2.GetTranspose();
												Q2 = Q2.GetIdentityExpansion(1, 3);

												Q = Q * Q2.GetTranspose();
												R = Q2 * R;
												QROkay = true;
											}
										}

										if (QROkay) {
											var largestEigenValue = R.Values[0, 0] > R.Values[1, 1] && R.Values[0, 0] > R.Values[2, 2] ? 0 : R.Values[1, 1] > R.Values[2, 2] ? 1 : 2;
											//Console.WriteLine("{0,9:F3}, {1,9:F3}, {2,9:F3} --> {3}", R.Values[0, 0], R.Values[1, 1], R.Values[2, 2], largestEigenValue);
											//Console.WriteLine("{0} --> eigen = {1,9:F3} * {3} ({2})", A, R.Values[largestEigenValue, largestEigenValue], largestEigenValue, Q.GetColumn(largestEigenValue));

											var featureVector = Q.GetColumn(largestEigenValue).GetNormalized();
											var finalData = colorsCentered.GetMinor(0, 1) * featureVector;
											//Console.WriteLine(finalData);

											var finalDataList = finalData.GetColumnList(0);
											var finalDataListMin = finalDataList.Min();
											var finalDataListMax = finalDataList.Max();
											var min = finalDataListMin * featureVector + componentMeans.GetMinor(0, 1).GetTranspose();
											var max = finalDataListMax * featureVector + componentMeans.GetMinor(0, 1).GetTranspose();
											//Console.WriteLine("Min = {0}, max = {1}", min, max);
											//Console.WriteLine();

											var minColor = new[] { ClampColor(min.Values[0, 0]), ClampColor(min.Values[1, 0]), ClampColor(min.Values[2, 0]) };
											var maxColor = new[] { ClampColor(max.Values[0, 0]), ClampColor(max.Values[1, 0]), ClampColor(max.Values[2, 0]) };
											var midColor = new[] { (minColor[0] + maxColor[0]) / 2, (minColor[1] + maxColor[1]) / 2, (minColor[2] + maxColor[2]) / 2 };

											var minColorValue = ((minColor[0] << 8) & 0xF800) + ((minColor[1] << 3) & 0x07E0) + ((minColor[2] >> 3) & 0x001F);
											var maxColorValue = ((maxColor[0] << 8) & 0xF800) + ((maxColor[1] << 3) & 0x07E0) + ((maxColor[2] >> 3) & 0x001F);
											var midColorValue = ((midColor[0] << 8) & 0xF800) + ((midColor[1] << 3) & 0x07E0) + ((midColor[2] >> 3) & 0x001F);

											var swapped = minColorValue > maxColorValue;
											if (swapped) {
												var temp = minColor;
												minColor = maxColor;
												maxColor = temp;
												var tempValue = minColorValue;
												minColorValue = maxColorValue;
												maxColorValue = tempValue;
											}

											Writer.Write((ushort)minColorValue);
											Writer.Write((ushort)maxColorValue);

											var indicies = 0;
											for (var i = 0; i < 16; i++) {
												if (colors.Values[i, 0] < 0x80) {
													indicies += 3 << (i * 2);
												} else {
													var colorErrors = new List<double>(new[] {
															Math.Abs(colors.Values[i, 1] - minColor[0]) + Math.Abs(colors.Values[i, 2] - minColor[1]) + Math.Abs(colors.Values[i, 3] - minColor[2]),
															Math.Abs(colors.Values[i, 1] - maxColor[0]) + Math.Abs(colors.Values[i, 2] - maxColor[1]) + Math.Abs(colors.Values[i, 3] - maxColor[2]),
															Math.Abs(colors.Values[i, 1] - midColor[0]) + Math.Abs(colors.Values[i, 2] - midColor[1]) + Math.Abs(colors.Values[i, 3] - midColor[2])
														});
													var index = colorErrors.IndexOf(colorErrors.Min());
													Debug.Assert(index >= 0 && index <= 3);
													indicies += index << (i * 2);
												}
											}
											Writer.Write(indicies);
										} else {
											var colorValue = (((int)colors.Values[0, 1] << 8) & 0xF800) + (((int)colors.Values[0, 2] << 3) & 0x07E0) + (((int)colors.Values[0, 3] >> 3) & 0x001F);
											Writer.Write((ushort)colorValue);
											Writer.Write((ushort)colorValue);
											var indicies = 0;
											for (var i = 0; i < 16; i++) {
												if (colors.Values[i, 0] < 0x80) {
													indicies += 3 << (i * 2);
												}
											}
											Writer.Write(indicies);
										}
									}
								}
							} else {
								Writer.Write(new byte[GetImageDataBlockSize(ace.Channel, true, image)]);
							}
						}

						break;
					default:
						throw new NotSupportedException("ACE DXT format unknown4=" + ace.Unknown4 + " is not supported.");
				}
			} else {
				// RGB format.

				// Jump table: offsets to start of each scanline of each image.
				var dataSize = 152 + 16 * ace.Channel.Count + 4 * ace.Image.Sum(i => i.Height);
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

		int ClampColor(double value) {
			var rv = (int)value;
			if (rv < 0) rv = 0;
			else if (rv > 255) rv = 255;
			return rv;
		}

		struct Matrix {
			public readonly int Height;
			public readonly int Width;
			public readonly double[,] Values;

			public Matrix(int h, int w) {
				Height = h;
				Width = w;
				Values = new double[Height, Width];
			}

			public Matrix(int h, int w, params double[] values)
				: this(h, w) {
				Debug.Assert(w * h == values.Length);
				for (var i = 0; i < w * h; i++) {
					Values[i / w, i % w] = values[i];
				}
			}

			public Matrix(int h, int w, double[,] values)
				: this(h, w) {
				Debug.Assert(h == values.GetLength(0));
				Debug.Assert(w == values.GetLength(1));
				for (var y = 0; y < Height; y++) {
					for (var x = 0; x < Width; x++) {
						Values[y, x] = values[y, x];
					}
				}
			}

			public override string ToString() {
				var sb = new StringBuilder();
				sb.Append('[');
				for (var y = 0; y < Height; y++) {
					sb.Append('[');
					for (var x = 0; x < Width; x++) {
						if (x > 0) sb.Append(", ");
						sb.AppendFormat("{0,9:F3}", Values[y, x]);
					}
					sb.Append(']');
				}
				sb.Append(']');
				return sb.ToString();
			}

			public double GetEuclideanNorm() {
				Debug.Assert(Width == 1 || Height == 1);
				var sum = 0d;
				for (var y = 0; y < Height; y++) {
					for (var x = 0; x < Width; x++) {
						sum += Values[y, x] * Values[y, x];
					}
				}
				return Math.Sqrt(sum);
			}

			public Matrix GetNormalized() {
				return this * GetEuclideanNorm();
			}

			public Matrix GetRow(int row) {
				var m = new Matrix(1, Width);
				for (var x = 0; x < Width; x++) {
					m.Values[0, x] = Values[row, x];
				}
				return m;
			}

			public double[] GetRowList(int row) {
				var list = new double[Width];
				for (var x = 0; x < Width; x++) {
					list[x] = Values[row, x];
				}
				return list;
			}

			public Matrix GetColumn(int column) {
				var m = new Matrix(Height, 1);
				for (var y = 0; y < Height; y++) {
					m.Values[y, 0] = Values[y, column];
				}
				return m;
			}

			public double[] GetColumnList(int column) {
				var list = new double[Height];
				for (var y = 0; y < Height; y++) {
					list[y] = Values[y, column];
				}
				return list;
			}

			public Matrix GetTranspose() {
				var m = new Matrix(Width, Height);
				for (var y = 0; y < Height; y++) {
					for (var x = 0; x < Width; x++) {
						m.Values[x, y] = Values[y, x];
					}
				}
				return m;
			}

			public Matrix GetMinor(int y, int x) {
				return GetMinor(y, x, Height - y, Width - x);
			}

			public Matrix GetMinor(int oy, int ox, int h, int w) {
				Debug.Assert(oy >= 0);
				Debug.Assert(oy < Height);
				Debug.Assert(ox >= 0);
				Debug.Assert(ox < Width);
				Debug.Assert(h > 0);
				Debug.Assert(oy + h <= Height);
				Debug.Assert(w > 0);
				Debug.Assert(ox + w <= Width);
				var m = new Matrix(h, w);
				for (var y = 0; y < m.Height; y++) {
					for (var x = 0; x < m.Width; x++) {
						m.Values[y, x] = Values[y + oy, x + ox];
					}
				}
				return m;
			}

			public Matrix GetIdentityExpansion(int offset, int size) {
				Debug.Assert(Height == Width);
				var m = new Matrix(size, size);
				for (var i = 0; i < size; i++) {
					if ((i < offset) || (i >= Height + offset)) {
						m.Values[i, i] = 1;
					}
				}
				for (var y = 0; y < Height; y++) {
					for (var x = 0; x < Width; x++) {
						m.Values[y + offset, x + offset] = Values[y, x];
					}
				}
				return m;
			}

			public static Matrix Identity(int size) {
				var m = new Matrix(size, size);
				for (var i = 0; i < size; i++) {
					m.Values[i, i] = 1;
				}
				return m;
			}

			public static Matrix operator *(double d, Matrix m) {
				return m * d;
			}

			public static Matrix operator *(Matrix m, double d) {
				m = new Matrix(m.Height, m.Width, m.Values);
				for (var y = 0; y < m.Height; y++) {
					for (var x = 0; x < m.Width; x++) {
						m.Values[y, x] *= d;
					}
				}
				return m;
			}

			public static Matrix operator /(Matrix m, double d) {
				m = new Matrix(m.Height, m.Width, m.Values);
				for (var y = 0; y < m.Height; y++) {
					for (var x = 0; x < m.Width; x++) {
						m.Values[y, x] /= d;
					}
				}
				return m;
			}

			public static Matrix operator +(Matrix m1, Matrix m2) {
				Debug.Assert(m1.Height == m2.Height);
				Debug.Assert(m1.Width == m2.Width);
				var m = new Matrix(m1.Height, m1.Width, m1.Values);
				for (var y = 0; y < m.Height; y++) {
					for (var x = 0; x < m.Width; x++) {
						m.Values[y, x] += m2.Values[y, x];
					}
				}
				return m;
			}

			public static Matrix operator -(Matrix m1, Matrix m2) {
				Debug.Assert(m1.Height == m2.Height);
				Debug.Assert(m1.Width == m2.Width);
				var m = new Matrix(m1.Height, m1.Width, m1.Values);
				for (var y = 0; y < m.Height; y++) {
					for (var x = 0; x < m.Width; x++) {
						m.Values[y, x] -= m2.Values[y, x];
					}
				}
				return m;
			}

			public static Matrix operator *(Matrix m1, Matrix m2) {
				Debug.Assert(m1.Width == m2.Height);
				var m = new Matrix(m1.Height, m2.Width);
				for (var y = 0; y < m1.Height; y++) {
					for (var x = 0; x < m2.Width; x++) {
						for (var i = 0; i < m1.Width; i++) {
							m.Values[y, x] += m1.Values[y, i] * m2.Values[i, x];
						}						
					}
				}
				return m;
			}
		}
	}
}
