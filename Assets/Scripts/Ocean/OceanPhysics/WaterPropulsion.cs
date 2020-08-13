using UnityEngine;
using Ships;

namespace Ocean.OceanPhysics {
    public class WaterPropulsion : MonoBehaviour {
        private const float RUDDER_FORCE_VELOCITY_IMPACT = 0.1f;
        private const float RUDDER_DRAG_IMPACT = 0.001f;

        [Header("Setup")]
        [SerializeField] private Ship ship = null;
        [SerializeField, Range(0f, 2f)] private float gizmoSize = 1f;
        [SerializeField] private uint enginePower = 10;
        [SerializeField, Range(0f, 1f)] private float throttleSpeed = 0.002f;
        [SerializeField] private uint rudderForce = 10_000_000;
        [SerializeField, Range(0, 90)] private byte maxRudderAngle = 45;
        [SerializeField, Range(0f, 180f)] private float rudderSpeed = 0.2f;
        [SerializeField] private Vector3 propellerOffset = Vector3.zero;

        [Header("Current state")]
        [SerializeField] private bool engineOn = true;
        [SerializeField, Range(0f, 1f)] private float throttle = 0f;
        [SerializeField, Range(-90f, 90f)] private float rudderAngle = 0f;

        public bool EngineOn { get => engineOn; set => engineOn = value; }
        public float Throttle { get => throttle; set => targetThrottle = Mathf.Clamp01(value); }
        public float RudderAngle { get => rudderAngle; set => targetRudderAngle = Mathf.Clamp(value, -maxRudderAngle, maxRudderAngle); }

        [Header("Internal vars")]
        [SerializeField] private Vector3 forceWorldPos = Vector3.zero;
        [SerializeField, Range(0f, 1f)] private float targetThrottle = 0f;
        [SerializeField, Range(-90f, 90f)] private float targetRudderAngle = 0f;

        private void Awake() {
            if (ship == null) Debug.LogWarning("WaterPropulsion script needs an assigned ship to apply forces on");
        }

        private void Start() {
            CalcForceWorldPos();
        }

        private void FixedUpdate() {
            CalcForceWorldPos();
            if (throttle != targetThrottle) UpdateThrottle();
            if (rudderAngle != targetRudderAngle) UpdateRudder();

            if (OceanManager.SampleWaterHeight(forceWorldPos) > forceWorldPos.y) {
                // Propeller is in water
                if (engineOn && throttle > 0f) ApplyThrottleForce();
                if (rudderAngle != 0f) ApplyRudderForce();
            }
        }

        private void UpdateThrottle() {
            throttle = Mathf.MoveTowards(throttle, targetThrottle, throttleSpeed);
        }

        private void UpdateRudder() {
            rudderAngle = Mathf.MoveTowards(rudderAngle, targetRudderAngle, rudderSpeed);
        }

        private void ApplyThrottleForce() {
            ship.Rigidbody.AddForceAtPosition(ThrottleForceFunc(), forceWorldPos, ForceMode.Acceleration);
        }

        private void ApplyRudderForce() {
            ship.Rigidbody.AddTorque(Vector3.up * rudderForce * rudderAngle * (ship.Rigidbody.velocity.magnitude * RUDDER_FORCE_VELOCITY_IMPACT), ForceMode.Force);
            ship.FloatPhysics.AdditionalDragWaterForward = Mathf.Abs(rudderAngle) * RUDDER_DRAG_IMPACT;
        }

        private Vector3 ThrottleForceFunc() {
            return ship.Rigidbody.transform.forward * enginePower * throttle;
        }

        public void CalcForceWorldPos() {
            forceWorldPos = ship.Rigidbody.transform.TransformPoint(propellerOffset);
        }

        private void OnDrawGizmosSelected() {
            if (ship && ship.Rigidbody) {
                Gizmos.color = Color.red;
                Gizmos.DrawWireCube(ship.Rigidbody.transform.TransformPoint(propellerOffset), Vector3.one * gizmoSize);
                Gizmos.DrawWireSphere(forceWorldPos, gizmoSize);

                Gizmos.color = Color.red;
                Gizmos.DrawLine(forceWorldPos, forceWorldPos - ThrottleForceFunc());
                Gizmos.color = Color.yellow;
                Gizmos.DrawLine(forceWorldPos, forceWorldPos + ship.Rigidbody.transform.right * rudderAngle * 0.08f * (ship.Rigidbody.velocity.magnitude * RUDDER_FORCE_VELOCITY_IMPACT));
            }
        }
    }
}