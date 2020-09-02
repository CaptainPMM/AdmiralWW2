using System.Collections.Generic;
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
        [SerializeField] private float hullHitpoints;

        public float Speed => rb.velocity.magnitude;
        public ushort Course => course;
        /// <summary>
        /// Remaining hitpoints. Durability of the ship (hull), each projectile that hits reduces this number by the projectiles caliber
        /// </summary>
        public float HullHitpoints => hullHitpoints;
        public void DamageHull(float damage) { hullHitpoints = Mathf.Max(0f, hullHitpoints - damage); }
        [SerializeField] private List<WaterIngressSection> waterIngressSections = new List<WaterIngressSection>();
        public void AddWaterIngress(byte sectionID) { waterIngressSections.Find(w => w.sectionID == sectionID).numHoles++; }

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

            // Init water ingress corridors
            for (int i = 0; i < floatPhysics.NumFloatPointSections; i++) {
                waterIngressSections.Add(new WaterIngressSection() {
                    sectionID = (byte)i,
                    floatPoints = floatPhysics.GetFloatPointsBySectionID((byte)i),
                    initFloatPointWeights = new List<float>(),
                    numHoles = 0,
                    waterLevel = 0f
                });
                waterIngressSections[i].floatPoints.ForEach(fp => waterIngressSections[i].initFloatPointWeights.Add(fp.weight));
            }
        }

        private void FixedUpdate() {
            UpdateCourse();
            HandleWaterIngress();
        }

        private void UpdateCourse() {
            float signedAngle = Vector3.SignedAngle(Vector3.forward, rb.transform.forward, Vector3.up);
            if (signedAngle >= 0) course = (ushort)signedAngle;
            else course = (ushort)(360 + signedAngle);
        }

        private void HandleWaterIngress() {
            foreach (WaterIngressSection section in waterIngressSections) {
                if (section.waterLevel < 1f) {
                    if (section.numHoles > 0) {
                        // Fill up section with water
                        section.waterLevel = Mathf.Min(1f, section.waterLevel + section.numHoles * Global.Ships.WATER_INGRESS_STRENGTH);
                        for (int i = 0; i < section.floatPoints.Count; i++) {
                            section.floatPoints[i].weight = Mathf.Lerp(0f, section.initFloatPointWeights[i], 1f - section.waterLevel);
                        }
                    }
                }
                if (section.waterLevel > 0f) {
                    // Damage ship
                    DamageHull(section.waterLevel * Global.Ships.WATER_INGRESS_HULL_DAMAGE_FACTOR);
                }
            }
        }

        private void OnInWaterChangeHandler(bool inWater) {
            oceanInputs.SetEnabled(inWater);
        }

        [System.Serializable]
        public class WaterIngressSection {
            public byte sectionID;
            public List<FloatPhysics.FloatPoint> floatPoints;
            public List<float> initFloatPointWeights;
            public int numHoles;
            public float waterLevel;
        }
    }
}