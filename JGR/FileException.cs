//------------------------------------------------------------------------------
// Jgr library, part of MSTS Editors & Tools (http://jgrmsts.codeplex.com/).
// License: New BSD License (BSD).
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
		/// <summary>
		/// Gets the filename for the file associated with this exception.
		/// </summary>
		public string FileName { get; private set; }

		/// <summary>
		/// Initializes a new instance of the <see cref="FileException"/> class with no message.
		/// </summary>
		/// <param name="fileName">The filename for the file that is the cause of the exception.</param>
		public FileException(string fileName)
			: this(fileName, "") {
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="FileException"/> class with no message and a reference to the inner exception that is the cause of this exception.
		/// </summary>
		/// <param name="fileName">The filename for the file that is the cause of the exception.</param>
		/// <param name="innerException">The exception that is the cause of the current exception.</param>
		public FileException(string fileName, Exception innerException)
			: this(fileName, "", innerException) {
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="FileException"/> class with its message string set to <paramref name="message"/>.
		/// </summary>
		/// <param name="fileName">The filename for the file that is the cause of the exception.</param>
		/// <param name="message">The error message that explains the reason for the exception.</param>
		public FileException(string fileName, string message)
			: base(message) {
			FileName = fileName;
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="FileException"/> class with its message string set to <paramref name="message"/> and a reference to the inner exception that is the cause of this exception.
		/// </summary>
		/// <param name="fileName">The filename for the file that is the cause of the exception.</param>
		/// <param name="message">The error message that explains the reason for the exception.</param>
		/// <param name="innerException">The exception that is the cause of the current exception.</param>
		public FileException(string fileName, string message, Exception innerException)
			: base(message, innerException) {
			FileName = fileName;
		}

		/// <summary>
		/// Creates and returns a string representation of the current exception.
		/// </summary>
		/// <returns>A string representation of the current exception.</returns>
		public override string ToString() {
			return FileName + "\r\n\r\n" + base.ToString();
		}
	}
}
