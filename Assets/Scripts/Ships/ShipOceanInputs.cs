using UnityEngine;

namespace Ships {
    public class ShipOceanInputs : MonoBehaviour {
        public void SetEnabled(bool enabled) {
            gameObject.SetActive(enabled);
        }
    }
}