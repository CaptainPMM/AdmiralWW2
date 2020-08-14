using System.Collections.Generic;
using UnityEngine;

namespace Ships.ShipSystems {
    public class Autopilot : MonoBehaviour {
        private readonly Dictionary<ChadburnSetting, float> CHADBURN_TO_THROTTLE = new Dictionary<ChadburnSetting, float>() {
            { ChadburnSetting.Stop, 0f },
            { ChadburnSetting.DeadSlowAhead, 0.1f },
            { ChadburnSetting.SlowAhead, 0.25f },
            { ChadburnSetting.HalfAhead, 0.5f },
            { ChadburnSetting.FullAhead, 1f }
        };

        [Header("Setup")]
        [SerializeField] private Ship ship = null;

        [Header("AP Settings")]
        [SerializeField] private bool engaged = true;
        [SerializeField, Range(0, 359)] private ushort course = 0;
        [SerializeField] private ChadburnSetting chadburn = ChadburnSetting.Stop;

        public bool Engaged { get => engaged; set => engaged = value; }
        public ushort Course { get => course; set => course = (ushort)Mathf.Clamp(value, 0, 359); }
        public ChadburnSetting Chadburn { get => chadburn; set { chadburn = value; ApplyChadburnSetting(); } }

        private void FixedUpdate() {
            if (engaged) {
                Steer();
            }
        }

        private void Steer() {
            ship.RudderAngle = Mathf.DeltaAngle(ship.Course, course);
        }

        private void ApplyChadburnSetting() {
            ship.Propulsion.Throttle = CHADBURN_TO_THROTTLE[chadburn];
        }

        public enum ChadburnSetting : byte {
            Stop,
            DeadSlowAhead,
            SlowAhead,
            HalfAhead,
            FullAhead
        }
    }
}