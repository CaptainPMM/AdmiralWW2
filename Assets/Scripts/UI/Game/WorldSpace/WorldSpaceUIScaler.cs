using UnityEngine;
using Cam;

namespace UI.Game.WorldSpace {
    public class WorldSpaceUIScaler : MonoBehaviour {
        [SerializeField] private float distanceDivider = 80f;
        [SerializeField] private float maxScale = 120f;
        [SerializeField] private float distanceMoveUpFactor = 0.04f;

        private Transform camTransform;
        private float dist;
        private Vector3 initScale;
        private float scaleFactor;
        private float initHeight;

        private void Start() {
            camTransform = CamController.MainCam.transform;
            initScale = transform.localScale;
            initHeight = transform.position.y;
        }

        private void Update() {
            dist = Vector3.Distance(transform.position, camTransform.position);
            scaleFactor = Mathf.Clamp(dist / distanceDivider, 1f, maxScale);
            transform.localScale = initScale * scaleFactor;
            transform.localPosition = new Vector3(transform.localPosition.x, initHeight + dist * distanceMoveUpFactor, transform.localPosition.z);
        }
    }
}