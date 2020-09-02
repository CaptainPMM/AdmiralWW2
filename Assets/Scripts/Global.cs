public static class Global {
    public static class LayerNames {
        public const string PROJECTILES = "Projectiles";
    }
    public static class Ships {
        public const float WATER_INGRESS_STRENGTH = 0.00004f;
        public const float WATER_INGRESS_HULL_DAMAGE_FACTOR = 0.25f;
        /// <summary>
        /// 0 = immediatley set force factor of floating physics to 0,
        /// 1 = no force factor change,
        /// >1 = force factor even increases,
        /// 0.5 = force factor is halfed each fixed update
        /// </summary>
        public const float SINKING_FORCE_FACTOR_REDUCTION_FACTOR = 0.999f;
    }
    public static class Penetration {
        /// <summary>
        /// 0f = min/off (no reduction),
        /// 1f = max/on (can reduce pen to 0mm when angle 0°),
        /// if the angle is 90° no reduction should be applied
        /// </summary>
        public const float MAX_PENETRATION_REDUCTION_FROM_ANGLE_PERCENTAGE = 0.5f;
        public const float PENETRATION_VARIANCE_MM = 20;
    }
    public static class Effects {
        public const float PROJECTILE_PEN_EFFECT_SIZEMOD_REFERENCE_CALIBER = 200f;
    }
}