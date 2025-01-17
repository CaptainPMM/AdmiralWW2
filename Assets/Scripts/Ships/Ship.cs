using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Ocean;
using Ocean.OceanPhysics;
using Ships.ShipSystems;
using Ships.DamageZones;
using UI.Game.WorldSpace;

namespace Ships {
    public class Ship : MonoBehaviour, ITarget {
        public delegate void ShipSinkingEvent(Ship ship);
        public event ShipSinkingEvent OnSinking;

        [Header("Setup")]
        [SerializeField] private Rigidbody rb = null;
        [SerializeField] private FloatPhysics floatPhysics = null;
        [SerializeField] private WaterPropulsion propulsion = null;
        [SerializeField] private Autopilot autopilot = null;
        [SerializeField] private ShipOceanInputs oceanInputs = null;
        [SerializeField] private Armament armament = null;
        [SerializeField] private TargetingSystem targeting = null;
        [SerializeField] private ShipUI ui = null;

        public Rigidbody Rigidbody => rb;
        public FloatPhysics FloatPhysics => floatPhysics;
        public WaterPropulsion Propulsion => propulsion;
        public float RudderAngle { get => propulsion.RudderAngle; set => propulsion.RudderAngle = value; }
        public Autopilot Autopilot => autopilot;
        public ShipOceanInputs OceanInputs => oceanInputs;
        public Armament Armament => armament;
        public TargetingSystem Targeting => targeting;
        public ShipUI UI => ui;

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
        [SerializeField] private List<DamageZone> damageZones = new List<DamageZone>();

        public ShipType Type => type;
        public Nationality Nationality => nationality;
        public ShipDesignation ShipDesignation => shipDesignation;
        public string ShipName => shipName;
        public string ShipClass => shipClass;
        public uint MaxHullHitpoints => maxHullHitpoints;
        public ushort HullArmor => hullArmor;
        public List<DamageZone> DamageZones => damageZones;

        [Header("Current state")]
        [SerializeField] private ID id = new ID("");
        [SerializeField] private PlayerTag playerTag = PlayerTag.Player0;
        [SerializeField] private bool destroyed = false;
        [SerializeField] private ushort course = 0;
        /// <summary>
        /// Remaining hitpoints. Durability of the ship (hull), each projectile that hits reduces this number by the projectiles caliber
        /// </summary>
        [SerializeField] private float hullHitpoints;

        public ID ID => id;
        public PlayerTag PlayerTag => playerTag;
        public float Speed => rb.velocity.magnitude;
        public bool Destroyed => destroyed;
        public ushort Course => course;
        /// <summary>
        /// Remaining hitpoints. Durability of the ship (hull), each projectile that hits reduces this number by the projectiles caliber
        /// </summary>
        public float HullHitpoints { get => hullHitpoints; set => hullHitpoints = value; }
        public void DamageHull(float damage) {
            float newHullHP = hullHitpoints - damage;
            if (newHullHP <= 0f) {
                DestroyShip();
                hullHitpoints = 0f;
            } else hullHitpoints = newHullHP;
        }
        [SerializeField] private List<WaterIngressSection> waterIngressSections = new List<WaterIngressSection>();
        public void AddWaterIngress(byte sectionID) { waterIngressSections.Find(w => w.sectionID == sectionID).numHoles++; }
        public List<WaterIngressSection> WaterIngressSections => waterIngressSections;

        public GameObject GameObject => gameObject;
        public Vector3 WorldPos => transform.position;
        public Vector3 Velocity { get => rb.velocity; set => rb.velocity = value; }

        private void Awake() {
            if (rb == null) Debug.LogWarning("Ship script needs an assigned rigidbody");
            if (floatPhysics == null) Debug.LogWarning("Ship script needs assigned float physics");
            if (propulsion == null) Debug.LogWarning("Ship script needs an assigned propulsion");
            if (autopilot == null) Debug.LogWarning("Ship script needs an assigned autopilot");
            if (oceanInputs == null) Debug.LogWarning("Ship script needs an assigned ocean inputs handler");
            if (armament == null) Debug.LogWarning("Ship script needs an armament");
            if (targeting == null) Debug.LogWarning("Ship script needs an targeting system");
            if (ui == null) Debug.LogWarning("Ship script needs an UI");
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

        [ContextMenu("Destroy Ship")]
        private void DestroyShip() {
            if (!destroyed) {
                destroyed = true;

                // Disable ship components
                propulsion.enabled = false;
                autopilot.enabled = false;
                armament.SetEngageGunTurrets(false);
                targeting.enabled = false;
                targeting.StopAllCoroutines();

                // Sink ship
                StartCoroutine(SinkingRoutine());

                OnSinking.Invoke(this);
            }
        }

        private IEnumerator SinkingRoutine() {
            while (transform.position.y > OceanManager.OceanSurfaceYPos - Global.Ships.SINKING_DESTROY_SHIP_DEPTH) {
                floatPhysics.ForceFactor *= Global.Ships.SINKING_FORCE_FACTOR_REDUCTION_FACTOR;
                yield return new WaitForFixedUpdate();
            }
            floatPhysics.ForceFactor = 0f;
            Destroy(gameObject);
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