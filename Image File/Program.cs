//------------------------------------------------------------------------------
// Image File, part of MSTS Editors & Tools (http://jgrmsts.codeplex.com/).
// License: New BSD License (BSD).
//------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace ImageFile {
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

			if (flags.Contains("?") || flags.Contains("h")) {
				ShowHelp();
			} else {
				ShowHelp();
			}
			if (flags.Contains("pause")) {
				Thread.Sleep(10000);
			}
		}

		static void ShowHelp() {
			Console.WriteLine("Performs operations on individual or collections of ACE files.");
			Console.WriteLine();
			Console.WriteLine("  ACEFILE");
			Console.WriteLine();
			//                 12345678901234567890123456789012345678901234567890123456789012345678901234567890
		}
	}
}
