using UnityEngine;
using Ships.ShipSystems.Armaments;
using Effects;

namespace Ships.DamageZones {
    /// <summary>
    /// oparam[0] GameObject reference with GunTurret component
    /// </summary>
    public class DTGunTurret : BaseDamageType {
        public override DamageType Type => DamageType.GunTurret;

        public override void InflictDamage(DamageZone damageZone, Projectiles.Projectile projectile, DamageParams param = default) {
            GunTurret turret = ((GameObject)param.oparam[0]).GetComponent<GunTurret>();
            if (PenetrationCheck(projectile, turret.TurretArmor, Vector3.Angle(damageZone.Ship.transform.forward, projectile.transform.forward))) {
                // Penetration effects
                EffectManager.InitProjectilePenEffect(projectile);
                damageZone.Ship.Rigidbody.AddForceAtPosition(projectile.Velocity * projectile.FromTurret.GunsCaliber * damageZone.Ship.Armament.GunTurrets[0].ShipRecoil, projectile.GetPreviousPosition(), ForceMode.Impulse);

                // Destroy Turret
                turret.Disable();
            } else {
                // Bounce effects
                EffectManager.InitProjectileBounceEffect(projectile);
            }
        }
    }
}