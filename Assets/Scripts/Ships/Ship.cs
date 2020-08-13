using UnityEngine;
using Ocean.OceanPhysics;

namespace Ships {
    public class Ship : MonoBehaviour {
        [Header("Setup")]
        [SerializeField] private Rigidbody rb = null;
        [SerializeField] private FloatPhysics floatPhysics = null;
        [SerializeField] private WaterPropulsion propulsion = null;

        public Rigidbody Rigidbody => rb;
        public FloatPhysics FloatPhysics => floatPhysics;
        public WaterPropulsion Propulsion => propulsion;

        private void Awake() {
            if (rb == null) Debug.LogWarning("Ship script needs an assigned rigidbody");
            if (floatPhysics == null) Debug.LogWarning("Ship script needs assigned float physics");
            if (propulsion == null) Debug.LogWarning("Ship script needs an assigned propulsion");
        }
    }
}