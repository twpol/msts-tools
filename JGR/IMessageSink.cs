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
	public interface IMessageSink
	{
		void MessageAccept(string source, byte level, string message);
	}
}
