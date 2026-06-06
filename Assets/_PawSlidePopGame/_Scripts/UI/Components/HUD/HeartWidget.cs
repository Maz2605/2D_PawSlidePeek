using System;
using DG.Tweening;
using TMPro;
using _PawSlidePopGame._Scripts.Gameplay.Meta.EconomyManager;
using UnityEngine;
using UnityEngine.UI;

namespace _PawSlidePopGame._Scripts.UI.Components.HUD
{
    [DisallowMultipleComponent]
    public sealed class HeartWidget : MonoBehaviour
    {
        [Header("Refs")]
        [SerializeField] private TMP_Text amountText; // Inside the heart icon
        [SerializeField] private RectTransform amountTextRoot;
        [SerializeField] private Image heartIcon;
        [SerializeField] private TMP_Text statusText; // Displays "Full" or timer "MM:SS"
        [SerializeField] private Button addButton; // Green plus button



        [Header("Format")]
        [SerializeField] private string amountFormat = "{0}";
        [SerializeField] private string fullText = "Full";

        [Header("Count Animation")]
        [SerializeField] private bool subscribeHeartManager = true;
        [SerializeField] private bool pollHeartManagerValue = true;
        [SerializeField] private bool useUnscaledTime = true;
        [SerializeField] private float countDuration = 0.35f;
        [SerializeField] private Ease countEase = Ease.OutCubic;

        [Header("Slide Animation")]
        [SerializeField] private float slideOffsetY = 18f;
        [SerializeField] private float slideOutDuration = 0.12f;
        [SerializeField] private float slideInDuration = 0.18f;
        [SerializeField] private Ease slideOutEase = Ease.InCubic;
        [SerializeField] private Ease slideInEase = Ease.OutBack;

        [Header("Feedback")]
        [SerializeField] private float iconPunchScale = 0.12f;
        [SerializeField] private float iconPunchDuration = 0.18f;
        [SerializeField] private float increaseScale = 1.08f;
        [SerializeField] private float decreaseScale = 0.94f;

        [Header("Icon Animation Settings")]
        [SerializeField] private IconIdleAnimType iconIdleAnimType = IconIdleAnimType.Floating;
        [SerializeField] private float idleDuration = 2.0f;
        [SerializeField] private float idleFloatAmount = 6f;
        [SerializeField] private float idlePulseScale = 1.06f;
        [SerializeField] private float shakeDuration = 0.5f;
        [SerializeField] private float shakeRotationStrength = 15f;
        [SerializeField] private float shakeScaleStrength = 0.15f;

        private int _displayedAmount;
        private int _targetAmount;
        private Sequence _updateSequence;
        private Tween _countTween;
        private Vector2 _amountBaseAnchoredPosition;
        private Vector3 _amountBaseScale;
        private Vector3 _iconBaseScale;
        private Vector2 _iconBaseAnchoredPosition;
        private Tween _idleTween;
        private Sequence _shakeTween;
        private bool _hasCachedBaseState;
        private bool _initialized;
        private bool _isAnimating;
        private HeartManager _heartManager;

        private RectTransform AmountRoot
        {
            get
            {
                if (amountTextRoot != null)
                {
                    return amountTextRoot;
                }

                return amountText != null ? amountText.rectTransform : null;
            }
        }

        private void Awake()
        {
            CacheBaseState(force: true);
            SetupIconInteraction();
        }

        private void OnEnable()
        {
            CacheBaseState(force: false);
            RestoreBaseState();

            HeartManager heartManager = GetHeartManager();
            if (subscribeHeartManager && heartManager != null)
            {
                heartManager.OnHeartsChanged -= HandleHeartsChanged;
                heartManager.OnHeartsChanged += HandleHeartsChanged;
                SetAmount(heartManager.Hearts, animate: false);
            }

            StartIdleAnimation();
        }

        private void OnDisable()
        {
            if (_heartManager != null)
            {
                _heartManager.OnHeartsChanged -= HandleHeartsChanged;
            }

            _heartManager = null;
            KillTweens();
            KillIdleAndShakeTweens();
            RestoreBaseState();
            _isAnimating = false;
        }

        private void Start()
        {
            RefreshFromManager(animate: false);
        }

        private void Update()
        {
            HeartManager heartManager = GetHeartManager();
            if (heartManager == null)
            {
                return;
            }

            // 1. Update amount text if not animating and amount changed
            int currentHearts = heartManager.Hearts;
            if (pollHeartManagerValue && !_isAnimating && _targetAmount != currentHearts)
            {
                SetAmount(currentHearts, animate: _initialized);
            }

            // 2. Update status (Full or Countdown timer)
            if (statusText != null)
            {
                if (currentHearts >= HeartManager.MaxHearts)
                {
                    statusText.text = fullText;
                }
                else
                {
                    double secondsRemaining = heartManager.SecondsUntilNextHeart;
                    int minutes = (int)(secondsRemaining / 60);
                    int seconds = (int)(secondsRemaining % 60);
                    statusText.text = $"{minutes:00}:{seconds:00}";
                }
            }


        }

        public void RefreshFromManager(bool animate)
        {
            HeartManager heartManager = GetHeartManager();
            if (heartManager != null)
            {
                SetAmount(heartManager.Hearts, animate);
            }
        }

        public void SetAmount(int amount, bool animate)
        {
            amount = Mathf.Max(0, amount);
            if (!_initialized || !animate)
            {
                _displayedAmount = amount;
                _targetAmount = amount;
                RefreshText();
                _initialized = true;
                return;
            }

            PlayAmountChanged(_displayedAmount, amount);
        }

        public void SetInputEnabled(bool enabled)
        {
            if (addButton != null)
            {
                addButton.interactable = enabled;
            }
        }

        private void HandleHeartsChanged(int previousAmount, int currentAmount)
        {
            if (!_initialized)
            {
                SetAmount(currentAmount, animate: false);
                return;
            }

            PlayAmountChanged(previousAmount, currentAmount);
        }

        private HeartManager GetHeartManager()
        {
            if (_heartManager != null)
            {
                return _heartManager;
            }

            _heartManager = FindFirstObjectByType<HeartManager>(FindObjectsInactive.Include);
            return _heartManager;
        }

        private void PlayAmountChanged(int previousAmount, int currentAmount)
        {
            currentAmount = Mathf.Max(0, currentAmount);
            if (previousAmount == currentAmount)
            {
                SetAmount(currentAmount, animate: false);
                return;
            }

            KillTweens();
            KillIdleAndShakeTweens();
            CacheBaseState(force: false);
            RestoreBaseState();

            _displayedAmount = Mathf.Max(0, previousAmount);
            _targetAmount = currentAmount;
            RefreshText();
            _isAnimating = true;

            int direction = currentAmount > previousAmount ? 1 : -1;
            RectTransform root = AmountRoot;
            Vector2 outPosition = _amountBaseAnchoredPosition + new Vector2(0f, slideOffsetY * direction);
            Vector2 inStartPosition = _amountBaseAnchoredPosition - new Vector2(0f, slideOffsetY * direction);
            Vector3 feedbackScale = _amountBaseScale * (direction > 0 ? increaseScale : decreaseScale);

            _updateSequence = DOTween.Sequence()
                .SetUpdate(useUnscaledTime)
                .SetLink(gameObject, LinkBehaviour.KillOnDisable);

            if (root != null)
            {
                _updateSequence.Append(root.DOAnchorPos(outPosition, slideOutDuration).SetEase(slideOutEase));
                _updateSequence.Join(root.DOScale(feedbackScale, slideOutDuration).SetEase(slideOutEase));
                _updateSequence.AppendCallback(() =>
                {
                    root.anchoredPosition = inStartPosition;
                    _displayedAmount = _targetAmount;
                    RefreshText();
                });
                _updateSequence.Append(root.DOAnchorPos(_amountBaseAnchoredPosition, slideInDuration).SetEase(slideInEase));
                _updateSequence.Join(root.DOScale(_amountBaseScale, slideInDuration).SetEase(slideInEase));
            }
            else
            {
                _updateSequence.AppendInterval(slideOutDuration);
                _updateSequence.AppendCallback(() =>
                {
                    _displayedAmount = _targetAmount;
                    RefreshText();
                });
                _updateSequence.AppendInterval(slideInDuration);
            }

            _countTween = DOTween.To(() => previousAmount, value =>
                {
                    _displayedAmount = Mathf.Max(0, value);
                    RefreshText();
                }, currentAmount, countDuration)
                .SetEase(countEase)
                .SetUpdate(useUnscaledTime)
                .SetLink(gameObject, LinkBehaviour.KillOnDisable);

            if (heartIcon != null)
            {
                heartIcon.transform.localScale = _iconBaseScale;
                _updateSequence.Join(heartIcon.transform.DOPunchScale(_iconBaseScale * iconPunchScale, iconPunchDuration, 8, 0.75f)
                    .SetUpdate(useUnscaledTime));
            }

            _updateSequence.OnComplete(() =>
            {
                _displayedAmount = _targetAmount;
                RefreshText();
                RestoreBaseState();
                _isAnimating = false;
                StartIdleAnimation();
            });
        }

        private void RefreshText()
        {
            if (amountText != null)
            {
                amountText.SetText(amountFormat, _displayedAmount);
            }
        }

        private void CacheBaseState(bool force)
        {
            if (_hasCachedBaseState && !force)
            {
                return;
            }

            RectTransform root = AmountRoot;
            if (root != null)
            {
                _amountBaseAnchoredPosition = root.anchoredPosition;
                _amountBaseScale = root.localScale;
            }

            if (heartIcon != null)
            {
                _iconBaseScale = heartIcon.transform.localScale;
                _iconBaseAnchoredPosition = heartIcon.rectTransform.anchoredPosition;
            }

            _hasCachedBaseState = true;
        }

        private void RestoreBaseState()
        {
            RectTransform root = AmountRoot;
            if (root != null)
            {
                root.anchoredPosition = _amountBaseAnchoredPosition;
                root.localScale = _amountBaseScale;
            }

            if (heartIcon != null)
            {
                heartIcon.transform.localScale = _iconBaseScale;
                heartIcon.rectTransform.anchoredPosition = _iconBaseAnchoredPosition;
                heartIcon.transform.localRotation = Quaternion.identity;
            }
        }

        private void KillTweens()
        {
            _updateSequence?.Kill();
            _updateSequence = null;
            _countTween?.Kill();
            _countTween = null;
        }

        private void KillIdleAndShakeTweens()
        {
            _idleTween?.Kill();
            _idleTween = null;
            _shakeTween?.Kill();
            _shakeTween = null;
        }

        private void SetupIconInteraction()
        {
            if (heartIcon != null)
            {
                Button button = heartIcon.GetComponent<Button>();
                if (button == null)
                {
                    button = heartIcon.gameObject.AddComponent<Button>();
                    button.transition = Selectable.Transition.None;
                }
                button.onClick.RemoveListener(PlayIconShake);
                button.onClick.AddListener(PlayIconShake);
            }
        }

        private void StartIdleAnimation()
        {
            if (heartIcon == null || iconIdleAnimType == IconIdleAnimType.None)
            {
                return;
            }

            _idleTween?.Kill();

            if (iconIdleAnimType == IconIdleAnimType.Floating)
            {
                float startY = _iconBaseAnchoredPosition.y;
                float targetY = startY + idleFloatAmount;
                heartIcon.rectTransform.anchoredPosition = new Vector2(_iconBaseAnchoredPosition.x, startY);

                _idleTween = heartIcon.rectTransform.DOAnchorPosY(targetY, idleDuration)
                    .SetEase(Ease.InOutSine)
                    .SetLoops(-1, LoopType.Yoyo)
                    .SetUpdate(useUnscaledTime)
                    .SetLink(heartIcon.gameObject);
            }
            else if (iconIdleAnimType == IconIdleAnimType.Pulsing)
            {
                heartIcon.transform.localScale = _iconBaseScale;
                Vector3 targetScale = _iconBaseScale * idlePulseScale;

                _idleTween = heartIcon.transform.DOScale(targetScale, idleDuration)
                    .SetEase(Ease.InOutSine)
                    .SetLoops(-1, LoopType.Yoyo)
                    .SetUpdate(useUnscaledTime)
                    .SetLink(heartIcon.gameObject);
            }
        }

        private void PlayIconShake()
        {
            if (heartIcon == null)
            {
                return;
            }

            _idleTween?.Kill();
            _idleTween = null;
            _shakeTween?.Kill();
            _shakeTween = null;

            heartIcon.transform.localScale = _iconBaseScale;
            heartIcon.rectTransform.anchoredPosition = _iconBaseAnchoredPosition;
            heartIcon.transform.localRotation = Quaternion.identity;

            _shakeTween = DOTween.Sequence()
                .SetUpdate(useUnscaledTime)
                .SetLink(heartIcon.gameObject);

            _shakeTween.Append(heartIcon.transform.DOPunchRotation(new Vector3(0f, 0f, shakeRotationStrength), shakeDuration, 10, 1f));
            _shakeTween.Join(heartIcon.transform.DOPunchScale(_iconBaseScale * shakeScaleStrength, shakeDuration, 10, 1f));

            _shakeTween.OnComplete(() =>
            {
                _shakeTween = null;
                StartIdleAnimation();
            });
        }
    }
}
