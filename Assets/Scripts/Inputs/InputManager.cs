using UnityEngine;
using Cam;

namespace Inputs {
    public class InputManager : MonoBehaviour {
        public static InputManager Inst { get; private set; }

        [Header("Current state")]
        [SerializeField] private bool camSpeedModMultiplierActive = false;

        private void Awake() {
            Inst = this;
        }

        private void Update() {
            HandleCamInputs();
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
            if (Input.mouseScrollDelta.y != 0f) {
                if (Input.mouseScrollDelta.y > 0f) CamController.Inst.ScrollUp(camSpeedModMultiplierActive);
                else CamController.Inst.ScrollDown(camSpeedModMultiplierActive);
            } else if (Input.mouseScrollDelta.x != 0 && camSpeedModMultiplierActive) {
                if (Input.mouseScrollDelta.x > 0f) CamController.Inst.ScrollUp(camSpeedModMultiplierActive);
                else CamController.Inst.ScrollDown(camSpeedModMultiplierActive);
            }

            // Check cam rotating
            if (Input.GetMouseButton(2) || Input.GetKey(KeyCode.LeftAlt)) {
                CamController.Inst.RotateAxisX(-Input.GetAxis("Mouse Y"), camSpeedModMultiplierActive);
                CamController.Inst.RotateAxisY(Input.GetAxis("Mouse X"), camSpeedModMultiplierActive);
            }
        }
    }
}