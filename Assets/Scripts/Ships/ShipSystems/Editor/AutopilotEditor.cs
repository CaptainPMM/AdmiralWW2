using UnityEditor;

namespace Ships.ShipSystems.Editors {
    [CustomEditor(typeof(Autopilot))]
    public class AutopilotEditor : Editor {
        private Autopilot.ChadburnSetting lastChadburn = Autopilot.ChadburnSetting.Stop;

        public override void OnInspectorGUI() {
            Autopilot a = (Autopilot)target;

            if (DrawDefaultInspector()) {
                if (a.Chadburn != lastChadburn) a.Chadburn = a.Chadburn; // Triggers the property setter
            }

            lastChadburn = a.Chadburn;
        }
    }
}