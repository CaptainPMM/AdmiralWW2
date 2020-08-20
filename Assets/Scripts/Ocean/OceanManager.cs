using UnityEngine;
using Crest;

namespace Ocean {
    public class OceanManager : MonoBehaviour {
        public static OceanManager Inst { get; private set; }

        public static float OceanSurfaceYPos => OceanRenderer.Instance.SeaLevel;

        private void Awake() {
            Inst = this;
        }

        /// <summary>
        /// Get the water height at the given world position
        /// </summary>
        /// <param name="worldPos">3D world position</param>
        /// <param name="minWaveLength">Optional: ignore wave lengths below this value; with ships use the ship width for this value</param>
        /// <returns>The height of the ocean at the given world position</returns>
        public static float SampleWaterHeight(Vector3 worldPos, float minWaveLength = 0f) {
            float height = 0f;

            SampleHeightHelper sampler = new SampleHeightHelper();
            sampler.Init(worldPos, minWaveLength);
            sampler.Sample(ref height);

            return height;
        }

        /// <summary>
        /// Get the water surface flow at the given world position
        /// </summary>
        /// <param name="worldPos">3D world position</param>
        /// <param name="minWaveLength">Optional: ignore wave lengths below this value; with ships use the ship width for this value</param>
        /// <returns>The surface flow of the ocean at the given world position</returns>
        public static Vector2 SampleWaterFlow(Vector3 worldPos, float minWaveLength = 0f) {
            Vector2 flow = Vector3.zero;

            SampleFlowHelper sampler = new SampleFlowHelper();
            sampler.Init(worldPos, minWaveLength);
            sampler.Sample(ref flow);

            return flow;
        }
    }
}