using System.Collections.Generic;
using UnityEngine;

namespace Ships.ShipSystems.Armaments {
    public class GunTurret : MonoBehaviour {
        /// <summary>
        /// Multiply the guns range with this constant to get the range for the game in meters/unity units
        /// </summary>
        public const float GUNS_RANGE_INGAME_MOD = 0.1f;

        [Header("Setup")]
        [SerializeField] private TurretType turretType = TurretType.BowA;
        [SerializeField] private Collider turretCollider = null;
        [SerializeField] private Transform turretTransform = null;
        [SerializeField] private List<GunRepresentation> guns = new List<GunRepresentation>();

        public TurretType Type => turretType;
        public int NumGuns => guns.Count;

        [Header("Stats")]
        /// <summary>
        /// Rotation speed of the turret in degrees/sec (multiply with Time.fixedDeltaTime)
        /// </summary>
        [SerializeField, Range(0, 45)] private byte turretRotationSpeed = 6;
        [SerializeField, Range(0, 180)] private byte turretMaxRotationAngle = 120;
        /// <summary>
        /// Elevation speed of the guns in degrees/sec (multiply with Time.fixedDeltaTime)
        /// </summary>
        [SerializeField, Range(0, 45)] private byte gunsElevationSpeed = 6;
        [SerializeField, Range(0, 90)] private byte gunsMaxElevationAngle = 42;
        [SerializeField, Range(-90, 90)] private sbyte gunsMinElevationAngle = -5;
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
        [SerializeField] private bool fire = false;
        [SerializeField] private bool readyToFire = false;
        [SerializeField] private bool fireAtWill = false;
        [SerializeField] private Vector3 aimWorldPos = Vector3.zero;
        [SerializeField] private bool aimReady = false;
        [SerializeField, Range(-180, 180)] private float turretRotation = 0f;
        [SerializeField, Range(-180, 180)] private float turretTargetRotation = 0f;
        [SerializeField, Range(-90, 90)] private float gunsElevation = 0f;
        [SerializeField, Range(-90, 90)] private float gunsTargetElevation = 0f;

        private void Awake() {
            if (turretCollider == null) Debug.LogWarning("GunTurret needs an assigned turret collider");
            if (turretTransform == null) Debug.LogWarning("GunTurret needs an assigned turret transform");
            if (guns.Count == 0) Debug.LogWarning("GunTurret needs minimum one gun");
        }

        private void FixedUpdate() {

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
        }
    }
}