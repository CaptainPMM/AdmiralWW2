using System.Collections.Generic;
using UnityEngine;
using Ships;

public class GameManager : MonoBehaviour {
    public static GameManager Inst { get; private set; }

    [Header("Setup")]
    [SerializeField] private List<Player> players = new List<Player>();
    [SerializeField] private List<PlayerFleet> fleets = new List<PlayerFleet>();
    [SerializeField] private PlayerTag thisPlayerTag = PlayerTag.Player0;

    [Header("Current state")]
    [SerializeField] private Player thisPlayer = null;

    public static PlayerTag ThisPlayerTag => Inst.thisPlayerTag;
    public static Player ThisPlayer => Inst.thisPlayer;
    public static Player GetPlayer(PlayerTag tag) { return Inst.players.Find(p => p.Tag == tag); }

    private void Awake() {
        Inst = this;
    }

    private void Start() {
        fleets.ForEach(f => f.Ships.ForEach(s => {
            s.OnSinking += ShipSinkingHandler;
        }));

        thisPlayer = GetPlayer(thisPlayerTag);
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