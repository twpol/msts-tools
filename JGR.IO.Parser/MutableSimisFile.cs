//------------------------------------------------------------------------------
// Jgr.IO.Parser library, part of MSTS Editors & Tools (http://jgrmsts.codeplex.com/).
// License: New BSD License (BSD).
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
		public bool StreamIsBinary { get; set; }
		public bool StreamIsCompressed { get; set; }
		public bool JinxStreamIsBinary { get; set; }
		public SimisJinxFormat JinxStreamFormat { get; set; }
		public virtual SimisTreeNode Tree { get; set; }
		public virtual SimisAce Ace { get; set; }
		readonly SimisProvider SimisProvider;

		public MutableSimisFile(string fileName, SimisProvider simisProvider) {
			FileName = fileName;
			SimisProvider = simisProvider;
		}

		public virtual void Read() {
			var simisFile = new SimisFile(FileName, SimisProvider);
			StreamIsBinary = simisFile.StreamIsBinary;
			StreamIsCompressed = simisFile.StreamIsCompressed;
			JinxStreamIsBinary = simisFile.JinxStreamIsBinary;
			JinxStreamFormat = simisFile.JinxStreamFormat;
			Tree = simisFile.Tree;
			Ace = simisFile.Ace;
		}

		public virtual void Read(Stream stream) {
			var simisFile = new SimisFile(stream, SimisProvider);
			StreamIsBinary = simisFile.StreamIsBinary;
			StreamIsCompressed = simisFile.StreamIsCompressed;
			JinxStreamIsBinary = simisFile.JinxStreamIsBinary;
			JinxStreamFormat = simisFile.JinxStreamFormat;
			Tree = simisFile.Tree;
			Ace = simisFile.Ace;
		}

		public void Write() {
			if (Tree != null) {
				var simisFile = new SimisFile(FileName, StreamIsBinary, StreamIsCompressed, JinxStreamIsBinary, JinxStreamFormat, Tree, SimisProvider);
				simisFile.Write();
			} else if (Ace != null) {
				var simisFile = new SimisFile(FileName, StreamIsBinary, StreamIsCompressed, Ace);
				simisFile.Write();
			}
		}

		public void Write(Stream stream) {
			if (Tree != null) {
				var simisFile = new SimisFile(FileName, StreamIsBinary, StreamIsCompressed, JinxStreamIsBinary, JinxStreamFormat, Tree, SimisProvider);
				simisFile.Write(stream);
			} else if (Ace != null) {
				var simisFile = new SimisFile(FileName, StreamIsBinary, StreamIsCompressed, Ace);
				simisFile.Write(stream);
			}
		}
	}
}
