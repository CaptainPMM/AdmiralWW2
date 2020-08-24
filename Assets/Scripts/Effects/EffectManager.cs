using UnityEngine;

namespace Effects {
    public class EffectManager : MonoBehaviour {
        public static EffectManager Inst { get; private set; }

        [Header("Setup")]
        [SerializeField] private GameObject gunFirePrefab = null;

        private void Awake() {
            Inst = this;
        }

        public static void InitGunFireEffect(Transform worldTransform) {
            GameObject eGO = Instantiate(Inst.gunFirePrefab, Inst.transform);
            eGO.name = "GunFireEffect";
            eGO.transform.position = worldTransform.position;
            eGO.transform.rotation = worldTransform.rotation;
            eGO.transform.localScale = worldTransform.localScale;
            Destroy(eGO, eGO.GetComponent<ParticleSystem>().main.duration);
        }
    }
}