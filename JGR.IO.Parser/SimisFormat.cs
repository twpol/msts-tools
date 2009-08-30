//------------------------------------------------------------------------------
// JGR.IO library, part of MSTS Editors & Tools (http://jgrmsts.codeplex.com/).
// License: Microsoft Public License (Ms-PL).
//------------------------------------------------------------------------------

using System.Collections.Generic;
using JGR.Grammar;

namespace JGR.IO.Parser
{
	public class SimisFormat
	{
		internal SimisFormat(BNFFile bnf) {
			Name = bnf.BNFFileName;
			Extension = bnf.BNFFileExtension;
			Format = bnf.BNFFileType + bnf.BNFFileTypeVersion;
			Roots = bnf.BNFFileRoots;
			BNF = bnf.BNF;
		}

		public string Name { get; protected set; }
		public string Extension { get; protected set; }
		public string Format { get; protected set; }
		public List<string> Roots { get; protected set; }
		public BNF BNF { get; protected set; }
	}
}
