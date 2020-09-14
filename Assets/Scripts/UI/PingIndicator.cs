using UnityEngine;
using TMPro;
using Net;

namespace UI {
    public class PingIndicator : MonoBehaviour {
        public static PingIndicator Inst { get; private set; }

        [SerializeField] private TextMeshProUGUI text = null;

        private void Awake() {
            if (Inst) {
                Destroy(transform.root.gameObject);
                return;
            } else {
                Inst = this;
                DontDestroyOnLoad(transform.root.gameObject);
            }

            text.text = "";
        }

        private void Start() {
            if (P2PManager.Inst) {
                P2PManager.Inst.OnPeerRTT += (peer, rtt) => {
                    text.text = rtt.ToString() + "ms";
                };
            }
        }
    }
}