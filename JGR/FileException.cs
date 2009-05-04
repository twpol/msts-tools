using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace JGR
{
	/// <summary>
	/// Represents errors that occur during application processing of a specific file.
	/// </summary>
	/// <remarks>
	/// <para>This class requires a filename to be provided when throwing; the filename is prepended to the provided message in a user-readable manner.</para>
	/// <para>The exception formats itself without the base type usually prefixed by <c>Exception</c> as this makes the text of the filename unreadable. Otherwise, the exception message appears normal.</para>
	/// </remarks>
	public class FileException : Exception
	{
		public FileException(string filename, string message)
			: base(filename + "\n\n" + message) {
		}

		public FileException(string filename, string message, Exception innerException)
			: base(filename + "\n\n" + message, innerException) {
		}

		public override string ToString() {
			var s = base.ToString();
			var pfx = this.GetType().ToString() + ": ";
			if (s.StartsWith(pfx)) s = s.Substring(pfx.Length);
			return s;
		}
	}
}
