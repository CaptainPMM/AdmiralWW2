using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using TMPro;
using Net;

namespace UI {
    public class NetInfoPanel : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler {
        public static NetInfoPanel Inst { get; private set; }

        [SerializeField] private TextMeshProUGUI text = null;
        [SerializeField] private TextMeshProUGUI infosCountTxt = null;
        [SerializeField, Range(1f, 10f)] private float showDuration = 5f;

        private bool showing = false;
        private bool pointerHover = false;
        private Queue<(string text, float duration)> messageQ = new Queue<(string text, float duration)>();

        private void Awake() {
            if (Inst) Destroy(transform.root.gameObject);
            else {
                Inst = this;
                DontDestroyOnLoad(transform.root.gameObject);
            }
        }

        private void Start() {
            text.text = "";
            infosCountTxt.text = "";

            P2PManager.Inst.OnPeerDisconnect += (peer, message, reason, exception) => {
                Show("Disconnect: " + reason.ToString(), showDuration);
            };

            P2PManager.Inst.OnPeerException += (peer, exception) => {
                Show("Error: " + exception.Message, showDuration);
            };

            gameObject.SetActive(false);
        }

        public IEnumerator ShowRoutine(string text, float duration) {
            showing = true;
            yield return new WaitForSecondsRealtime(duration);
            yield return new WaitUntil(() => pointerHover == false);
            try {
                var nextMessage = messageQ.Dequeue();
                StartCoroutine(ShowRoutine(nextMessage.text, nextMessage.duration));
            } catch {
                showing = false;
                gameObject.SetActive(false);
            } finally {
                UpdateInfosCountTxt();
            }
        }

        public void Show(string text, float duration) {
            if (showing) messageQ.Enqueue((text, duration));
            else {
                pointerHover = false;
                Inst.text.text = text;
                gameObject.SetActive(true);
                StartCoroutine(ShowRoutine(text, duration));
            }
            UpdateInfosCountTxt();
        }

        private void UpdateInfosCountTxt() {
            if (messageQ.Count > 0) infosCountTxt.text = messageQ.Count.ToString();
            else infosCountTxt.text = "";
        }

        public void Hide() {
            StopAllCoroutines();
            messageQ.Clear();
            pointerHover = false;
            gameObject.SetActive(false);
        }

        public void OnPointerEnter(PointerEventData eventData) {
            pointerHover = true;
        }

        public void OnPointerExit(PointerEventData eventData) {
            pointerHover = false;
        }

#if UNITY_EDITOR
        [ContextMenu("Test")]
        private void Test() {
            Show("Test", showDuration);
        }
#endif
    }
}