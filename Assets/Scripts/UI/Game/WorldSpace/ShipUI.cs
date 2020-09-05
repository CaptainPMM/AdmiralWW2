using UnityEngine;
using UnityEngine.UI;
using Ships;
using TMPro;

namespace UI.Game.WorldSpace {
    public class ShipUI : MonoBehaviour {
        [SerializeField] private Ship ship = null;
        [SerializeField] private TextMeshProUGUI shipNameTxt = null;
        [SerializeField] private Slider healthbar = null;
        [SerializeField] private TextMeshProUGUI healthTxt = null;

        private void Awake() {
            if (ship == null) Debug.LogWarning("ShipUI needs ship");
            if (shipNameTxt == null) Debug.LogWarning("ShipUI needs ship name text");
            if (healthbar == null) Debug.LogWarning("ShipUI needs healthbar");
            if (healthTxt == null) Debug.LogWarning("ShipUI needs health text");
        }

        private void Start() {
            shipNameTxt.text = ship.ShipName;
            healthbar.maxValue = ship.MaxHullHitpoints;
            healthbar.value = healthbar.maxValue;
            UpdateHealthTxt();
            ship.OnSinking += ShipSinkingHandler;
        }

        private void FixedUpdate() {
            healthbar.value = ship.HullHitpoints;
            UpdateHealthTxt();
        }

        private void UpdateHealthTxt() {
            healthTxt.text = ship.HullHitpoints + " / " + ship.MaxHullHitpoints;
        }

        private void ShipSinkingHandler(Ship ship) {
            ship.OnSinking -= ShipSinkingHandler;
            healthTxt.text = "Sinking";
            healthbar.fillRect.gameObject.SetActive(false);
            enabled = false;
        }
    }
}