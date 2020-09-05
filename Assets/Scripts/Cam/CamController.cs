using UnityEngine;

namespace Cam {
    public class CamController : MonoBehaviour {
        public static CamController Inst { get; private set; }

        [SerializeField] private Camera mainCam = null;
        [SerializeField] private Transform camRigTransform = null;

        public float movementSpeed = 5f;
        public float scrollSpeed = 6f;
        public float rotateSpeed = 360f;
        public float speedModMultiplier = 2f;
        public float heightModMultiplier = 0.5f;
        public float inertia = 20f;

        //public Vector2 xMovementBounds;
        //public Vector2 zMovementBounds;

        public Vector2 scrollHeightBounds;

        private Vector3 newPos;
        private float newScrollHeight;
        private Vector3 newRotation;

        private void Awake() {
            Inst = this;
            if (mainCam == null) Debug.LogWarning("CamController has no camera");
            if (camRigTransform == null) Debug.LogWarning("CamController has no cam rig transform");
        }

        private void Start() {
            camRigTransform.Translate(0f, Ocean.OceanManager.OceanSurfaceYPos, 0f, Space.World);

            newPos = camRigTransform.position;
            newScrollHeight = mainCam.transform.localPosition.y;
            newRotation = mainCam.transform.localRotation.eulerAngles;
        }

        private void Update() {
            camRigTransform.position = Vector3.Lerp(camRigTransform.position, newPos, Time.deltaTime * inertia);
            mainCam.transform.localPosition = Vector3.Lerp(mainCam.transform.localPosition, new Vector3(0f, newScrollHeight, 0f), Time.deltaTime * inertia);
            mainCam.transform.localRotation = Quaternion.Lerp(mainCam.transform.localRotation, Quaternion.Euler(newRotation), Time.deltaTime * inertia);
        }

        public void MoveLeft(bool mod = false) {
            newPos += -camRigTransform.right * movementSpeed * SpeedMod(mod) * HeightMod() * Time.deltaTime;
            //ClampPos();
        }

        public void MoveRight(bool mod = false) {
            newPos += camRigTransform.right * movementSpeed * SpeedMod(mod) * HeightMod() * Time.deltaTime;
            //ClampPos();
        }

        public void MoveForward(bool mod = false) {
            newPos += camRigTransform.forward * movementSpeed * SpeedMod(mod) * HeightMod() * Time.deltaTime;
            //ClampPos();
        }

        public void MoveBackward(bool mod = false) {
            newPos += -camRigTransform.forward * movementSpeed * SpeedMod(mod) * HeightMod() * Time.deltaTime;
            //ClampPos();
        }

        public void ScrollDown(bool mod = false) {
            newScrollHeight += -scrollSpeed * SpeedMod(mod) * HeightMod() * Time.deltaTime;
            ClampHeight();
        }

        public void ScrollUp(bool mod = false) {
            newScrollHeight += scrollSpeed * SpeedMod(mod) * HeightMod() * Time.deltaTime;
            ClampHeight();
        }

        public void RotateAxisX(float value, bool mod = false) {
            newRotation += Vector3.right * value * rotateSpeed * SpeedMod(mod) * Time.deltaTime;
            newRotation.x = newRotation.x % 360f;
        }

        public void RotateAxisY(float value, bool mod = false) {
            newRotation += Vector3.up * value * rotateSpeed * SpeedMod(mod) * Time.deltaTime;
            newRotation.y = newRotation.y % 360f;
        }

        private float SpeedMod(bool mod) {
            if (mod) return speedModMultiplier;
            else return 1f;
        }

        private float HeightMod() {
            return mainCam.transform.position.y * heightModMultiplier;
        }

        // private void ClampPos() {
        //     newPos.x = Mathf.Clamp(newPos.x, xMovementBounds.x, xMovementBounds.y);
        //     newPos.z = Mathf.Clamp(newPos.z, zMovementBounds.y, zMovementBounds.x); // x and y inverted because the grid goes from top to bottom
        // }

        private void ClampHeight() {
            newScrollHeight = Mathf.Clamp(newScrollHeight, scrollHeightBounds.x, scrollHeightBounds.y);
        }
    }
}