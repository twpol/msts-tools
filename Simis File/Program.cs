//------------------------------------------------------------------------------
// Simis File, part of MSTS Editors & Tools (http://jgrmsts.codeplex.com/).
// License: Microsoft Public License (Ms-PL).
//------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
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

	class Program
	{
		static void Main(string[] args) {
			// Flags: at least 2 characters, starts with "/" or "-", stored without the "/" or "-".
			// Items: anything not starting "/" or "-".
			// "/" or "-" alone is ignored.
			var flags = args.Where<string>(s => (s.Length > 1) && (s.StartsWith("/") || s.StartsWith("-"))).Select<string, string>(s => s.Substring(1));
			var items = args.Where<string>(s => !s.StartsWith("/") && !s.StartsWith("-"));
			var verbose = flags.Any<string>(s => "verbose".StartsWith(s, StringComparison.InvariantCultureIgnoreCase));

			if (flags.Contains("?") || flags.Contains("h")) {
				ShowHelp();
			} else if (flags.Any<string>(s => "formats".StartsWith(s, StringComparison.InvariantCultureIgnoreCase))) {
				ShowFormats();
			} else if (flags.Any<string>(s => "dump".StartsWith(s, StringComparison.InvariantCultureIgnoreCase))) {
				RunDump(ExpandFilesAndDirectories(items), verbose);
			} else if (flags.Any<string>(s => "normalize".StartsWith(s, StringComparison.InvariantCultureIgnoreCase))) {
				RunNormalize(ExpandFilesAndDirectories(items), verbose);
			} else if (flags.Any<string>(s => "test".StartsWith(s, StringComparison.InvariantCultureIgnoreCase))) {
				RunTest(ExpandFilesAndDirectories(items), verbose);
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
			Console.WriteLine("  SIMISFILE /D[DUMP] [/V[ERBOSE]] [file ...]");
			Console.WriteLine();
			Console.WriteLine("  SIMISFILE /N[ORMALIZE] [file ...]");
			Console.WriteLine();
			Console.WriteLine("  SIMISFILE /T[EST] [/V[ERBOSE]] [file ...] [dir ...]");
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
			if (node is SimisTreeNodeValue) {
				if (node.Name.Length > 0) {
					Console.Write(node.Name);
					Console.Write(" ");
				}
				Console.Write("(");
				Console.Write(node.Type);
				Console.Write(")");
				Console.Write(": ");
				Console.WriteLine((node as SimisTreeNodeValue).Value);
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

		static void ShowFormats() {
			var resourcesDirectory = Application.ExecutablePath;
			resourcesDirectory = resourcesDirectory.Substring(0, resourcesDirectory.LastIndexOf('\\')) + @"\Resources";
			var provider = new SimisProvider(resourcesDirectory);
			try {
				provider.Join();
			} catch (FileException ex) {
				Console.WriteLine(ex.ToString());
				return;
			}
			var outFormat = "{0,-40:S}  {1,-15:S}  {2,-15:S}";
			Console.WriteLine(String.Format(outFormat, "Format Name", "File Extension", "Internal Type"));
			Console.WriteLine(String.Empty.PadLeft(40 + 2 + 15 + 2 + 15 + 2, '='));
			foreach (var format in provider.Formats) {
				Console.WriteLine(String.Format(outFormat, format.Name, format.Extension, format.Format));
			}
		}

		static void RunDump(IEnumerable<string> files, bool verbose) {
			var resourcesDirectory = Application.ExecutablePath;
			resourcesDirectory = resourcesDirectory.Substring(0, resourcesDirectory.LastIndexOf('\\')) + @"\Resources";
			var provider = new SimisProvider(resourcesDirectory);
			try {
				provider.Join();
			} catch (FileException ex) {
				Console.WriteLine(ex.ToString());
				return;
			}

			foreach (var inputFile in files) {
				try {
					var parsedFile = new SimisFile(inputFile, provider);
					parsedFile.ReadFile();
					Console.WriteLine(inputFile);
					PrintSimisTree(0, parsedFile.Tree);
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
						using (var outputStreamWriter = new BinaryWriter(outputStream, new ByteEncoding())) {
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

		static void RunTest(IEnumerable<string> files, bool verbose) {
			var resourcesDirectory = Application.ExecutablePath;
			resourcesDirectory = resourcesDirectory.Substring(0, resourcesDirectory.LastIndexOf('\\')) + @"\Resources";
			var provider = new SimisProvider(resourcesDirectory);
			try {
				provider.Join();
			} catch (FileException ex) {
				Console.WriteLine(ex.ToString());
				return;
			}

			var totalCount = new TestFormatCount();
			var supportedCount = new TestFormatCount();
			var formatCounts = new Dictionary<string, TestFormatCount>();
			var messageLog = new BufferedMessageSource();
			var timeStart = DateTime.Now;

			foreach (var file in files) {
				totalCount.Total++;

				var extension = Path.GetExtension(file).ToUpperInvariant();
				if (!formatCounts.ContainsKey(extension)) {
					if (provider.GetForPath(file) == null) {
						continue;
					}
					formatCounts[extension] = new TestFormatCount() { FormatName = provider.GetForPath(file).Name };
					formatCounts[extension].SortKey = formatCounts[extension].FormatName;
				}
				var formatCount = formatCounts[extension];

				supportedCount.Total++;
				formatCount.Total++;

				var success = true;
				var newFile = new SimisFile(file, provider);
				newFile.SimisFormat = provider.GetForPath(file);
				Stream readStream = new BufferedInMemoryStream(File.OpenRead(file));
				Stream saveStream = new MemoryStream();

				// First, read the file in.
				if (success) {
					try {
						try {
							newFile.ReadStream(readStream);
						} catch (Exception e) {
							throw new FileException(file, e);
						}
						totalCount.ReadSuccess++;
						supportedCount.ReadSuccess++;
						formatCount.ReadSuccess++;
					} catch (FileException ex) {
						success = false;
						if (verbose) {
							Console.WriteLine("Read: " + ex + "\n");
						}
					}
				}

				// Second, write the file out into memory.
				if (success) {
					try {
						try {
							newFile.WriteStream(saveStream);
						} catch (Exception e) {
							throw new FileException(file, e);
						}
						// WriteSuccess is delayed until after the comparison. We won't claim write support without comparison support.
					} catch (FileException ex) {
						success = false;
						if (verbose) {
							Console.WriteLine("Write: " + ex + "\n");
						}
					}
				}

				// Third, verify that the output is the same as the input.
				if (success) {
					readStream.Seek(0, SeekOrigin.Begin);
					saveStream.Seek(0, SeekOrigin.Begin);
					var readReader = new BinaryReader(new SimisTestableStream(readStream), newFile.StreamFormat == SimisStreamFormat.Binary ? new ByteEncoding() : Encoding.Unicode);
					var saveReader = new BinaryReader(new SimisTestableStream(saveStream), newFile.StreamFormat == SimisStreamFormat.Binary ? new ByteEncoding() : Encoding.Unicode);
					while ((readReader.BaseStream.Position < readReader.BaseStream.Length) && (saveReader.BaseStream.Position < saveReader.BaseStream.Length)) {
						var oldPos = readReader.BaseStream.Position;
						var fileChar = readReader.ReadChar();
						var saveChar = saveReader.ReadChar();
						if (fileChar != saveChar) {
							success = false;
							var readEx = new ReaderException(readReader, newFile.StreamFormat == SimisStreamFormat.Binary, (int)(readReader.BaseStream.Position - oldPos), "");
							var saveEx = new ReaderException(saveReader, newFile.StreamFormat == SimisStreamFormat.Binary, (int)(readReader.BaseStream.Position - oldPos), "");
							if (verbose) {
								Console.WriteLine("Compare: " + String.Format("{0}\n\nFile character {1:N0} does not match: {2:X4} vs {3:X4}.\n\n{4}{5}\n", file, oldPos, fileChar, saveChar, readEx.ToString(), saveEx.ToString()));
							}
							break;
						}
					}
					if (success && (readReader.BaseStream.Length != saveReader.BaseStream.Length)) {
						success = false;
						var readEx = new ReaderException(readReader, newFile.StreamFormat == SimisStreamFormat.Binary, 0, "");
						var saveEx = new ReaderException(saveReader, newFile.StreamFormat == SimisStreamFormat.Binary, 0, "");
						if (verbose) {
							Console.WriteLine("Compare: " + String.Format("{0}\n\nFile and stream length do not match: {1:N0} vs {2:N0}.\n\n{3}{4}\n", file, readReader.BaseStream.Length, saveReader.BaseStream.Length, readEx.ToString(), saveEx.ToString()));
						}
					}
				}

				// It all worked!
				if (success) {
					totalCount.WriteSuccess++;
					supportedCount.WriteSuccess++;
					formatCount.WriteSuccess++;
				}
			}

			supportedCount.FormatName = "(Total supported files of " + totalCount.Total + ")";
			supportedCount.SortKey = "ZZZ";
			formatCounts[""] = supportedCount;

			//{
			//    var timeTaken = DateTime.Now - timeStart;
			//    messageLog.MessageAccept("Test", BufferedMessageSource.LevelInformation, String.Format("Tested {0} files; {1} passed ({2:F0}%). Took {3:F0} minutes.", supportedCount.Total, supportedCount.WriteSuccess, ((double)100 * supportedCount.WriteSuccess / supportedCount.Total), timeTaken.TotalMinutes));
			//}

			var outFormat = "{0,-40:S} {1,1:S}{2,-7:D} {3,1:S}{4,-7:D} {5,1:S}{6,-7:D}";
			Console.WriteLine(String.Format(outFormat, "Format Name", "", "Total", "", "Read", "", "Write"));
			Console.WriteLine(String.Empty.PadLeft(69, '='));
			foreach (var formatCount in formatCounts.OrderBy<KeyValuePair<string, TestFormatCount>, string>(kvp => kvp.Value.SortKey).Select<KeyValuePair<string, TestFormatCount>, TestFormatCount>(kvp => kvp.Value)) {
				Console.WriteLine(String.Format(outFormat,
						formatCount.FormatName,
						"", formatCount.Total,
						formatCount.Total == formatCount.ReadSuccess ? "*" : "", formatCount.ReadSuccess,
						formatCount.Total == formatCount.WriteSuccess ? "*" : formatCount.ReadSuccess == formatCount.WriteSuccess ? "+" : "", formatCount.WriteSuccess));
			}

			//Console.WriteLine("Tested " + totalCount.Total + " files; " + totalCount.CompareSuccess + " passed (" + ((double)100 * totalCount.CompareSuccess / totalCount.Total).ToString("F0") + "%).");
			//Messages = new Messages();
			//messageLog.RegisterMessageSink(Messages);
		}
	}
}
