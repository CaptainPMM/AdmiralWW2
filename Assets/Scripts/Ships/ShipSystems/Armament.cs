using System.Collections.Generic;
using UnityEngine;
using Ships.ShipSystems.Armaments;

namespace Ships.ShipSystems {
    public class Armament : MonoBehaviour {
        [Header("Armament Setup")]
        [SerializeField] private List<GunTurret> gunTurrets = new List<GunTurret>();

        public List<GunTurret> GunTurrets => gunTurrets;

        public void SetEngageGunTurrets(bool engaged) {
            gunTurrets.ForEach(gt => gt.Engaged = engaged);
        }

        public void SetGunTurretsStabilization(bool stabilize) {
            gunTurrets.ForEach(gt => gt.Stabilization = stabilize);
        }


        public bool GunTurretsReadyAndAimed() {
            foreach (GunTurret gt in gunTurrets) {
                if (!gt.Disabled && (!gt.ReadyToFire || !gt.AimReady)) return false;
            }
            return true;
        }
    }
}