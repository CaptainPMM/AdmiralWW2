using System;
using System.Collections.Concurrent;
using UnityEngine;
using SuperNet.Transport;
using SuperNet.Util;
using Net.MessageTypes;

namespace Net {
    public class MessageHandler : MonoBehaviour {
        public static MessageHandler Inst { get; private set; }

        public delegate void ReceivedGameReadyEvent(Peer peer);
        public event ReceivedGameReadyEvent OnReceivedGameReady;

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
                    Run(() => Debug.Log("Received MTTest with msg: " + test.Msg));
                    break;
                case MessageType.GameReady:
                    Run(() => Inst.OnReceivedGameReady?.Invoke(peer));
                    break;
                case MessageType.RandomSeed:
                    MTRandomSeed seed = new MTRandomSeed();
                    seed.Read(reader);
                    Run(() => SafeRandom.Seed = seed.Seed);
                    break;
                case MessageType.ShipChadburn:
                    MTShipChadburn chad = new MTShipChadburn();
                    chad.Read(reader);
                    Run(() => GameManager.Inst.SetShipChadburn(chad.PlayerTag, chad.ShipID, chad.ChadburnSetting));
                    break;
                case MessageType.ShipCourse:
                    MTShipCourse course = new MTShipCourse();
                    course.Read(reader);
                    Run(() => GameManager.Inst.SetShipCourse(course.PlayerTag, course.ShipID, course.Course));
                    break;
                case MessageType.ShipTarget:
                    MTShipTarget target = new MTShipTarget();
                    target.Read(reader);
                    Run(() => GameManager.Inst.SetShipTarget(target.PlayerTag, target.ShipID, target.HasTarget, target.TargetShipID));
                    break;
                case MessageType.SyncGame:
                    MTSyncGame sync = new MTSyncGame();
                    sync.Read(reader);
                    Run(() => GameManager.Inst.SyncGames(sync));
                    break;
                default:
                    Run(() => Debug.LogWarning("Could not handle net message of type " + messageType));
                    break;
            }
        }
    }
}