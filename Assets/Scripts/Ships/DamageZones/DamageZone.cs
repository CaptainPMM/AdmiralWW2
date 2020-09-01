using System.Collections.Generic;
using UnityEngine;
using Projectiles;

namespace Ships.DamageZones {
    public class DamageZone : MonoBehaviour {
        [SerializeField] private Ship ship = null;
        [SerializeField] private Collider hitCollider = null;
        [SerializeField] private List<DamageTypeEntry> damageTypes = new List<DamageTypeEntry>();

        public Ship Ship => ship;

        private void Awake() {
            if (ship == null) Debug.LogWarning("DamageZone needs an assigned ship");
            if (hitCollider == null) Debug.LogWarning("DamageZone needs an assigned collider");
            if (damageTypes.Count == 0) Debug.LogWarning("DamageZone needs min 1 damageType");

            float probabilitySum = 0f;
            damageTypes.ForEach(dt => probabilitySum += dt.probability);
            if (probabilitySum != 1f) Debug.LogWarning("DamageZone damageTypeEntries probability sum is not 1");
        }

        private void OnTriggerEnter(Collider other) {
            if (other.gameObject.layer == LayerMask.NameToLayer(Global.LayerNames.PROJECTILES)) {
                Projectile p = other.GetComponentInParent<Projectile>();
                if (p.FromTurret.Ship != ship && p.enabled) {
                    ChooseStochasticDamageType(p);
                    p.InvokeHitEvent();
                    Destroy(p.gameObject);
                }
            }
        }

        private void ChooseStochasticDamageType(Projectile projectile) {
            float r = Random.Range(0f, 1f) * 0.999f;
            float probabilitySum = 0f;
            for (int i = 0; i < damageTypes.Count; i++) {
                if (damageTypes[i].probability + probabilitySum > r) {
                    BaseDamageType.CreateDamage(damageTypes[i].damageType).InflictDamage(this, projectile, damageTypes[i].param);
                    return;
                } else probabilitySum += damageTypes[i].probability;
            }
        }

        [System.Serializable]
        public struct DamageTypeEntry {
            [Range(0f, 1f)]
            public float probability;
            public DamageType damageType;
            public DamageParams param;
        }
    }
}