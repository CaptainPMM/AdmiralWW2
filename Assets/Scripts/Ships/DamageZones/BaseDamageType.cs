using UnityEngine;

namespace Ships.DamageZones {
    public abstract class BaseDamageType {
        public abstract DamageType Type { get; }

        public abstract void InflictDamage(
            DamageZone damageZone,
            Projectiles.Projectile projectile,
            DamageParams param = default
        );

        public static BaseDamageType CreateDamage(DamageType type) {
            switch (type) {
                case DamageType.Hull:
                    return new DTHull();
                case DamageType.WaterIngress:
                    return new DTWaterIngress();
                case DamageType.Rudder:
                    return new DTRudder();
                case DamageType.Propeller:
                    return new DTPropeller();
                case DamageType.GunTurret:
                    return new DTGunTurret();
                default:
                    return null;
            }
        }

        protected bool PenetrationCheck(Projectiles.Projectile projectile, ushort armorThickness) {
            ushort appliedProjectileCaliber = (ushort)(projectile.FromTurret.GunsCaliber + (ushort)Mathf.Lerp(-Global.Penetration.PROJECTILE_CALIBER_VARIANCE, Global.Penetration.PROJECTILE_CALIBER_VARIANCE, Random.Range(0f, 1f)));
            if (appliedProjectileCaliber >= armorThickness) return true;
            else return false;
        }
    }
}