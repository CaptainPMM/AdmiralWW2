using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Net;

namespace UI.Menu.MultiplayerGames {
    public class HostPanelUI : MonoBehaviour {
        [SerializeField] private JoinPanelUI joinPanel = null;
        [SerializeField] private TMP_InputField nameInput = null;
        [SerializeField] private TMP_InputField portInput = null;
        [SerializeField] private TextMeshProUGUI infoTxt = null;
        [SerializeField] private Button hostBtn = null;

        public Button HostBtn => hostBtn;

        private Color initInfoTxtColor;

        private bool nameValid = false;
        private bool portValid = false;
        private bool hosting = false;
        private string token = "";

        public bool Hosting => hosting;

        private void Awake() {
            initInfoTxtColor = infoTxt.color;
        }

        private void Start() {
            nameInput.text = "";
            portInput.text = "";
            infoTxt.text = "";

            joinPanel.SetLockAllJoinButtons(false);

            nameInput.interactable = true;
            portInput.interactable = true;

            nameValid = false;
            portValid = false;
            hosting = false;

            hostBtn.interactable = false;

            nameInput.onValueChanged.RemoveAllListeners();
            nameInput.onValueChanged.AddListener((value) => {
                nameValid = value != "";
            });

            portInput.onValueChanged.RemoveAllListeners();
            portInput.onValueChanged.AddListener((value) => {
                if (value != "" && !value.StartsWith("-")) {
                    int intVal = int.Parse(value);
                    portValid = intVal >= 0 && intVal <= 65535;
                } else portValid = false;
            });

            hostBtn.onClick.RemoveAllListeners();
            hostBtn.onClick.AddListener(() => {
                HostGame();
            });
        }

        private void Update() {
            if (hosting && Input.GetKeyDown(KeyCode.Escape)) {
                // Cancel hosting
                StopAllCoroutines();
                Start();
                WebRequest.Get(this, Global.WebRequestURLs.START_OR_REMOVE_GAME, (req, res, error, errorMsg) => {
                    if (error) {
                        Error("Could not stop hosting:\n" + errorMsg);
                    } else {
                        Info("Stopped hosting");
                    }
                }, new WebRequest.GetParam[] {
                    new WebRequest.GetParam("token", token)
                });
                token = "";
            }
        }

        private void OnGUI() {
            if (!hosting) hostBtn.interactable = nameValid && portValid;
        }

        private void HostGame() {
            joinPanel.SetLockAllJoinButtons(true);
            hosting = true;
            token = "";
            hostBtn.interactable = false;
            nameInput.interactable = false;
            portInput.interactable = false;

            Info("Obtaining public IP...");
            PublicIP.Fetch(this, (ip) => {
                if (ip == null) {
                    Error("Could not obtain public IP address", true);
                } else {
                    Info("Connecting...");
                    WebRequest.Get(this, Global.WebRequestURLs.REGISTER_MPGAME, (req, res, error, errorMsg) => {
                        if (error) {
                            Error("Unlucky, there was an error:\n" + errorMsg, true);
                        } else {
                            ListenForPlayerJoin(res);
                        }
                    }, new WebRequest.GetParam[] {
                        new WebRequest.GetParam("name", nameInput.text),
                        new WebRequest.GetParam("hostIP", ip.ToString()),
                        new WebRequest.GetParam("hostPort", portInput.text)
                    });
                }
            });
        }

        private void ListenForPlayerJoin(string tokenJSON) {
            Info("Waiting for player to join...\n(Press ESC to cancel hosting)");
            try { token = JsonUtility.FromJson<WebRequest.WebToken>(tokenJSON).token; } catch { Error("Could not parse token", true); return; }

            WebRequest.Get(this, Global.WebRequestURLs.LISTEN_FOR_JOIN, (req, res, error, errorMsg) => {
                if (error) {
                    Error("Unlucky, there was an error:\n" + errorMsg, true);
                } else {
                    if (req.responseCode == 204) {
                        // No connected player timeout -> retry long polling
                        ListenForPlayerJoin(tokenJSON);
                    } else {
                        // Player joined, hooray!
                        // Try to connect to player/peer...
                        WebRequest.Peer peer;
                        try { peer = JsonUtility.FromJson<WebRequest.Peer>(res); } catch { Error("Could not parse peer infos", true); return; }
                        Info($"Player joined: {peer.peerIP}:{peer.peerPort}\nConnecting to player...");

                        // TEST
                        print("Connecting to player at " + peer.peerIP + ":" + peer.peerPort);
                        P2PManager.Inst.InitHost(int.Parse(portInput.text));
                        P2PManager.Inst.OnPeerConnect += (SuperNet.Transport.Peer p) => {
                            Debug.Log(p.Remote + " connected, juhu!");
                        };
                        P2PManager.Inst.Connect(peer.peerIP, peer.peerPort);
                    }
                }
            }, new WebRequest.GetParam[] {
                new WebRequest.GetParam("token", token)
            });
        }

        private void Info(string txt) {
            infoTxt.text = txt;
            infoTxt.color = initInfoTxtColor;
        }

        private void Error(string txt, bool resetHosting = false) {
            if (resetHosting) Start();
            infoTxt.text = txt;
            infoTxt.color = Color.red;
        }
    }
}