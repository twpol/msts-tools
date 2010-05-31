//------------------------------------------------------------------------------
// Jgr.IO.Parser library, part of MSTS Editors & Tools (http://jgrmsts.codeplex.com/).
// License: Microsoft Public License (Ms-PL).
//------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.IO;

namespace Jgr.IO.Parser {
	/// <summary>
	/// A support class for using <see cref="SimisReader"/> and <see cref="SimisWriter"/> with on-disk files.
	/// </summary>
	public class MutableSimisFile {
		public string FileName { get; set; }
		public SimisFormat SimisFormat { get; set; }
		public SimisStreamFormat StreamFormat { get; set; }
		public bool StreamCompressed { get; set; }
		public virtual SimisTreeNode Tree { get; set; }
		SimisProvider SimisProvider;

		public MutableSimisFile(string fileName, SimisProvider simisProvider) {
			FileName = fileName;
			SimisProvider = simisProvider;
		}

		public void Read() {
			var simisFile = new SimisFile(FileName, SimisProvider);
			SimisFormat = simisFile.SimisFormat;
			StreamFormat = simisFile.StreamFormat;
			StreamCompressed = simisFile.StreamCompressed;
			Tree = simisFile.Tree;
		}

		public virtual void Read(Stream stream) {
			var simisFile = new SimisFile(stream, SimisProvider);
			SimisFormat = simisFile.SimisFormat;
			StreamFormat = simisFile.StreamFormat;
			StreamCompressed = simisFile.StreamCompressed;
			Tree = simisFile.Tree;
		}

		public void Write() {
			var simisFile = new SimisFile(FileName, SimisFormat, StreamFormat, StreamCompressed, Tree, SimisProvider);
			simisFile.Write();
		}

		public void Write(Stream stream) {
			var simisFile = new SimisFile(FileName, SimisFormat, StreamFormat, StreamCompressed, Tree, SimisProvider);
			simisFile.Write(stream);
		}
	}
}
