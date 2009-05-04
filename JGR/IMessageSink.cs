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
