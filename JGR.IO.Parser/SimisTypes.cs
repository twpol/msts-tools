//------------------------------------------------------------------------------
// JGR.IO.Parser library, part of MSTS Editors & Tools (http://jgrmsts.codeplex.com/).
// License: Microsoft Public License (Ms-PL).
//------------------------------------------------------------------------------

namespace Jgr.IO.Parser
{
	public enum SimisStreamFormat
	{
		AutoDetect,
		Binary,
		Text
	}

	public enum SimisTokenKind
	{
		None,
		Block,
		BlockBegin,
		BlockEnd,
		String,
		Integer,
		Float
	}

	public class SimisToken
	{
		public SimisToken() {
			Type = "";
			String = "";
		}

		public SimisTokenKind Kind { get; set; }
		public string Type { get; set; }
		public string String { get; set; }
		public long Integer { get; set; }
		public float Float { get; set; }
	}
}
