using _PawSlidePopGame._Scripts.Data.Events;
using _PawSlidePopGame._Scripts.UI.Manager;
using _PawSlidePopGame._Scripts.UI.Popups;
using DG.Tweening;
using _PawSlidePopGame.Scripts.DesignPattern.ObserverPattern;
using UnityEngine;
using UnityEngine.UI;

namespace _PawSlidePopGame._Scripts.UI.Components
{
    /// <summary>
    /// Gắn vào bất kỳ Button nào trong game để mở SettingsPopup.
    /// Tự quản lý punch-scale animation và kiểm tra UIManager.
    /// </summary>
    [RequireComponent(typeof(Button))]
    [DisallowMultipleComponent]
    public sealed class SettingsButtonController : MonoBehaviour
    {
        [Header("--- Animation ---")]
        [SerializeField] private float punchScale = 0.12f;
        [SerializeField] private float punchDuration = 0.18f;
        [SerializeField] private int punchVibrato = 6;

        private Button _button;

        // ──────────────────────────────────────────────────────────
        // Lifecycle
        // ──────────────────────────────────────────────────────────

        private void Awake()
        {
            _button = GetComponent<Button>();
        }

        private void OnEnable()
        {
            _button.onClick.RemoveListener(HandleClicked);
            _button.onClick.AddListener(HandleClicked);
        }

        private void OnDisable()
        {
            _button.onClick.RemoveListener(HandleClicked);
        }

        // ──────────────────────────────────────────────────────────
        // Handler
        // ──────────────────────────────────────────────────────────

        private void HandleClicked()
        {
            EventManager<FeedbackEvent>.Post(FeedbackEvent.UiButtonTap);

            if (UIManager.Instance == null)
            {
                Debug.LogWarning("[SettingsButtonController] UIManager.Instance is null.", this);
                return;
            }

            // Punch scale trước, sau đó mới mở popup
            transform.DOKill();
            transform.localScale = Vector3.one;
            transform
                .DOPunchScale(Vector3.one * -punchScale, punchDuration, punchVibrato)
                .SetUpdate(true)
                .SetLink(gameObject, LinkBehaviour.KillOnDisable)
                .OnComplete(OpenSettingsPopup);
        }

        private void OpenSettingsPopup()
        {
            UIManager.Instance.ShowPopup<SettingsPopup>();
        }
    }
}
