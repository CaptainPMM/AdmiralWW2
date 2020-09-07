using System.Threading;
using SuperNet.Util;

namespace SuperNet.Transport {

	/// <summary>
	/// Network message that has been sent to a connected peer.
	/// </summary>
	public class MessageSent {
		
		/// <summary>Peer that the message was sent through.</summary>
		public readonly Peer Peer;

		/// <summary>Listener used for this message or null if not provided.</summary>
		public readonly IMessageListener Listener;

		/// <summary>Message payload that is used to write to internal buffers.</summary>
		public readonly IWritable Payload;

		/// <summary>Internal message type.</summary>
		internal readonly MessageType Type;

		/// <summary>Internal message flags.</summary>
		internal readonly MessageFlags Flags;

		/// <summary>Internal sequence number of the message.</summary>
		public readonly ushort Sequence;

		/// <summary>Data channel this message is sent over.</summary>
		public readonly byte Channel;

		/// <summary>Number of times this message has been sent.</summary>
		public int Attempts { get; private set; }

		/// <summary>Host timestamp at the moment of creation of this message.</summary>
		public HostTimestamp TimeCreated { get; private set; }

		/// <summary>Host timestamp at the moment the message was sent to the network socket.</summary>
		public HostTimestamp TimeSent { get; private set; }

		/// <summary>True if message is reliable and has been acknowledged.</summary>
		public bool Acknowledged { get; private set; }

		/// <summary>Cancellation token used to cancel resending of this message.</summary>
		internal readonly CancellationTokenSource Token;

		/// <summary>Used internally by the netcode to create a new sent message.</summary>
		internal MessageSent(
			Peer peer,
			IMessageListener listener,
			IWritable payload,
			MessageType type,
			MessageFlags flags,
			ushort sequence,
			byte channel
		) {
			Peer = peer;
			Listener = listener;
			Payload = payload;
			Type = type;
			Flags = flags;
			Sequence = sequence;
			Channel = channel;
			Attempts = 0;
			TimeCreated = peer.Host.Timestamp;
			TimeSent = peer.Host.Timestamp;
			Acknowledged = false;
			Token = flags.HasFlag(MessageFlags.Reliable) ? new CancellationTokenSource() : null;
		}

		/// <summary>Stop resending this message if reliable. May cause the message to be lost.</summary>
		public void StopResending() {
			try { Token?.Cancel(); } catch { }
			try { Token?.Dispose(); } catch { }
		}
		
		/// <summary>Used internally by the netcode to notify listener.</summary>
		internal void OnMessageSend() {
			Attempts++;
			TimeSent = Peer.Host.Timestamp;
			Listener?.OnMessageSend(Peer, this);
		}

		/// <summary>Used internally by the netcode to notify listener.</summary>
		internal void OnMessageAcknowledge() {
			Acknowledged = true;
			Listener?.OnMessageAcknowledge(Peer, this);
		}

	}

}
