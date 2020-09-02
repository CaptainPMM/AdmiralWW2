using UnityEngine;
using Effects;

namespace Ships.DamageZones {
    /// <summary>
    /// No params
    /// </summary>
    public class DTRudder : BaseDamageType {
        public override DamageType Type => DamageType.Rudder;

        public override void InflictDamage(DamageZone damageZone, Projectiles.Projectile projectile, DamageParams param = default) {
            if (PenetrationCheck(projectile, damageZone.Ship.HullArmor, Vector3.Angle(damageZone.Ship.transform.forward, projectile.transform.forward))) {
                // Penetration effects
                EffectManager.InitProjectilePenEffect(projectile);
                damageZone.Ship.Rigidbody.AddForceAtPosition(projectile.Velocity * projectile.FromTurret.GunsCaliber * damageZone.Ship.Armament.GunTurrets[0].ShipRecoil, projectile.GetPreviousPosition(), ForceMode.Impulse);

                // Add ship damage
                damageZone.Ship.Propulsion.DisableRudder();
            } else {
                // Bounce effects
                EffectManager.InitProjectileBounceEffect(projectile);
            }
        }
    }
}