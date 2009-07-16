//------------------------------------------------------------------------------
// JGR.IO.Parser library, part of MSTS Editors & Tools (http://jgrmsts.codeplex.com/).
// License: Microsoft Public License (Ms-PL).
//------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading;
using JGR.Grammar;

namespace JGR.IO.Parser
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
		protected Thread BackgroundLoader;
		protected Exception LoadError = null;
		public readonly List<SimisFormat> Formats;
		protected Dictionary<string, SimisFormat> FormatByRoot;
		public Dictionary<uint, string> TokenNames;

		public SimisProvider(string directory) {
			Formats = new List<SimisFormat>();
			FormatByRoot = new Dictionary<string, SimisFormat>();
			TokenNames = new Dictionary<uint, string>();

			BackgroundLoader = new Thread(() => BackgroundLoad(directory));
			BackgroundLoader.Start();
		}

		public void Join() {
			BackgroundLoader.Join();
			if (LoadError != null) throw LoadError;
		}

		public BNF GetBNF(string simisFormat, string root) {
			var key = simisFormat + "|" + root;
			if (FormatByRoot.ContainsKey(key)) {
				return FormatByRoot[key].BNF;
			}
			throw new UnknownSimisFormatException(simisFormat, root);
		}

		private void BackgroundLoad(string directory) {
			foreach (var filename in Directory.GetFiles(directory, "*.bnf")) {
				var BNF = new BNFFile(filename);
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
				foreach (var root in BNF.BNFFileRoots) {
					FormatByRoot.Add(BNF.BNFFileType + BNF.BNFFileTypeVersion + "|" + root, simisFormat);
				}
			}

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
					}
					if (ffReader.EndOfStream) break;
				}
			}
		}
	}
}
