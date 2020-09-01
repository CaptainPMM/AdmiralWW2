namespace Ships.DamageZones {
    /// <summary>
    /// oparam[0] GameObject reference with GunTurret component
    /// </summary>
    public class DTGunTurret : BaseDamageType {
        public override DamageType Type => DamageType.GunTurret;

        public override void InflictDamage(DamageZone damageZone, Projectiles.Projectile projectile, DamageParams param = default) {
            throw new System.NotImplementedException();
        }
    }
}