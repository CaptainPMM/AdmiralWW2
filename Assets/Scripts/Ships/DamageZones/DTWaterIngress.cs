using UnityEngine;
using Effects;

namespace Ships.DamageZones {
    /// <summary>
    /// fparam[0] sectionID, containing all the affected floating points by this damage zone
    /// </summary>
    public class DTWaterIngress : BaseDamageType {
        public override DamageType Type => DamageType.WaterIngress;

        public override void InflictDamage(DamageZone damageZone, Projectiles.Projectile projectile, DamageParams param = default) {
            if (PenetrationCheck(projectile, damageZone.Ship.HullArmor, Vector3.Angle(damageZone.Ship.transform.forward, projectile.transform.forward))) {
                // Penetration effects
                EffectManager.InitProjectilePenEffect(projectile);
                damageZone.Ship.Rigidbody.AddForceAtPosition(projectile.Velocity * projectile.FromTurret.GunsCaliber * damageZone.Ship.Armament.GunTurrets[0].ShipRecoil, projectile.GetPreviousPosition(), ForceMode.Impulse);

                // Add water ingress to ship
                damageZone.Ship.AddWaterIngress((byte)param.fparam[0]);
            } else {
                // Bounce effects
                EffectManager.InitProjectileBounceEffect(projectile);
            }
        }
    }
}