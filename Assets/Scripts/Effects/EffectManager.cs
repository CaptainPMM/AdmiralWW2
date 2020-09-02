using UnityEngine;

namespace Effects {
    public class EffectManager : MonoBehaviour {
        public static EffectManager Inst { get; private set; }

        [Header("Setup")]
        [SerializeField] private GameObject gunFirePrefab = null;
        [SerializeField] private GameObject waterSplashPrefab = null;
        [SerializeField] private GameObject projectilePenPrefab = null;
        [SerializeField] private GameObject projectileBouncePrefab = null;

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

        public static void InitWaterSplashEffect(Transform worldTransform) {
            GameObject eGO = Instantiate(Inst.waterSplashPrefab, Inst.transform);
            eGO.name = "WaterSplashEffect";
            eGO.transform.position = worldTransform.position;
            eGO.transform.rotation = worldTransform.rotation;
            eGO.transform.localScale = worldTransform.localScale;
            Destroy(eGO, eGO.GetComponent<ParticleSystem>().main.duration);
        }

        public static void InitProjectilePenEffect(Projectiles.Projectile projectile) {
            GameObject eGO = Instantiate(Inst.projectilePenPrefab, Inst.transform);
            eGO.name = "ProjectilePenEffect";
            eGO.transform.position = projectile.GetPreviousPosition();
            eGO.transform.rotation = projectile.transform.rotation;
            eGO.transform.localScale *= projectile.FromTurret.GunsCaliber / Global.Effects.PROJECTILE_PEN_EFFECT_SIZEMOD_REFERENCE_CALIBER;
            Destroy(eGO, eGO.GetComponent<ParticleSystem>().main.duration);
        }

        public static void InitProjectileBounceEffect(Projectiles.Projectile projectile) {
            GameObject eGO = Instantiate(Inst.projectileBouncePrefab, Inst.transform);
            eGO.name = "ProjectileBounceEffect";
            eGO.transform.position = projectile.GetPreviousPosition();
            eGO.transform.rotation = projectile.transform.rotation;
            eGO.transform.localScale *= projectile.FromTurret.GunsCaliber / Global.Effects.PROJECTILE_PEN_EFFECT_SIZEMOD_REFERENCE_CALIBER;
            Destroy(eGO, eGO.GetComponent<ParticleSystem>().main.duration);
        }
    }
}