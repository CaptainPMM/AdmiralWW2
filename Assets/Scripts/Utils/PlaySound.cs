using UnityEngine;

namespace Utils {
    [RequireComponent(typeof(AudioSource))]
    public class PlaySound : MonoBehaviour {
        public AudioClip clip;
        public bool loop;

        private void Start() {
            if (clip) {
                AudioSource source = GetComponent<AudioSource>();
                if (source) {
                    source.clip = clip;
                    source.loop = loop;
                    source.Play();
                }
            }
        }
    }
}