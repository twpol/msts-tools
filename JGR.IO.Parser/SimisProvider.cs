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
		public IEnumerable<SimisFormat> Formats { get; private set; }

		public SimisProvider(string directory) {
			var tokenNames = new Dictionary<uint, string>();
			var tokenIds = new Dictionary<string, uint>();
			var formats = new List<SimisFormat>();

			foreach (var filename in Directory.GetFiles(directory, "*.bnf")) {
				var BNF = new BnfFile(filename);
				try {
					BNF.ReadFile();
				} catch (InvalidDataException) {
					// BNF didn't specify all required stuff, skip it.
					continue;
				}
				var simisFormat = new SimisFormat(BNF);
				formats.Add(simisFormat);
			}
			formats.Sort((a, b) => StringComparer.InvariantCultureIgnoreCase.Compare(a.Name, b.Name));

			foreach (var filename in Directory.GetFiles(directory, "*.tok")) {
				var ffReader = new StreamReader(System.IO.File.OpenRead(filename), Encoding.ASCII);
				var tokenType = (ushort)0x0000;
				var tokenIndex = (ushort)0x0000;
				for (var ffLine = ffReader.ReadLine(); ; ffLine = ffReader.ReadLine()) {
					if (ffLine.StartsWith("SID_DEFINE_FIRST_ID(", StringComparison.Ordinal)) {
						var type = ffLine.Substring(ffLine.IndexOf('(') + 1, ffLine.LastIndexOf(')') - ffLine.IndexOf('(') - 1);
						tokenType = ushort.Parse(type.Substring(2), NumberStyles.HexNumber, CultureInfo.InvariantCulture);
						tokenIndex = 0x0000;
					} else if (ffLine.StartsWith("SIDDEF(", StringComparison.Ordinal)) {
						var name = ffLine.Substring(ffLine.IndexOf('"') + 1, ffLine.LastIndexOf('"') - ffLine.IndexOf('"') - 1);
						tokenNames.Add(((uint)tokenType << 16) + ++tokenIndex, name);
						tokenIds.Add(name, ((uint)tokenType << 16) + tokenIndex);
					}
					if (ffReader.EndOfStream) break;
				}
			}

			TokenNames = tokenNames;
			TokenIds = tokenIds;
			Formats = formats;
		}

		internal SimisProvider(IDictionary<uint, string> tokenNames, IDictionary<string, uint> tokenIds, IEnumerable<SimisFormat> formats) {
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
			var formats = new List<SimisFormat>();
			var extension = "";

			var inputFileName = Path.GetFileName(fileName);
			var inputExtension = Path.GetExtension(fileName);

			// We allow extensions (e.g. "trk") and filenames (e.g. "tsection.dat") in SimisFormat.Extension. If there is a filename match, we should
			// use that and only that; otherwise, any extension matches are in.
			if (Formats.Any(f => inputFileName.Equals(f.Extension, StringComparison.OrdinalIgnoreCase))) {
				formats = new List<SimisFormat>(Formats.Where(f => inputFileName.Equals(f.Extension, StringComparison.OrdinalIgnoreCase)));
				extension = inputFileName;
			} else {
				formats = new List<SimisFormat>(Formats.Where(f => inputExtension.Equals("." + f.Extension, StringComparison.OrdinalIgnoreCase)));
				extension = inputExtension;
			}

			return new SimisProviderForFile(tokenNames, tokenIds, formats, extension);
		}

		public SimisFormat GetForFormat(string format) {
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

		internal SimisProviderForFile(IDictionary<uint, string> tokenNames, IDictionary<string, uint> tokenIds, IEnumerable<SimisFormat> formats, string extension)
			: base(tokenNames, tokenIds, formats) {
			Extension = extension;
		}

		public override string ToString() {
			return "SimisProvider(" + Extension + ")";
		}
	}
}
