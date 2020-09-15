using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Ships;
using Ships.ShipSystems;
using Cam;
using Net;
using Net.MessageTypes;
using SuperNet.Transport;

public class GameManager : MonoBehaviour {
    public static GameManager Inst { get; private set; }

    private const float DISTRIBUTE_RANDOM_SEED_INTERVAL = 1f;

    [Header("Setup")]
    [SerializeField] private List<Player> players = new List<Player>();
    [SerializeField] private List<PlayerFleet> fleets = new List<PlayerFleet>();
    [SerializeField] private PlayerTag thisPlayerTag = PlayerTag.Player0;
    [SerializeField] private CamController camController = null;

    [Header("Current state")]
    [SerializeField] private Player thisPlayer = null;

    private bool allPlayersReady = false;

    public static PlayerTag ThisPlayerTag => Inst.thisPlayerTag;
    public static Player ThisPlayer => Inst.thisPlayer;
    public static Player GetPlayer(PlayerTag tag) { return Inst.players.Find(p => p.Tag == tag); }

    private void Awake() {
        Inst = this;

        thisPlayerTag = Global.State.playerTag;
        thisPlayer = GetPlayer(thisPlayerTag);

        // Move camera to first ship in fleet
        Vector3 shipPos = fleets.Find(f => f.PlayerTag == thisPlayerTag).Ships[0].transform.position;
        camController.transform.position = new Vector3(shipPos.x, 0f, shipPos.z);
    }

    private void Start() {
        fleets.ForEach(f => f.Ships.ForEach(s => {
            s.OnSinking += ShipSinkingHandler;
        }));

        if (P2PManager.Inst != null && P2PManager.Inst.Peer != null && P2PManager.Inst.Peer.Connected) {
            // Stop game until all players are loaded
            Time.timeScale = 0f;
            MessageHandler.Inst.OnReceivedGameReady += ReceiveGameReadyHandler;
            StartCoroutine(WaitForPeerGameReady());
        }
    }

    private IEnumerator WaitForPeerGameReady() {
        do {
            P2PManager.Inst.Send(new MTGameReady());
            yield return new WaitForSecondsRealtime(0.05f);
        } while (!allPlayersReady);

        // Start game
        if (Global.State.isRandomSeedHost) {
            StartCoroutine(DistributeRandomSeed());
        }
        Time.timeScale = 1f;
    }

    private void ReceiveGameReadyHandler(Peer peer) {
        MessageHandler.Inst.OnReceivedGameReady -= ReceiveGameReadyHandler;
        allPlayersReady = true;
    }

    private IEnumerator DistributeRandomSeed() {
        while (P2PManager.Inst.Peer.Connected) {
            int seed = (int)System.DateTime.Now.Ticks;
            P2PManager.Inst.Send(new MTRandomSeed { Seed = seed });
            SafeRandom.Seed = seed;
            yield return new WaitForSecondsRealtime(DISTRIBUTE_RANDOM_SEED_INTERVAL);
        }
    }

    private void ShipSinkingHandler(Ship ship) {
        ship.OnSinking -= ShipSinkingHandler;

        foreach (PlayerFleet fleet in fleets) {
            int shipIndex = fleet.Ships.IndexOf(ship);
            if (shipIndex != -1) {
                fleet.Ships.RemoveAt(shipIndex);
                fleet.SunkShips.Add(ship);

                if (fleet.Ships.Count == 0) {
                    Debug.Log("Fleet destroyed: " + fleet.Name + "; PlayerTag: " + fleet.PlayerTag + ", Player name: " + GetPlayer(fleet.PlayerTag).Name);
                }
                return;
            }
        }
    }

    public void SetShipChadburn(PlayerTag playerTag, ID shipID, Autopilot.ChadburnSetting chadburnSetting) {
        PlayerFleet fleet = fleets.Find(f => f.PlayerTag == playerTag);
        if (fleet != null) {
            Ship ship = fleet.Ships.Find(s => s.ID == shipID);
            if (ship != null) {
                ship.Autopilot.Chadburn = chadburnSetting;
            } else Debug.LogWarning("Ship with ID " + shipID + " not found");
        } else Debug.LogWarning("Fleet of player tag " + playerTag + " not found");
    }
}