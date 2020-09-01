using UnityEngine;
using Ships.ShipSystems.Armaments;

namespace Ships.DamageZones {
    /// <summary>
    /// oparam[0] GameObject reference with GunTurret component
    /// </summary>
    public class DTGunTurret : BaseDamageType {
        public override DamageType Type => DamageType.GunTurret;

        public override void InflictDamage(DamageZone damageZone, Projectiles.Projectile projectile, DamageParams param = default) {
            GunTurret turret = ((GameObject)param.oparam[0]).GetComponent<GunTurret>();
            if (PenetrationCheck(projectile, turret.TurretArmor)) {
                // Hit particle effect


                // Destroy Turret
                turret.Disable();
            } else {
                // No penetration particle effect

            }
        }
    }
}