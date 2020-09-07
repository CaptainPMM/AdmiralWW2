using UnityEngine;
using UnityEngine.UI;
using Inputs;
using Ships;
using TMPro;

namespace UI.Game.ShipSelection {
    public class WaterIngressIndicator : MonoBehaviour {
        [SerializeField] private GameObject waterIngressIndicatorPrefab = null;
        [SerializeField] private Transform indicatorListContainer = null;

        private void OnEnable() {
            if (InputManager.SelectedShip) {
                for (int i = 0; i < indicatorListContainer.childCount; i++) {
                    Destroy(indicatorListContainer.GetChild(i).gameObject);
                }
                InputManager.SelectedShip.WaterIngressSections.ForEach(wi => {
                    GameObject go = Instantiate(waterIngressIndicatorPrefab, indicatorListContainer);
                    go.name = "WaterIngressIndicator" + wi.sectionID;
                });
            } else GameUI.Inst?.SetShipSelectionVisible(false);
        }

        private void FixedUpdate() {
            UpdateWaterIngressStatus();
        }

        private void UpdateWaterIngressStatus() {
            for (int i = 0; i < indicatorListContainer.childCount; i++) {
                Transform currIndicator = indicatorListContainer.GetChild(i);
                Ship.WaterIngressSection currSection = InputManager.SelectedShip.WaterIngressSections[i];
                currIndicator.GetComponent<Slider>().value = currSection.waterLevel;
                currIndicator.GetComponentInChildren<TextMeshProUGUI>().text = currSection.numHoles.ToString();
            }
        }
    }
}