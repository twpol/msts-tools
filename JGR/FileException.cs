//------------------------------------------------------------------------------
// JGR library, part of MSTS Editors & Tools (http://jgrmsts.codeplex.com/).
// License: Microsoft Public License (Ms-PL).
//------------------------------------------------------------------------------

using System;

namespace Jgr
{
	/// <summary>
	/// An exception which has occured during application processing of a specific file.
	/// </summary>
	/// <remarks>
	/// <para>This class requires a filename to be provided when throwing; the filename is prepended to the provided message in a user-readable manner.</para>
	/// <para>The exception formats itself without the base type usually prefixed by <c>Exception</c> as this makes the text of the filename unreadable. Otherwise, the exception message appears normal.</para>
	/// </remarks>
	public class FileException : DescriptiveException
	{
		public string FileName { get; private set; }

		public FileException(string fileName)
			: this(fileName, "") {
		}

		public FileException(string fileName, Exception innerException)
			: this(fileName, "", innerException) {
		}

		public FileException(string fileName, string message)
			: base(message) {
			FileName = fileName;
		}

		public FileException(string fileName, string message, Exception innerException)
			: base(message, innerException) {
			FileName = fileName;
		}

		public override string ToString() {
			return FileName + "\n\n" + base.ToString();
		}
	}
}
