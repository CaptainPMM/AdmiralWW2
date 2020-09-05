using System.Collections.Generic;
using UnityEngine;
using Ships;

[System.Serializable]
public class PlayerFleet {
    [SerializeField] private PlayerTag playerTag = PlayerTag.Player0;
    [SerializeField] private string name = "Fleet 1";
    [SerializeField] private List<Ship> ships = new List<Ship>();
    [SerializeField] private List<Ship> sunkShips = new List<Ship>(); // TODO, should store a struct with infos, because the sunk ship will be destroyed and missing

    public PlayerTag PlayerTag => playerTag;
    public string Name => name;
    public List<Ship> Ships => ships;
    public List<Ship> SunkShips => sunkShips;
}