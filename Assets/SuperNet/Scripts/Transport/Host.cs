﻿#if NETSTANDARD || NETCOREAPP
	using System.Runtime.InteropServices;
#endif

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using SuperNet.Compress;
using SuperNet.Crypto;
using SuperNet.Util;

namespace SuperNet.Transport {

	/// <summary>
	/// Manages a network socket and all network communication between peers.
	/// </summary>
	public class Host : IDisposable {

		//////////////////////////////// Host state ///////////////////////////////

		/// <summary>Configuration values for this host.</summary>
		public readonly HostConfig Config;

		/// <summary>Listener used by this host.</summary>
		public readonly IHostListener Listener;

		/// <summary>Packet statistics.</summary>
		public readonly HostStatistics Statistics;

		/// <summary>True if host is disposed and cannot be used anymore.</summary>
		public bool Disposed { get; private set; }

		//////////////////////////////// Packet processing ///////////////////////////////

		/// <summary>Allocator used to manage internal buffers.</summary>
		internal readonly Allocator Allocator;

		/// <summary>Cryptographic random number generator.</summary>
		internal readonly ICryptoRandom Random;

		/// <summary>Authenticator for verifying secure hosts.</summary>
		internal readonly ICryptoAuthenticator Authenticator;

		/// <summary>Compressor used to compress and decompress packets.</summary>
		internal readonly ICompressor Compressor;

		/// <summary>Key exchanger factor for peers.</summary>
		internal Func<ICryptoExchanger> Exchanger => () => new CryptoECDH(Random, Allocator);

		/// <summary>Exchange key length.</summary>
		internal int ExchangerKeyLength => CryptoECDH.KeyLength;

		//////////////////////////////// Timing ///////////////////////////////

		/// <summary>Number of milliseconds that have elapsed since the host was created.</summary>
		public long Ticks => Stopwatch.ElapsedMilliseconds;

		/// <summary>Create a new timestamp at the current host time.</summary>
		public HostTimestamp Timestamp => new HostTimestamp(this);

		/// <summary>Stopwatch used to create accurate timings.</summary>
		private readonly Stopwatch Stopwatch;

		//////////////////////////////// Peers ///////////////////////////////

		/// <summary>All peers on this host.</summary>
		private readonly Dictionary<IPEndPoint, Peer> Peers;

		/// <summary>Lock used to syncronize access to peers.</summary>
		private readonly ReaderWriterLockSlim PeersLock;

		//////////////////////////////// Socket ///////////////////////////////

		/// <summary>Address this host is listening on.</summary>
		public IPEndPoint LocalEndPoint => Socket.LocalEndPoint as IPEndPoint;

		/// <summary>Address used to send broadcast messages.</summary>
		private readonly IPAddress BroadcastAddress;

		/// <summary>Network socket created by the system.</summary>
		private readonly Socket Socket;

		//////////////////////////////// Receive worker ///////////////////////////////

		/// <summary>Semaphore used to limit the amount of concurrent read operations.</summary>
		private readonly SemaphoreSlim ReceiveSemaphore;

		/// <summary>Cancellation token that ends the receive worker when cancelled.</summary>
		private readonly CancellationTokenSource ReceiveCancel;

		/// <summary>Receive worker task.</summary>
		private readonly Task ReceiveWorker;

		//////////////////////////////// Implementation ///////////////////////////////

		/// <summary>Create a new UDP socket and start listening for packets.</summary>
		/// <param name="config">Host configuration values. If null, defaults are used.</param>
		/// <param name="listener">Host listener to use. If null, event based listener is created.</param>
		public Host(HostConfig config, IHostListener listener) {

			// Initialize
			Config = config ?? new HostConfig();
			Listener = listener ?? new HostEvents();
			Statistics = new HostStatistics();
			Disposed = false;
			Allocator = new Allocator(Config);
			Random = new CryptoRandom();
			Authenticator = new CryptoRSA(Allocator);
			Compressor = new CompressorDeflate(Allocator);
			Stopwatch = Stopwatch.StartNew();
			Peers = new Dictionary<IPEndPoint, Peer>(new IPComparer());
			PeersLock = new ReaderWriterLockSlim(LockRecursionPolicy.NoRecursion);
			ReceiveSemaphore = new SemaphoreSlim(Config.ReceiveCount, Config.ReceiveCount);
			ReceiveCancel = new CancellationTokenSource();

			// Import private key
			if (Config.PrivateKey != null) {
				Authenticator.ImportPrivateKey(Config.PrivateKey);
			}

			// Create socket
			if (Config.DualMode && SupportsIPv6) {
				Socket = new Socket(AddressFamily.InterNetworkV6, SocketType.Dgram, ProtocolType.Udp);
			} else {
				Socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
			}

			// If true, socket is a dual-mode socket used for both IPv4 and IPv6
			if (Socket.AddressFamily == AddressFamily.InterNetworkV6) Socket.DualMode = true;

			// If false and packet size + header is larger than MTU, fragment it
			// MTU on ethernet is 1500 bytes - 20 bytes for IP header - 8 bytes for UDP header
			if (Socket.AddressFamily == AddressFamily.InterNetwork && SocketSetDontFragment) {
				Socket.DontFragment = true;
			}

			// Disable SIO_UDP_CONNRESET
			// If enabled, and this socket sends a packet to a destination without a socket it returns ICMP port unreachable
			// When this is received, the next operation on the socket will throw a SocketException
			try { if (SocketSetSioUdpConnreset) Socket.IOControl(-1744830452, new byte[] { 0 }, null); } catch { }

			// If true, any call that doesnt complete immediately will wait until it finishes
			Socket.Blocking = false;

			// Allow broadcasts to 255.255.255.255
			Socket.EnableBroadcast = Config.Broadcast;
			if (Socket.AddressFamily == AddressFamily.InterNetworkV6) {
				BroadcastAddress = Config.Broadcast ? IPAddress.Parse("FF02:0:0:0:0:0:0:1") : IPAddress.IPv6None;
				if (Config.Broadcast) {
					IPv6MulticastOption option = new IPv6MulticastOption(BroadcastAddress);
					Socket.SetSocketOption(SocketOptionLevel.IPv6, SocketOptionName.AddMembership, option);
				}
			} else {
				BroadcastAddress = Config.Broadcast ? IPAddress.Broadcast : IPAddress.None;
			}

			// If exclusive, Bind will fail if address is already in use
			try {
				Socket.ExclusiveAddressUse = true;
				Socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, false);
			} catch {
				if (SocketReuseAddressThrowException) throw;
			}

			// If true, this socket receives outgoing multicast packets
			Socket.MulticastLoopback = false;

			// Maximum number of router hops
			Socket.Ttl = Config.TTL;

			// Buffer sizes
			Socket.SendBufferSize = Config.SendBufferSize;
			Socket.ReceiveBufferSize = Config.ReceiveBufferSize;

			// The amount of time in milliseconds after which a synchronous Send/Receive call will time out, 0 for infinite
			Socket.ReceiveTimeout = 0;
			Socket.SendTimeout = 0;

			// Set to true if you intend to call DuplicateAndClose
			Socket.UseOnlyOverlappedIO = false;

			// Bind socket
			if (Socket.AddressFamily == AddressFamily.InterNetwork) {
				IPAddress bind = Config.BindAddress?.MapToIPv4() ?? IPAddress.Any;
				Socket.Bind(new IPEndPoint(bind, Config.Port));
			} else {
				IPAddress bind = Config.BindAddress?.MapToIPv6() ?? IPAddress.IPv6Any;
				try {
					Socket.Bind(new IPEndPoint(bind, Config.Port));
				} catch (SocketException exception) {
					switch (exception.SocketErrorCode) {
						case SocketError.AddressAlreadyInUse:
							try {
								Socket.SetSocketOption(SocketOptionLevel.IPv6, (SocketOptionName)27, true);
								Socket.Bind(new IPEndPoint(bind, Config.Port));
							} catch {
								if (SocketBindThrowIpv6Rebind) throw;
							}
							break;
						case SocketError.AddressFamilyNotSupported:
							if (SocketBindThrowIpv6NotSupported) throw;
							break;
						default:
							throw;
					}
				}
			}

			// Start receive worker
			ReceiveWorker = Task.Factory.StartNew(
				ReceiveWorkerAsync,
				ReceiveCancel.Token,
				TaskCreationOptions.PreferFairness | TaskCreationOptions.LongRunning,
				TaskScheduler.Default
			);

		}

		/// <summary>Instantly dispose all resources held by this host and connected peers.</summary>
		public void Dispose() {

			// State
			Disposed = true;

			// Receive worker
			try { ReceiveSemaphore.Dispose(); } catch { }
			try { ReceiveCancel.Cancel(); } catch { }
			try { ReceiveCancel.Dispose(); } catch { }
			// ReceiveWorker.Wait();

			// Peers
			try {
				PeersLock.EnterWriteLock();
				foreach (Peer peer in Peers.Values) peer.Dispose();
				Peers.Clear();
			} catch { } finally {
				try { PeersLock.ExitWriteLock(); } catch { }
			}
			try { PeersLock.Dispose(); } catch { }

			// Socket
			try { Socket.Dispose(); } catch { }

			// Packet processing
			//try { Allocator.Dispose(); } catch { }
			try { Random.Dispose(); } catch { }
			try { Authenticator.Dispose(); } catch { }
			try { Compressor.Dispose(); } catch { }

			// Notify listener
			try { Listener.OnHostShutdown(); } catch { }

		}

		/// <summary>Gracefully disconnect all peers and perform a shutdown.</summary>
		public void Shutdown() {
			Task.Factory.StartNew(() => ShutdownAsync(), TaskCreationOptions.PreferFairness);
		}

		/// <summary>Gracefully disconnect all peers and perform a shutdown.</summary>
		/// <returns>Task that completes when shutdown is completed.</returns>
		public async Task ShutdownAsync() {
			
			// Prevent any new connections
			Disposed = true;
			
			// Get all peers
			Peer[] peers = null;
			try {
				PeersLock.EnterReadLock();
				peers = new Peer[Peers.Count];
				Peers.Values.CopyTo(peers, 0);
			} catch { } finally {
				try { PeersLock.ExitReadLock(); } catch { }
			}

			// Disconnect all peers
			try {
				Task[] tasks = new Task[peers.Length];
				for (int i = 0; i < peers.Length; i++) {
					tasks[i] = peers[i].DisconnectAsync();
				}
				await Task.WhenAll(tasks);
			} catch { }

			// Shutdown receive worker
			try { ReceiveCancel?.Cancel(); } catch { }
			try { await ReceiveWorker; } catch { }

			// Dispose
			Dispose();

		}

		/// <summary>Create a local peer and start connecting to an active remote host.</summary>
		/// <param name="remote">Remote address to connect to.</param>
		/// <param name="config">Peer configuration values. If null, default is used.</param>
		/// <param name="listener">Peer listener to use. If null, <see cref="PeerEvents"/> is used.</param>
		/// <param name="message">Connect message to use.</param>
		/// <returns>Local peer that attempts to connect.</returns>
		public Peer Connect(IPEndPoint remote, PeerConfig config, IPeerListener listener, IWritable message = null) {

			// Validate
			if (Disposed) {
				throw new ObjectDisposedException(GetType().FullName);
			} else if (remote == null) {
				throw new ArgumentNullException(nameof(remote), "Remote connect address is null");
			}

			// Map address
			if (remote.AddressFamily != Socket.AddressFamily) {
				if (remote.AddressFamily == AddressFamily.InterNetwork) {
					remote = new IPEndPoint(remote.Address.MapToIPv6(), remote.Port);
				} else if (remote.Address.IsIPv4MappedToIPv6) {
					remote = new IPEndPoint(remote.Address.MapToIPv4(), remote.Port);
				} else {
					throw new ArgumentException(string.Format("Bad peer address {0}", remote), nameof(remote));
				}
			}

			// Create peer
			Peer peer = null;
			try {
				PeersLock.EnterWriteLock();
				Peers.TryGetValue(remote, out peer);
				if (peer == null || peer.Disposed) {
					peer = new Peer(this, remote, config, listener);
					Peers[remote] = peer;
				} else {
					// Peer already exists
				}
			} finally {
				PeersLock.ExitWriteLock();
			}

			// Start connecting
			peer.Connect(message);

			// Return peer
			return peer;

		}

		/// <summary>Accept a connection request and return a connected local peer.</summary>
		/// <param name="request">Connection request to accept.</param>
		/// <param name="config">Peer configuration values. If null, default is used.</param>
		/// <param name="listener">Peer listener to use. If null, event based listener is created.</param>
		/// <returns>Connected local peer.</returns>
		public Peer Accept(ConnectionRequest request, PeerConfig config, IPeerListener listener) {

			// Validate request
			if (Disposed) {
				throw new ObjectDisposedException(GetType().FullName);
			} else if (request == null) {
				throw new ArgumentNullException(nameof(request), "Connection request is null");
			} else if (request.Host != this) {
				throw new ArgumentException("Connection request host mismatch", nameof(request));
			} else if (request.Disposed) {
				throw new InvalidOperationException(string.Format(
					"Connection request from {0} is disposed", request.Remote
				));
			} else if (config.RemotePublicKey != null) {
				throw new ArgumentException(string.Format(
					"Connection request from {0} cannot be authenticated", request.Remote
				), nameof(config));
			} else if (request.Key.Count > 0 && request.Key.Count != ExchangerKeyLength) {
				throw new InvalidOperationException(string.Format(
					"Connection request from {0} has {1} exchanger bytes", request.Remote, request.Key.Count
				));
			} else if (request.Random.Count > 0 && request.Random.Count != Authenticator.SignatureLength) {
				throw new InvalidOperationException(string.Format(
					"Connection request from {0} has {1} random bytes", request.Remote, request.Random.Count
				));
			} else if (request.Random.Count > 0 && Config.PrivateKey == null) {
				throw new InvalidOperationException(string.Format(
					"Connection request from {0} has authentication", request.Remote
				));
			}

			// Create peer
			Peer peer = null;
			try {
				PeersLock.EnterWriteLock();
				Peers.TryGetValue(request.Remote, out peer);
				if (peer == null || peer.Disposed) {
					peer = new Peer(this, request.Remote, config, listener);
					Peers[request.Remote] = peer;
				} else {
					// Peer already exists
				}
			} finally {
				PeersLock.ExitWriteLock();
			}

			// Accept the request
			peer.Accept(request);

			// Return peer
			return peer;

		}

		/// <summary>Reject a connection request.</summary>
		/// <param name="request">Connection request to reject.</param>
		/// <param name="message">Rejection message.</param>
		public void Reject(ConnectionRequest request, IWritable message = null) {

			// Validate
			if (request == null) {
				throw new ArgumentNullException(nameof(request), "Rejected connection request is null");
			} else if (request.Host != this) {
				throw new ArgumentException("Rejected connection request host mismatch", nameof(request));
			}
			
			// Start sending reject packet
			Task.Factory.StartNew(
				() => SendAsync(PacketType.Reject, request.Remote, message),
				TaskCreationOptions.PreferFairness
			);

		}

		/// <summary>Send a message to all connected peers.</summary>
		/// <param name="message">Message to send.</param>
		/// <param name="exclude">Peers to exclude.</param>
		public void SendAll(IMessage message, params Peer[] exclude) {

			// Validate
			if (message == null) {
				throw new ArgumentNullException(nameof(message), "Message to send to all peers is null");
			}
			
			// Send to all connected peers
			try {
				PeersLock.EnterReadLock();
				foreach (Peer peer in Peers.Values) {
					if (peer.Connected) {
						bool send = true;
						foreach (Peer test in exclude) {
							if (test == peer) {
								send = false;
								break;
							}
						}
						if (send) peer.Send(message);
					}
				}
			} finally {
				PeersLock.ExitReadLock();
			}

		}

		/// <summary>Used internally by the netcode to remove peers when they disconnect.</summary>
		internal void OnPeerDispose(Peer peer) {

			// Validate
			if (peer == null) {
				throw new ArgumentNullException(nameof(peer), "Peer to remove is null");
			} else if (peer.Host != this) {
				throw new ArgumentException("Removed peer host mismatch", nameof(peer.Host));
			}

			// Remove peer if exists
			try {
				PeersLock.EnterWriteLock();
				Peers.Remove(peer.Remote);
			} finally {
				PeersLock.ExitWriteLock();
			}

		}

		/// <summary>Attempt to find an existing peer based on remote address.</summary>
		/// <param name="remote">Remote address of the peer.</param>
		/// <returns>An existing peer or null if not found.</returns>
		public Peer FindPeer(IPEndPoint remote) {

			try {

				// If no address, peer cannot be found
				if (remote == null) return null;

				// Map address
				if (remote.AddressFamily != Socket.AddressFamily) {
					if (remote.AddressFamily == AddressFamily.InterNetwork) {
						remote = new IPEndPoint(remote.Address.MapToIPv6(), remote.Port);
					} else if (remote.Address.IsIPv4MappedToIPv6) {
						remote = new IPEndPoint(remote.Address.MapToIPv4(), remote.Port);
					} else {
						return null;
					}
				}

				// Find peer
				try {
					PeersLock.EnterReadLock();
					Peers.TryGetValue(remote, out Peer peer);
					return peer;
				} finally {
					PeersLock.ExitReadLock();
				}

			} catch (Exception exception) {
				Listener.OnHostException(remote, exception);
				return null;
			}

		}
		
		/// <summary>Send an unconnected message to a remote host.</summary>
		/// <param name="remote">Remote address to send to.</param>
		/// <param name="message">Message to send.</param>
		public void SendUnconnected(IPEndPoint remote, IWritable message) {

			// Validate
			if (Disposed) throw new ObjectDisposedException(GetType().FullName);

			// Start sending unconnected packet
			Task.Factory.StartNew(
				() => SendAsync(PacketType.Unconnected, remote, message),
				TaskCreationOptions.PreferFairness
			);

		}

		/// <summary>Send an unconnected message to all machines on the local network.</summary>
		/// <param name="port">Network port to send to.</param>
		/// <param name="message">Message to send.</param>
		public void SendBroadcast(int port, IWritable message) {

			// Validate
			if (Disposed)
				throw new ObjectDisposedException(GetType().FullName);
			if (!Config.Broadcast)
				throw new InvalidOperationException("Broadcast not enabled");

			// Start sending broadcast packet
			IPEndPoint address = new IPEndPoint(BroadcastAddress, port);
			Task.Factory.StartNew(
				() => SendAsync(PacketType.Broadcast, address, message),
				TaskCreationOptions.PreferFairness
			);

		}

		/// <summary>Send an unconnected message to a remote host.</summary>
		/// <param name="remote">Remote address to send to.</param>
		/// <param name="message">Message to send.</param>
		/// <returns>Task that returns number of bytes sent.</returns>
		public async Task<int> SendUnconnectedAsync(IPEndPoint remote, IWritable message) {

			// Validate
			if (Disposed) throw new ObjectDisposedException(GetType().FullName);

			// Send unconnected packet
			return await SendAsync(PacketType.Unconnected, remote, message);

		}

		/// <summary>Send an unconnected message to all machines on the local network.</summary>
		/// <param name="port">Network port to send to.</param>
		/// <param name="message">Message to send.</param>
		/// <returns>Task that returns number of bytes sent.</returns>
		public async Task<int> SendBroadcastAsync(int port, IWritable message) {

			// Validate
			if (Disposed)
				throw new ObjectDisposedException(GetType().FullName);
			if (!Config.Broadcast)
				throw new InvalidOperationException("Broadcast not enabled");

			// Send broadcast packet
			return await SendAsync(PacketType.Broadcast, new IPEndPoint(BroadcastAddress, port), message);

		}

		/// <summary>Used internally by the netcode to send raw packets directly to the socket.</summary>
		/// <param name="remote">Remote address to send to.</param>
		/// <param name="buffer">Array to read from.</param>
		/// <param name="offset">Array offset to read from.</param>
		/// <param name="count">Number of bytes to read.</param>
		/// <returns>Task that returns number of bytes sent.</returns>
		internal async Task<int> SendSocketAsync(IPEndPoint remote, byte[] buffer, int offset, int count) {
			try {

				// Convert remote address if needed
				if (Socket.AddressFamily == AddressFamily.InterNetworkV6) {
					if (remote.AddressFamily != AddressFamily.InterNetworkV6) {
						remote = new IPEndPoint(remote.Address.MapToIPv6(), remote.Port);
					}
				} else if (remote.Address.IsIPv4MappedToIPv6) {
					remote = new IPEndPoint(remote.Address.MapToIPv4(), remote.Port);
				}

				// Send packet and wait for completion
				TaskCompletionSource<int> tcs = new TaskCompletionSource<int>();
				Socket.BeginSendTo(buffer, offset, count, SocketFlags.None, remote, result => {
					try {
						tcs.TrySetResult(Socket.EndSendTo(result));
					} catch (Exception e) {
						tcs.TrySetException(e);
					}
				}, null);
				count = await tcs.Task;

				// Increment statistics
				Statistics.SetSocketSendTicks(Ticks);
				Statistics.IncrementSocketSendCount();
				Statistics.AddSocketSendBytes(count);

				// Return number of bytes sent
				return count;

			} catch (Exception exception) {
				Listener.OnHostException(remote, exception);
				throw;
			}
		}

		/// <summary>Used internally by the netcode to send unconnected messages.</summary>
		private async Task<int> SendAsync(PacketType type, IPEndPoint remote, IWritable message) {

			// Buffers allocated by the allocator
			byte[] compress = null;
			Writer writer = null;

			try {

				// Write message
				writer = new Writer(Allocator, 5);
				message?.Write(writer);
				ArraySegment<byte> packet = new ArraySegment<byte>(writer.Buffer, 5, writer.Position - 5);

				// Create flags
				PacketFlags flags = PacketFlags.None;

				// Compress
				if (Config.Compression) {
					int max = Compressor.MaxCompressedLength(packet.Count) + packet.Offset;
					compress = Allocator.CreateMessage(max);
					int len = Compressor.Compress(packet, compress, packet.Offset);
					if (len <= 0) {
						throw new InvalidOperationException(string.Format(
							"Compressing {0} bytes returned {1} bytes", packet.Count, len
						));
					} else if (len < packet.Count) {
						packet = new ArraySegment<byte>(compress, packet.Offset, len);
						flags |= PacketFlags.Compressed;
					}
				}

				// Write CRC32
				if (Config.CRC32) {
					uint crc32 = CRC32.Compute(packet.Array, packet.Offset, packet.Count);
					Serializer.Write32(packet.Array, packet.Offset - 4, crc32);
					packet = new ArraySegment<byte>(packet.Array, packet.Offset - 4, packet.Count + 4);
					flags |= PacketFlags.Verified;
				}

				// Write header
				packet.Array[packet.Offset - 1] = (byte)(((byte)type) | ((byte)flags));
				packet = new ArraySegment<byte>(packet.Array, packet.Offset - 1, packet.Count + 1);

				// Send
				return await SendSocketAsync(remote, packet.Array, packet.Offset, packet.Count);

			} catch (Exception exception) {

				// Notify listener about the exception
				Listener.OnHostException(remote, exception);
				throw;

			} finally {

				// Return the allocated buffers back to the allocator
				writer?.Dispose();
				Allocator.ReturnMessage(ref compress);
				
			}
		}
		
		/// <summary>Task responsible for receiving all packets from this host.</summary>
		private async Task ReceiveWorkerAsync() {
			while (!ReceiveCancel.IsCancellationRequested) {
				try {

					// Block if too many receive operations
					await ReceiveSemaphore.WaitAsync();

					// Retrieve next available receive buffer from pool
					byte[] buffer = Allocator.CreatePacket(Config.ReceiveMTU);

					try {

						// Read next packet to the buffer
						EndPoint remote = new IPEndPoint(
							Socket.AddressFamily == AddressFamily.InterNetwork ? IPAddress.Any : IPAddress.IPv6Any, 0
						);
						TaskCompletionSource<int> tcs = new TaskCompletionSource<int>();
						Socket.BeginReceiveFrom(buffer, 0, buffer.Length, SocketFlags.None, ref remote, result => {
							try {
								tcs.TrySetResult(Socket.EndReceiveFrom(result, ref remote));
							} catch (Exception e) {
								tcs.TrySetException(e);
							}
						}, null);
						int length = await tcs.Task;

						// Increment statistics
						Statistics.SetSocketReceiveTicks(Ticks);
						Statistics.IncrementSocketReceiveCount();
						Statistics.AddSocketReceiveBytes(length);

						// Start processing packet in a separate task
						// The task then releases semaphore and returns the buffer upon completion
						_ = Task.Factory.StartNew(
							() => Receive(remote as IPEndPoint, buffer, length),
							TaskCreationOptions.PreferFairness
						);

					} catch {

						// Exception occured, release semaphore and return buffer here
						Allocator.ReturnPacket(ref buffer);
						ReceiveSemaphore.Release();
						throw;

					}

				} catch (TaskCanceledException) when (ReceiveCancel.IsCancellationRequested) {

					// Receive worker has been stopped, ignore
					return;

				} catch (ObjectDisposedException) {

					// Host has been disposed, ignore
					return;

				} catch (Exception exception) {

					// Exception while receiving packet
					Listener.OnHostException(null, exception);

				}
			}
		}

		/// <summary>Used internally by the netcode to process a received packet.</summary>
		private async Task Receive(IPEndPoint remote, byte[] buffer, int length) {
			byte[] bufferDecompress = null;
			try {

				// Notify listener
				Listener.OnHostReceiveSocket(remote, buffer, length);

				// Discard if empty
				if (length < 1) return;
				
				// Extract packet information
				PacketType type = (PacketType)(buffer[0] & (byte)PacketType.Mask);
				PacketFlags flags = (PacketFlags)(buffer[0] & (byte)PacketFlags.Mask);
				ArraySegment<byte> packet = new ArraySegment<byte>(buffer, 1, length - 1);

				// Peer packets
				if (type == PacketType.Connected || type == PacketType.Reject || type == PacketType.Accept) {
					Peer peer = FindPeer(remote);
					if (peer == null) {
						throw new FormatException(string.Format(
							"No peer to receive packet {0} ({1}) from {2}", type, flags, remote
						));
					} else {
						await peer.OnReceiveAsync(buffer, length);
						return;
					}
				}

				// Discard if unused packet type
				if (type == PacketType.Unused1 || type == PacketType.Unused2) {
					throw new FormatException(string.Format(
						"Unused packet {0} ({1} received from {2}", type, flags, remote
					));
				}
				
				// Fragmentation, multiple messages and timing not supported
				if (flags.HasFlag(PacketFlags.Fragmented)) {
					throw new FormatException(string.Format(
						"Packet {0} ({1}) from {2} length {3} is fragmented", flags, type, remote, length
					));
				} else if (flags.HasFlag(PacketFlags.Combined)) {
					throw new FormatException(string.Format(
						"Packet {0} ({1}) from {2} length {3} has multiple messages", flags, type, remote, length
					));
				} else if (flags.HasFlag(PacketFlags.Timed)) {
					throw new FormatException(string.Format(
						"Packet {0} ({1}) from {2} length {3} is timed", flags, type, remote, length
					));
				}
				
				// Verify CRC32
				if (flags.HasFlag(PacketFlags.Verified)) {
					if (packet.Count < 4) {
						throw new FormatException(string.Format(
							"Packet {0} ({1}) from {2} with length {3} is too short for CRC32",
							type, flags, remote, length
						));
					} else if (Config.CRC32) {
						uint computed = CRC32.Compute(packet.Array, packet.Offset + 4, packet.Count - 4);
						uint received = Serializer.ReadUInt32(packet.Array, packet.Offset);
						if (computed != received) {
							throw new FormatException(string.Format(
								"CRC32 {0} does not match {1} in packet {2} ({3}) from {4} with length {5}",
								received, computed, type, flags, remote, length
							));
						}
					}
					packet = new ArraySegment<byte>(packet.Array, packet.Offset + 4, packet.Count - 4);
				}

				// Decompress
				if (flags.HasFlag(PacketFlags.Compressed)) {
					bufferDecompress = Allocator.CreateMessage(packet.Count);
					int len = Compressor.Decompress(packet, ref bufferDecompress, 0);
					packet = new ArraySegment<byte>(bufferDecompress, 0, len);
				}

				// Process packet based on type
				switch (type) {
					case PacketType.Request:
						ReceiveRequest(remote, packet);
						break;
					case PacketType.Unconnected:
						using (Reader reader = new Reader(packet.Array, 0, packet.Count)) {
							Listener.OnHostReceiveUnconnected(remote, reader);
						}
						break;
					case PacketType.Broadcast:
						using (Reader reader = new Reader(packet.Array, 0, packet.Count)) {
							Listener.OnHostReceiveBroadcast(remote, reader);
						}
						break;
				}

			} catch (Exception exception) {
				Listener.OnHostException(remote, exception);
			} finally {
				Allocator.ReturnPacket(ref buffer);
				Allocator.ReturnMessage(ref bufferDecompress);
				ReceiveSemaphore.Release();
			}
		}

		/// <summary>Used internally by the netcode to process a received connection request.</summary>
		private void ReceiveRequest(IPEndPoint remote, ArraySegment<byte> packet) {

			// Make sure packet is not too short for header
			if (packet.Count < 4) {
				throw new FormatException(string.Format(
					"Connection request from {0} with length {1} is too short for header",
					remote, packet.Count
				));
			}

			// Extract header
			ushort lengthKey = Serializer.ReadUInt16(packet.Array, packet.Offset);
			ushort lengthData = Serializer.ReadUInt16(packet.Array, packet.Offset + 2);
			int lengthMessage = packet.Count - lengthKey - lengthData - 4;

			// Make sure packet is not too short for data
			if (packet.Count < 4 + lengthKey + lengthData) {
				throw new FormatException(string.Format(
					"Connection request from {0} with length {1} is less than header (4) + key {2} + data {3}",
					remote, packet.Count, lengthKey, lengthData
				));
			}

			// Extract data
			ArraySegment<byte> key = new ArraySegment<byte>(packet.Array, packet.Offset + 4, lengthKey);
			ArraySegment<byte> random = new ArraySegment<byte>(packet.Array, packet.Offset + 4 + lengthKey, lengthData);
			ArraySegment<byte> message = new ArraySegment<byte>(packet.Array, 4 + lengthKey + lengthData, lengthMessage);

			// Process connection request
			using (ConnectionRequest request = new ConnectionRequest(this, remote, key, random))
			using (Reader reader = new Reader(message)) {
				Peer peer = FindPeer(remote);
				if (peer == null) {

					// Peer does not yet exist, notify listener
					Listener.OnHostReceiveRequest(request, reader);

				} else {

					// Peer already exists, automatically accept
					// This is done because connection requests can be resent
					peer.Accept(request);

				}
			}

		}

		/// <summary>Platform dependant IPv6 support check. True if IPv6 is supported, false if not.</summary>
		public static bool SupportsIPv6 {
			get {
				#if DISABLE_IPV6 || (!UNITY_EDITOR && ENABLE_IL2CPP && !UNITY_2018_3_OR_NEWER)
					return false;
				#elif !UNITY_2019_1_OR_NEWER && !UNITY_2018_4_OR_NEWER && (!UNITY_EDITOR && ENABLE_IL2CPP && UNITY_2018_3_OR_NEWER)
					string version = UnityEngine.Application.unityVersion;
					return Socket.OSSupportsIPv6 && int.Parse(version.Remove(version.IndexOf('f')).Split('.')[2]) >= 6;
				#elif UNITY_2018_2_OR_NEWER
					return Socket.OSSupportsIPv6;
				#elif UNITY_5_3_OR_NEWER
					#pragma warning disable 618
						return Socket.SupportsIPv6;
					#pragma warning restore 618
				#else
					return Socket.OSSupportsIPv6;
				#endif
			}
		}

		private static bool SocketSetDontFragment {
			get {
				#if NETSTANDARD || NETCOREAPP
					return !RuntimeInformation.IsOSPlatform(OSPlatform.OSX);
				#else
					return true;
				#endif
			}
		}

		private static bool SocketSetSioUdpConnreset {
			get {
				#if !UNITY_5_3_OR_NEWER || UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
					#if NETSTANDARD || NETCOREAPP
						return RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
					#else
						return true;
					#endif
				#else
					return false;
				#endif
			}
		}

		private static bool SocketReuseAddressThrowException {
			get {
				#if ENABLE_IL2CPP
					return false;
				#else
					return true;
				#endif
			}
		}

		private static bool SocketBindThrowIpv6NotSupported {
			get {
				#if UNITY_IOS
					return false;
				#else
					return true;
				#endif
			}
		}

		private static bool SocketBindThrowIpv6Rebind {
			get {
				#if UNITY_2018_3_OR_NEWER
					return false;
				#else
					return true;
				#endif
			}
		}

	}

}
