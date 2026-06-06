using System;
using System.Collections.Generic;
using _PawSlidePopGame._Scripts.Feature.Match3.Boosters;
using _PawSlidePopGame._Scripts.Feature.Meta.Wheel;
using _PawSlidePopGame._Scripts.UI.Base;
using _PawSlidePopGame._Scripts.UI.Manager;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace _PawSlidePopGame._Scripts.UI.Screens.SubScreens
{
    public class WheelSubScreen : BaseSubScreen
    {
        [Header("Data")]
        [SerializeField] private WheelConfigSO config;
        [SerializeField] private TextAsset jsonOverride;
        [SerializeField] private List<BoosterDefinitionSO> boosterCatalog = new List<BoosterDefinitionSO>();

        [Header("Refs")]
        [SerializeField] private RectTransform wheelRoot;
        [SerializeField] private Button spinButton;
        [SerializeField] private List<WheelSegmentView> segmentViews = new List<WheelSegmentView>();
        [SerializeField] private TMP_Text statusText;
        [SerializeField] private TMP_Text resultText;

        [Header("Animation")]
        [SerializeField] private bool useUnscaledTime = true;
        [SerializeField] private float pointerAngleDegrees = 90f;
        [SerializeField] private WheelSpinDirection spinDirection = WheelSpinDirection.Clockwise;
        [SerializeField, Range(0.05f, 0.4f)] private float accelerationTimeRatio = 0.18f;
        [SerializeField, Range(0.05f, 0.5f)] private float cruiseTimeRatio = 0.22f;
        [SerializeField, Range(0.02f, 0.25f)] private float accelerationDistanceRatio = 0.1f;
        [SerializeField, Range(0.05f, 0.45f)] private float cruiseDistanceRatio = 0.24f;
        [SerializeField] private float startPunchScale = 0.04f;
        [SerializeField] private float startPunchDuration = 0.18f;
        [SerializeField] private float settleOvershootDegrees = 2.5f;
        [SerializeField] private float settleDuration = 0.18f;
        [SerializeField] private Ease settleEase = Ease.OutSine;
        [SerializeField] private float settleShakeStrength = 0.8f;
        [SerializeField] private int settleShakeVibrato = 10;

        private readonly WheelStateRepository _stateRepository = new WheelStateRepository();
        private WheelConfigSnapshot _snapshot;
        private Sequence _spinSequence;
        private bool _isSpinning;
        private string _lastStatusText;

        public override void Init()
        {
            if (isInitialized)
            {
                return;
            }

            base.Init();
            BindButton(spinButton, HandleSpinClicked);
            LoadSnapshot();
            BindSegments();
            RefreshState();
        }

        public override void Show()
        {
            base.Show();
            LoadSnapshot();
            BindSegments();
            RefreshState();
        }

        public override void Hide()
        {
            base.Hide();
            KillSpinTween();
        }

        public void ResetFreeSpinCooldownForDev()
        {
            _stateRepository.ResetFreeSpinCooldown();
            _stateRepository.Reload();
            LoadSnapshot();
            BindSegments();
            RefreshState();
            SetStatus("Free spin restored!");
        }

        public void SetStatus(string status)
        {
            RefreshStatusText(status);
        }

        private void OnDisable()
        {
            KillSpinTween();
        }

        private void Update()
        {
            if (!_isSpinning)
            {
                RefreshCooldownTextOnly();
            }
        }

        private void LoadSnapshot()
        {
            string jsonError = null;
            if (jsonOverride != null && WheelJsonConfigProvider.TryCreateSnapshot(jsonOverride, boosterCatalog, out WheelConfigSnapshot jsonSnapshot, out jsonError))
            {
                _snapshot = jsonSnapshot;
                return;
            }

            if (jsonOverride != null && !string.IsNullOrWhiteSpace(jsonError))
            {
                Debug.LogWarning($"[WheelSubScreen] Failed to load JSON override: {jsonError}", this);
            }

            _snapshot = config != null ? config.CreateSnapshot() : null;
        }

        private void BindSegments()
        {
            if (_snapshot == null || _snapshot.Rewards.Count == 0)
            {
                return;
            }

            for (int i = 0; i < segmentViews.Count; i++)
            {
                WheelSegmentView view = segmentViews[i];
                if (view == null)
                {
                    continue;
                }

                WheelRewardEntryData reward = i < _snapshot.Rewards.Count ? _snapshot.Rewards[i] : null;
                view.Bind(reward);
            }
        }

        private void RefreshState()
        {
            bool canSpin = CanSpinNow(out string reason);
            if (spinButton != null)
            {
                spinButton.interactable = canSpin;
            }

            if (resultText != null && !canSpin && string.IsNullOrWhiteSpace(resultText.text))
            {
                resultText.SetText(string.Empty);
            }

            RefreshStatusText(reason);
        }

        private bool CanSpinNow(out string reason)
        {
            reason = string.Empty;

            if (_isSpinning)
            {
                reason = "Spinning...";
                return false;
            }

            if (_snapshot == null)
            {
                reason = "Wheel config is missing.";
                return false;
            }

            if (_snapshot.Rewards.Count == 0 || !_snapshot.HasSpinableReward())
            {
                reason = "Wheel has no valid rewards.";
                return false;
            }

            if (segmentViews.Count > 0 && segmentViews.Count != _snapshot.Rewards.Count)
            {
                reason = "Wheel item count does not match reward data.";
                return false;
            }

            double remainingSeconds = _stateRepository.GetRemainingCooldownSeconds(_snapshot.CooldownSeconds);
            if (remainingSeconds > 0)
            {
                reason = $"Free spin in {FormatTime(remainingSeconds)}";
                return false;
            }

            reason = "Free spin ready!";
            return true;
        }

        private void HandleSpinClicked()
        {
            if (!CanSpinNow(out string reason))
            {
                RefreshStatusText(reason);
                UIManager.Instance?.ShowToast(reason);
                return;
            }

            int rewardIndex = WheelSpinService.SelectRewardIndex(_snapshot.Rewards, UnityEngine.Random.value);
            if (rewardIndex < 0)
            {
                RefreshStatusText("Wheel has no valid rewards.");
                return;
            }

            WheelRewardEntryData reward = _snapshot.Rewards[rewardIndex];
            PlaySpin(rewardIndex, reward);
        }

        private void PlaySpin(int rewardIndex, WheelRewardEntryData reward)
        {
            if (wheelRoot == null || reward == null)
            {
                Debug.LogWarning("[WheelSubScreen] Missing wheel root or reward data.", this);
                return;
            }

            KillSpinTween();
            _isSpinning = true;
            if (spinButton != null)
            {
                spinButton.interactable = false;
            }

            if (resultText != null)
            {
                resultText.SetText(string.Empty);
            }

            RefreshStatusText("Spinning...");

            float targetAngle = WheelSpinService.CalculateTargetZAngle(
                rewardIndex,
                _snapshot.Rewards.Count,
                wheelRoot.localEulerAngles.z,
                _snapshot.MinimumFullTurns,
                _snapshot.SegmentLandingPaddingDegrees,
                UnityEngine.Random.value,
                pointerAngleDegrees,
                spinDirection);

            float startAngle = wheelRoot.localEulerAngles.z;
            float spinSign = Mathf.Sign(targetAngle - startAngle);
            float settleOffset = Mathf.Max(0f, settleOvershootDegrees) * (spinSign == 0f ? 1f : spinSign);
            float mainEndAngle = targetAngle + settleOffset;

            _spinSequence = DOTween.Sequence()
                .SetUpdate(useUnscaledTime)
                .SetLink(gameObject, LinkBehaviour.KillOnDisable);

            if (startPunchScale > 0f && startPunchDuration > 0f)
            {
                _spinSequence.Join(wheelRoot
                    .DOPunchScale(Vector3.one * startPunchScale, startPunchDuration, 6, 0.65f)
                    .SetUpdate(useUnscaledTime));
            }

            _spinSequence.Append(DOVirtual.Float(0f, 1f, _snapshot.SpinDuration, progress =>
                {
                    float angle = Mathf.LerpUnclamped(startAngle, mainEndAngle, EvaluateSpinProgress(progress));
                    wheelRoot.localRotation = Quaternion.Euler(0f, 0f, angle);
                })
                .SetEase(Ease.Linear));

            if (Mathf.Abs(settleOffset) > 0.01f && settleDuration > 0f)
            {
                _spinSequence.Append(DOVirtual.Float(mainEndAngle, targetAngle, settleDuration, angle =>
                    {
                        wheelRoot.localRotation = Quaternion.Euler(0f, 0f, angle);
                    })
                    .SetEase(settleEase));
            }

            if (settleShakeStrength > 0f && settleShakeVibrato > 0)
            {
                _spinSequence.Join(wheelRoot
                    .DOShakeRotation(Mathf.Max(0.05f, settleDuration), new Vector3(0f, 0f, settleShakeStrength), settleShakeVibrato, 60f, false)
                    .SetUpdate(useUnscaledTime));
            }

            _spinSequence.OnComplete(() => CompleteSpin(reward));
        }

        private void CompleteSpin(WheelRewardEntryData reward)
        {
            _isSpinning = false;
            bool granted = WheelRewardGrantService.TryGrant(reward);
            if (granted)
            {
                _stateRepository.MarkFreeSpinUsed();
                Debug.Log($"[WheelSubScreen] Reward granted: id='{reward.RewardId}', kind={reward.RewardKind}, amount={reward.Amount}, display='{reward.DisplayName}'.", this);
                if (resultText != null)
                {
                    resultText.SetText($"You got {reward.DisplayName}");
                }

                UIManager.Instance?.ShowToast($"You got {reward.DisplayName}!");
            }
            else
            {
                Debug.LogWarning($"[WheelSubScreen] Failed to grant reward: id='{reward.RewardId}', kind={reward.RewardKind}, amount={reward.Amount}, display='{reward.DisplayName}'.", this);
                UIManager.Instance?.ShowToast("Reward could not be granted.");
            }

            RefreshState();
        }

        private void RefreshCooldownTextOnly()
        {
            if (_snapshot == null)
            {
                return;
            }

            bool canSpin = CanSpinNow(out string reason);
            if (spinButton != null)
            {
                spinButton.interactable = canSpin;
            }

            RefreshStatusText(reason);
        }

        private void RefreshStatusText(string text)
        {
            if (statusText != null)
            {
                text ??= string.Empty;
                if (_lastStatusText == text)
                {
                    return;
                }

                _lastStatusText = text;
                statusText.SetText(text);
            }
        }

        private void KillSpinTween()
        {
            _spinSequence?.Kill();
            _spinSequence = null;
            _isSpinning = false;
        }

        private float EvaluateSpinProgress(float timeProgress)
        {
            float accelerationTime = Mathf.Clamp01(accelerationTimeRatio);
            float cruiseTime = Mathf.Clamp01(cruiseTimeRatio);
            if (accelerationTime + cruiseTime > 0.85f)
            {
                float scale = 0.85f / (accelerationTime + cruiseTime);
                accelerationTime *= scale;
                cruiseTime *= scale;
            }

            float accelerationDistance = Mathf.Clamp01(accelerationDistanceRatio);
            float cruiseDistance = Mathf.Clamp01(cruiseDistanceRatio);
            if (accelerationDistance + cruiseDistance > 0.85f)
            {
                float scale = 0.85f / (accelerationDistance + cruiseDistance);
                accelerationDistance *= scale;
                cruiseDistance *= scale;
            }

            float decelerationTime = Mathf.Max(0.0001f, 1f - accelerationTime - cruiseTime);
            float decelerationDistance = Mathf.Max(0f, 1f - accelerationDistance - cruiseDistance);
            float t = Mathf.Clamp01(timeProgress);

            if (t < accelerationTime)
            {
                float p = t / Mathf.Max(0.0001f, accelerationTime);
                return accelerationDistance * p * p * p;
            }

            if (t < accelerationTime + cruiseTime)
            {
                float p = (t - accelerationTime) / Mathf.Max(0.0001f, cruiseTime);
                return accelerationDistance + cruiseDistance * p;
            }

            float decelP = (t - accelerationTime - cruiseTime) / decelerationTime;
            float easedDecel = 1f - Mathf.Pow(1f - Mathf.Clamp01(decelP), 4f);
            return accelerationDistance + cruiseDistance + decelerationDistance * easedDecel;
        }

        private static string FormatTime(double seconds)
        {
            TimeSpan time = TimeSpan.FromSeconds(Math.Max(0, seconds));
            if (time.TotalHours >= 1)
            {
                return $"{(int)time.TotalHours:00}:{time.Minutes:00}:{time.Seconds:00}";
            }

            return $"{time.Minutes:00}:{time.Seconds:00}";
        }
    }
}
