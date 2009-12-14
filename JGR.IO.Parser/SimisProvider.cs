//------------------------------------------------------------------------------
// JGR.IO.Parser library, part of MSTS Editors & Tools (http://jgrmsts.codeplex.com/).
// License: Microsoft Public License (Ms-PL).
//------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using Jgr.Grammar;

namespace Jgr.IO.Parser
{
	public class UnknownSimisFormatException : DescriptiveException
	{
		public UnknownSimisFormatException(string simisFormat, string root)
			: base("Unknown Simis Format '" + simisFormat + "' with root '" + root + "'.") {
		}

		public UnknownSimisFormatException(string simisFormat, string root, Exception innerException)
			: base("Unknown Simis Format '" + simisFormat + "' with root '" + root + "'.", innerException) {
		}
	}

	public class SimisProvider
	{
		public Dictionary<uint, string> TokenNames { get; private set; }
		public Dictionary<string, uint> TokenIds { get; private set; }
		public List<SimisFormat> Formats { get; private set; }
		Thread BackgroundLoader;
		Exception LoadError;
		Dictionary<string, SimisFormat> FormatByRoot;

		public SimisProvider(string directory) {
			TokenNames = new Dictionary<uint, string>();
			TokenIds = new Dictionary<string, uint>();
			Formats = new List<SimisFormat>();
			FormatByRoot = new Dictionary<string, SimisFormat>();

			BackgroundLoader = new Thread(() => BackgroundLoad(directory));
			BackgroundLoader.Start();
		}

		public void Join() {
			BackgroundLoader.Join();
			if (LoadError != null) throw LoadError;
		}

		public SimisFormat GetForPath(string fileName) {
			return Formats.FirstOrDefault<SimisFormat>(f => Path.GetExtension(fileName).Equals("." + f.Extension, StringComparison.InvariantCultureIgnoreCase));
		}

		public SimisFormat GetForFormat(string format) {
			return Formats.FirstOrDefault<SimisFormat>(f => f.Format == format);
		}

		public Bnf GetBnf(string simisFormat, string root) {
			var key = simisFormat + "|" + root;
			if (FormatByRoot.ContainsKey(key)) {
				return FormatByRoot[key].Bnf;
			}
			throw new UnknownSimisFormatException(simisFormat, root);
		}

		void BackgroundLoad(string directory) {
			foreach (var filename in Directory.GetFiles(directory, "*.bnf")) {
				var BNF = new BnfFile(filename);
				try {
					BNF.ReadFile();
				} catch (FileException e) {
					LoadError = e;
					return;
				} catch (InvalidDataException) {
					// BNF didn't specify all required stuff, skip it.
					continue;
				}
				var simisFormat = new SimisFormat(BNF);
				Formats.Add(simisFormat);
				foreach (var root in BNF.BnfFileRoots) {
					FormatByRoot.Add(BNF.BnfFileType + BNF.BnfFileTypeVersion + "|" + root, simisFormat);
				}
			}
			Formats.Sort((a, b) => StringComparer.InvariantCultureIgnoreCase.Compare(a.Name, b.Name));

			foreach (var filename in Directory.GetFiles(directory, "*.tok")) {
				var ffReader = new StreamReader(System.IO.File.OpenRead(filename), Encoding.ASCII);
				var tokenType = (ushort)0x0000;
				var tokenIndex = (ushort)0x0000;
				for (var ffLine = ffReader.ReadLine(); ; ffLine = ffReader.ReadLine()) {
					if (ffLine.StartsWith("SID_DEFINE_FIRST_ID(")) {
						var type = ffLine.Substring(ffLine.IndexOf('(') + 1, ffLine.LastIndexOf(')') - ffLine.IndexOf('(') - 1);
						tokenType = ushort.Parse(type.Substring(2), NumberStyles.HexNumber);
						tokenIndex = 0x0000;
					} else if (ffLine.StartsWith("SIDDEF(")) {
						var name = ffLine.Substring(ffLine.IndexOf('"') + 1, ffLine.LastIndexOf('"') - ffLine.IndexOf('"') - 1);
						TokenNames.Add(((uint)tokenType << 16) + ++tokenIndex, name);
						TokenIds.Add(name, ((uint)tokenType << 16) + tokenIndex);
					}
					if (ffReader.EndOfStream) break;
				}
			}
		}
	}
}
