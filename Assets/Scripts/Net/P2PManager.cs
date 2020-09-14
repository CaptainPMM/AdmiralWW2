using System;
using System.Net;
using UnityEngine;
using SuperNet.Transport;
using SuperNet.Util;

namespace Net {
    public class P2PManager : MonoBehaviour {
        public static P2PManager Inst { get; private set; }

        public Host Host { get; private set; }
        public Peer Peer { get; private set; }

        public event PeerEvents.OnConnectHandler OnPeerConnect;
        public event PeerEvents.OnDisconnectHandler OnPeerDisconnect;
        public event PeerEvents.OnExceptionHandler OnPeerException;
        public event PeerEvents.OnUpdateRTTHandler OnPeerRTT;

        private void Awake() {
            if (Inst != null) Destroy(gameObject);
            else {
                Inst = this;
                DontDestroyOnLoad(gameObject);
            }
        }

        public void InitHost(int port) {
            HostConfig config = new HostConfig {
                DualMode = true,
                Port = port
            };
            Host = new Host(config, null);
        }

        public bool Connect(string remoteIP, int remotePort) {
            PeerEvents listener = new PeerEvents();
            listener.OnConnect += OnConnect;
            listener.OnDisconnect += OnDisconnent;
            listener.OnException += OnException;
            listener.OnReceive += OnReceive;
            listener.OnUpdateRTT += OnUpdateRTT;

            IPEndPoint address = IPResolver.TryParse(remoteIP, remotePort);
            if (address == null) return false;

            PeerConfig config = new PeerConfig() {
                ConnectAttempts = 20,
                ConnectDelay = 1000,
                DisconnectDelay = 1000,
                DuplicateTimeout = 3000,
                PingDelay = 2000
            };
            Peer = Host.Connect(address, config, listener);
            return true;
        }

        public void Send(IMessage msg) {
            Peer.Send(msg);
        }

        private void OnConnect(Peer peer) {
            MessageHandler.Run(() => {
                Debug.Log(peer.Remote + " connected");
                OnPeerConnect?.Invoke(peer);
            });
        }

        private void OnDisconnent(Peer peer, Reader message, DisconnectReason reason, Exception exception) {
            MessageHandler.Run(() => {
                Debug.Log(peer?.Remote + " disconnected with reason " + reason + " and optional exception " + exception?.Message);
                OnPeerDisconnect?.Invoke(peer, message, reason, exception);
            });
        }

        private void OnException(Peer peer, Exception exception) {
            MessageHandler.Run(() => {
                Debug.LogError(peer?.Remote + ": " + exception?.Message);
                OnPeerException?.Invoke(peer, exception);
            });
        }

        private void OnReceive(Peer peer, Reader message, MessageReceived info) {
            MessageHandler.Handle(peer, message, info);
        }

        private void OnUpdateRTT(Peer peer, ushort rtt) {
            MessageHandler.Run(() => OnPeerRTT?.Invoke(peer, rtt));
        }

        private void OnDestroy() {
            Peer?.Disconnect();
            Host?.Shutdown();
        }
    }
}