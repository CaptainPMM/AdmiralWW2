public static class Global {
    public static class LayerNames {
        public const string PROJECTILES = "Projectiles";
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
}