using SuperNet.Util;

namespace SuperNet.Transport {

	/// <summary>
	/// Implements a message that can be sent by the netcode to a connected peer.
	/// </summary>
	public interface IMessage : IWritable {

		/// <summary>
		/// Message includes a timestamp at the moment of creation.
		/// <para>If this is false, received timestamp might be innacurate due to message delays.</para>
		/// </summary>
		bool Timed { get; }

		/// <summary>
		/// Message requires an acknowledgment and needs to be resent until acknowledged.
		/// <para>This makes sure the message will never be lost.</para>
		/// </summary>
		bool Reliable { get; }

		/// <summary>
		/// Message must be delivered in order within the channel.
		/// <para>Any unreliable messages that arrive out of order are dropped.</para>
		/// <para>Any reliable messages that arrive out of order are reordered automatically.</para>
		/// </summary>
		bool Ordered { get; }

		/// <summary>
		/// Message is guaranteed not to be duplicated.
		/// </summary>
		bool Unique { get; }

		/// <summary>
		/// Which data channel to send the message on.
		/// </summary>
		byte Channel { get; }

	}

}
