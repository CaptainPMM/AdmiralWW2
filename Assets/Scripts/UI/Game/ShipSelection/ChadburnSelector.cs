using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Inputs;
using Ships.ShipSystems;

namespace UI.Game.ShipSelection {
    public class ChadburnSelector : MonoBehaviour {
        [SerializeField] private ToggleGroup toggleGroup = null;
        [SerializeField] private List<ToggleChadburnSettingAssoc> toggleChadburnSettingAssocs = new List<ToggleChadburnSettingAssoc>();

        private void OnEnable() {
            if (InputManager.SelectedShip) {
                toggleGroup.GetFirstActiveToggle().isOn = false;
                GetToggleByChadburn(InputManager.SelectedShip.Autopilot.Chadburn).isOn = true;
            } else GameUI.Inst?.SetShipSelectionVisible(false);
        }

        private Toggle GetToggleByChadburn(Autopilot.ChadburnSetting chadburn) {
            return toggleChadburnSettingAssocs.Find(tcs => tcs.chadburnSetting == chadburn).toggle;
        }

        private Autopilot.ChadburnSetting GetChadburnByToggle(Toggle toggle) {
            return toggleChadburnSettingAssocs.Find(tcs => tcs.toggle == toggle).chadburnSetting;
        }

        public void OnToggleChange(bool isOn) {
            if (isOn) InputManager.SelectedShip.Autopilot.Chadburn = GetChadburnByToggle(toggleGroup.GetFirstActiveToggle());
        }

        [System.Serializable]
        public struct ToggleChadburnSettingAssoc {
            public Autopilot.ChadburnSetting chadburnSetting;
            public Toggle toggle;
        }
    }
}