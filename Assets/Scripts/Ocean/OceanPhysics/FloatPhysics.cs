using System.Collections.Generic;
using UnityEngine;
using Crest;

namespace Ocean.OceanPhysics {
    [RequireComponent(typeof(Rigidbody))]
    public class FloatPhysics : MonoBehaviour {
        private const float WATER_DENSITY = 1000f;
        private readonly float WATER_BUOYANCY = WATER_DENSITY * Mathf.Abs(Physics.gravity.y);

        [Range(0f, 2f)]
        public float gizmoSize = 1f;

        public Vector3 centerOfMass;
        public List<FloatPoint> floatPoints = new List<FloatPoint>();
        public float forceFactor = 1f;
        /// <summary>
        /// Ignore wave lengths below this value, usually the boat width
        /// </summary>
        [Tooltip("Ingore wave lengths below this value, usually the boat width")]
        public float minWaveLength = 0f;

        private Rigidbody rb;
        private float totalFloatPointsWeight;

        private Vector3[] samplePoints;
        private Vector3[] sampleResDisplacements;
        private Vector3[] sampleResVelocities;

        private void Awake() {
            rb = GetComponent<Rigidbody>();
        }

        private void Start() {
            rb.centerOfMass = centerOfMass;
            CalcTotalFloatPointsWeigth();

            samplePoints = new Vector3[floatPoints.Count + 1];
            sampleResDisplacements = new Vector3[floatPoints.Count + 1];
            sampleResVelocities = new Vector3[floatPoints.Count + 1];
        }

        private void FixedUpdate() {
#if UNITY_EDITOR
            CalcTotalFloatPointsWeigth();
#endif

            UpdateWaterSamples();

            ApplyBuoyancy();
            ApplyDrag();
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
            for (int i = 0; i < floatPoints.Count; i++) {
                float waterHeight = OceanRenderer.Instance.SeaLevel + sampleResDisplacements[i].y;
                float heightDiff = waterHeight - samplePoints[i].y;
                if (heightDiff > 0) {
                    // Below water surface -> apply water buoyancy force
                    rb.AddForceAtPosition(WATER_BUOYANCY * heightDiff * Vector3.up * floatPoints[i].weight * forceFactor / totalFloatPointsWeight, samplePoints[i]);
                }
            }
        }

        private void ApplyDrag() {
            Vector3 waterSurfaceVelocity = sampleResVelocities[floatPoints.Count];
            Vector2 surfaceFlow = OceanManager.SampleWaterFlow(transform.position, minWaveLength);
            waterSurfaceVelocity += new Vector3(surfaceFlow.x, 0, surfaceFlow.y);

            // Apply drag relative to water
            Vector3 velocityRelativeToWater = rb.velocity - waterSurfaceVelocity;

            Vector3 forcePosition = rb.position;
            rb.AddForceAtPosition(Vector3.up * Vector3.Dot(Vector3.up, -velocityRelativeToWater), forcePosition);
            rb.AddForceAtPosition(transform.right * Vector3.Dot(transform.right, -velocityRelativeToWater), forcePosition);
            rb.AddForceAtPosition(transform.forward * Vector3.Dot(transform.forward, -velocityRelativeToWater), forcePosition);
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
        }
    }
}