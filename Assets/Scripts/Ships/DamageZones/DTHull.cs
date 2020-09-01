namespace Ships.DamageZones {
    /// <summary>
    /// No params
    /// </summary>
    public class DTHull : BaseDamageType {
        public override DamageType Type => DamageType.Hull;

        public override void InflictDamage(DamageZone damageZone, Projectiles.Projectile projectile, DamageParams param = default) {
            throw new System.NotImplementedException();
        }
    }
}