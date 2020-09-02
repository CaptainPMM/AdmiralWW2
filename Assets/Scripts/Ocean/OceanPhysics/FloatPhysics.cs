using System.Collections.Generic;
using UnityEngine;
using Crest;

namespace Ocean.OceanPhysics {
    public class FloatPhysics : FloatingObjectBase {
        private const float WATER_DENSITY = 1000f;
        private readonly float WATER_BUOYANCY = WATER_DENSITY * Mathf.Abs(Physics.gravity.y);

        [Header("Setup")]
        [SerializeField] private Rigidbody rb = null;
        [SerializeField, Range(0f, 2f)] private float gizmoSize = 1f;

        [SerializeField] private Vector3 centerOfMass = Vector3.zero;
        [SerializeField] private List<FloatPoint> floatPoints = new List<FloatPoint>();
        [SerializeField] private float forceFactor = 5000f;

        /// <summary>
        /// Ignore wave lengths below this value, usually the boat width
        /// </summary>
        [SerializeField, Tooltip("Ingore wave lengths below this value, usually the boat width")]
        private float minWaveLength = 8f;

        [SerializeField] private float rbDragInWater = 1.5f;
        [SerializeField] private float dragWaterUp = 1f;
        [SerializeField] private float dragWaterSide = 0.5f;
        [SerializeField] private float dragWaterForward = 0.1f;

        [SerializeField] private float gravityForce = 0.5f;

        [Header("Current state")]
        [SerializeField] private bool inWater = false;
        [SerializeField] private float additionalDragWaterForward = 0f;

        public float AdditionalDragWaterForward { get => additionalDragWaterForward; set => additionalDragWaterForward = value; }

        public delegate void InWaterChange(bool inWater);
        public event InWaterChange OnInWaterChange;

        private float totalFloatPointsWeight;

        private Vector3[] samplePoints;
        private Vector3[] sampleResDisplacements;
        private Vector3[] sampleResVelocities;

        private Dictionary<byte, List<FloatPoint>> floatPointsBySectionID = new Dictionary<byte, List<FloatPoint>>();
        public byte NumFloatPointSections => (byte)floatPointsBySectionID.Count;
        public List<FloatPoint> GetFloatPointsBySectionID(byte sectionID) { return floatPointsBySectionID[sectionID]; }

        private bool newInWater;
        private float initRbDrag; // Air drag (not suitable for water)

        /* FloatingObjectBase overrides */
        public override float ObjectWidth => minWaveLength;
        public override bool InWater => inWater;
        public override Vector3 Velocity => rb.velocity;

        private Vector3 displacementToObject = Vector3.zero;
        public override Vector3 CalculateDisplacementToObject() => displacementToObject;
        /* ----- */

        private void Awake() {
            if (rb == null) Debug.LogWarning("FloatPhysics script needs an assigned rigidbody to apply forces on");

            // Assign float points to section mapping dict
            foreach (FloatPoint p in floatPoints) {
                if (floatPointsBySectionID.ContainsKey(p.sectionID)) floatPointsBySectionID[p.sectionID].Add(p);
                else floatPointsBySectionID.Add(p.sectionID, new List<FloatPoint>() { p });
            }
        }

        private void Start() {
            rb.centerOfMass = centerOfMass;
            CalcTotalFloatPointsWeigth();

            samplePoints = new Vector3[floatPoints.Count + 1];
            sampleResDisplacements = new Vector3[floatPoints.Count + 1];
            sampleResVelocities = new Vector3[floatPoints.Count + 1];

            initRbDrag = rb.drag;
        }

        private void FixedUpdate() {
#if UNITY_EDITOR
            CalcTotalFloatPointsWeigth();
#endif

            UpdateWaterSamples();

            displacementToObject = sampleResDisplacements[floatPoints.Count];

            ApplyBuoyancy();
            if (inWater) ApplyDrag();
        }

        private void CalcTotalFloatPointsWeigth() {
            totalFloatPointsWeight = 0f;
            floatPoints.ForEach(fp => totalFloatPointsWeight += fp.weight);
        }

        private void UpdateWaterSamples() {
            for (int i = 0; i < floatPoints.Count; i++) {
                samplePoints[i] = transform.TransformPoint(floatPoints[i].offsetPos + Vector3.up * centerOfMass.y);
            }
            samplePoints[floatPoints.Count] = transform.position;

            OceanRenderer.Instance.CollisionProvider.Query(GetHashCode(), minWaveLength, samplePoints, sampleResDisplacements, null, sampleResVelocities);
        }

        private void ApplyBuoyancy() {
            newInWater = false;

            for (int i = 0; i < floatPoints.Count; i++) {
                float waterHeight = OceanRenderer.Instance.SeaLevel + sampleResDisplacements[i].y;
                float heightDiff = waterHeight - samplePoints[i].y;
                if (heightDiff > 0) {
                    // Below water surface -> apply water buoyancy force
                    rb.AddForceAtPosition(WATER_BUOYANCY * heightDiff * Vector3.up * floatPoints[i].weight * forceFactor / totalFloatPointsWeight, samplePoints[i]);
                    if (!newInWater) newInWater = true;
                } else {
                    // Above water surface, apply some additional gravity (default gravity to soft)
                    rb.AddForceAtPosition(gravityForce * WATER_BUOYANCY * Physics.gravity * floatPoints[i].weight * forceFactor / totalFloatPointsWeight, samplePoints[i]);
                }
            }

            if (newInWater != inWater) {
                rb.drag = newInWater ? rbDragInWater : initRbDrag;
                inWater = newInWater;
                OnInWaterChange.Invoke(newInWater);
            }
        }

        private void ApplyDrag() {
            Vector3 waterSurfaceVelocity = sampleResVelocities[floatPoints.Count];
            Vector2 surfaceFlow = OceanManager.SampleWaterFlow(transform.position, minWaveLength);
            waterSurfaceVelocity += new Vector3(surfaceFlow.x, 0, surfaceFlow.y);

            // Apply drag relative to water
            Vector3 velocityRelativeToWater = rb.velocity - waterSurfaceVelocity;

            Vector3 forcePosition = rb.position;
            rb.AddForceAtPosition(Vector3.up * Vector3.Dot(Vector3.up, -velocityRelativeToWater) * dragWaterUp, forcePosition, ForceMode.Acceleration);
            rb.AddForceAtPosition(transform.right * Vector3.Dot(transform.right, -velocityRelativeToWater) * dragWaterSide, forcePosition, ForceMode.Acceleration);
            rb.AddForceAtPosition(transform.forward * Vector3.Dot(transform.forward, -velocityRelativeToWater) * (dragWaterForward + additionalDragWaterForward), forcePosition, ForceMode.Acceleration);
        }

        private void OnDrawGizmosSelected() {
            Gizmos.color = Color.yellow;
            Gizmos.DrawCube(transform.TransformPoint(centerOfMass), Vector3.one * gizmoSize);

            Gizmos.color = Color.cyan;
            foreach (FloatPoint fp in floatPoints) Gizmos.DrawSphere(transform.TransformPoint(fp.offsetPos + new Vector3(0, centerOfMass.y, 0)), gizmoSize * (0.1f + fp.weight));
        }

        [System.Serializable]
        public class FloatPoint {
            /// <summary>
            /// Offset position from the parent gameobject
            /// </summary>
            public Vector3 offsetPos;
            public float weight = 1f;
            public byte sectionID;
        }
    }
}