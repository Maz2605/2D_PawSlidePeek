using DG.Tweening;
using TMPro;
using _PawSlidePopGame._Scripts.Gameplay.Meta.EconomyManager;
using UnityEngine;
using UnityEngine.UI;

namespace _PawSlidePopGame._Scripts.UI.Components.HUD
{
    [DisallowMultipleComponent]
    public sealed class CoinCurrencyWidget : MonoBehaviour
    {
        [Header("Refs")]
        [SerializeField] private TMP_Text amountText;
        [SerializeField] private RectTransform amountTextRoot;
        [SerializeField] private Image coinIcon;
        [SerializeField] private Button addButton;

        [Header("Format")]
        [SerializeField] private string amountFormat = "{0}";

        [Header("Count Animation")]
        [SerializeField] private bool subscribeEconomyManager = true;
        [SerializeField] private bool pollEconomyManagerValue = true;
        [SerializeField] private bool enableDevAddCoinHotkey = true;
        [SerializeField] private UnityEngine.InputSystem.Key devAddCoinKey = UnityEngine.InputSystem.Key.Digit0;
        [SerializeField] private int devAddCoinAmount = 1000;
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

        private int _displayedAmount;
        private int _targetAmount;
        private Sequence _updateSequence;
        private Tween _countTween;
        private Vector2 _amountBaseAnchoredPosition;
        private Vector3 _amountBaseScale;
        private Vector3 _iconBaseScale;
        private bool _hasCachedBaseState;
        private bool _initialized;

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
        }

        private void OnEnable()
        {
            CacheBaseState(force: false);
            RestoreBaseState();

            if (subscribeEconomyManager && EconomyManager.Instance != null)
            {
                EconomyManager.Instance.OnCoinsChanged -= HandleCoinsChanged;
                EconomyManager.Instance.OnCoinsChanged += HandleCoinsChanged;
                SetAmount(EconomyManager.Instance.Coins, animate: false);
            }
        }

        private void OnDisable()
        {
            if (EconomyManager.Instance != null)
            {
                EconomyManager.Instance.OnCoinsChanged -= HandleCoinsChanged;
            }

            KillTweens();
            RestoreBaseState();
        }

        private void Start()
        {
            RefreshFromEconomy(animate: false);
        }

        private void Update()
        {
            if (pollEconomyManagerValue && EconomyManager.Instance != null && _targetAmount != EconomyManager.Instance.Coins)
            {
                SetAmount(EconomyManager.Instance.Coins, animate: _initialized);
            }

            if (!enableDevAddCoinHotkey || devAddCoinAmount <= 0)
            {
                return;
            }

            UnityEngine.InputSystem.Keyboard keyboard = UnityEngine.InputSystem.Keyboard.current;
            if (keyboard == null)
            {
                return;
            }

            UnityEngine.InputSystem.Controls.KeyControl keyControl = keyboard[devAddCoinKey];
            if (keyControl != null && keyControl.wasPressedThisFrame)
            {
                EconomyManager.Instance.AddCoins(devAddCoinAmount, "dev_hotkey");
            }
        }

        public void RefreshFromEconomy(bool animate)
        {
            if (EconomyManager.Instance != null)
            {
                SetAmount(EconomyManager.Instance.Coins, animate);
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

        private void HandleCoinsChanged(int previousAmount, int currentAmount, string reason)
        {
            if (!_initialized)
            {
                SetAmount(currentAmount, animate: false);
                return;
            }

            PlayAmountChanged(previousAmount, currentAmount);
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
            CacheBaseState(force: false);
            RestoreBaseState();

            _displayedAmount = Mathf.Max(0, previousAmount);
            _targetAmount = currentAmount;
            RefreshText();

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

            if (coinIcon != null)
            {
                coinIcon.transform.localScale = _iconBaseScale;
                _updateSequence.Join(coinIcon.transform.DOPunchScale(_iconBaseScale * iconPunchScale, iconPunchDuration, 8, 0.75f)
                    .SetUpdate(useUnscaledTime));
            }

            _updateSequence.OnComplete(() =>
            {
                _displayedAmount = _targetAmount;
                RefreshText();
                RestoreBaseState();
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

            if (coinIcon != null)
            {
                _iconBaseScale = coinIcon.transform.localScale;
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

            if (coinIcon != null)
            {
                coinIcon.transform.localScale = _iconBaseScale;
            }
        }

        private void KillTweens()
        {
            _updateSequence?.Kill();
            _updateSequence = null;
            _countTween?.Kill();
            _countTween = null;
        }
    }
}
