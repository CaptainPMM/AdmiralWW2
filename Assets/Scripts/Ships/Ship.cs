using UnityEngine;
using Ocean.OceanPhysics;
using Ships.ShipSystems;

namespace Ships {
    public class Ship : MonoBehaviour, ITarget {
        [Header("Setup")]
        [SerializeField] private Rigidbody rb = null;
        [SerializeField] private FloatPhysics floatPhysics = null;
        [SerializeField] private WaterPropulsion propulsion = null;
        [SerializeField] private Autopilot autopilot = null;
        [SerializeField] private ShipOceanInputs oceanInputs = null;
        [SerializeField] private Armament armament = null;
        [SerializeField] private TargetingSystem targeting = null;

        public Rigidbody Rigidbody => rb;
        public FloatPhysics FloatPhysics => floatPhysics;
        public WaterPropulsion Propulsion => propulsion;
        public float RudderAngle { get => propulsion.RudderAngle; set => propulsion.RudderAngle = value; }
        public Autopilot Autopilot => autopilot;
        public ShipOceanInputs OceanInputs => oceanInputs;
        public Armament Armament => armament;
        public TargetingSystem Targeting => targeting;

        [Header("Settings")]
        [SerializeField] private ShipType type = ShipType.Default;
        [SerializeField] private Nationality nationality = Nationality.German;
        [SerializeField] private ShipDesignation shipDesignation = ShipDesignation.None;
        [SerializeField] private string shipName = "";
        [SerializeField] private string shipClass = "";
        /// <summary>
        /// Durability of the ship (hull), each projectile that hits reduces this number by the projectiles caliber
        /// </summary>
        [SerializeField] private uint maxHullHitpoints = 5000;
        /// <summary>
        /// Armor thickness in mm of the hull
        /// </summary>
        [SerializeField] private ushort hullArmor = 80;

        public ShipType Type => type;
        public Nationality Nationality => nationality;
        public ShipDesignation ShipDesignation => shipDesignation;
        public string ShipName => shipName;
        public string ShipClass => shipClass;
        public uint MaxHullHitpoints => maxHullHitpoints;
        public ushort HullArmor => hullArmor;

        [Header("Current state")]
        [SerializeField] private ushort course;
        /// <summary>
        /// Remaining hitpoints. Durability of the ship (hull), each projectile that hits reduces this number by the projectiles caliber
        /// </summary>
        [SerializeField] private uint hullHitpoints;

        public float Speed => rb.velocity.magnitude;
        public ushort Course => course;
        /// <summary>
        /// Remaining hitpoints. Durability of the ship (hull), each projectile that hits reduces this number by the projectiles caliber
        /// </summary>
        public uint HullHitpoints => hullHitpoints;
        public void DamageHull(uint damage) { hullHitpoints -= damage; }

        public GameObject GameObject => gameObject;
        public Vector3 WorldPos => transform.position;
        public Vector3 Velocity => rb.velocity;

        private void Awake() {
            if (rb == null) Debug.LogWarning("Ship script needs an assigned rigidbody");
            if (floatPhysics == null) Debug.LogWarning("Ship script needs assigned float physics");
            if (propulsion == null) Debug.LogWarning("Ship script needs an assigned propulsion");
            if (autopilot == null) Debug.LogWarning("Ship script needs an assigned autopilot");
            if (oceanInputs == null) Debug.LogWarning("Ship script needs an assigned ocean inputs handler");
            if (armament == null) Debug.LogWarning("Ship script needs an armament");
            if (targeting == null) Debug.LogWarning("Ship script needs an targeting system");
        }

        private void Start() {
            floatPhysics.OnInWaterChange += OnInWaterChangeHandler;
            hullHitpoints = maxHullHitpoints;
        }

        private void FixedUpdate() {
            UpdateCourse();
        }

        private void UpdateCourse() {
            float signedAngle = Vector3.SignedAngle(Vector3.forward, rb.transform.forward, Vector3.up);
            if (signedAngle >= 0) course = (ushort)signedAngle;
            else course = (ushort)(360 + signedAngle);
        }

        private void OnInWaterChangeHandler(bool inWater) {
            oceanInputs.SetEnabled(inWater);
        }
    }
}