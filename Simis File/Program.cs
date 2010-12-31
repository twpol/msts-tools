﻿//------------------------------------------------------------------------------
// Simis File, part of MSTS Editors & Tools (http://jgrmsts.codeplex.com/).
// License: New BSD License (BSD).
//------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using Jgr;
using Jgr.IO;
using Jgr.IO.Parser;

namespace Normalize
{
	class TestFormatCount {
		public string SortKey;
		public string FormatName;
		public int Total;
		public int ReadSuccess;
		public int WriteSuccess;
	}

	class ProcessFileResults {
		public bool Total;
		public bool ReadSuccess;
		public bool WriteSuccess;
		public SimisJinxFormat JinxStreamFormat;
	}

	class Program
	{
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

			if (flags.Contains("?") || flags.Contains("h")) {
				ShowHelp();
			} else if (flags.Any(s => "formats".StartsWith(s, StringComparison.OrdinalIgnoreCase))) {
				ShowFormats();
			} else if (flags.Any(s => "dump".StartsWith(s, StringComparison.OrdinalIgnoreCase))) {
				RunDump(ExpandFilesAndDirectories(items), verbose);
			} else if (flags.Any(s => "normalize".StartsWith(s, StringComparison.OrdinalIgnoreCase))) {
				RunNormalize(ExpandFilesAndDirectories(items), verbose);
			} else if (flags.Any(s => "test".StartsWith(s, StringComparison.OrdinalIgnoreCase))) {
				RunTest(ExpandFilesAndDirectories(items), verbose, threading);
			} else {
				ShowHelp();
			}
			if (flags.Contains("pause")) {
				Thread.Sleep(10000);
			}
		}

		static void ShowHelp() {
			Console.WriteLine("Performs operations on individual or collections of Simis files.");
			Console.WriteLine();
			Console.WriteLine("  SIMISFILE /F[ORMATS]");
			Console.WriteLine();
			Console.WriteLine("  SIMISFILE /D[UMP] [/V[ERBOSE]] [file ...]");
			Console.WriteLine();
			Console.WriteLine("  SIMISFILE /N[ORMALIZE] [file ...]");
			Console.WriteLine();
			Console.WriteLine("  SIMISFILE /T[EST] [/V[ERBOSE]] [/J[threads]] [file ...] [dir ...]");
			Console.WriteLine();
			//                 12345678901234567890123456789012345678901234567890123456789012345678901234567890
			Console.WriteLine("  /FORMATS  Displays a list of the supported Simis file formats.");
			Console.WriteLine("  /DUMP     Reads all files and displays the resulting Simis tree for each.");
			Console.WriteLine("  /NORMALIZE");
			Console.WriteLine("            Normalizes the specified files for comparisons. For binary files,");
			Console.WriteLine("            this uncompresses the contents only. For text files, whitespace,");
			Console.WriteLine("            quoted strings and numbers are all sanitized. The normalized file");
			Console.WriteLine("            is written to the current directory, with the '.normalized'");
			Console.WriteLine("            extension added if the source file is also in the current");
			Console.WriteLine("            directory. This will overwrite files in the current directory only.");
			Console.WriteLine("  /TEST     Tests all the files specified and found in the directories");
			Console.WriteLine("            specified against the reading and writing code. No files are");
			Console.WriteLine("            changed. A report of read/write success by file type is produced.");
			Console.WriteLine("  /VERBOSE  Produces more output. For /DUMP and /TEST, displays the individual");
			Console.WriteLine("            failures encountered while reading or writing files.");
			Console.WriteLine("  /J        Uses multiple threads for running the operation. By default, the");
			Console.WriteLine("            number of threads equals the number of logical processors.");
			Console.WriteLine("  threads   Explicitly specifies the number of threads to use.");
			Console.WriteLine("  file      One or more Simis files to process.");
			Console.WriteLine("  dir       One or more directories containing Simis files. These will be");
			Console.WriteLine("            scanned recursively.");
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

		static void PrintSimisTree(int indent, SimisTreeNode node) {
			Console.Write(new String(' ', 2 * indent));
			var nodeValue = node as SimisTreeNodeValue;
			if (nodeValue != null) {
				if (nodeValue.Name.Length > 0) {
					Console.Write(nodeValue.Name);
					Console.Write(" ");
				}
				Console.Write("(");
				Console.Write(nodeValue.Type);
				Console.Write(")");
				Console.Write(": ");
				Console.WriteLine(nodeValue.Value);
			} else {
				Console.Write(node.Type);
				if (node.Name.Length > 0) {
					Console.Write(" \"");
					Console.Write(node.Name);
					Console.WriteLine("\" {");
				} else {
					Console.WriteLine(" {");
				}
				foreach (var child in node) {
					PrintSimisTree(indent + 1, child);
				}
				Console.Write(new String(' ', 2 * indent));
				Console.WriteLine("}");
			}
		}

		static void PrintSimisAce(SimisAce ace, string fileName) {
			Console.WriteLine("Ace {");
			PrintSimisAce(1, ace, fileName);
			Console.WriteLine("}");
		}

		static void PrintSimisAce(int indent, SimisAce ace, string fileName) {
			var indentString = new String(' ', 2 * indent);
			Console.WriteLine("{0}Format:        0x{1:X}", indentString, ace.Format);
			Console.WriteLine("{0}Width:         {1}", indentString, ace.Width);
			Console.WriteLine("{0}Height:        {1}", indentString, ace.Height);
			Console.WriteLine("{0}Unk4:          0x{1:X}", indentString, ace.Unknown4);
			Console.WriteLine("{0}Unk6:          0x{1:X}", indentString, ace.Unknown6);
			Console.WriteLine("{0}Unk7:          {1}", indentString, ace.Unknown7);
			Console.WriteLine("{0}Creator:       {1}", indentString, ace.Creator);
			for (var i = 0; i < 11; i++) {
				Console.WriteLine("{0}Unk9.{2:X}:        {1}", indentString, String.Join(" ", ace.Unknown9.Skip(i * 4).Take(4).Select(b => b.ToString("X2")).ToArray()), i);
			}
			Console.WriteLine("{0}Channels:      {1}", indentString, String.Join(", ", ace.Channel.Select(c => String.Format("{0} ({1} bits)", c.Type, c.Size)).ToArray()));

#if DEBUG_ACE
			Console.WriteLine("{0}D1: f={1:X2} unk4={2:X2} chs={3}", indentString, ace.Format, ace.Unknown4, String.Join(", ", ace.Channel.Select(c => String.Format("{0} ({1} bits)", c.Type, c.Size)).ToArray()));
			Console.WriteLine("{0}D2: f={1:X2} chs={3}", indentString, ace.Format, ace.Unknown4, String.Join(", ", ace.Channel.Select(c => String.Format("{0} ({1} bits)", c.Type, c.Size)).ToArray()));
			Console.WriteLine("{0}D3: unk4={2:X2} chs={3}", indentString, ace.Format, ace.Unknown4, String.Join(", ", ace.Channel.Select(c => String.Format("{0} ({1} bits)", c.Type, c.Size)).ToArray()));
			Console.WriteLine("{0}D4: f={1:X2} unk4={2:X2}", indentString, ace.Format, ace.Unknown4, String.Join(", ", ace.Channel.Select(c => String.Format("{0} ({1} bits)", c.Type, c.Size)).ToArray()));

			var fileBytes = (double)File.ReadAllBytes(fileName).Length - 168 - 16 * ace.Channel.Count;
			var imageCount = 1 + (int)((ace.Format & 0x01) == 0x01 ? Math.Log(ace.Width) / Math.Log(2) : 0);
			if ((ace.Format & 0x10) == 0x10) {
				fileBytes -= 4 * imageCount;
			} else {
				fileBytes -= 4 * imageCount * ace.Height;
			}
			var bytesPerPixel = (double)ace.Channel.Sum(c => c.Size) / 8;
			if ((ace.Format & 0x10) == 0x10) bytesPerPixel = 0.5;
			var imageBytes = ace.Width * ace.Height * bytesPerPixel;
			Console.WriteLine("{0}E1: f={1:X2} fs={2,8:F0} im={3,8:F0} ratio={4,3:F1} {5}", indentString, ace.Format, fileBytes, imageBytes, fileBytes / imageBytes, fileBytes / imageBytes > 1.1 ? "MIPS" : "");
#endif

			foreach (var channel in ace.Channel) {
				Console.WriteLine("{0}Channel {{", indentString);
				PrintSimisAce(indent + 1, channel);
				Console.WriteLine("{0}}}", indentString);
			}

			foreach (var image in ace.Image) {
				Console.WriteLine("{0}Image {{", indentString);
				PrintSimisAce(indent + 1, image);
				Console.WriteLine("{0}}}", indentString);
			}
		}

		static void PrintSimisAce(int indent, SimisAceChannel channel) {
			var indentString = new String(' ', 2 * indent);
			Console.WriteLine("{0}Type: {1}", indentString, channel.Type);
			Console.WriteLine("{0}Size: {1} bits", indentString, channel.Size);
		}

		static void PrintSimisAce(int indent, SimisAceImage image) {
			var indentString = new String(' ', 2 * indent);
			Console.WriteLine("{0}NOT SUPPORTED IN DUMP YET", indentString);
		}

		static void ShowFormats() {
			SimisProvider provider;
			try {
				provider = new SimisProvider(Path.GetDirectoryName(Application.ExecutablePath) + @"\Resources");
			} catch (FileException ex) {
				Console.WriteLine(ex.ToString());
				return;
			}

			var outFormat = "{0,-40:S}  {1,-15:S}  {2,-15:S}";
			Console.WriteLine(String.Format(CultureInfo.CurrentCulture, outFormat, "Format Name", "File Type", "Internal Type"));
			Console.WriteLine(String.Empty.PadLeft(40 + 2 + 15 + 2 + 15 + 2, '='));
			foreach (var format in provider.Formats) {
				Console.WriteLine(String.Format(CultureInfo.CurrentCulture, outFormat, format.Name, format.Extension, format.Format));
			}
		}

		static void RunDump(IEnumerable<string> files, bool verbose) {
			SimisProvider provider;
			try {
				provider = new SimisProvider(Path.GetDirectoryName(Application.ExecutablePath) + @"\Resources");
			} catch (FileException ex) {
				Console.WriteLine(ex.ToString());
				return;
			}

			foreach (var inputFile in files) {
				try {
					var parsedFile = new SimisFile(inputFile, provider.GetForPath(inputFile));
					Console.WriteLine(inputFile);
					if (parsedFile.Tree != null) {
						PrintSimisTree(0, parsedFile.Tree);
					} else if (parsedFile.Ace != null) {
						PrintSimisAce(parsedFile.Ace, inputFile);
					}
				} catch (Exception ex) {
					if (verbose) {
						Console.WriteLine("Read: " + ex + "\n");
					}
				}
			}
		}

		static void RunNormalize(IEnumerable<string> files, bool verbose) {
			foreach (var inputFile in files) {
				try {
					var inputStream = new SimisTestableStream(File.OpenRead(inputFile));
					var outputFile = Path.GetFileName(inputFile);
					if (inputFile == outputFile) {
						outputFile += ".normalized";
					}
					using (var outputStream = File.OpenWrite(outputFile)) {
						using (var outputStreamWriter = new BinaryWriter(outputStream, ByteEncoding.Encoding)) {
							while (inputStream.Position < inputStream.Length) {
								outputStreamWriter.Write((byte)inputStream.ReadByte());
							}
						}
					}
					Console.WriteLine(inputFile + " --> " + outputFile);
				} catch (Exception ex) {
					if (verbose) {
						Console.WriteLine("Read: " + ex + "\n");
					}
				}
			}
		}

		static void RunTest(IEnumerable<string> files, bool verbose, int threading) {
			SimisProvider provider;
			try {
				provider = new SimisProvider(Path.GetDirectoryName(Application.ExecutablePath) + @"\Resources");
			} catch (FileException ex) {
				Console.WriteLine(ex.ToString());
				return;
			}

			var totalCount = new TestFormatCount();
			var supportedCount = new TestFormatCount();
			var formatCounts = new Dictionary<string, TestFormatCount>();
			var timeStart = DateTime.Now;

			Func<SimisJinxFormat, TestFormatCount> GetFormatFor = (simisFormat) => {
				var formatName = simisFormat.Name;
				if (!formatCounts.ContainsKey(formatName)) {
					formatCounts[formatName] = new TestFormatCount() { FormatName = formatName, SortKey = formatName };
				}
				return formatCounts[formatName];
			};

			Func<string, ProcessFileResults> ProcessFile = (file) => {
				if (verbose && (threading > 1)) {
					lock (formatCounts) {
						Console.WriteLine(String.Format("[Thread {0}] {1}", Thread.CurrentThread.ManagedThreadId, file));
					}
				}

				var result = new ProcessFileResults();
				var formatCount = new TestFormatCount();
				var fileProvider = provider.GetForPath(file);
				SimisFile newFile = null;
				Stream readStream = new UnclosableStream(new BufferedInMemoryStream(File.OpenRead(file)));
				Stream saveStream = new UnclosableStream(new MemoryStream());

				{
					result.Total = true;
					try {
						using (var reader = SimisReader.FromStream(readStream, fileProvider)) {
							var readerJinx = reader as SimisJinxReader;
							var readerAce = reader as SimisAceReader;
							if (readerJinx != null) {
								readerJinx.ReadToken();
								if (readerJinx.JinxStreamFormat == null) {
									return result;
								}
								result.JinxStreamFormat = readerJinx.JinxStreamFormat;
							} else if (readerAce != null) {
								if (fileProvider.Formats.FirstOrDefault() == null) {
									return result;
								}
								result.JinxStreamFormat = fileProvider.Formats.First();
							} else {
								return result;
							}
						}
					} catch (ReaderException) {
						return result;
					}
					readStream.Position = 0;
				}

				// First, read the file in.
				try {
					try {
						newFile = new SimisFile(readStream, fileProvider);
					} catch (Exception e) {
						throw new FileException(file, e);
					}
					result.ReadSuccess = true;
				} catch (FileException ex) {
					if (verbose) {
						lock (formatCounts) {
							Console.WriteLine("Read: " + ex + "\n");
						}
					}
					return result;
				}

				// Second, write the file out into memory.
				try {
					try {
						newFile.Write(saveStream);
					} catch (Exception e) {
						throw new FileException(file, e);
					}
					// WriteSuccess is delayed until after the comparison. We won't claim write support without comparison support.
				} catch (FileException ex) {
					if (verbose) {
						lock (formatCounts) {
							Console.WriteLine("Write: " + ex + "\n");
						}
					}
					return result;
				}

				// Third, verify that the output is the same as the input.
				readStream.Seek(0, SeekOrigin.Begin);
				saveStream.Seek(0, SeekOrigin.Begin);
				var readReader = new BinaryReader(new SimisTestableStream(readStream), newFile.StreamIsBinary ? ByteEncoding.Encoding : Encoding.Unicode);
				var saveReader = new BinaryReader(new SimisTestableStream(saveStream), newFile.StreamIsBinary ? ByteEncoding.Encoding : Encoding.Unicode);
				var isDXTACE = (result.JinxStreamFormat.Extension == "ace") && ((newFile.Ace.Format & 0x10) != 0);
				while ((readReader.BaseStream.Position < readReader.BaseStream.Length) && (saveReader.BaseStream.Position < saveReader.BaseStream.Length)) {
					var oldPos = readReader.BaseStream.Position;
					if (isDXTACE && (oldPos > 168)) break;
					var fileChar = readReader.ReadChar();
					var saveChar = saveReader.ReadChar();
					if (fileChar != saveChar) {
						var readEx = new ReaderException(readReader, newFile.StreamIsBinary, (int)(readReader.BaseStream.Position - oldPos), "");
						var saveEx = new ReaderException(saveReader, newFile.StreamIsBinary, (int)(readReader.BaseStream.Position - oldPos), "");
						if (verbose) {
							lock (formatCounts) {
								Console.WriteLine("Compare: " + String.Format(CultureInfo.CurrentCulture, "{0}\n\nFile character {1:N0} does not match: {2:X4} vs {3:X4}.\n\n{4}{5}\n", file, oldPos, fileChar, saveChar, readEx.ToString(), saveEx.ToString()));
							}
						}
						return result;
					}
				}
				if ((result.JinxStreamFormat.Extension == "ace") && ((newFile.Ace.Format & 0x10) != 0)) {
					// DXT images are a massive pain because it is a lossy compression.
					saveStream.Seek(0, SeekOrigin.Begin);
					var saveOutput = new SimisFile(saveStream, fileProvider);
					Debug.Assert(saveOutput.Ace != null);
					Debug.Assert(saveOutput.Ace.Format == newFile.Ace.Format);

					if (newFile.Ace.Width != saveOutput.Ace.Width) {
					}
					if (newFile.Ace.Height != saveOutput.Ace.Height) {
					}
					if (newFile.Ace.Unknown7 != saveOutput.Ace.Unknown7) {
					}
					if (newFile.Ace.Creator != saveOutput.Ace.Creator) {
					}
					if (String.Join(",", newFile.Ace.Channel.Select(c => c.Type.ToString() + c.Size).ToArray()) != String.Join(",", saveOutput.Ace.Channel.Select(c => c.Type.ToString() + c.Size).ToArray())) {
						// TODO: ERROR with channels.
					}
					if (newFile.Ace.Image.Count != saveOutput.Ace.Image.Count) {
						// TODO: ERROR with image count.
					}

					var errors = new List<double>();
					for (var i = 0; i < newFile.Ace.Image.Count; i++) {
						if (newFile.Ace.Image[i].ImageColor != null) {
							errors.Add(ImageComparison.GetRootMeanSquareError(newFile.Ace.Image[i].ImageColor, saveOutput.Ace.Image[i].ImageColor));
						}
						if (newFile.Ace.Image[i].ImageMask != null) {
							errors.Add(ImageComparison.GetRootMeanSquareError(newFile.Ace.Image[i].ImageMask, saveOutput.Ace.Image[i].ImageMask));
						}
					}
					//Console.WriteLine("Image error: {0,3:F0} {1,3:F0} {1,3:F0} (min/max/mean)", errors.Min(), errors.Max(), errors.Average());

					// Any error over 1.0 is considered a fail.
					if (errors.Max() > 1.0) {
						if (verbose) {
							lock (formatCounts) {
								Console.WriteLine("Compare: " + String.Format(CultureInfo.CurrentCulture, "{0}\n\nWorst image root mean square error is too high: {1:F1} > 1.0.\n", file, errors.Max()));
							}
						}
						return result;
					}
				} else {
					if (readReader.BaseStream.Length != saveReader.BaseStream.Length) {
						var readEx = new ReaderException(readReader, newFile.StreamIsBinary, 0, "");
						var saveEx = new ReaderException(saveReader, newFile.StreamIsBinary, 0, "");
						if (verbose) {
							lock (formatCounts) {
								Console.WriteLine("Compare: " + String.Format(CultureInfo.CurrentCulture, "{0}\n\nFile and stream length do not match: {1:N0} vs {2:N0}.\n\n{3}{4}\n", file, readReader.BaseStream.Length, saveReader.BaseStream.Length, readEx.ToString(), saveEx.ToString()));
							}
						}
						return result;
					}
				}

				// It all worked!
				result.WriteSuccess = true;
				return result;
			};

			if (threading > 1) {
				var filesEnumerator = files.GetEnumerator();
				var filesFinished = false;
				var threads = new List<Thread>(threading);
				for (var i = 0; i < threading; i++) {
					threads.Add(new Thread(() => {
						var file = "";
						var results = new List<ProcessFileResults>();
						while (true) {
							lock (filesEnumerator) {
								if (filesFinished || !filesEnumerator.MoveNext()) {
									filesFinished = true;
									break;
								}
								file = filesEnumerator.Current;
							}
							results.Add(ProcessFile(file));
						}
						lock (totalCount) {
							foreach (var result in results) {
								if (result.Total) totalCount.Total++;
								if (result.ReadSuccess) totalCount.ReadSuccess++;
								if (result.WriteSuccess) totalCount.WriteSuccess++;
								if (result.JinxStreamFormat != null) {
									var formatCount = GetFormatFor(result.JinxStreamFormat);
									if (result.Total) supportedCount.Total++;
									if (result.ReadSuccess) supportedCount.ReadSuccess++;
									if (result.WriteSuccess) supportedCount.WriteSuccess++;
									if (result.Total) formatCount.Total++;
									if (result.ReadSuccess) formatCount.ReadSuccess++;
									if (result.WriteSuccess) formatCount.WriteSuccess++;
								}
							}
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
					var result = ProcessFile(file);
					if (result.Total) totalCount.Total++;
					if (result.ReadSuccess) totalCount.ReadSuccess++;
					if (result.WriteSuccess) totalCount.WriteSuccess++;
					if (result.JinxStreamFormat != null) {
						var formatCount = GetFormatFor(result.JinxStreamFormat);
						if (result.Total) supportedCount.Total++;
						if (result.ReadSuccess) supportedCount.ReadSuccess++;
						if (result.WriteSuccess) supportedCount.WriteSuccess++;
						if (result.Total) formatCount.Total++;
						if (result.ReadSuccess) formatCount.ReadSuccess++;
						if (result.WriteSuccess) formatCount.WriteSuccess++;
					}
				}
			}

			supportedCount.FormatName = "(Total supported files of " + totalCount.Total + ")";
			supportedCount.SortKey = "ZZZ";
			formatCounts[""] = supportedCount;

			var outFormat = "{0,-40:S} {1,1:S}{2,-7:D} {3,1:S}{4,-7:D} {5,1:S}{6,-7:D}";
			Console.WriteLine(String.Format(CultureInfo.CurrentCulture, outFormat, "Format Name", "", "Total", "", "Read", "", "Write"));
			Console.WriteLine(String.Empty.PadLeft(69, '='));
			foreach (var formatCount in formatCounts.OrderBy(kvp => kvp.Value.SortKey).Select(kvp => kvp.Value)) {
				Console.WriteLine(String.Format(CultureInfo.CurrentCulture, outFormat,
						formatCount.FormatName,
						"", formatCount.Total,
						formatCount.Total == formatCount.ReadSuccess ? "*" : "", formatCount.ReadSuccess,
						formatCount.Total == formatCount.WriteSuccess ? "*" : formatCount.ReadSuccess == formatCount.WriteSuccess ? "+" : "", formatCount.WriteSuccess));
			}
		}
	}
}
