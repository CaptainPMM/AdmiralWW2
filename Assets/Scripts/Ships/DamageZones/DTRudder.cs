namespace Ships.DamageZones {
    /// <summary>
    /// No params
    /// </summary>
    public class DTRudder : BaseDamageType {
        public override DamageType Type => DamageType.Rudder;

        public override void InflictDamage(DamageZone damageZone, Projectiles.Projectile projectile, DamageParams param = default) {
            throw new System.NotImplementedException();
        }
    }
}