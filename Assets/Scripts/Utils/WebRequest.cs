using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

namespace Utils {
    public static class WebRequest {
        public static Coroutine Get(MonoBehaviour origin, string url, WebRequestCompleted onComplete, GetParam[] param = null) {
            // Add optional parameters to url
            if (param != null && param.Length > 0) {
                url += "?";
                foreach (GetParam p in param) url += $"{p.key}={p.value}&";
                url.Remove(url.Length - 1);
            }

            return origin.StartCoroutine(GetRoutine(url, onComplete));
        }

        private static IEnumerator GetRoutine(string url, WebRequestCompleted onComplete) {
            using (UnityWebRequest webRequest = UnityWebRequest.Get(url)) {
                webRequest.timeout = 30;
                yield return webRequest.SendWebRequest();

                if (webRequest.isNetworkError) {
                    onComplete.Invoke(webRequest, null, true, webRequest.error);
                } else if (webRequest.responseCode >= 400) {
                    try {
                        WebError err = JsonUtility.FromJson<WebError>(webRequest.downloadHandler.text);
                        onComplete.Invoke(webRequest, null, true, $"ERROR {err.status}: {err.type}\n{err.msg}");
                    } catch {
                        onComplete.Invoke(webRequest, null, true, "ERROR " + webRequest.error);
                    }
                } else {
                    onComplete.Invoke(webRequest, webRequest.downloadHandler.text, false, null);
                }
            }
        }

        public delegate void WebRequestCompleted(UnityWebRequest req, string res, bool error, string errorMsg);

        [System.Serializable]
        public struct GetParam {
            public string key;
            public string value;

            public GetParam(string key, string value) {
                this.key = key;
                this.value = value;
            }
        }

        [System.Serializable]
        public class WebToken {
            public string token;
        }

        [System.Serializable]
        public class Peer {
            public string peerIP;
            public int peerPort;
        }

        [System.Serializable]
        private class WebError {
            public int status = -1;
            public string type = "<empty>";
            public string msg = "<empty>";
        }
    }
}