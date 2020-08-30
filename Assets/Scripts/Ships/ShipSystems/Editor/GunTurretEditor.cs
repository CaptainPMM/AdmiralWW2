using UnityEditor;
using Ships.ShipSystems.Armaments;

namespace Ships.ShipSystems.Editors {
    [CustomEditor(typeof(GunTurret))]
    public class GunTurretEditor : Editor {
        private bool lastStabilizeValue = false;

        public override void OnInspectorGUI() {
            GunTurret gt = (GunTurret)target;

            if (DrawDefaultInspector()) {
                if (gt.Stabilization == false && lastStabilizeValue == true) {
                    // Trigger stabilization reset
                    gt.Stabilization = true;
                    gt.Stabilization = false;
                }
            }

            lastStabilizeValue = gt.Stabilization;
        }
    }
}