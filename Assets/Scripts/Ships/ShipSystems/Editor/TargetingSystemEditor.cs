using UnityEngine;
using UnityEditor;

namespace Ships.ShipSystems.Editors {
    [CustomEditor(typeof(TargetingSystem))]
    public class TargetingSystemEditor : Editor {
        private GameObject lastTargetGO = null;
        private bool wrongTarget = false;

        public override void OnInspectorGUI() {
            TargetingSystem t = (TargetingSystem)target;

            if (DrawDefaultInspector()) {
                if (t.TargetGO != lastTargetGO) {
                    ITarget itarget = t.TargetGO.GetComponent<ITarget>();
                    if (itarget != null) {
                        t.Target = itarget;
                        wrongTarget = false;
                    } else {
                        t.Target = null;
                        wrongTarget = true;
                    }
                }
            }

            if (t.Target != null) {
                EditorGUILayout.HelpBox("Target aquired: Engaged", MessageType.Info);
            } else {
                EditorGUILayout.HelpBox("No Target: Disengaged", MessageType.Warning);
            }
            if (wrongTarget) {
                EditorGUILayout.HelpBox("Target GameObject did not implement ITarget interface!", MessageType.Error);
            }

            lastTargetGO = t.TargetGO;
        }
    }
}