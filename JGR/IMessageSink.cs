//------------------------------------------------------------------------------
// Jgr library, part of MSTS Editors & Tools (http://jgrmsts.codeplex.com/).
// License: New BSD License (BSD).
//------------------------------------------------------------------------------


namespace Jgr
{
	/// <summary>
	/// Provides an interface through which an object can receive messages from an <see cref="IMessageSource"/>.
	/// </summary>
	public interface IMessageSink
	{
		void MessageAccept(string source, byte level, string message);
	}
}
