public static class Global {
    public static class LayerNames {
        public const string UI = "UI";
        public const string SHIPS = "Ships";
        public const string PROJECTILES = "Projectiles";
    }
    public static class SceneNames {
        public const string GAME_SCENE = "GameScene";
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
        public const ushort SINKING_DESTROY_SHIP_DEPTH = 300;
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
    public static class WebRequestURLs {
        public const string REGISTER_MPGAME = "https://admiralww2-p2p-manager.herokuapp.com/registermpgame";
        public const string LISTEN_FOR_JOIN = "https://admiralww2-p2p-manager.herokuapp.com/listenforjoin";
        public const string START_OR_REMOVE_GAME = "https://admiralww2-p2p-manager.herokuapp.com/startorremovempgame";
        public const string FETCH_MP_GAMES_LIST = "https://admiralww2-p2p-manager.herokuapp.com/mpgameslist";
        public const string JOIN_MPGAME = "https://admiralww2-p2p-manager.herokuapp.com/joinmpgame";
    }

    public static class State {
        public static PlayerTag playerTag;
    }
}