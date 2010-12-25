//------------------------------------------------------------------------------
// Jgr.IO.Parser library, part of MSTS Editors & Tools (http://jgrmsts.codeplex.com/).
// License: New BSD License (BSD).
//------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;

namespace Jgr.IO.Parser {
	public class UnknownSimisFormatException : DescriptiveException {
		public UnknownSimisFormatException(string simisFormat, string root)
			: base("Unknown Simis Format '" + simisFormat + "' with root '" + root + "'.") {
		}

		public UnknownSimisFormatException(string simisFormat, string root, Exception innerException)
			: base("Unknown Simis Format '" + simisFormat + "' with root '" + root + "'.", innerException) {
		}
	}

	/// <summary>
	/// Loads and stores data necessary for processing Simis files: token name-id mappings and all the <see cref="SimisFormat"/>s.
	/// </summary>
	[Immutable]
	public class SimisProvider {
		/// <summary>
		/// Provides a mapping of token IDs (as <see cref="uint"/>) to token names (as <see cref="string"/>).
		/// </summary>
		public IDictionary<uint, string> TokenNames { get; private set; }
		/// <summary>
		/// Provides a mapping of token names (as <see cref="string"/>) to token IDs (as <see cref="uint"/>).
		/// </summary>
		public IDictionary<string, uint> TokenIds { get; private set; }
		/// <summary>
		/// Provides a collection of <see cref="SimisFormat"/>s which have been loaded.
		/// </summary>
		public IEnumerable<SimisJinxFormat> Formats { get; private set; }

		public SimisProvider(string directory) {
			var tokenNames = new Dictionary<uint, string>();
			var tokenIds = new Dictionary<string, uint>();
			var formats = new List<SimisJinxFormat>();

			foreach (var filename in Directory.GetFiles(directory, "*.bnf")) {
				var BNF = new BnfFile(filename);
				try {
					BNF.ReadFile();
				} catch (FileException ex) {
					if (ex.InnerException is InvalidDataException) {
						// BNF didn't specify all required stuff, skip it.
						continue;
					}
					throw ex;
				}
				var simisFormat = new SimisJinxFormat(BNF);
				formats.Add(simisFormat);
			}
			formats.Sort((a, b) => StringComparer.InvariantCultureIgnoreCase.Compare(a.Name, b.Name));

			using (var tokens = new StreamReader(File.OpenRead(directory + @"\tokens.csv"), Encoding.ASCII)) {
				while (!tokens.EndOfStream) {
					var csv = tokens.ReadLine().Split(',');
					if ((csv.Length != 3) || !csv[0].StartsWith("0x") || !csv[1].StartsWith("0x"))
						continue;
					var tokenId = (uint)(ushort.Parse(csv[0].Substring(2), NumberStyles.HexNumber, CultureInfo.InvariantCulture) << 16) +
						ushort.Parse(csv[1].Substring(2), NumberStyles.HexNumber, CultureInfo.InvariantCulture);
					tokenNames.Add(tokenId, csv[2]);
					tokenIds.Add(csv[2], tokenId);
				}
			}

			TokenNames = tokenNames;
			TokenIds = tokenIds;
			Formats = formats;
		}

		internal SimisProvider(IDictionary<uint, string> tokenNames, IDictionary<string, uint> tokenIds, IEnumerable<SimisJinxFormat> formats) {
			TokenNames = tokenNames;
			TokenIds = tokenIds;
			Formats = formats;
		}

		/// <summary>
		/// Gets the subset of <see cref="SimisFormat"/>s appropriate for a given filename.
		/// </summary>
		/// <param name="fileName">The filename with which to restrict the formats. Does not need the path.</param>
		/// <returns>A <see cref="SimisProvider"/> which only has <see cref="SimisFormat"/>s appropriate for the given filename.</returns>
		public SimisProvider GetForPath(string fileName) {
			var tokenNames = new Dictionary<uint, string>(TokenNames);
			var tokenIds = new Dictionary<string, uint>(TokenIds);
			var formats = new List<SimisJinxFormat>();
			var extension = "";

			var inputFileName = Path.GetFileName(fileName);
			var inputExtension = Path.GetExtension(fileName);

			// We allow extensions (e.g. "trk") and filenames (e.g. "tsection.dat") in SimisFormat.Extension. If there is a filename match, we should
			// use that and only that; otherwise, any extension matches are in.
			if (Formats.Any(f => inputFileName.Equals(f.Extension, StringComparison.OrdinalIgnoreCase))) {
				formats = new List<SimisJinxFormat>(Formats.Where(f => inputFileName.Equals(f.Extension, StringComparison.OrdinalIgnoreCase)));
				extension = inputFileName;
			} else {
				formats = new List<SimisJinxFormat>(Formats.Where(f => inputExtension.Equals("." + f.Extension, StringComparison.OrdinalIgnoreCase)));
				extension = inputExtension;
			}

			return new SimisProviderForFile(tokenNames, tokenIds, formats, extension);
		}

		public SimisJinxFormat GetForFormat(string format) {
			return Formats.FirstOrDefault(f => f.Format == format);
		}

		public override string ToString() {
			return "SimisProvider()";
		}
	}

	/// <summary>
	/// A <see cref="SimisProvider"/> which contains only the subset of Simis formats appropriate for a given filename.
	/// </summary>
	[Immutable]
	public class SimisProviderForFile : SimisProvider {
		public readonly string Extension;

		internal SimisProviderForFile(IDictionary<uint, string> tokenNames, IDictionary<string, uint> tokenIds, IEnumerable<SimisJinxFormat> formats, string extension)
			: base(tokenNames, tokenIds, formats) {
			Extension = extension;
		}

		public override string ToString() {
			return "SimisProvider(" + Extension + ")";
		}
	}
}
