using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace UI.Menu.MultiplayerGames {
    public class MPGameEntry : MonoBehaviour {
        [Header("Setup")]
        [SerializeField] private TextMeshProUGUI gameNameTxt = null;
        [SerializeField] private Button joinBtn = null;

        public Button JoinBtn => joinBtn;

        [Header("Current state")]
        [SerializeField] private int ownPort;
        [SerializeField] private string gameName;

        public int OwnPort => ownPort;
        public string GameName => gameName;

        public MPGameEntry Init(int port, string gameName) {
            ownPort = port;
            this.gameName = gameName;
            return this;
        }

        private void Start() {
            gameNameTxt.text = gameName;
        }
    }
}