//------------------------------------------------------------------------------
// JGR library, part of MSTS Editors & Tools (http://jgrmsts.codeplex.com/).
// License: Microsoft Public License (Ms-PL).
//------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Jgr
{
	/// <summary>
	/// Provides a means to get messages from a source. The receiver must have an object implementing <see cref="IMessageSink"/>.
	/// </summary>
	public interface IMessageSource
	{
		string MessageSourceName { get; }
		bool RegisterMessageSink(IMessageSink sink);
		bool UnregisterMessageSink(IMessageSink sink);
	}
}
