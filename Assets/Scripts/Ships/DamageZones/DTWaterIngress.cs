namespace Ships.DamageZones {
    /// <summary>
    /// fparam[0] number of affected float points of the ship
    /// fparam[0 + ...fparam[0]] affected float point indices
    /// </summary>
    public class DTWaterIngress : BaseDamageType {
        public override DamageType Type => DamageType.WaterIngress;

        public override void InflictDamage(DamageZone damageZone, Projectiles.Projectile projectile, DamageParams param = default) {
            throw new System.NotImplementedException();
        }
    }
}