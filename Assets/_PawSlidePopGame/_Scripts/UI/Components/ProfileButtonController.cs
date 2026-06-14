using _PawSlidePopGame._Scripts.Data.Events;
using _PawSlidePopGame._Scripts.UI.Manager;
using _PawSlidePopGame._Scripts.UI.Popups;
using _PawSlidePopGame._Scripts.Gameplay.Meta.EconomyManager;
using DG.Tweening;
using _PawSlidePopGame.Scripts.DesignPattern.ObserverPattern;
using UnityEngine;
using UnityEngine.UI;

namespace _PawSlidePopGame._Scripts.UI.Components
{
    /// <summary>
    /// Attached to the TopBar Avatar button to open ProfilePopup.
    /// Manages punch-scale animation and updates the avatar sprite dynamically.
    /// </summary>
    [RequireComponent(typeof(Button))]
    [DisallowMultipleComponent]
    public sealed class ProfileButtonController : MonoBehaviour
    {
        [Header("--- Avatar Image ---")]
        [SerializeField] private Image imgAvatar;
        [SerializeField] private Sprite[] avatarSprites;

        [Header("--- Animation ---")]
        [SerializeField] private float punchScale = 0.12f;
        [SerializeField] private float punchDuration = 0.18f;
        [SerializeField] private int punchVibrato = 6;

        private Button _button;

        private void Awake()
        {
            _button = GetComponent<Button>();
            if (imgAvatar == null)
            {
                imgAvatar = GetComponentInChildren<Image>();
            }
        }

        private void OnEnable()
        {
            _button.onClick.RemoveListener(HandleClicked);
            _button.onClick.AddListener(HandleClicked);

            PlayerEconomyRepository.OnDataChanged += RefreshAvatar;
            RefreshAvatar();
        }

        private void OnDisable()
        {
            _button.onClick.RemoveListener(HandleClicked);
            PlayerEconomyRepository.OnDataChanged -= RefreshAvatar;
        }

        private void HandleClicked()
        {
            EventManager<FeedbackEvent>.Post(FeedbackEvent.UiButtonTap);

            if (UIManager.Instance == null)
            {
                Debug.LogWarning("[ProfileButtonController] UIManager.Instance is null.", this);
                return;
            }

            transform.DOKill();
            transform.localScale = Vector3.one;
            transform
                .DOPunchScale(Vector3.one * -punchScale, punchDuration, punchVibrato)
                .SetUpdate(true)
                .SetLink(gameObject, LinkBehaviour.KillOnDisable)
                .OnComplete(OpenProfilePopup);
        }

        private void OpenProfilePopup()
        {
            UIManager.Instance.ShowPopup<ProfilePopup>();
        }

        public void RefreshAvatar()
        {
            if (imgAvatar == null || avatarSprites == null || avatarSprites.Length == 0) return;
            if (PlayerEconomyRepository.Instance == null || PlayerEconomyRepository.Instance.Data == null) return;

            int index = PlayerEconomyRepository.Instance.Data.avatarIndex;
            if (index >= 0 && index < avatarSprites.Length)
            {
                imgAvatar.sprite = avatarSprites[index];
            }
        }
    }
}
