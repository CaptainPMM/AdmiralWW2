using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Inputs;
using Net;
using Net.MessageTypes;

namespace UI.Game.ShipSelection {
    public class CourseSelector : MonoBehaviour, IScrollHandler {
        [SerializeField] private Image compassNeedleImg = null;
        [SerializeField] private Image compassShipNeedleImg = null;
        [SerializeField] private Image compassTargetIndicatorImg = null;

        private void OnEnable() {
            if (InputManager.SelectedShip) {
                compassNeedleImg.rectTransform.rotation = Quaternion.Euler(0f, 0f, 360 - InputManager.SelectedShip.Autopilot.Course);
                compassShipNeedleImg.rectTransform.rotation = Quaternion.Euler(0f, 0f, 360 - InputManager.SelectedShip.Course);
                UpdateTargetIndicator();
            } else GameUI.Inst?.SetShipSelectionVisible(false);
        }

        private void FixedUpdate() {
            compassShipNeedleImg.rectTransform.rotation = Quaternion.Euler(0f, 0f, 360 - InputManager.SelectedShip.Course);
            UpdateTargetIndicator();
        }

        private void UpdateTargetIndicator() {
            if (InputManager.SelectedShip.Targeting.Target != null && !InputManager.SelectedShip.Targeting.Target.Equals(null)) {
                compassTargetIndicatorImg.rectTransform.rotation = Quaternion.Euler(0f, 0f, compassShipNeedleImg.rectTransform.localRotation.eulerAngles.z - Vector3.SignedAngle(InputManager.SelectedShip.transform.forward, (InputManager.SelectedShip.Targeting.TargetGO.transform.position - InputManager.SelectedShip.transform.position).normalized, Vector3.up));
                compassTargetIndicatorImg.gameObject.SetActive(true);
            } else compassTargetIndicatorImg.gameObject.SetActive(false);
        }

        public void OnScroll(PointerEventData eventData) {
            compassNeedleImg.rectTransform.Rotate(0f, 0f, -eventData.scrollDelta.y, Space.Self);
            ushort course = (ushort)(360 - Mathf.RoundToInt(compassNeedleImg.rectTransform.localRotation.eulerAngles.z));
            if (P2PManager.IsMPActive()) P2PManager.Inst.Send(new MTShipCourse { PlayerTag = GameManager.ThisPlayerTag, ShipID = InputManager.SelectedShip.ID, Course = course });
            InputManager.SelectedShip.Autopilot.Course = course;
            eventData.Use();
        }
    }
}