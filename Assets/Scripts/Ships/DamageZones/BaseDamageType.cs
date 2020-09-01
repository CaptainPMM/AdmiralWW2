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
    }
}