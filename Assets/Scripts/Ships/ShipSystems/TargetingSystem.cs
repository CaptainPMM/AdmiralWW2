using UnityEngine;

namespace Ships.ShipSystems {
    public class TargetingSystem : MonoBehaviour {
        [Header("Setup")]
        [SerializeField] private Ship ship;

        [Header("Current state")]
        [SerializeField] private GameObject targetGO = null;

        private ITarget target = null;

        public GameObject TargetGO => targetGO;
        public ITarget Target { get => target; set { target = value; targetGO = value?.GameObject; } }

        private void FixedUpdate() {
            if (target != null) {

            }
        }
    }
}