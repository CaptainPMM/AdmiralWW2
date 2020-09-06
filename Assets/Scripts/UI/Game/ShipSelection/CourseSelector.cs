using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Inputs;

namespace UI.Game.ShipSelection {
    public class CourseSelector : MonoBehaviour, IScrollHandler {
        [SerializeField] private Image compassNeedleImg = null;
        [SerializeField] private Image compassShipNeedleImg = null;

        private void OnEnable() {
            if (InputManager.SelectedShip) {
                compassNeedleImg.rectTransform.rotation = Quaternion.Euler(0f, 0f, 360 - InputManager.SelectedShip.Autopilot.Course);
                compassShipNeedleImg.rectTransform.rotation = Quaternion.Euler(0f, 0f, 360 - InputManager.SelectedShip.Course);
            } else GameUI.Inst.SetShipSelectionVisible(false);
        }

        private void FixedUpdate() {
            compassShipNeedleImg.rectTransform.rotation = Quaternion.Euler(0f, 0f, 360 - InputManager.SelectedShip.Course);
        }

        public void OnScroll(PointerEventData eventData) {
            compassNeedleImg.rectTransform.Rotate(0f, 0f, -eventData.scrollDelta.y, Space.Self);
            InputManager.SelectedShip.Autopilot.Course = (ushort)(360 - Mathf.RoundToInt(compassNeedleImg.rectTransform.localRotation.eulerAngles.z));
            eventData.Use();
        }
    }
}