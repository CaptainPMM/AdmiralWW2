using UnityEngine;
using Cam;

namespace UI {
    public class WorldSpaceUIScaler : MonoBehaviour {
        [SerializeField] private float distanceDivider = 80f;
        [SerializeField] private float maxScale = 120f;
        [SerializeField] private float distanceMoveUpFactor = 0.04f;

        private Transform camTransform;
        private float dist;
        private float scaleFactor;
        private float initHeight;

        private void Start() {
            camTransform = CamController.MainCam.transform;
            initHeight = transform.position.y;
        }

        private void Update() {
            dist = Vector3.Distance(transform.position, camTransform.position);
            scaleFactor = Mathf.Clamp(dist / distanceDivider, 1f, maxScale);
            transform.localScale = new Vector3(scaleFactor, scaleFactor, scaleFactor);
            transform.localPosition = new Vector3(transform.localPosition.x, initHeight + dist * distanceMoveUpFactor, transform.localPosition.z);
        }
    }
}