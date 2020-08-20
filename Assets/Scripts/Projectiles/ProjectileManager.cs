using UnityEngine;
using Ships.ShipSystems.Armaments;

namespace Projectiles {
    public class ProjectileManager : MonoBehaviour {
        public static ProjectileManager Inst { get; private set; }

        [SerializeField] private GameObject projectilePrefab = null;

        private void Awake() {
            Inst = this;
            if (projectilePrefab == null) Debug.LogWarning("ProjectileManager needs the assigned projectile prefab");
        }

        public static void CreateProjectile(GunTurret fromTurret, Transform initTransform) {
            GameObject projectileGO = Instantiate(Inst.projectilePrefab, Inst.transform);
            projectileGO.name = "Projectile<" + fromTurret.Ship.ShipName + ">";
            Projectile projectile = projectileGO.GetComponent<Projectile>();
            projectile.Init(fromTurret, initTransform);
        }
    }
}