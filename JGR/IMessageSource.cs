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
	public interface IMessageSource
	{
		string MessageSourceName { get; }
		bool RegisterMessageSink(IMessageSink sink);
		bool UnregisterMessageSink(IMessageSink sink);
	}
}
