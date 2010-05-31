//------------------------------------------------------------------------------
// Jgr.IO.Parser library, part of MSTS Editors & Tools (http://jgrmsts.codeplex.com/).
// License: New BSD License (BSD).
//------------------------------------------------------------------------------

using System.Collections.Generic;
using Jgr.Grammar;

namespace Jgr.IO.Parser
{
	/// <summary>
	/// Contains all the information for reading and writing a single Simis format.
	/// </summary>
	[Immutable]
	public class SimisFormat
	{
		/// <summary>
		/// A human-readable string for identifying the format.
		/// </summary>
		public string Name { get; private set; }
		/// <summary>
		/// The file extension (e.g. <c>"dat"</c>) or filename (e.g. <c>"tsection.dat"</c>) to which this format applies.
		/// </summary>
		public string Extension { get; private set; }
		/// <summary>
		/// The Simis stream format (e.g. <c>"a1"</c>) for this Simis format.
		/// </summary>
		public string Format { get; private set; }
		/// <summary>
		/// A list of all the root productions in the BNF for parsing.
		/// </summary>
		public IEnumerable<string> Roots { get; private set; }
		/// <summary>
		/// The <see cref="Bnf"/> used for verification during reading and writing of this format.
		/// </summary>
		public Bnf Bnf { get; private set; }

		/// <summary>
		/// Constructs a <see cref="SimisFormat"/> from a <see cref="BnfFile"/>.
		/// </summary>
		/// <param name="bnf">The <see cref="BnfFile"/> to read Simis format data from.</param>
		internal SimisFormat(BnfFile bnf) {
			Name = bnf.BnfFileName;
			Extension = bnf.BnfFileExtension;
			Format = bnf.BnfFileType + bnf.BnfFileTypeVersion;
			Roots = bnf.BnfFileRoots;
			Bnf = bnf.Bnf;
		}
	}
}
