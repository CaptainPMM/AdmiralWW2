using System;
using System.Collections.Concurrent;
using UnityEngine;
using SuperNet.Transport;
using SuperNet.Util;
using Net.MessageTypes;

namespace Net {
    public class MessageHandler : MonoBehaviour {
        public static MessageHandler Inst { get; private set; }

        private ConcurrentQueue<Action> runQ = new ConcurrentQueue<Action>();
        public static void Run(Action action) { Inst.runQ.Enqueue(action); }

        private void Awake() {
            if (Inst != null) Destroy(gameObject);
            else {
                Inst = this;
                DontDestroyOnLoad(gameObject);
            }
        }

        private void Update() {
            while (runQ.TryDequeue(out Action action)) {
                action.Invoke();
            }
        }

        public static void Handle(Peer peer, Reader reader, MessageReceived info) {
            MessageType messageType = (MessageType)info.Channel;

            switch (messageType) {
                case MessageType.Test:
                    MTTest test = new MTTest();
                    test.Read(reader);
                    Run(() => HandleMTTest(test));
                    break;
                default:
                    Run(() => Debug.LogWarning("Could not handle net message of type " + messageType));
                    break;
            }
        }

        private static void HandleMTTest(MTTest msg) {
            Debug.Log("Received MTTest with msg: " + msg.Msg);
        }
    }
}