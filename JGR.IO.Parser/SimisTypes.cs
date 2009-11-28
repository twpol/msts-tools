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
		IntegerUnsigned,
		IntegerSigned,
		IntegerDWord,
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
		public uint IntegerUnsigned { get; set; }
		public int IntegerSigned { get; set; }
		public uint IntegerDWord { get; set; }
		public float Float { get; set; }
	}
}
