using _PawSlidePopGame._Scripts.Data.Events.Payloads;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace _PawSlidePopGame._Scripts.UI.Components.HUD
{
    public class TargetItemView : MonoBehaviour
    {
        [SerializeField] private Image iconImage;
        [SerializeField] private Image shadowImage;
        [SerializeField] private Image backgroundImage;
        [SerializeField] private TMP_Text currentCountText;
        [SerializeField] private TMP_Text targetCountText;
        [SerializeField] private Color defaultBackgroundColor = Color.white;
        [SerializeField] private Color completedBackgroundColor = new Color(0.72f, 0.94f, 0.61f, 1f);

        public int BoundTileId { get; private set; }

        private void OnValidate()
        {
            if (iconImage == null)
            {
                Transform iconTransform = transform.Find("Animal/Animal");
                if (iconTransform != null)
                {
                    iconImage = iconTransform.GetComponent<Image>();
                }
            }

            if (shadowImage == null)
            {
                Transform shadowTransform = transform.Find("Animal/Shadow");
                if (shadowTransform != null)
                {
                    shadowImage = shadowTransform.GetComponent<Image>();
                }
            }

            if (backgroundImage == null)
            {
                Transform backgroundTransform = transform.Find("Background");
                if (backgroundTransform != null)
                {
                    backgroundImage = backgroundTransform.GetComponent<Image>();
                }
            }

            if (currentCountText == null)
            {
                Transform currentTextTransform = transform.Find("Text/txtCurr");
                if (currentTextTransform != null)
                {
                    currentCountText = currentTextTransform.GetComponent<TMP_Text>();
                }
            }

            if (targetCountText == null)
            {
                Transform targetTextTransform = transform.Find("Text/txtTarget");
                if (targetTextTransform != null)
                {
                    targetCountText = targetTextTransform.GetComponent<TMP_Text>();
                }
            }

            if (backgroundImage != null)
            {
                defaultBackgroundColor = backgroundImage.color;
            }
        }

        public void SetData(TargetProgressData data)
        {
            if (data == null)
            {
                gameObject.SetActive(false);
                BoundTileId = 0;
                return;
            }

            gameObject.SetActive(true);
            BoundTileId = data.tileId;

            if (iconImage != null && data.icon != null)
            {
                iconImage.sprite = data.icon;
            }

            if (shadowImage != null)
            {
                shadowImage.sprite = data.icon;
                shadowImage.enabled = data.icon != null;
            }

            if (currentCountText != null)
            {
                currentCountText.text = data.currentCount.ToString();
            }

            if (targetCountText != null)
            {
                targetCountText.text = $"/{data.requiredCount}";
            }

            ApplyCompletedState(data.isCompleted, true);
        }

        public void ApplyCompletedState(bool isCompleted, bool instant)
        {
            if (backgroundImage == null)
            {
                return;
            }

            Color targetColor = isCompleted ? completedBackgroundColor : defaultBackgroundColor;
            backgroundImage.DOKill();
            if (instant)
            {
                backgroundImage.color = targetColor;
            }
            else
            {
                backgroundImage.DOColor(targetColor, 0.2f).SetLink(gameObject, LinkBehaviour.KillOnDisable);
            }
        }

        public void PlayProgressFx(TargetProgressChangedPayload payload)
        {
            if (payload.TileId != BoundTileId)
            {
                return;
            }

            transform.DOKill();
            transform.localScale = Vector3.one;
            transform.DOPunchScale(Vector3.one * 0.08f, 0.18f, 4)
                .SetLink(gameObject, LinkBehaviour.KillOnDisable);

            if (currentCountText != null)
            {
                currentCountText.text = payload.CurrentCount.ToString();
            }

            if (payload.JustCompleted)
            {
                ApplyCompletedState(true, false);
            }
        }
    }
}
