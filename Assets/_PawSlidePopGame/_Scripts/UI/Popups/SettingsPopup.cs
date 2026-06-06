using System;
using _PawSlidePopGame._Scripts.UI.Base;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace _PawSlidePopGame._Scripts.UI.Popups
{
    public sealed class SettingsPopup : BasePopup
    {
        [Header("--- Elements ---")]
        [SerializeField] private RectTransform animatedContent;

        // ── Music ────────────────────────────────────────────────
        [Header("--- Music Settings ---")]
        [SerializeField] private Slider musicSlider;
        /// <summary>GameObject Icon hien thi khi Music > 0 (chua tat)</summary>
        [SerializeField] private GameObject musicIconOn;
        /// <summary>GameObject Icon hien thi khi Music = 0 (da tat)</summary>
        [SerializeField] private GameObject musicIconOff;

        // ── SFX ──────────────────────────────────────────────────
        [Header("--- SFX Settings ---")]
        [SerializeField] private Slider sfxSlider;
        /// <summary>GameObject Icon hien thi khi SFX > 0 (chua tat)</summary>
        [SerializeField] private GameObject sfxIconOn;
        /// <summary>GameObject Icon hien thi khi SFX = 0 (da tat)</summary>
        [SerializeField] private GameObject sfxIconOff;

        // ── Vibration ────────────────────────────────────────────
        [Header("--- Vibration (Slider as Toggle) ---")]
        [SerializeField] private Slider vibrationSlider;
        [SerializeField] private GameObject vibrationIconOn;
        [SerializeField] private GameObject vibrationIconOff;

        [Header("--- Buttons ---")]
        [SerializeField] private Button btnClose;
        [SerializeField] private Button btnCancel;
        [SerializeField] private Button btnSave;

        [Header("--- Sequence Settings ---")]
        [SerializeField] private float moveOffsetY = 200f;
        [SerializeField] private float moveDuration = 0.3f;
        [SerializeField] private Ease showEase = Ease.OutBack;
        [SerializeField] private Ease hideEase = Ease.InCubic;

        private Sequence _showSequence;
        private Vector2 _contentOriginalPos;

        // ──────────────────────────────────────────────────────────
        // Lifecycle
        // ──────────────────────────────────────────────────────────

        protected override void Awake()
        {
            base.Awake();
            if (animatedContent != null)
                _contentOriginalPos = animatedContent.anchoredPosition;

            // Vibration hoat dong nhu Toggle (chi 0 hoac 1)
            if (vibrationSlider != null)
            {
                vibrationSlider.minValue = 0f;
                vibrationSlider.maxValue = 1f;
                vibrationSlider.wholeNumbers = true;
            }

            SetupSliderDragEffects();
        }

        // ──────────────────────────────────────────────────────────
        // BasePopup Overrides
        // ──────────────────────────────────────────────────────────

        protected override void OnBeforeShow()
        {
            KillActiveTweens();
            ResetLayoutState();
            BindButtons();
            BindSliderIcons();
            RefreshMusicIcon(animate: false);
            RefreshSfxIcon(animate: false);
            RefreshVibrationIcon(animate: false);
        }

        protected override void PlayShowAnimation()
        {
            _showSequence = DOTween.Sequence()
                .SetUpdate(true)
                .SetLink(gameObject, LinkBehaviour.KillOnDisable);

            if (animatedContent != null)
            {
                _showSequence.Append(
                    animatedContent
                        .DOAnchorPos(_contentOriginalPos, moveDuration)
                        .SetEase(showEase));
            }

            _showSequence.OnComplete(() => SetButtonsInteractable(true));
        }

        protected override void PlayHideAnimation(Action onComplete)
        {
            KillActiveTweens();

            Sequence hideSeq = DOTween.Sequence()
                .SetUpdate(true)
                .SetLink(gameObject, LinkBehaviour.KillOnDisable);

            if (animatedContent != null)
            {
                hideSeq.Join(
                    animatedContent
                        .DOAnchorPos(_contentOriginalPos + new Vector2(0f, -moveOffsetY), moveDuration)
                        .SetEase(hideEase));
            }

            hideSeq.Join(canvasGroup.DOFade(0f, moveDuration * 0.8f).SetEase(Ease.InQuad));
            hideSeq.OnComplete(() => onComplete?.Invoke());
        }

        // ──────────────────────────────────────────────────────────
        // Binding
        // ──────────────────────────────────────────────────────────

        private void BindButtons()
        {
            SetButtonsInteractable(false);
            BindButton(btnClose,  HandleClosePressed);
            BindButton(btnCancel, HandleCancelPressed);
            BindButton(btnSave,   HandleSavePressed);
        }

        private void BindSliderIcons()
        {
            if (musicSlider != null)
            {
                musicSlider.onValueChanged.RemoveAllListeners();
                musicSlider.onValueChanged.AddListener(_ => RefreshMusicIcon(animate: true));
            }

            if (sfxSlider != null)
            {
                sfxSlider.onValueChanged.RemoveAllListeners();
                sfxSlider.onValueChanged.AddListener(_ => RefreshSfxIcon(animate: true));
            }

            if (vibrationSlider != null)
            {
                vibrationSlider.onValueChanged.RemoveAllListeners();
                vibrationSlider.onValueChanged.AddListener(_ => RefreshVibrationIcon(animate: true));
            }
        }

        // ──────────────────────────────────────────────────────────
        // Icon Refresh - dung GameObject On/Off cho ca 3
        // ──────────────────────────────────────────────────────────

        private void RefreshMusicIcon(bool animate = true)
        {
            bool isMuted = musicSlider == null || musicSlider.value <= 0.001f;
            TransitionIcon(musicSlider, musicIconOn, musicIconOff, !isMuted, animate);
        }

        private void RefreshSfxIcon(bool animate = true)
        {
            bool isMuted = sfxSlider == null || sfxSlider.value <= 0.001f;
            TransitionIcon(sfxSlider, sfxIconOn, sfxIconOff, !isMuted, animate);
        }

        private void RefreshVibrationIcon(bool animate = true)
        {
            bool isOn = vibrationSlider != null && vibrationSlider.value >= 0.5f;
            TransitionIcon(vibrationSlider, vibrationIconOn, vibrationIconOff, isOn, animate);
        }

        private void TransitionIcon(Slider slider, GameObject iconOn, GameObject iconOff, bool showOn, bool animate)
        {
            if (iconOn == null || iconOff == null) return;

            GameObject targetActive = showOn ? iconOn : iconOff;
            GameObject targetInactive = showOn ? iconOff : iconOn;

            if (!animate)
            {
                targetActive.SetActive(true);
                targetActive.transform.localScale = Vector3.one;
                targetInactive.SetActive(false);
                targetInactive.transform.localScale = Vector3.zero;
                return;
            }

            if (!targetActive.activeSelf)
            {
                targetInactive.SetActive(false);
                targetInactive.transform.localScale = Vector3.zero;

                targetActive.SetActive(true);
                targetActive.transform.localScale = Vector3.one;

                // Tac dong hieu ung nay (Punch Scale) len Knob (Handle cua Slider)
                if (slider != null && slider.handleRect != null)
                {
                    slider.handleRect.DOKill();
                    slider.handleRect.DOPunchScale(new Vector3(0.12f, 0.12f, 0f), 0.25f, 4, 0.5f).SetUpdate(true);
                }
            }
            else
            {
                targetActive.SetActive(true);
                targetActive.transform.localScale = Vector3.one;
                targetInactive.SetActive(false);
                targetInactive.transform.localScale = Vector3.zero;
            }
        }

        // ──────────────────────────────────────────────────────────
        // Handlers
        // ──────────────────────────────────────────────────────────

        private void HandleClosePressed() => Hide();

        private void HandleCancelPressed()
        {
            // TODO: khoi phuc gia tri cu truoc khi dong
            Hide();
        }

        private void HandleSavePressed()
        {
            // TODO: luu gia tri Music, SFX, Vibration vao PlayerPrefs
            Hide();
        }

        // ──────────────────────────────────────────────────────────
        // Helpers
        // ──────────────────────────────────────────────────────────

        private void SetButtonsInteractable(bool enable)
        {
            if (btnClose)  btnClose.interactable  = enable;
            if (btnCancel) btnCancel.interactable  = enable;
            if (btnSave)   btnSave.interactable    = enable;
        }

        private void PrepareForShow()
        {
            if (animatedContent == null) return;
            animatedContent.anchoredPosition = _contentOriginalPos + new Vector2(0f, -moveOffsetY);
        }

        private void ResetLayoutState()
        {
            if (animatedContent == null) return;
            PrepareForShow();
            animatedContent.localScale = Vector3.one;
            animatedContent.localRotation = Quaternion.identity;
        }

        private void KillActiveTweens()
        {
            _showSequence?.Kill();
            animatedContent?.DOKill();
            transform.DOKill();

            if (musicIconOn != null) musicIconOn.transform.DOKill();
            if (musicIconOff != null) musicIconOff.transform.DOKill();
            if (sfxIconOn != null) sfxIconOn.transform.DOKill();
            if (sfxIconOff != null) sfxIconOff.transform.DOKill();
            if (vibrationIconOn != null) vibrationIconOn.transform.DOKill();
            if (vibrationIconOff != null) vibrationIconOff.transform.DOKill();

            if (musicSlider != null && musicSlider.handleRect != null) musicSlider.handleRect.DOKill();
            if (sfxSlider != null && sfxSlider.handleRect != null) sfxSlider.handleRect.DOKill();
            if (vibrationSlider != null && vibrationSlider.handleRect != null) vibrationSlider.handleRect.DOKill();
        }

        // ──────────────────────────────────────────────────────────
        // Private Interaction Handlers (Tu dong dang ky EventTrigger qua code)
        // ──────────────────────────────────────────────────────────

        private readonly System.Collections.Generic.Dictionary<Slider, bool> _isSliderPressed = new System.Collections.Generic.Dictionary<Slider, bool>();

        private void SetupSliderDragEffects()
        {
            SetupDragEffectForSlider(musicSlider);
            SetupDragEffectForSlider(sfxSlider);
            SetupDragEffectForSlider(vibrationSlider);
        }

        private void SetupDragEffectForSlider(Slider slider)
        {
            if (slider == null) return;

            RectTransform handle = slider.handleRect;
            if (handle == null) return;

            EventTrigger trigger = slider.GetComponent<EventTrigger>();
            if (trigger == null)
            {
                trigger = slider.gameObject.AddComponent<EventTrigger>();
            }

            // Xoa cac event cu de tranh trung lap
            trigger.triggers.Clear();

            // Pointer Enter (Hover vao)
            EventTrigger.Entry pointerEnter = new EventTrigger.Entry();
            pointerEnter.eventID = EventTriggerType.PointerEnter;
            pointerEnter.callback.AddListener((data) =>
            {
                if (_isSliderPressed.TryGetValue(slider, out bool pressed) && pressed) return;
                handle.DOKill();
                handle.DOScale(1.08f, 0.15f).SetEase(Ease.OutCubic).SetUpdate(true);
            });
            trigger.triggers.Add(pointerEnter);

            // Pointer Exit (Hover ra)
            EventTrigger.Entry pointerExit = new EventTrigger.Entry();
            pointerExit.eventID = EventTriggerType.PointerExit;
            pointerExit.callback.AddListener((data) =>
            {
                if (_isSliderPressed.TryGetValue(slider, out bool pressed) && pressed) return;
                handle.DOKill();
                handle.DOScale(1.0f, 0.15f).SetEase(Ease.OutCubic).SetUpdate(true);
            });
            trigger.triggers.Add(pointerExit);

            // Pointer Down (Nhan giu/Keo)
            EventTrigger.Entry pointerDown = new EventTrigger.Entry();
            pointerDown.eventID = EventTriggerType.PointerDown;
            pointerDown.callback.AddListener((data) =>
            {
                _isSliderPressed[slider] = true;
                handle.DOKill();
                handle.DOScale(1.16f, 0.15f).SetEase(Ease.OutBack).SetUpdate(true);
            });
            trigger.triggers.Add(pointerDown);

            // Pointer Up (Tha ra)
            EventTrigger.Entry pointerUp = new EventTrigger.Entry();
            pointerUp.eventID = EventTriggerType.PointerUp;
            pointerUp.callback.AddListener((data) =>
            {
                _isSliderPressed[slider] = false;
                handle.DOKill();
                handle.DOScale(1.0f, 0.25f).SetEase(Ease.OutBack).SetUpdate(true);
            });
            trigger.triggers.Add(pointerUp);
        }
    }
}
