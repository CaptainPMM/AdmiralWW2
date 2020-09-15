using UnityEngine;
using UnityEngine.EventSystems;
using Cam;
using Ships;
using UI.Game;
using Net;
using Net.MessageTypes;

namespace Inputs {
    public class InputManager : MonoBehaviour {
        public static InputManager Inst { get; private set; }

        [Header("Setup")]
        [SerializeField] private LayerMask shipsLayer = new LayerMask();

        [Header("Current state")]
        [SerializeField] private bool camSpeedModMultiplierActive = false;
        [SerializeField] private Ship selectedShip = null;

        public static Ship SelectedShip => Inst?.selectedShip;

        private Camera mainCam = null;

        private void Awake() {
            Inst = this;
        }

        private void Start() {
            mainCam = CamController.MainCam;
        }

        private void Update() {
            HandleCamInputs();
            HandleMouseInputs();
        }

        private void HandleCamInputs() {
            // Check for cam speed multiplier
            if (Input.GetKey(KeyCode.LeftShift)) camSpeedModMultiplierActive = true;
            else camSpeedModMultiplierActive = false;

            // Check cam movement
            if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow)) {
                CamController.Inst.MoveLeft(camSpeedModMultiplierActive);
            }
            if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow)) {
                CamController.Inst.MoveRight(camSpeedModMultiplierActive);
            }
            if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow)) {
                CamController.Inst.MoveForward(camSpeedModMultiplierActive);
            }
            if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow)) {
                CamController.Inst.MoveBackward(camSpeedModMultiplierActive);
            }

            // Check cam scrolling
            if (!MouseHoversUI()) {
                if (Input.mouseScrollDelta.y != 0f) {
                    if (Input.mouseScrollDelta.y > 0f) CamController.Inst.ScrollUp(camSpeedModMultiplierActive);
                    else CamController.Inst.ScrollDown(camSpeedModMultiplierActive);
                } else if (Input.mouseScrollDelta.x != 0 && camSpeedModMultiplierActive) {
                    if (Input.mouseScrollDelta.x > 0f) CamController.Inst.ScrollUp(camSpeedModMultiplierActive);
                    else CamController.Inst.ScrollDown(camSpeedModMultiplierActive);
                }
            }

            // Check cam rotating
            if (Input.GetMouseButton(2) || Input.GetKey(KeyCode.LeftAlt)) {
                CamController.Inst.RotateAxisX(-Input.GetAxis("Mouse Y"), camSpeedModMultiplierActive);
                CamController.Inst.RotateAxisY(Input.GetAxis("Mouse X"), camSpeedModMultiplierActive);
            }
        }

        private void HandleMouseInputs() {
            if (Input.GetMouseButtonDown(0) && !MouseHoversUI()) {
                Ray ray = mainCam.ScreenPointToRay(Input.mousePosition);
                RaycastHit hit;
                if (Physics.Raycast(ray, out hit, mainCam.farClipPlane, shipsLayer, QueryTriggerInteraction.Collide)) {
                    Ship ship = hit.collider.gameObject.GetComponentInParent<Ship>();
                    SelectShip(ship?.PlayerTag == GameManager.ThisPlayerTag ? ship : null);
                } else SelectShip(null);
            }
            if (SelectedShip && Input.GetMouseButtonDown(1) && !MouseHoversUI()) {
                Ray ray = mainCam.ScreenPointToRay(Input.mousePosition);
                RaycastHit hit;
                if (Physics.Raycast(ray, out hit, mainCam.farClipPlane, shipsLayer, QueryTriggerInteraction.Collide)) {
                    Ship ship = hit.collider.gameObject.GetComponentInParent<Ship>();
                    TargetShip(ship?.PlayerTag != GameManager.ThisPlayerTag ? ship : null);
                } else TargetShip(null);
            }
        }

        private bool MouseHoversUI() {
            return EventSystem.current.IsPointerOverGameObject();
        }

        private void SelectShip(Ship ship) {
            if (selectedShip != null) selectedShip.UI.SetSelected(false);
            selectedShip = ship;
            if (ship != null) {
                selectedShip.UI.SetSelected(true);
                GameUI.Inst.SetShipSelectionVisible(true);
            } else {
                GameUI.Inst.SetShipSelectionVisible(false);
            }
        }

        private void TargetShip(Ship ship) {
            if (ship == null) {
                P2PManager.Inst.Send(new MTShipTarget { PlayerTag = GameManager.ThisPlayerTag, ShipID = selectedShip.ID, HasTarget = false });
            } else {
                P2PManager.Inst.Send(new MTShipTarget { PlayerTag = GameManager.ThisPlayerTag, ShipID = selectedShip.ID, HasTarget = true, TargetShipID = ship.ID });
            }
            selectedShip.Targeting.Target = ship;
            GameUI.Inst.SetCurrShipTarget(ship); // Currently only ship targets are supported!
        }
    }
}