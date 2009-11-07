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
		/// <summary>
		/// Initializes a new instance of the <see cref="DescriptiveException"/> class with no message.
		/// </summary>
		public DescriptiveException()
			: base("") {
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="DescriptiveException"/> class with its message string set to <paramref name="message"/>.
		/// </summary>
		/// <param name="message">The error message that explains the reason for the exception.</param>
		public DescriptiveException(string message)
			: base(message) {
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="DescriptiveException"/> class with its message string set to <paramref name="message"/> and a reference to the inner exception that is the cause of this exception.
		/// </summary>
		/// <param name="message">The error message that explains the reason for the exception.</param>
		/// <param name="innerException">The exception that is the cause of the current exception.</param>
		public DescriptiveException(string message, Exception innerException)
			: base(message, innerException) {
		}

		/// <summary>
		/// Creates and returns a string representation of the current exception.
		/// </summary>
		/// <returns>A string representation of the current exception.</returns>
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
