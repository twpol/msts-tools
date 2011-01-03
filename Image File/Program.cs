//------------------------------------------------------------------------------
// Image File, part of MSTS Editors & Tools (http://jgrmsts.codeplex.com/).
// License: New BSD License (BSD).
//------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using Jgr;
using Jgr.IO.Parser;

namespace ImageFile {
	class ConversionFile {
		public string Input;
		public string Output;
	}

	class Program {
		static void Main(string[] args) {
			// Flags: at least 2 characters, starts with "/" or "-", stored without the "/" or "-".
			// Items: anything not starting "/" or "-".
			// "/" or "-" alone is ignored.
			var flags = args.Where(s => (s.Length > 1) && (s.StartsWith("/", StringComparison.Ordinal) || s.StartsWith("-", StringComparison.Ordinal))).Select(s => s.Substring(1));
			var items = args.Where(s => !s.StartsWith("/", StringComparison.Ordinal) && !s.StartsWith("-", StringComparison.Ordinal));

			var verbose = flags.Any(s => "verbose".StartsWith(s, StringComparison.OrdinalIgnoreCase));

			var jFlag = flags.LastOrDefault(s => s.StartsWith("j", StringComparison.OrdinalIgnoreCase));
			var threading = 1;
			if (jFlag != null) {
				threading = jFlag.Length > 1 ? int.Parse(jFlag.Substring(1)) : Environment.ProcessorCount;
			}

			var convertRoundtrip = flags.Any(s => s.Equals("roundtrip", StringComparison.OrdinalIgnoreCase));
			var convertTexture = flags.Any(s => s.Equals("texture", StringComparison.OrdinalIgnoreCase));
			var convertDXT1 = flags.Any(s => s.Equals("dxt1", StringComparison.OrdinalIgnoreCase));
			var convertZLIB = flags.Any(s => s.Equals("zlib", StringComparison.OrdinalIgnoreCase));

			try {
				if (flags.Contains("?") || flags.Contains("h")) {
					ShowHelp();
				} else if (flags.Any(s => "convert".StartsWith(s, StringComparison.OrdinalIgnoreCase))) {
					DoConversion(ExpandConversionFiles(items), verbose, threading, convertRoundtrip, convertTexture, convertDXT1, convertZLIB);
				} else {
					ShowHelp();
				}
			} catch (OperationCanceledException ex) {
				Console.Error.WriteLine(ex.Message);
			}
			if (flags.Contains("pause")) {
				Thread.Sleep(10000);
			}
		}

		static void ShowHelp() {
			Console.WriteLine("Performs operations on individual or collections of image files.");
			Console.WriteLine();
			Console.WriteLine("  IMAGEFILE /C[ONVERT] [/V[ERBOSE]] [/J[threads]] input output [...]");
			Console.WriteLine("            [/ROUNDTRIP] [/TEXTURE] [/DXT1 | /ZLIB]");
			Console.WriteLine();
			//                 12345678901234567890123456789012345678901234567890123456789012345678901234567890
			Console.WriteLine("  /CONVERT  Performs an image format conversion between any two supported");
			Console.WriteLine("            image formats (see below).");
			Console.WriteLine("  /VERBOSE  Produces more output. For /CONVERT, the file names are printed as");
			Console.WriteLine("            they are converted.");
			Console.WriteLine("  /J        Uses multiple threads for running the operation. By default, the");
			Console.WriteLine("            number of threads equals the number of logical processors.");
			Console.WriteLine("  threads   Explicitly specifies the number of threads to use.");
			Console.WriteLine("  input output");
			Console.WriteLine("            Specifies a pair of files to convert. Multiple pairs may be used to");
			Console.WriteLine("            convert multiple files at once. Wildcards and directories may be");
			Console.WriteLine("            used for input files; in this case, output file names will be the");
			Console.WriteLine("            input file names with the output file's extension.");
			Console.WriteLine("  /ROUNDTRIP");
			Console.WriteLine("            Specifies that the conversion must preserve everything as best as");
			Console.WriteLine("            it possibly can; what this means is that conversion to/from ACE");
			Console.WriteLine("            files will include all the mipmaps and masks (if they exist).");
			Console.WriteLine("  /TEXTURE  For conversion to ACE only; specifies that the output should be");
			Console.WriteLine("            marked as a texture and contain mipmaps. Input images must be");
			Console.WriteLine("            square, with width and height an integral power-of-2.");
			Console.WriteLine("  /DXT1     For conversion to ACE only; specifies that the output should be");
			Console.WriteLine("            compressed using the lossy DXT1 format. If there is no mask in");
			Console.WriteLine("            the input, any alpha will be converted to a mask.");
			Console.WriteLine("  /ZLIB     For conversion to ACE only; specifies that the output should be");
			Console.WriteLine("            compressed using the lossless ZLIB format.");
			Console.WriteLine();
			Console.WriteLine("Supported Image Formats:");
			Console.WriteLine("  ACE   Simis format used by MSTS");
			Console.WriteLine("  BMP   Bitmap");
			Console.WriteLine("  EXIF  Exchangeable Image File");
			Console.WriteLine("  GIF   Graphics Interchange Format");
			Console.WriteLine("  JPEG  Joint Photographic Experts Group");
			Console.WriteLine("  PNG   Portable Network Graphics");
			Console.WriteLine("  TIFF  Tagged Image File Format");
		}

		static IEnumerable<string> ExpandFilesAndDirectories(IEnumerable<string> items) {
			foreach (var item in items) {
				if (File.Exists(item)) {
					yield return item;
				} else if (Directory.Exists(item)) {
					foreach (var filepath in Directory.GetFiles(item, "*", SearchOption.AllDirectories)) {
						yield return filepath;
					}
				} else if (item.Contains("?") || item.Contains("*")) {
					var rootpath = Path.GetDirectoryName(item);
					var filename = Path.GetFileName(item);
					foreach (var filepath in Directory.GetFileSystemEntries(rootpath, filename)) {
						yield return filepath;
					}
					foreach (var path in Directory.GetDirectories(rootpath, "*", SearchOption.AllDirectories)) {
						foreach (var filepath in Directory.GetFileSystemEntries(path, filename)) {
							yield return filepath;
						}
					}
				} else {
					yield return item;
				}
			}
			yield break;
		}

		static IEnumerable<ConversionFile> ExpandConversionFiles(IEnumerable<string> items) {
			var input = "";
			foreach (var output in items) {
				if (input.Length == 0) {
					input = output;
					continue;
				}
				// TODO
				if (File.Exists(input)) {
					yield return new ConversionFile() { Input = input, Output = output };
				} else if (Directory.Exists(input)) {
					var ext = Path.GetExtension(output);
					foreach (var filepath in Directory.GetFiles(input, "*", SearchOption.AllDirectories)) {
						yield return new ConversionFile() { Input = filepath, Output = Path.ChangeExtension(filepath, ext) };
					}
				} else if (input.Contains("?") || input.Contains("*")) {
					var ext = Path.GetExtension(output);
					var rootpath = Path.GetDirectoryName(input);
					var filename = Path.GetFileName(input);
					foreach (var filepath in Directory.GetFileSystemEntries(rootpath, filename)) {
						yield return new ConversionFile() { Input = filepath, Output = Path.ChangeExtension(filepath, ext) };
					}
					foreach (var path in Directory.GetDirectories(rootpath, "*", SearchOption.AllDirectories)) {
						foreach (var filepath in Directory.GetFileSystemEntries(path, filename)) {
							yield return new ConversionFile() { Input = filepath, Output = Path.ChangeExtension(filepath, ext) };
						}
					}
				} else {
					yield return new ConversionFile() { Input = input, Output = output };
				}
				input = "";
			}
			if (input.Length != 0) throw new OperationCanceledException("Must specify an even number of files for conversion.");
			yield break;
		}

		static void DoConversion(IEnumerable<ConversionFile> files, bool verbose, int threading, bool convertRoundtrip, bool convertTexture, bool convertDXT1, bool convertZLIB) {
			if (!files.Any()) throw new OperationCanceledException("Must specify files for conversion.");

			SimisProvider provider;
			try {
				provider = new SimisProvider(Path.GetDirectoryName(Application.ExecutablePath) + @"\Resources");
			} catch (FileException ex) {
				Console.WriteLine(ex.ToString());
				return;
			}

			Action<ConversionFile> ConvertFile = (file) => {
				if (verbose) {
					lock (files) {
						if (threading > 1) {
							Console.WriteLine(String.Format("[Thread {0}] {1} -> {2}", Thread.CurrentThread.ManagedThreadId, file.Input, file.Output));
						} else {
							Console.WriteLine(String.Format("{0} -> {1}", file.Input, file.Output));
						}
					}
				}

				try {
					var inputExt = Path.GetExtension(file.Input).ToUpperInvariant();
					var outputExt = Path.GetExtension(file.Output).ToUpperInvariant();
					if ((inputExt == ".ACE") && (outputExt == ".ACE")) {
						// ACE -> ACE
						var inputAce = new SimisFile(file.Input, provider);
						var outputAce = new SimisFile(file.Output, true, convertZLIB, inputAce.Ace);
						outputAce.Write();
					} else if (inputExt == ".ACE") {
						// ACE -> ***
						var inputAce = new SimisFile(file.Input, provider);
						var width = convertRoundtrip ? inputAce.Ace.Image.Max(i => i.Width) : inputAce.Ace.Image[0].Width;
						var height = convertRoundtrip ? inputAce.Ace.Image.Sum(i => i.Height) : inputAce.Ace.Image[0].Height;
						var outputImage = new Bitmap(width * (convertRoundtrip ? 2 : 1), height, PixelFormat.Format32bppArgb);
						using (var g = Graphics.FromImage(outputImage)) {
							g.FillRectangle(Brushes.Transparent, 0, 0, outputImage.Width, outputImage.Height);
							if (convertRoundtrip) {
								var y = 0;
								foreach (var image in inputAce.Ace.Image) {
									g.DrawImageUnscaled(image.ImageColor, 0, y);
									g.DrawImageUnscaled(image.ImageMask, width, y);
									y += image.Height;
								}
							} else {
								g.DrawImageUnscaled(inputAce.Ace.Image[0].GetImage(inputAce.Ace.HasAlpha ? SimisAceImageType.ColorAndAlpha : inputAce.Ace.HasMask ? SimisAceImageType.ColorAndMask : SimisAceImageType.ColorOnly), 0, 0);
							}
						}
						outputImage.Save(file.Output);
					} else if (outputExt == ".ACE") {
						// *** -> ACE
						var inputImage = Image.FromFile(file.Input);
						var width = inputImage.Width;
						var height = inputImage.Height;

						// Roundtripping or not, textures have special requirements of 2^n width and height.
						if (convertTexture) {
							if (convertRoundtrip) {
								var expectedHeight = (int)Math.Pow(2, (int)(Math.Log(height + 1) / Math.Log(2))) - 1;
								if (height != expectedHeight) throw new InvalidOperationException(String.Format("Image height {0} is not correct for round-tripping a texture. It must be 2^n-1.", height, expectedHeight));
								height = (height + 1) / 2;
								// Roundtripping always has two columns: color and mask.
								width /= 2;
							} else {
								var expectedHeight = (int)Math.Pow(2, (int)(Math.Log(height) / Math.Log(2)));
								if (height != expectedHeight) throw new InvalidOperationException(String.Format("Image height {0} is not correct for a texture. It must be 2^n.", height, expectedHeight));
							}
							var expectedWidth = (int)Math.Pow(2, (int)(Math.Log(width) / Math.Log(2)));
							if (width != expectedWidth) throw new InvalidOperationException(String.Format("Image width {0} is not correct for a texture. It must be 2^n.", width, expectedWidth));
							if (width != height) throw new InvalidOperationException(String.Format("Image width {0} and height {1} must be equal for a texture.", width, height));
						}

						if (convertRoundtrip || convertTexture) {
							var imageCount = 1 + (int)(convertTexture ? Math.Log(height) / Math.Log(2) : 0);
							var aceChannels = new[] {
										new SimisAceChannel(8, SimisAceChannelId.Red),
										new SimisAceChannel(8, SimisAceChannelId.Green),
										new SimisAceChannel(8, SimisAceChannelId.Blue),
										new SimisAceChannel(8, SimisAceChannelId.Alpha),
										new SimisAceChannel(1, SimisAceChannelId.Mask),
									};
							// Remove the alpha channel for DXT1.
							if (convertDXT1) {
								aceChannels = new[] { aceChannels[0], aceChannels[1], aceChannels[2], aceChannels[4] };
							}
							var aceImages = new SimisAceImage[imageCount];
							var y = 0;
							for (var i = 0; i < imageCount; i++) {
								var scale = (int)Math.Pow(2, i);
								var colorImage = new Bitmap(width / scale, height / scale, PixelFormat.Format32bppArgb);
								var maskImage = new Bitmap(width / scale, height / scale, PixelFormat.Format32bppRgb);
								using (var g = Graphics.FromImage(colorImage)) {
									g.DrawImage(inputImage, new Rectangle(Point.Empty, colorImage.Size), new Rectangle(0, y, colorImage.Width, colorImage.Height), GraphicsUnit.Pixel);
								}
								using (var g = Graphics.FromImage(maskImage)) {
									g.DrawImage(inputImage, new Rectangle(Point.Empty, maskImage.Size), new Rectangle(width, y, maskImage.Width, maskImage.Height), GraphicsUnit.Pixel);
								}
								aceImages[i] = new SimisAceImage(colorImage, maskImage);
								y += colorImage.Height;
							}
							var ace = new SimisAce((convertDXT1 ? 0x10 : 0x00) + (convertTexture ? 0x05 : 0x00), width, height, convertDXT1 ? 0x12 : 0x00, 0, "Unknown", "JGR Image File", new byte[44], aceChannels, aceImages, new byte[0], new byte[0]);
							var aceFile = new SimisFile(file.Output, true, convertZLIB, ace);
							aceFile.Write();
						} else {
							// TODO: Handle the various alpha/mask fun here.
							var aceChannels = new[] {
										new SimisAceChannel(8, SimisAceChannelId.Red),
										new SimisAceChannel(8, SimisAceChannelId.Green),
										new SimisAceChannel(8, SimisAceChannelId.Blue),
										new SimisAceChannel(8, SimisAceChannelId.Alpha),
									};
							// Replace the alpha channel with mask channel for DXT1.
							if (convertDXT1) {
								aceChannels[3] = new SimisAceChannel(1, SimisAceChannelId.Mask);
							}
							var maskImage = new Bitmap(width, height, PixelFormat.Format32bppRgb);
							using (var g = Graphics.FromImage(maskImage)) {
								g.FillRectangle(Brushes.White, 0, 0, maskImage.Width, maskImage.Height);
							}
							var aceImages = new[] {
								new SimisAceImage(new Bitmap(inputImage), maskImage),
							};
							var ace = new SimisAce(convertDXT1 ? 0x10 : 0x00, width, height, convertDXT1 ? 0x12 : 0x00, 0, "Unknown", "JGR Image File", new byte[44], aceChannels, aceImages, new byte[0], new byte[0]);
							var aceFile = new SimisFile(file.Output, true, convertZLIB, ace);
							aceFile.Write();
						}
					} else {
						// *** -> ***
						Image.FromFile(file.Input).Save(file.Output);
					}
				} catch (Exception ex) {
					Console.Error.WriteLine(ex.ToString());
				}
			};

			if (threading > 1) {
				var filesEnumerator = files.GetEnumerator();
				var filesFinished = false;
				var threads = new List<Thread>(threading);
				for (var i = 0; i < threading; i++) {
					threads.Add(new Thread(() => {
						ConversionFile file;
						while (true) {
							lock (filesEnumerator) {
								if (filesFinished || !filesEnumerator.MoveNext()) {
									filesFinished = true;
									break;
								}
								file = filesEnumerator.Current;
							}
							ConvertFile(file);
						}
					}));
				}
				foreach (var thread in threads) {
					thread.Start();
				}
				foreach (var thread in threads) {
					thread.Join();
				}
			} else {
				foreach (var file in files) {
					ConvertFile(file);
				}
			}
		}
	}
}
