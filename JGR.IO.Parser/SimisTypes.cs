//------------------------------------------------------------------------------
// Jgr.IO.Parser library, part of MSTS Editors & Tools (http://jgrmsts.codeplex.com/).
// License: Microsoft Public License (Ms-PL).
//------------------------------------------------------------------------------

namespace Jgr.IO.Parser
{
	/// <summary>
	/// Specifies whether the Simis stream is binary or text, with the option for auto-detection in certain cases.
	/// </summary>
	public enum SimisStreamFormat
	{
		AutoDetect,
		Binary,
		Text
	}

	/// <summary>
	/// Defines all the possible tokens found in a Simis stream.
	/// </summary>
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
		IntegerWord,
		IntegerByte,
		Float
	}

	/// <summary>
	/// Represents a single token found in a Simis stream.
	/// </summary>
	/// <remarks>
	/// <para>A token always has a <see cref="Kind"/>, but the other properties vary by <see cref="SimisTokenKind"/>:</para>
	/// <list type="bullet">
	/// <item><description>For <see cref="SimisTokenKind.Block"/>, a <see cref="Type"/> is always set, and a <see cref="Name"/> might be set.</description></item>
	/// <item><description>For <see cref="SimisTokenKind.BlockBegin"/>, no other properties will be set.</description></item>
	/// <item><description>For <see cref="SimisTokenKind.BlockEnd"/>, no other properties will be set.</description></item>
	/// <item><description>For <see cref="SimisTokenKind.String"/>, <see cref="Type"/> is set to <c>"string"</c> and <see cref="String"/> contains the string literal found. If the token is named in the BNF, <see cref="Name"/> will be set to that name.</description></item>
	/// <item><description>For <see cref="SimisTokenKind.IntegerUnsigned"/>, <see cref="Type"/> is set to <c>"uint"</c> and <see cref="IntegerUnsigned"/> contains the numeric literal found. If the token is named in the BNF, <see cref="Name"/> will be set to that name.</description></item>
	/// <item><description>For <see cref="SimisTokenKind.IntegerSigned"/>, <see cref="Type"/> is set to <c>"sint"</c> and <see cref="IntegerSigned"/> contains the numeric literal found. If the token is named in the BNF, <see cref="Name"/> will be set to that name.</description></item>
	/// <item><description>For <see cref="SimisTokenKind.IntegerDWord"/>, <see cref="Type"/> is set to <c>"dword"</c> and <see cref="IntegerDWord"/> contains the numeric literal found. If the token is named in the BNF, <see cref="Name"/> will be set to that name.</description></item>
	/// <item><description>For <see cref="SimisTokenKind.IntegerWord"/>, <see cref="Type"/> is set to <c>"word"</c> and <see cref="IntegerDWord"/> contains the numeric literal found. If the token is named in the BNF, <see cref="Name"/> will be set to that name.</description></item>
	/// <item><description>For <see cref="SimisTokenKind.IntegerByte"/>, <see cref="Type"/> is set to <c>"byte"</c> and <see cref="IntegerDWord"/> contains the numeric literal found. If the token is named in the BNF, <see cref="Name"/> will be set to that name.</description></item>
	/// <item><description>For <see cref="SimisTokenKind.Float"/>, <see cref="Type"/> is set to <c>"float"</c> and <see cref="Float"/> contains the numeric literal found. If the token is named in the BNF, <see cref="Name"/> will be set to that name.</description></item>
	/// </list>
	/// </remarks>
	public class SimisToken
	{
		public SimisToken() {
			Type = "";
			Name = "";
			String = "";
		}

		public SimisTokenKind Kind { get; set; }
		public string Type { get; set; }
		public string Name { get; set; }
		public string String { get; set; }
		public uint IntegerUnsigned { get; set; }
		public int IntegerSigned { get; set; }
		public uint IntegerDWord { get; set; }
		public float Float { get; set; }

		public override string ToString() {
			return Kind + "(" + (Type.Length > 0 ? Type + " " : "") + (Name.Length > 0 ? "\"" + Name + "\" " : "") + "string=" + String + ", uint=" + IntegerUnsigned + ", sint=" + IntegerSigned + ", dword=" + IntegerDWord + ", float=" + Float + ")";
		}
	}
}
