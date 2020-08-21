using UnityEngine;

namespace Ocean.OceanPhysics {
    public class WaterDisplaceEffect : MonoBehaviour {
        [Header("Setup")]
        [SerializeField] private GameObject waterDisplaceSphere = null;
        [SerializeField] private GameObject waterFoamQuad = null;
        [SerializeField] private float gizmoSize = 0.75f;

        [Header("Runtime settings (set by calling Init() after instantiation)")]
        [SerializeField] private WaterDisplaceEffectSettings effectSettings;

        [SerializeField] private bool initialized = false;
        private Vector3 targetWorldPos = Vector3.zero;

        public void Init(WaterDisplaceEffectSettings effectSettings) {
            this.effectSettings = effectSettings;
            initialized = true;
        }

        private void Awake() {
            if (waterDisplaceSphere == null) Debug.LogWarning("WaterDisplaceEffect needs an assigned waterDisplaceSphere");
            if (waterFoamQuad == null) Debug.LogWarning("WaterDisplaceEffect needs an assigned waterFoamQuad");
        }

        private void Start() {
            if (!initialized) { Debug.LogWarning(("Call the Init() method on the WaterDisplaceEffect first")); enabled = false; return; }

            transform.position = effectSettings.origin + effectSettings.originOffset;
            waterDisplaceSphere.transform.localScale = new Vector3(effectSettings.size, effectSettings.size, effectSettings.size);
            waterFoamQuad.transform.localScale = new Vector3(effectSettings.size * effectSettings.foamSizeMultiplier, effectSettings.size * effectSettings.foamSizeMultiplier, 1);
            targetWorldPos = effectSettings.origin + effectSettings.targetOffset;
        }

        private void FixedUpdate() {
            transform.position = Vector3.MoveTowards(transform.position, targetWorldPos, effectSettings.speed);
            if (transform.position == targetWorldPos) Destroy(gameObject);
        }

        private void OnDrawGizmosSelected() {
            Gizmos.color = Color.blue;
            Gizmos.DrawSphere(effectSettings.origin, gizmoSize);
            Gizmos.color = Color.yellow;
            Vector3 originInitPos = effectSettings.origin + effectSettings.originOffset;
            Gizmos.DrawSphere(originInitPos, gizmoSize);
            Gizmos.DrawWireCube(effectSettings.origin, new Vector3(effectSettings.size, effectSettings.size, effectSettings.size));
            Gizmos.color = Color.red;
            Gizmos.DrawSphere(effectSettings.origin + effectSettings.targetOffset, gizmoSize);
            Gizmos.DrawLine(originInitPos, effectSettings.origin + effectSettings.targetOffset);
        }

        [System.Serializable]
        public struct WaterDisplaceEffectSettings {
            public Vector3 origin;
            public Vector3 originOffset;
            public Vector3 targetOffset;
            public float size;
            public float foamSizeMultiplier;
            public float speed;
        }
    }
}