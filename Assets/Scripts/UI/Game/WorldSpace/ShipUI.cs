using UnityEngine;
using UnityEngine.UI;
using Ships;
using TMPro;

namespace UI.Game.WorldSpace {
    public class ShipUI : MonoBehaviour {
        [SerializeField] private Ship ship = null;
        [SerializeField] private Image panel = null;
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

            Color playerColor = GameManager.GetPlayer(ship.PlayerTag).Color;
            panel.color = new Color(playerColor.r, playerColor.g, playerColor.b, panel.color.a);
            healthbar.fillRect.GetComponent<Image>().color = playerColor;

            ship.OnSinking += ShipSinkingHandler;
        }

        private void FixedUpdate() {
            healthbar.value = ship.HullHitpoints;
            UpdateHealthTxt();
        }

        private void UpdateHealthTxt() {
            healthTxt.text = (int)ship.HullHitpoints + " / " + ship.MaxHullHitpoints;
        }

        private void ShipSinkingHandler(Ship ship) {
            ship.OnSinking -= ShipSinkingHandler;
            healthTxt.text = "Sinking";
            healthbar.fillRect.gameObject.SetActive(false);
            enabled = false;
        }

        public void SetSelected(bool selected) {
            if (selected) {
                Color color = GameManager.GetPlayer(ship.PlayerTag).Color;
                color.r = 1f - color.r;
                color.g = 1f - color.g;
                color.b = 1f - color.b;
                shipNameTxt.color = color;
            } else shipNameTxt.color = Color.white;
        }
    }
}