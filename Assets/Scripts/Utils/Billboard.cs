using UnityEngine;
using Cam;

namespace Utils {
    public class Billboard : MonoBehaviour {
        private Transform camTransform;

        private void Start() {
            camTransform = CamController.MainCam.transform;
        }

        private void Update() {
            transform.rotation = camTransform.rotation;
        }
    }
}