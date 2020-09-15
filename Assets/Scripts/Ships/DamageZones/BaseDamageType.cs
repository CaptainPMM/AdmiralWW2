using UnityEngine;

namespace Ships.DamageZones {
    public abstract class BaseDamageType {
        private const float SHELL_SHAPE_AND_ARMOR_QUALITY_K = 2400f;

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

        protected bool PenetrationCheck(Projectiles.Projectile projectile, ushort armorThickness, float angle) {
            float penetrationMM = ((projectile.Velocity.magnitude * Mathf.Sqrt(projectile.Mass)) / (SHELL_SHAPE_AND_ARMOR_QUALITY_K * Mathf.Sqrt((float)projectile.FromTurret.GunsCaliber / 100f))) * 100f;
            float varianceMM = Mathf.Lerp(-Global.Penetration.PENETRATION_VARIANCE_MM, Global.Penetration.PENETRATION_VARIANCE_MM, SafeRandom.Range(0f, 1f));
            float angleImpact = 1f - (Mathf.InverseLerp(0f, 90f, Mathf.Abs(90f - angle)) * Global.Penetration.MAX_PENETRATION_REDUCTION_FROM_ANGLE_PERCENTAGE);

            float appliedPenetrationMM = (penetrationMM + varianceMM) * angleImpact;

            if (appliedPenetrationMM > armorThickness) return true;
            else return false;
        }
    }
}