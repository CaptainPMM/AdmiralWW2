using UnityEngine;
using Ships.ShipSystems.Armaments;
using Ocean;
using Ocean.OceanPhysics;
using Effects;

namespace Projectiles {
    public class Projectile : MonoBehaviour {
        private const float MAX_DISPERSION_ANGLE = 2f;
        private const float ADDITIONAL_GRAVITY_MULTIPLIER = 100f;
        private const float DESTROY_DELAY_AFTER_HIT_OCEAN_SURFACE = 3f;

        /// <summary>
        /// Multiply the caliber with this value to scale the projectile model correctly
        /// </summary>
        public const float CALIBER_TO_SCALE = 0.01f;

        public delegate void HitEvent(Projectile projectile, Vector3 worldPos);
        public event HitEvent OnHit;

        [Header("Setup")]
        [SerializeField] private Rigidbody rb = null;

        public Vector3 Velocity => rb.velocity;
        public float Mass => rb.mass;

        [Header("Runtime settings (must set after instantiation)")]
        [SerializeField] private GunTurret fromTurret = null;

        public GunTurret FromTurret => fromTurret;

        /// <summary>
        /// Call this "constructor" after instantiating the projectile prefab
        /// </summary>
        /// <param name="fromTurret">GunTurret that fired the projectile</param>
        public void Init(GunTurret fromTurret) {
            this.fromTurret = fromTurret;
        }

        private void Awake() {
            if (rb == null) Debug.LogWarning("Projectile needs an assigned rigidbody");
        }

        private void Start() {
            if (fromTurret == null) { Debug.LogWarning("Projetile needs a fromTurret (call Init() after instantiating the prefab)"); return; }

            // Apply dispersion
            float invAccuracy = 1f - fromTurret.GunsPrecision;
            transform.Rotate(Random.Range(0f, invAccuracy) * MAX_DISPERSION_ANGLE, Random.Range(0f, invAccuracy) * MAX_DISPERSION_ANGLE, Random.Range(0f, invAccuracy) * MAX_DISPERSION_ANGLE, Space.World);

            float scale = fromTurret.GunsCaliber * CALIBER_TO_SCALE;
            transform.localScale = new Vector3(scale, scale, scale);

            rb.mass = fromTurret.GunsCaliber;

            rb.velocity = transform.forward * fromTurret.MuzzleVelocity;
        }

        private void FixedUpdate() {
            // Fix rotation based on velocity
            transform.rotation = Quaternion.LookRotation(rb.velocity, Vector3.up);

            // Apply additional gravity down force
            rb.AddForce(Physics.gravity * ADDITIONAL_GRAVITY_MULTIPLIER);

            // Check water hit
            if (transform.position.y <= OceanManager.OceanSurfaceYPos) {
                enabled = false;

                // Add splash effects
                Transform waterSplashTransform = transform;
                waterSplashTransform.rotation = Quaternion.Euler(0, 0, 0);
                waterSplashTransform.localScale = Vector3.one;
                EffectManager.InitWaterSplashEffect(waterSplashTransform);

                OceanManager.InitWaterDisplaceEffect(new WaterDisplaceEffect.WaterDisplaceEffectSettings() {
                    origin = transform.position,
                    originOffset = Vector3.zero,
                    targetOffset = new Vector3(0, -30, 0),
                    size = 10f,
                    speed = 3f,
                    foamSizeMultiplier = 4f
                });

                InvokeHitEvent();
                Destroy(gameObject, DESTROY_DELAY_AFTER_HIT_OCEAN_SURFACE);
            }
        }

        public void InvokeHitEvent() {
            OnHit?.Invoke(this, transform.position);
        }
    }
}