using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using Net;

namespace UI.Menu.MultiplayerGames {
    public class JoinPanelUI : MonoBehaviour {
        [SerializeField] private GameObject gameListEntryPrefab = null;

        [SerializeField] private HostPanelUI hostPanel = null;
        [SerializeField] private TMP_InputField portInput = null;
        [SerializeField] private Button reloadGamesBtn = null;
        [SerializeField] private TextMeshProUGUI infoTxt = null;
        [SerializeField] private RectTransform scrollContent = null;

        private Color initInfoTxtColor;

        private bool portValid = false;

        private void Awake() {
            initInfoTxtColor = infoTxt.color;
        }

        private void Start() {
            portInput.text = "";
            reloadGamesBtn.interactable = false;
            infoTxt.text = "";
            ResetGamesList();

            portValid = false;

            portInput.onValueChanged.RemoveAllListeners();
            portInput.onValueChanged.AddListener((value) => {
                if (value != "" && !value.StartsWith("-")) {
                    int intVal = int.Parse(value);
                    portValid = intVal >= 0 && intVal <= 65535;
                } else portValid = false;
                reloadGamesBtn.interactable = portValid;
                ResetGamesList();
            });

            reloadGamesBtn.onClick.AddListener(() => {
                ReloadGames();
            });
        }

        private void ResetGamesList() {
            for (int i = 0; i < scrollContent.childCount; i++) Destroy(scrollContent.GetChild(i).gameObject);
            scrollContent.sizeDelta = new Vector2(scrollContent.sizeDelta.x, 0f);
        }

        private void ReloadGames() {
            ResetGamesList();
            Info("Loading multiplayer games list...");
            portInput.interactable = false;
            reloadGamesBtn.interactable = false;

            WebRequest.Get(this, Global.WebRequestURLs.FETCH_MP_GAMES_LIST, (req, res, error, errorMsg) => {
                portInput.interactable = true;
                reloadGamesBtn.interactable = true;
                if (error) {
                    Error("Unlucky, there was an error:\n" + errorMsg);
                } else {
                    WebRequest.MPGamesList mpGamesList;
                    try { mpGamesList = JsonUtility.FromJson<WebRequest.MPGamesList>(res); } catch { Error("Could not parse games list"); return; }
                    Info("Done, fetched " + mpGamesList.mpGames.Length + " games");
                    BuildMPGamesList(mpGamesList);
                }
            });
        }

        private void BuildMPGamesList(WebRequest.MPGamesList mpGamesList) {
            scrollContent.sizeDelta = new Vector2(scrollContent.sizeDelta.x, mpGamesList.mpGames.Length * (gameListEntryPrefab.GetComponent<RectTransform>().sizeDelta.y + scrollContent.GetComponent<VerticalLayoutGroup>().spacing));
            foreach (WebRequest.MPGame game in mpGamesList.mpGames) {
                GameObject go = Instantiate(gameListEntryPrefab, scrollContent);
                go.name = "MPGameEntry<" + game.name + ">";

                MPGameEntry ge = go.GetComponent<MPGameEntry>().Init(int.Parse(portInput.text), game.name);
                ge.JoinBtn.interactable = !hostPanel.Hosting;
                ge.JoinBtn.onClick.RemoveAllListeners();
                ge.JoinBtn.onClick.AddListener(() => {
                    SetLockAllButtons(true);

                    Info("Obtaining public IP...");
                    PublicIP.Fetch(this, (ip) => {
                        if (ip == null) {
                            SetLockAllButtons(false);
                            Error("Could not obtain public IP address");
                        } else {
                            Info("Joining the game...");
                            WebRequest.Get(this, Global.WebRequestURLs.JOIN_MPGAME, (req, res, error, errorMsg) => {
                                if (error) {
                                    SetLockAllButtons(false);
                                    Error("Could not join the game:\n" + errorMsg);
                                } else {
                                    WebRequest.MPGame mpGame;
                                    try { mpGame = JsonUtility.FromJson<WebRequest.MPGame>(res); } catch { Error("Could not parse game infos"); return; }
                                    Info("Connecting to host...");

                                    P2PManager.Inst.InitHost(ge.OwnPort);
                                    P2PManager.Inst.OnPeerConnect += (peer) => {
                                        Info("Connected. Starting game...");
                                        Global.State.playerTag = PlayerTag.Player1;
                                        SceneManager.LoadScene(Global.SceneNames.GAME_SCENE, LoadSceneMode.Single);
                                    };
                                    P2PManager.Inst.OnPeerException += (peer, exception) => {
                                        Error("Could not connect to host:\n" + exception.Message);
                                    };
                                    P2PManager.Inst.Connect(mpGame.hostIP, mpGame.hostPort);
                                }
                            }, new WebRequest.GetParam[] {
                                new WebRequest.GetParam("name", ge.GameName),
                                new WebRequest.GetParam("ownIP", ip.ToString()),
                                new WebRequest.GetParam("ownPort", ge.OwnPort.ToString())
                            });
                        }
                    });
                });
            }
        }

        public void SetLockAllJoinButtons(bool locked) {
            foreach (MPGameEntry e in scrollContent.GetComponentsInChildren<MPGameEntry>()) e.JoinBtn.interactable = !locked;
        }

        private void SetLockAllButtons(bool locked) {
            reloadGamesBtn.interactable = !locked;
            foreach (MPGameEntry e in scrollContent.GetComponentsInChildren<MPGameEntry>()) e.JoinBtn.interactable = !locked;
            hostPanel.HostBtn.interactable = !locked;
        }

        private void Info(string txt) {
            infoTxt.text = txt;
            infoTxt.color = initInfoTxtColor;
        }

        private void Error(string txt) {
            infoTxt.text = txt;
            infoTxt.color = Color.red;
        }
    }
}