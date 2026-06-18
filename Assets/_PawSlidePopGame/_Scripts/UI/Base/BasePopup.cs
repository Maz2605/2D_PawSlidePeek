using System;
using _PawSlidePopGame._Scripts.Data.Audio;
using _PawSlidePopGame._Scripts.Core.Audio;
using _PawSlidePopGame._Scripts.Core.Vibration;
using _PawSlidePopGame._Scripts.Data.Events;
using DG.Tweening;
using _PawSlidePopGame.Scripts.DesignPattern.ObserverPattern;
using UnityEngine;
using UnityEngine.UI;

namespace _PawSlidePopGame._Scripts.UI.Base
{
    [RequireComponent(typeof(CanvasGroup))]
    public abstract class BasePopup : MonoBehaviour
    {
        [Header("--- Base Popup Settings ---")] 
        [SerializeField] protected CanvasGroup canvasGroup;
        [SerializeField] protected float animDuration = 0.25f;

        protected Tween _animTween;

        public Action OnOpened;
        public Action OnClosed;

        protected virtual UISoundType OpenSound => UISoundType.PopupOpenStandard;
        protected virtual UISoundType CloseSound => UISoundType.PopupCloseStandard;

        protected virtual void Awake()
        {
            if (canvasGroup == null)
            {
                canvasGroup = GetComponent<CanvasGroup>();
            }

            if (canvasGroup == null)
            {
                canvasGroup = gameObject.AddComponent<CanvasGroup>();
            }
        }

        public virtual void Show(Action onOpenedCallback = null)
        {
            gameObject.SetActive(true);
            
            OnBeforeShow();
            canvasGroup.blocksRaycasts = false; 
            
            OnOpened = onOpenedCallback;

            if (OpenSound != UISoundType.None && AudioController.Instance != null)
            {
                AudioController.Instance.PlayUISound(OpenSound);
            }
            if (OpenSound != UISoundType.None && VibrationManager.Instance != null)
            {
                VibrationManager.Instance.PlayLightImpact();
            }

            _animTween?.Kill();
            canvasGroup.alpha = 0f;
            _animTween = canvasGroup.DOFade(1f, animDuration)
                .SetUpdate(true)
                .SetLink(gameObject, LinkBehaviour.KillOnDisable)
                .OnComplete(() => 
                {
                    canvasGroup.blocksRaycasts = true;
                    OnOpened?.Invoke();
                }); 
            
            PlayShowAnimation();
        }

        public virtual void Hide()
        {
            canvasGroup.blocksRaycasts = false; 
            
            if (CloseSound != UISoundType.None && AudioController.Instance != null)
            {
                AudioController.Instance.PlayUISound(CloseSound);
            }

            _animTween?.Kill();
            
            PlayHideAnimation(() => 
            {
                gameObject.SetActive(false);
                OnClosed?.Invoke();
                OnOpened = null;
                OnClosed = null;
            });
        }
        
        protected void BindButton(Button btn, Action onClickAction)
        {
            if (btn == null) return;
            
            btn.onClick?.RemoveAllListeners();
            btn.onClick?.AddListener(() =>
            {
                bool hasButtonSound = btn.TryGetComponent<UIButtonSound>(out var _);
                Debug.Log($"[BasePopup] BindButton clicked: '{btn.name}' on popup '{gameObject.name}'. hasUIButtonSound={hasButtonSound}");
                
                if (!hasButtonSound)
                {
                    Debug.Log("[BasePopup] Posting FeedbackEvent.UiButtonTap");
                    EventManager<FeedbackEvent>.Post(FeedbackEvent.UiButtonTap);
                }
                
                btn.transform.DOKill();
                btn.transform.localScale = Vector3.one;
                btn.transform
                    .DOPunchScale(Vector3.one * -0.1f, 0.15f, 5) 
                    .SetUpdate(true) 
                    .SetLink(btn.gameObject, LinkBehaviour.KillOnDisable)
                    .OnComplete(() => onClickAction?.Invoke());
            });
        }

        // Bắt buộc các Popup con (Setting, Confirm) phải tự định nghĩa anim
        protected virtual void OnBeforeShow() { }
        protected abstract void PlayShowAnimation();
        protected abstract void PlayHideAnimation(Action onComplete);
    }
}
