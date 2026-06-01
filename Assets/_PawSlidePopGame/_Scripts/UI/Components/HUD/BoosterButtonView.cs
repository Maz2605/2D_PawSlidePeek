using System;
using _PawSlidePopGame._Scripts.Feature.Match3.Boosters;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace _PawSlidePopGame._Scripts.UI.Components.HUD
{
    [DisallowMultipleComponent]
    public sealed class BoosterButtonView : MonoBehaviour
    {
        [Header("Refs")]
        [SerializeField] private Button button;
        [SerializeField] private Image backgroundImage;
        [SerializeField] private Image iconImage;
        [SerializeField] private TMP_Text countText;
        [SerializeField] private GameObject lockRoot;

        [Header("Visuals")]
        [SerializeField] private Sprite unlockedBackgroundSprite;
        [SerializeField] private Sprite lockedBackgroundSprite;
        [SerializeField] private string countFormat = "x{0}";
        [SerializeField] private string unlimitedCountText = "∞";
        [SerializeField] private float selectedIconScale = 1.12f;
        [SerializeField] private float selectedIconOffsetY = 10f;

        private BoosterDefinitionSO _definition;
        private Action<BoosterDefinitionSO> _onClicked;
        private RectTransform _iconRectTransform;
        private Vector2 _iconBaseAnchoredPosition;
        private Vector3 _iconBaseScale = Vector3.one;
        private bool _hasIconBaseState;

        public BoosterDefinitionSO Definition => _definition;

        private void Awake()
        {
            CacheIconBaseState();
        }

        private void OnDisable()
        {
            if (button != null)
            {
                button.onClick.RemoveListener(HandleClicked);
            }
        }

        public void Bind(BoosterDefinitionSO definition, int count, bool isSelected, Action<BoosterDefinitionSO> onClicked)
        {
            _definition = definition;
            _onClicked = onClicked;
            CacheIconBaseState();

            if (button != null)
            {
                button.onClick.RemoveListener(HandleClicked);
                button.onClick.AddListener(HandleClicked);
                button.interactable = definition != null;
            }

            if (iconImage != null)
            {
                iconImage.sprite = definition != null ? definition.Icon : null;
                iconImage.enabled = definition != null && definition.Icon != null;
            }

            SetState(count, isSelected);
        }

        public void SetState(int count, bool isSelected)
        {
            bool isUnlocked = _definition != null && _definition.IsUnlocked;

            if (backgroundImage != null)
            {
                Sprite targetSprite = isUnlocked ? unlockedBackgroundSprite : lockedBackgroundSprite;
                if (targetSprite != null)
                {
                    backgroundImage.sprite = targetSprite;
                }

                backgroundImage.color = Color.white;
            }

            if (lockRoot != null)
            {
                lockRoot.SetActive(!isUnlocked);
            }

            ApplySelectedIconState(isSelected);

            if (countText != null)
            {
                countText.SetText(ResolveCountText(count));
            }
        }

        private void CacheIconBaseState()
        {
            if (iconImage == null)
            {
                return;
            }

            _iconRectTransform = iconImage.rectTransform;
            _iconBaseAnchoredPosition = _iconRectTransform.anchoredPosition;
            _iconBaseScale = _iconRectTransform.localScale;
            _hasIconBaseState = true;
        }

        private void ApplySelectedIconState(bool isSelected)
        {
            if (!_hasIconBaseState || _iconRectTransform == null)
            {
                return;
            }

            _iconRectTransform.anchoredPosition = isSelected
                ? _iconBaseAnchoredPosition + new Vector2(0f, selectedIconOffsetY)
                : _iconBaseAnchoredPosition;

            _iconRectTransform.localScale = isSelected
                ? _iconBaseScale * selectedIconScale
                : _iconBaseScale;
        }

        private string ResolveCountText(int count)
        {
            if (_definition != null && _definition.IsUnlimitedForDev)
            {
                return unlimitedCountText;
            }

            return string.Format(countFormat, Mathf.Max(0, count));
        }

        private void HandleClicked()
        {
            if (_definition != null)
            {
                _onClicked?.Invoke(_definition);
            }
        }
    }
}
