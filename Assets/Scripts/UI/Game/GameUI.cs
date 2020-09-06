using UnityEngine;

namespace UI.Game {
    public class GameUI : MonoBehaviour {
        public static GameUI Inst { get; private set; }

        [SerializeField] private GameObject shipSelection = null;

        private void Awake() {
            Inst = this;
        }

        private void Start() {
            SetShipSelectionVisible(false);
        }

        public void SetShipSelectionVisible(bool visible) {
            shipSelection.SetActive(visible);
        }
    }
}