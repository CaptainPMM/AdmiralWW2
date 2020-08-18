using System.Collections.Generic;
using UnityEngine;
using Ships.ShipSystems.Armaments;

namespace Ships.ShipSystems {
    public class Armament : MonoBehaviour {
        [Header("Armament Setup")]
        [SerializeField] private List<GunTurret> gunTurrets = new List<GunTurret>();

        public List<GunTurret> GunTurrets => gunTurrets;
    }
}