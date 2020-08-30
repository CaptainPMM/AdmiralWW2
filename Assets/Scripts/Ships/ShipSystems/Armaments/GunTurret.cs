using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Projectiles;
using Ocean;
using Ocean.OceanPhysics;
using Effects;

namespace Ships.ShipSystems.Armaments {
    public class GunTurret : MonoBehaviour {
        private const float GUN_RECOIL_RECOVER_SPEED_MULTIPLIER = 0.1f;

        /// <summary>
        /// Multiply the guns range with this constant to get the range for the game in meters/unity units
        /// </summary>
        public const float GUNS_RANGE_INGAME_MOD = 0.25f;

        public delegate void FireEvent(GunTurret turret, Projectile[] projectiles);
        public event FireEvent OnFire;

        [Header("Setup")]
        [SerializeField] private TurretType turretType = TurretType.BowA;
        [SerializeField] private Ship ship = null;
        [SerializeField] private Collider turretCollider = null;
        [SerializeField] private Transform turretTransform = null;
        [SerializeField] private bool sternTurret = false;
        [SerializeField] private List<GunRepresentation> guns = new List<GunRepresentation>();

        public TurretType Type => turretType;
        public Ship Ship => ship;
        public bool SternTurret => sternTurret;
        public int NumGuns => guns.Count;

        [Header("Stats")]
        [SerializeField, Range(0, 180)] private byte turretMaxRotationAngle = 120;
        /// <summary>
        /// Rotation speed of the turret in degrees/sec (multiply with Time.fixedDeltaTime)
        /// </summary>
        [SerializeField, Range(0, 45)] private byte turretRotationSpeed = 6;
        [SerializeField, Range(0, 90)] private byte gunsMaxElevationAngle = 42;
        [SerializeField, Range(-90, 90)] private sbyte gunsMinElevationAngle = -5;
        /// <summary>
        /// Elevation speed of the guns in degrees/sec (multiply with Time.fixedDeltaTime)
        /// </summary>
        [SerializeField, Range(0, 45)] private byte gunsElevationSpeed = 6;
        /// <summary>
        /// The size/diameter of the the projectiles in millimeters (caliber/1000f => unity units/meters)
        /// </summary>
        [SerializeField] private ushort gunsCaliber = 200;
        /// <summary>
        /// The initial speed of the fired projectile in m/sec
        /// </summary>
        [SerializeField] private float muzzleVelocity = 1000f;
        /// <summary>
        /// The maximum firing range of the guns in meters/unity units; this is more a statistical value rather than a mathmetical result of the gun properties;
        /// this value represents the real range of the guns, to get the value relevant for the game multiply this with GUNS_RANGE_INGAME_MOD
        /// </summary>
        [SerializeField] private int gunsRange = 25_000;
        /// <summary>
        /// Guns reload time in seconds
        /// </summary>
        [SerializeField] private float gunsReload = 10f;
        /// <summary>
        /// Accuracy of the guns, (0-1)
        /// </summary>
        [SerializeField, Range(0f, 1f)] private float gunsPrecision = 0.5f;
        [SerializeField] private float gunRecoil = 1f;
        [SerializeField] private float gunRecoilSpeed = 0.5f;
        [SerializeField] private float shipRecoil = 40f;
        /// <summary>
        /// Ignore the origin position as it is overridden by the gun transforms and field waterDisplaceEffectsDist
        /// </summary>
        [SerializeField, Tooltip("Ignore the origin position as it is overridden by the gun transforms and field waterDisplaceEffectsDist")]
        private WaterDisplaceEffect.WaterDisplaceEffectSettings waterDisplaceEffectSettings;
        [SerializeField] private float waterDisplaceEffectsDist = 1f;
        [SerializeField] private float gunFireEffectSize = 1f;

        /// <summary>
        /// Rotation speed of the turret in degrees/sec (multiply with Time.fixedDeltaTime)
        /// </summary>
        public byte TurretRotationSpeed => turretRotationSpeed;
        public byte TurretMaxRotationAngle => turretMaxRotationAngle;
        /// <summary>
        /// Elevation speed of the guns in degrees/sec (multiply with Time.fixedDeltaTime)
        /// </summary>
        public byte GunsElevationSpeed => gunsElevationSpeed;
        public byte GunsMaxElevationAngle => gunsMaxElevationAngle;
        public sbyte GunsMinElevationAngle => gunsMinElevationAngle;
        /// <summary>
        /// The size/diameter of the the projectiles in millimeters (caliber/1000f => unity units/meters)
        /// </summary>
        public ushort GunsCaliber => gunsCaliber;
        /// <summary>
        /// The initial speed of the fired projectile in m/sec
        /// </summary>
        public float MuzzleVelocity => muzzleVelocity;
        /// <summary>
        /// The maximum firing range of the guns in meters/unity units; this is more a statistical value rather than a mathmetical result of the gun properties;
        /// this value represents the real range of the guns, to get the value relevant for the game multiply this with GUNS_RANGE_INGAME_MOD
        /// </summary>
        public int GunsRealRange => gunsRange;
        /// <summary>
        /// The maximum firing range of the guns in meters/unity units; this is more a statistical value rather than a mathmetical result of the gun properties;
        /// this value represents the in-game range of the guns, the RealGunsRange value is multiplied with GUNS_RANGE_INGAME_MOD; RealGunsRange is the real-world value of the guns range
        /// </summary>
        public float GunsRange => (float)gunsRange * GUNS_RANGE_INGAME_MOD;
        /// <summary>
        /// Guns reload time in seconds
        /// </summary>
        public float GunsReload => gunsReload;
        /// <summary>
        /// Accuracy of the guns, (0-1)
        /// </summary>
        public float GunsPrecision => gunsPrecision;

        [Header("Current state")]
        [SerializeField] private bool engaged = false;
        /// <summary>
        /// Stabilize the gun elevation to counteract waves and other forces dispersing the shot
        /// </summary>
        [SerializeField] private bool stabilization = false;
        /// <summary>
        /// Fire as soon as ReadyToFire (reloaded) [once]
        /// </summary>
        [SerializeField] private bool fire = false;
        /// <summary>
        /// Fire immediately after the guns are ReadyToFire (reloaded) and aimed (AimReady) [repeat]
        /// </summary>
        [SerializeField] private bool fireAtWill = false;
        [SerializeField, Range(0f, 1f)] private float reloadProgress = 0f;
        [SerializeField, Range(-180, 180)] private float turretRotation = 0f;
        [SerializeField, Range(-180, 180)] private float targetTurretRotation = 0f;
        [SerializeField, Range(-90, 90)] private float gunsElevation = 0f;
        [SerializeField, Range(-90, 90)] private float targetGunsElevation = 0f;
        [SerializeField, Range(-90, 90)] private float stabilizationAngle = 0f;

        public bool Engaged { get => engaged; set => engaged = value; }
        public bool Stabilization {
            get => stabilization;
            set {
                if (value == false && stabilization == true) StartCoroutine(ResetGunStabilization());
                stabilization = value;
            }
        }
        /// <summary>
        /// Fire as soon as ReadyToFire (reloaded) [once]
        /// </summary>
        public void Fire() { fire = true; }
        /// <summary>
        /// Abort single fire order
        /// </summary>
        public void AbortFire() { fire = false; }
        /// <summary>
        /// Fire immediately after the guns are ReadyToFire (reloaded) and aimed (AimReady) [repeat]
        /// </summary>
        public bool FireAtWill { get => fireAtWill; set => fireAtWill = value; }
        /// <summary>
        /// Stop all firing orders
        /// </summary>
        public void CeaseFire() { fire = false; fireAtWill = false; }
        /// <summary>
        /// Guns are reloaded
        /// </summary>
        public bool ReadyToFire => reloadProgress >= 1f;
        /// <summary>
        /// Guns are aimed
        /// </summary>
        public bool AimReady => turretRotation == targetTurretRotation && gunsElevation == targetGunsElevation;
        public float TargetTurretRotation { get => targetTurretRotation; set => targetTurretRotation = Mathf.Clamp(value, -turretMaxRotationAngle, turretMaxRotationAngle); }
        public float TargetGunsElevation { get => targetGunsElevation; set => targetGunsElevation = Mathf.Clamp(value, gunsMinElevationAngle, gunsMaxElevationAngle); }

        private bool tempAimReady = false;
        private List<Coroutine> gunRecoilRoutines = new List<Coroutine>();
        private sbyte turretLocationMod = -1;

        private void Awake() {
            if (ship == null) Debug.LogWarning("GunTurret needs an assigned ship");
            if (turretCollider == null) Debug.LogWarning("GunTurret needs an assigned turret collider");
            if (turretTransform == null) Debug.LogWarning("GunTurret needs an assigned turret transform");
            if (guns.Count == 0) Debug.LogWarning("GunTurret needs minimum one gun");
        }

        private void Start() {
            for (int i = 0; i < guns.Count; i++) {
                GunRepresentation gun = guns[i];
                gun.initLocalPos = gun.gunTransform.localPosition;
                guns[i] = gun;
            }
            turretLocationMod = sternTurret ? (sbyte)1 : (sbyte)-1;
        }

        private void FixedUpdate() {
            if (engaged) {
                tempAimReady = AimReady;

                if (!ReadyToFire) {
                    Reload();
                } else {
                    if (fire || (fireAtWill && tempAimReady)) {
                        FireNow();
                    }
                }

                if (!tempAimReady) Aim();

                if (stabilization) {
                    Vector3 forward = turretTransform.forward * turretLocationMod;
                    Vector3 flatForward = Quaternion.AngleAxis(-turretTransform.rotation.eulerAngles.x, turretTransform.right) * forward;
                    float forwardToFlatAngle = Vector3.SignedAngle(forward, flatForward, turretTransform.right) * -turretLocationMod;

                    stabilizationAngle = Mathf.MoveTowards(stabilizationAngle, forwardToFlatAngle, gunsElevationSpeed * Time.fixedDeltaTime);
                    guns.ForEach(gun => gun.gunTransform.localRotation = Quaternion.Euler(Mathf.Clamp(stabilizationAngle + gunsElevation, gunsMinElevationAngle, gunsMaxElevationAngle) * -turretLocationMod, 0, 0));
                }
            }
        }

        private void Reload() {
            reloadProgress = Mathf.MoveTowards(reloadProgress, 1f, Time.fixedDeltaTime / gunsReload);
        }

        private void Aim() {
            // Update values
            turretRotation = Mathf.MoveTowards(turretRotation, targetTurretRotation, turretRotationSpeed * Time.fixedDeltaTime);
            gunsElevation = Mathf.MoveTowards(gunsElevation, targetGunsElevation, gunsElevationSpeed * Time.fixedDeltaTime);

            // Update transforms
            turretTransform.localRotation = Quaternion.Euler(0, turretRotation * (sternTurret ? -1 : 1), 0);
            guns.ForEach(gun => gun.gunTransform.localRotation = Quaternion.Euler(gunsElevation * (sternTurret ? -1 : 1), 0, 0));
        }

        /// <summary>
        /// Does the actual fire stuff
        /// </summary>
        [ContextMenu("Fire Now!")]
        private void FireNow() {
            // Update values
            fire = false;
            reloadProgress = 0f;

            // Spawn projectile
            List<Projectile> projectiles = new List<Projectile>();
            guns.ForEach(gun => projectiles.Add(ProjectileManager.CreateProjectile(this, gun.gunEffectTransform)));

            // Add effects
            gunRecoilRoutines.ForEach(r => StopCoroutine(r));
            gunRecoilRoutines.Clear();
            guns.ForEach(gun => {
                // Gun recoil animation
                gunRecoilRoutines.Add(StartCoroutine(GunRecoilEffectRoutine(gun)));

                // Ship recoil force
                ship.Rigidbody.AddForceAtPosition(gun.gunTransform.forward * muzzleVelocity * gunsCaliber * shipRecoil * (sternTurret ? -1 : 1), gun.gunTransform.position, ForceMode.Impulse);

                // Particle effects
                Transform gunFireTransform = gun.gunEffectTransform;
                gunFireTransform.localScale = new Vector3(gunFireEffectSize, gunFireEffectSize, gunFireEffectSize);
                EffectManager.InitGunFireEffect(gunFireTransform);

                // Water effects
                waterDisplaceEffectSettings.origin = gun.gunEffectTransform.position + gun.gunEffectTransform.forward * waterDisplaceEffectsDist;
                OceanManager.InitWaterDisplaceEffect(waterDisplaceEffectSettings);
            });

            OnFire?.Invoke(this, projectiles.ToArray());
        }

        private IEnumerator GunRecoilEffectRoutine(GunRepresentation gun) {
            Vector3 recoilTargetPos = gun.initLocalPos - gun.gunTransform.localRotation * Vector3.forward * gunRecoil * (gun.gunTransformForwardInversed ? -1 : 1);
            // Backwards impulse
            while (gun.gunTransform.localPosition != recoilTargetPos) {
                gun.gunTransform.localPosition = Vector3.Lerp(gun.gunTransform.localPosition, recoilTargetPos, gunRecoilSpeed);
                yield return new WaitForFixedUpdate();
            }
            // Recover init pos
            while (gun.gunTransform.localPosition != gun.initLocalPos) {
                gun.gunTransform.localPosition = Vector3.Lerp(gun.gunTransform.localPosition, gun.initLocalPos, gunRecoilSpeed * GUN_RECOIL_RECOVER_SPEED_MULTIPLIER);
                yield return new WaitForFixedUpdate();
            }
        }

        private IEnumerator ResetGunStabilization() {
            while (stabilizationAngle != 0f) {
                stabilizationAngle = Mathf.MoveTowards(stabilizationAngle, 0f, gunsElevationSpeed * Time.fixedDeltaTime);
                guns.ForEach(gun => gun.gunTransform.localRotation = Quaternion.Euler(Mathf.Clamp(stabilizationAngle + gunsElevation, gunsMinElevationAngle, gunsMaxElevationAngle) * -turretLocationMod, 0, 0));
                yield return new WaitForFixedUpdate();
            }
        }

        public enum TurretType : byte {
            BowA,
            BowB,
            BowC,
            BowD,
            SternA,
            SternB,
            SternC,
            SternD,
            StarboardA,
            StarboardB,
            PortA,
            PortB
        }

        [System.Serializable]
        public struct GunRepresentation {
            public Transform gunTransform;
            public Transform gunEffectTransform;
            public bool gunTransformForwardInversed;

            [HideInInspector]
            public Vector3 initLocalPos;
        }
    }
}