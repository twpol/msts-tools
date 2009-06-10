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
	public class MessageItem
	{
		public readonly string Source;
		public readonly byte Level;
		public readonly string Message;

		public MessageItem(string source, byte level, string message) {
			Source = source;
			Level = level;
			Message = message;
		}
	}

	public class MessageSink
	{
		public readonly IMessageSink Sink;
		public int Index;

		public MessageSink(IMessageSink sink) {
			Sink = sink;
			Index = 0;
		}
	}

	public class BufferedMessageSource : IMessageSource, IMessageSink
	{
		public const byte LEVEL_DEBUG = 0;
		public const byte LEVEL_INFORMATION = 4;
		public const byte LEVEL_WARNING = 6;
		public const byte LEVEL_ERROR = 8;
		public const byte LEVEL_CRITIAL = 10;

		protected List<MessageSink> Sinks;
		private List<MessageItem> Messages;

		public BufferedMessageSource() {
			Sinks = new List<MessageSink>();
			Messages = new List<MessageItem>();
		}

		protected void MessageSend(byte level, string message) {
			MessageAccept(this.GetMessageSourceName(), level, message);
		}

		private void FlushMessages() {
			foreach (var sinkItem in Sinks) {
				while (sinkItem.Index < Messages.Count) {
					var msg = Messages[sinkItem.Index++];
					sinkItem.Sink.MessageAccept(msg.Source, msg.Level, msg.Message);
				}
			}
		}

		#region IMessageSource Members

		public virtual string GetMessageSourceName() {
			return this.GetType().ToString();
		}

		public bool RegisterMessageSink(IMessageSink sink) {
			if (Sinks.Any<MessageSink>(s => s.Sink == sink)) return true;
			Sinks.Add(new MessageSink(sink));
			FlushMessages();
			return true;
		}

		public bool UnregisterMessageSink(IMessageSink sink) {
			if (!Sinks.Any<MessageSink>(s => s.Sink == sink)) return false;
			Sinks.RemoveAll(s => s.Sink == sink);
			return true;
		}

		#endregion

		#region IMessageSink Members

		public void MessageAccept(string source, byte level, string message) {
			Messages.Add(new MessageItem(source, level, message));
			FlushMessages();
		}

		#endregion
	}
}
