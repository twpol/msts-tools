//------------------------------------------------------------------------------
// JGR library, part of MSTS Editors & Tools (http://jgrmsts.codeplex.com/).
// License: Microsoft Public License (Ms-PL).
//------------------------------------------------------------------------------

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
	public class FileException : DescriptiveException
	{
		public string Filename { get; protected set; }

		public FileException(string filename)
			: this(filename, "") {
		}

		public FileException(string filename, Exception innerException)
			: this(filename, "", innerException) {
		}

		public FileException(string filename, string message)
			: base(message) {
			Filename = filename;
		}

		public FileException(string filename, string message, Exception innerException)
			: base(message, innerException) {
			Filename = filename;
		}

		public override string ToString() {
			return Filename + "\n\n" + base.ToString();
		}
	}
}
