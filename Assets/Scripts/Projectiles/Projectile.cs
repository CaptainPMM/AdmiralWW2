using UnityEngine;
using Ships.ShipSystems.Armaments;
using Ocean;

namespace Projectiles {
    public class Projectile : MonoBehaviour {
        private const float ADDITIONAL_GRAVITY_MULTIPLIER = 100f;
        private const float DESTROY_DELAY_AFTER_HIT_OCEAN_SURFACE = 3f;

        /// <summary>
        /// Multiply the caliber with this value to scale the projectile model correctly
        /// </summary>
        public const float CALIBER_TO_SCALE = 0.01f;

        [Header("Setup")]
        [SerializeField] private Rigidbody rb = null;

        [Header("Runtime settings (must set after instantiation)")]
        [SerializeField] private GunTurret fromTurret = null;
        [SerializeField] private Transform initTransform = null;

        /// <summary>
        /// Call this "constructor" after instantiating the projectile prefab
        /// </summary>
        /// <param name="fromTurret">GunTurret that fired the projectile</param>
        /// <param name="initTransform">The transform to initiate this projectile at with correct position and orientation/rotation</param>
        public void Init(GunTurret fromTurret, Transform initTransform) {
            this.fromTurret = fromTurret;
            this.initTransform = initTransform;
        }

        private void Awake() {
            if (rb == null) Debug.LogWarning("Projectile needs an assigned rigidbody");
        }

        private void Start() {
            if (fromTurret == null) { Debug.LogWarning("Projetile needs a fromTurret (call Init() after instantiating the prefab)"); return; }
            if (initTransform == null) { Debug.LogWarning("Projetile needs an initTransform (call Init() after instantiating the prefab)"); return; }

            transform.position = initTransform.position;
            transform.rotation = initTransform.rotation;

            float scale = fromTurret.GunsCaliber * CALIBER_TO_SCALE;
            transform.localScale = new Vector3(scale, scale, scale);

            rb.mass = fromTurret.GunsCaliber;

            rb.velocity = initTransform.forward * fromTurret.MuzzleVelocity;
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
                // TODO

                Destroy(gameObject, DESTROY_DELAY_AFTER_HIT_OCEAN_SURFACE);
            }
        }
    }
}