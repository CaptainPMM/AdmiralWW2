namespace Ships.DamageZones {
    /// <summary>
    /// No params
    /// </summary>
    public class DTPropeller : BaseDamageType {
        public override DamageType Type => DamageType.Propeller;

        public override void InflictDamage(DamageZone damageZone, Projectiles.Projectile projectile, DamageParams param = default) {
            throw new System.NotImplementedException();
        }
    }
}