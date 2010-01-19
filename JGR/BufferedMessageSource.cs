//------------------------------------------------------------------------------
// Jgr library, part of MSTS Editors & Tools (http://jgrmsts.codeplex.com/).
// License: Microsoft Public License (Ms-PL).
//------------------------------------------------------------------------------

using System.Collections.ObjectModel;

namespace Jgr
{
	/// <summary>
	/// A single message to be reported through <see cref="BufferedMessageSource"/>.
	/// </summary>
	public class MessageItem
	{
		public string Source { get; private set; }
		public byte Level { get; private set; }
		public string Message { get; private set; }

		public MessageItem(string source, byte level, string message) {
			Source = source;
			Level = level;
			Message = message;
		}
	}

	/// <summary>
	/// A base class which can be used to get a simple, but useful, implementation of both <see cref="IMessageSource"/> and <see cref="IMessageSink"/>.
	/// </summary>
	public class BufferedMessageSource : IMessageSource, IMessageSink
	{
		public const byte LevelDebug = 0;
		public const byte LevelInformation = 4;
		public const byte LevelWarning = 6;
		public const byte LevelError = 8;
		public const byte LevelCritical = 10;
		protected KeyedCollection<IMessageSink, MessageSink> Sinks { get; private set; }
		Collection<MessageItem> Messages;

		/// <summary>
		/// A utility class for keepting track of how many messages have been read from a given <see cref="IMessageSink"/>.
		/// </summary>
		protected class MessageSink
		{
			public IMessageSink Sink { get; private set; }
			public int Index { get; private set; }
			Collection<MessageItem> Messages;

			public MessageSink(IMessageSink sink, Collection<MessageItem> messages) {
				Sink = sink;
				Index = 0;
				Messages = messages;
			}

			public bool HaveMore {
				get {
					return Index < Messages.Count;
				}
			}

			public MessageItem GetNext() {
				return Messages[Index++];
			}
		}

		protected class SinkCollection : KeyedCollection<IMessageSink, MessageSink>
		{
			protected override IMessageSink GetKeyForItem(MessageSink item) {
				return item.Sink;
			}
		}

		public BufferedMessageSource() {
			Sinks = new SinkCollection();
			Messages = new Collection<MessageItem>();
		}

		protected void MessageSend(byte level, string message) {
			MessageAccept(this.MessageSourceName, level, message);
		}

		void FlushMessages() {
			foreach (var sinkItem in Sinks) {
				while (sinkItem.HaveMore) {
					var msg = sinkItem.GetNext();
					sinkItem.Sink.MessageAccept(msg.Source, msg.Level, msg.Message);
				}
			}
		}

		#region IMessageSource Members

		public virtual string MessageSourceName {
			get {
				return this.GetType().ToString();
			}
		}

		public bool RegisterMessageSink(IMessageSink sink) {
			if (Sinks.Contains(sink)) return true;
			Sinks.Add(new MessageSink(sink, Messages));
			FlushMessages();
			return true;
		}

		public bool UnregisterMessageSink(IMessageSink sink) {
			if (!Sinks.Contains(sink)) return false;
			Sinks.Remove(sink);
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
