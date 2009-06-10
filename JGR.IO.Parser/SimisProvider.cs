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
using JGR.Grammar;

namespace JGR.IO.Parser
{
	public class SimisProvider
	{
		protected Thread BackgroundLoader;
		protected Exception LoadError = null;
		public Dictionary<string, string> FileFormats;
		public Dictionary<string, BNF> BNFs;
		public Dictionary<uint, string> TokenNames;

		public SimisProvider(string directory) {
			FileFormats = new Dictionary<string, string>();
			BNFs = new Dictionary<string, BNF>();
			TokenNames = new Dictionary<uint, string>();

			BackgroundLoader = new Thread(() => BackgroundLoad(directory));
			BackgroundLoader.Start();
		}

		public void Join() {
			BackgroundLoader.Join();
			if (LoadError != null) throw LoadError;
		}

		private void BackgroundLoad(string directory) {
			foreach (var filename in Directory.GetFiles(directory, "*.bnf")) {
				var BNF = new BNFFile(filename);
				try {
					BNF.ReadFile();
				} catch (FileException e) {
					LoadError = e;
					return;
				}
				FileFormats.Add(BNF.BNFFileName, BNF.BNFFileExt);
				BNFs.Add(BNF.BNFFileType + BNF.BNFFileTypeVer, BNF.BNF);
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
