using System.Collections.Generic;
using UnityEngine;
using Ships;
using Cam;

public class GameManager : MonoBehaviour {
    public static GameManager Inst { get; private set; }

    [Header("Setup")]
    [SerializeField] private List<Player> players = new List<Player>();
    [SerializeField] private List<PlayerFleet> fleets = new List<PlayerFleet>();
    [SerializeField] private PlayerTag thisPlayerTag = PlayerTag.Player0;
    [SerializeField] private CamController camController = null;

    [Header("Current state")]
    [SerializeField] private Player thisPlayer = null;

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
}