//------------------------------------------------------------------------------
// JGR.IO.Parser library, part of MSTS Editors & Tools (http://jgrmsts.codeplex.com/).
// License: Microsoft Public License (Ms-PL).
//------------------------------------------------------------------------------

namespace JGR.IO.Parser
{
	public enum SimisStreamFormat
	{
		Autodetect,
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

		public SimisTokenKind Kind;
		public string Type;
		public string String;
		public long Integer;
		public double Float;
	}
}
