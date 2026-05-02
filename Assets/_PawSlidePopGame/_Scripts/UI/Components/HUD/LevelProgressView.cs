using System.Collections.Generic;
using _PawSlidePopGame._Scripts.Data.Events.Payloads;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace _PawSlidePopGame._Scripts.UI.Components.HUD
{
    public class LevelProgressView : MonoBehaviour
    {
        [SerializeField] private TMP_Text levelText;
        [SerializeField] private Slider progressSlider;
        [SerializeField] private List<Image> starImages = new List<Image>();
        [SerializeField] private Color lockedStarColor = new Color(1f, 1f, 1f, 0.55f);
        [SerializeField] private Color unlockedStarColor = Color.white;

        private void OnValidate()
        {
            if (levelText == null)
            {
                levelText = GetComponentInChildren<TMP_Text>(true);
            }

            if (progressSlider == null)
            {
                progressSlider = GetComponentInChildren<Slider>(true);
            }

            if (starImages.Count == 0)
            {
                Image[] images = GetComponentsInChildren<Image>(true);
                for (int i = 0; i < images.Length; i++)
                {
                    if (images[i] != null && images[i].name.Contains("Star"))
                    {
                        starImages.Add(images[i]);
                    }
                }
            }
        }

        public void SetData(GameplayHudSnapshot snapshot)
        {
            if (snapshot == null)
            {
                return;
            }

            if (levelText != null)
            {
                levelText.text = $"Level {snapshot.levelNumber}";
            }

            if (progressSlider != null)
            {
                int maxThreshold = 1;
                if (snapshot.starScoreThresholds != null && snapshot.starScoreThresholds.Length > 0)
                {
                    maxThreshold = Mathf.Max(1, snapshot.starScoreThresholds[snapshot.starScoreThresholds.Length - 1]);
                }

                progressSlider.minValue = 0f;
                progressSlider.maxValue = maxThreshold;
                progressSlider.SetValueWithoutNotify(Mathf.Clamp(snapshot.currentScore, 0, maxThreshold));
            }

            RefreshStars(snapshot.reachedStars);
        }

        public void PlayStarReachedFx(StarReachedPayload payload)
        {
            int starListIndex = payload.StarIndex - 1;
            if (starListIndex < 0 || starListIndex >= starImages.Count || starImages[starListIndex] == null)
            {
                return;
            }

            Image star = starImages[starListIndex];
            star.DOKill();
            star.color = unlockedStarColor;
            star.transform.localScale = Vector3.one;
            star.transform.DOPunchScale(Vector3.one * 0.2f, 0.25f, 5)
                .SetLink(star.gameObject, LinkBehaviour.KillOnDisable);
        }

        private void RefreshStars(int reachedStars)
        {
            for (int i = 0; i < starImages.Count; i++)
            {
                if (starImages[i] == null)
                {
                    continue;
                }

                starImages[i].color = i < reachedStars ? unlockedStarColor : lockedStarColor;
            }
        }
    }
}
