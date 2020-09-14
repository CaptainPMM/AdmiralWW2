using System.Collections;
using System.Net;
using UnityEngine;

namespace Net {
    public static class PublicIP {
        private static readonly string[] PUBLIC_IP_SERVICES = new string[] {
            "https://icanhazip.com/",
            "https://bot.whatismyipaddress.com/"
        };

        public static Coroutine Fetch(MonoBehaviour origin, FetchIPComplete onComplete) {
            return origin.StartCoroutine(FetchRoutine(origin, onComplete));
        }

        private static IEnumerator FetchRoutine(MonoBehaviour origin, FetchIPComplete onComplete) {
            IPAddress resIP = null;
            foreach (string provider in PUBLIC_IP_SERVICES) {
                yield return WebRequest.Get(origin, provider, (req, res, error, errorMsg) => {
                    res = res.Trim();
                    if (res != "" && IPAddress.TryParse(res, out IPAddress ip)) {
                        resIP = ip;
                    }
                });
                if (resIP != null) break;
            }
            onComplete(resIP);
        }

        public delegate void FetchIPComplete(IPAddress publicIP);
    }
}