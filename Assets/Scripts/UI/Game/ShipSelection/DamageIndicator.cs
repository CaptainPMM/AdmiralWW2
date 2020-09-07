using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Ships.DamageZones;
using Inputs;

namespace UI.Game.ShipSelection {
    public class DamageIndicator : MonoBehaviour {
        private const float DAMAGE_ICON_INACTIVE_ALPHA = 0.1f;

        [Header("Setup")]
        [SerializeField] private GameObject damageIconPrefab = null;
        [SerializeField] private List<DamageTypeToSprite> damageTypeToSprites = new List<DamageTypeToSprite>();
        [SerializeField] private Transform damageListContainer = null;

        [Header("Current state")]
        [SerializeField] private List<DamageImages> damageImages = new List<DamageImages>();

        private void OnEnable() {
            if (InputManager.SelectedShip) {
                for (int i = 0; i < damageListContainer.childCount; i++) {
                    Destroy(damageListContainer.GetChild(i).gameObject);
                }
                damageImages.Clear();
                InputManager.SelectedShip.DamageZones.ForEach(dz => {
                    dz.DamageTypes.ForEach(dt => {
                        DamageTypeToSprite damageTypeToSprite = damageTypeToSprites.Find(dts => dts.damageType == dt.damageType);
                        if (damageTypeToSprite.sprite != null) {
                            GameObject go = Instantiate(damageIconPrefab, damageListContainer);
                            go.name = "DamageIcon" + damageTypeToSprite.damageType;
                            Image img = go.GetComponent<Image>();
                            img.sprite = damageTypeToSprite.sprite;
                            img.color = new Color(1, 1, 1, DAMAGE_ICON_INACTIVE_ALPHA);

                            damageImages.Add(new DamageImages() {
                                damageZone = dz,
                                damageType = dt.damageType,
                                image = img
                            });
                        }
                    });
                });
                UpdateDamageStatus();
            } else GameUI.Inst?.SetShipSelectionVisible(false);
        }

        private void FixedUpdate() {
            UpdateDamageStatus();
        }

        private void UpdateDamageStatus() {
            InputManager.SelectedShip.DamageZones.ForEach(dz => {
                dz.Damages.ForEach(d => {
                    Image damageImage = damageImages.Find(di => di.damageZone == dz && di.damageType == d).image;
                    if (damageImage != null) damageImage.color = new Color(1, 1, 1, 1);
                });
            });
        }

        [System.Serializable]
        public struct DamageTypeToSprite {
            public DamageType damageType;
            public Sprite sprite;
        }

        [System.Serializable]
        public struct DamageImages {
            public DamageZone damageZone;
            public DamageType damageType;
            public Image image;
        }
    }
}