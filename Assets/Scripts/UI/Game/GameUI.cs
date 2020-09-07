using UnityEngine;
using Inputs;
using Ships;

namespace UI.Game {
    public class GameUI : MonoBehaviour {
        public static GameUI Inst { get; private set; }

        [SerializeField] private GameObject shipSelection = null;

        private Ship currShipTarget = null;

        private void Awake() {
            Inst = this;
        }

        private void Start() {
            SetShipSelectionVisible(false);
        }

        public void SetShipSelectionVisible(bool visible) {
            shipSelection.SetActive(visible);
            SetCurrShipTarget(visible ? InputManager.SelectedShip.Targeting.TargetGO?.GetComponent<Ship>() : null); // Currently only ship targets are supported!
        }

        public void SetCurrShipTarget(Ship ship) {
            if (currShipTarget != null) currShipTarget.UI.SetSelected(false);
            currShipTarget = ship;
            if (currShipTarget != null) currShipTarget.UI.SetSelected(true);
        }
    }
}