//------------------------------------------------------------------------------
// JGR.IO library, part of MSTS Editors & Tools (http://jgrmsts.codeplex.com/).
// License: Microsoft Public License (Ms-PL).
//------------------------------------------------------------------------------

using System.Collections.Generic;
using Jgr.Grammar;

namespace Jgr.IO.Parser
{
	public class SimisFormat
	{
		public string Name { get; private set; }
		public string Extension { get; private set; }
		public string Format { get; private set; }
		public List<string> Roots { get; private set; }
		public Bnf Bnf { get; private set; }
	
		internal SimisFormat(BnfFile bnf) {
			Name = bnf.BnfFileName;
			Extension = bnf.BnfFileExtension;
			Format = bnf.BnfFileType + bnf.BnfFileTypeVersion;
			Roots = bnf.BnfFileRoots;
			Bnf = bnf.Bnf;
		}
	}
}
