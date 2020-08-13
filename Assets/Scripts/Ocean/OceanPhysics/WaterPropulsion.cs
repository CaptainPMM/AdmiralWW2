using UnityEngine;

namespace Ocean.OceanPhysics {
    public class WaterPropulsion : MonoBehaviour {
        private const float RUDDER_FORCE_VELOCITY_IMPACT = 0.01f;

        [Header("Setup")]
        [SerializeField] private Rigidbody rb = null;
        [SerializeField] private uint enginePower = 100_000_000;
        [SerializeField, Range(0f, 1f)] private float throttleSpeed = 0.001f;
        [SerializeField] private uint rudderSize = 1000;
        [SerializeField, Range(0, 90)] private byte maxRudderAngle = 45;
        [SerializeField, Range(0f, 180f)] private float rudderSpeed = 0.1f;
        [SerializeField] private Vector3 propellerOffset = Vector3.zero;
        [SerializeField, Range(0f, 2f)] private float gizmoSize = 1f;

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
            if (rb == null) Debug.LogWarning("WaterPropulsion script needs an assigned rigidbody to apply forces on");
        }

        private void Start() {
            CalcForceWorldPos();
        }

        private void FixedUpdate() {
            CalcForceWorldPos();
            if (throttle != targetThrottle) UpdateThrottle();
            if (rudderAngle != targetRudderAngle) UpdateRudder();

            if (engineOn && throttle > 0f) ApplyThrottleForce();
            if (rudderAngle != 0f) ApplyRudderForce();
        }

        private void UpdateThrottle() {
            throttle = Mathf.MoveTowards(throttle, targetThrottle, throttleSpeed);
        }

        private void UpdateRudder() {
            rudderAngle = Mathf.MoveTowards(rudderAngle, targetRudderAngle, rudderSpeed);
        }

        private void ApplyThrottleForce() {
            rb.AddForceAtPosition(ThrottleForceFunc(), forceWorldPos);
        }

        private void ApplyRudderForce() {
            rb.AddForceAtPosition(RudderForceFunc(), forceWorldPos);
        }

        private Vector3 ThrottleForceFunc() {
            return rb.transform.forward * enginePower * throttle;
        }

        private Vector3 RudderForceFunc() {
            return rb.transform.right * rudderSize * rudderAngle * (rb.velocity.magnitude * RUDDER_FORCE_VELOCITY_IMPACT);
        }

        public void CalcForceWorldPos() {
            forceWorldPos = rb.transform.TransformPoint(propellerOffset);
        }

        private void OnDrawGizmosSelected() {
            if (rb) {
                Gizmos.color = Color.red;
                Gizmos.DrawWireCube(rb.transform.TransformPoint(propellerOffset), Vector3.one * gizmoSize);
                Gizmos.DrawWireSphere(forceWorldPos, gizmoSize);

                Gizmos.color = Color.red;
                Gizmos.DrawLine(forceWorldPos, forceWorldPos - ThrottleForceFunc() / enginePower);
                Gizmos.color = Color.yellow;
                Gizmos.DrawLine(forceWorldPos, forceWorldPos + RudderForceFunc() / rudderSize);
            }
        }
    }
}