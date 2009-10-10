//------------------------------------------------------------------------------
// JGR library, part of MSTS Editors & Tools (http://jgrmsts.codeplex.com/).
// License: Microsoft Public License (Ms-PL).
//------------------------------------------------------------------------------

using System;

namespace Jgr
{
	/// <summary>
	/// A base class for exceptions which prefer an "e-mail quotting" nesting of messages. Useful for specially formatted, multi-line exception messages.
	/// </summary>
	public class DescriptiveException : Exception
	{
		public DescriptiveException()
			: base("") {
		}

		public DescriptiveException(string message)
			: base(message) {
		}

		public DescriptiveException(string message, Exception innerException)
			: base(message, innerException) {
		}

		public override string ToString() {
			var rv = "";
			if (InnerException != null) {
				rv += "> " + InnerException.ToString().Replace("\n", "\n> ") + "\r\n\r\n";
			}
			if (Message.Length > 0) rv += Message + "\r\n\r\n";
			rv += StackTrace;
			return rv.Replace("\0", "\\0");
		}
	}
}
