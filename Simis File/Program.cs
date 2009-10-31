//------------------------------------------------------------------------------
// Simis Editor, part of MSTS Editors & Tools (http://jgrmsts.codeplex.com/).
// License: Microsoft Public License (Ms-PL).
//------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows.Forms;
using Jgr;
using Jgr.IO;
using Jgr.IO.Parser;

namespace Normalize
{
	class Program
	{
		static void Main(string[] args) {
			// Flags: at least 2 characters, starts with "/" or "-", stored without the "/" or "-".
			// Items: anything not starting "/" or "-".
			// "/" or "-" alone is ignored.
			var flags = args.Where<string>(s => (s.Length > 1) && (s.StartsWith("/") || s.StartsWith("-"))).Select<string, string>(s => s.Substring(1));
			var items = args.Where<string>(s => !s.StartsWith("/") && !s.StartsWith("-"));

			if (flags.Contains("?") || flags.Contains("h")) {
				ShowHelp();
			} else if (flags.Any<string>(s => "formats".StartsWith(s, StringComparison.InvariantCultureIgnoreCase))) {
				ShowFormats();
			} else if (flags.Any<string>(s => "test".StartsWith(s, StringComparison.InvariantCultureIgnoreCase))) {
				var verbose = flags.Any<string>(s => "verbose".StartsWith(s, StringComparison.InvariantCultureIgnoreCase));
				RunTest(items, verbose);
			} else if (flags.Any<string>(s => "normalize".StartsWith(s, StringComparison.InvariantCultureIgnoreCase))) {
				RunNormalize(items);
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
			Console.WriteLine("  SIMISFILE /T[EST] [/V[ERBOSE]] [file ...] [dir ...]");
			Console.WriteLine();
			Console.WriteLine("  SIMISFILE /N[ORMALIZE] [file ...]");
			Console.WriteLine();
			//                 12345678901234567890123456789012345678901234567890123456789012345678901234567890
			Console.WriteLine("  /FORMATS  Displays a list of the supported Simis file formats.");
			Console.WriteLine("  /TEST     Tests all the files specified and found in the directories");
			Console.WriteLine("            specified against the reading and writing code. No files are");
			Console.WriteLine("            changed. A report of read/write success by file type is produced.");
			Console.WriteLine("  /NORMALIZE");
			Console.WriteLine("            Normalizes the specified files for comparisons. For binary files,");
			Console.WriteLine("            this uncompresses the contents only. For text files, whitespace,");
			Console.WriteLine("            quoted strings and numbers are all sanitized. The normalized file");
			Console.WriteLine("            is written to the current directory, with the '.normalized'");
			Console.WriteLine("            extension added if the source file is also in the current");
			Console.WriteLine("            directory. This will overwrite files in the current directory only.");
			Console.WriteLine("  /VERBOSE  Produces more output. For /TEST, displays the individual failures");
			Console.WriteLine("            encountered while testing.");
			Console.WriteLine("  file      One or more Simis files to process.");
			Console.WriteLine("  dir       One or more directories containing Simis files. These will be");
			Console.WriteLine("            scanned recursively.");
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

		static void RunTest(IEnumerable<string> items, bool verbose) {
		}

		static void RunNormalize(IEnumerable<string> files) {
			foreach (var inputFile in files) {
				try {
					var inputStream = new SimisTestableStream(File.OpenRead(inputFile));
					var outputFile = inputFile.Substring(inputFile.LastIndexOf("\\") + 1);
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
					Console.WriteLine(inputFile + " error:");
					Console.WriteLine(ex.ToString());
				}
			}
		}
	}
}
