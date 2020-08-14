using UnityEngine;
using Ocean.OceanPhysics;
using Ships.ShipSystems;

namespace Ships {
    public class Ship : MonoBehaviour {
        [Header("Setup")]
        [SerializeField] private Rigidbody rb = null;
        [SerializeField] private FloatPhysics floatPhysics = null;
        [SerializeField] private WaterPropulsion propulsion = null;
        [SerializeField] private Autopilot autopilot = null;

        public Rigidbody Rigidbody => rb;
        public FloatPhysics FloatPhysics => floatPhysics;
        public WaterPropulsion Propulsion => propulsion;
        public float RudderAngle { get => propulsion.RudderAngle; set => propulsion.RudderAngle = value; }
        public Autopilot Autopilot => autopilot;

        [Header("Current state")]
        [SerializeField] private ushort course;

        public float Speed => rb.velocity.magnitude;
        public ushort Course => course;

        private void Awake() {
            if (rb == null) Debug.LogWarning("Ship script needs an assigned rigidbody");
            if (floatPhysics == null) Debug.LogWarning("Ship script needs assigned float physics");
            if (propulsion == null) Debug.LogWarning("Ship script needs an assigned propulsion");
            if (autopilot == null) Debug.LogWarning("Ship script needs an assigned autopilot");
        }

        private void FixedUpdate() {
            UpdateCourse();
        }

        private void UpdateCourse() {
            float signedAngle = Vector3.SignedAngle(Vector3.forward, rb.transform.forward, Vector3.up);
            if (signedAngle >= 0) course = (ushort)signedAngle;
            else course = (ushort)(360 + signedAngle);
        }
    }
}