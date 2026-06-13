using _PawSlidePopGame._Scripts.Core.Boostrap;
using _PawSlidePopGame._Scripts.Core.System.DesignPattern.Singleton;
using UnityEngine;

namespace _PawSlidePopGame._Scripts.Core.Vibration
{
    public sealed class VibrationManager : Singleton<VibrationManager>, IAppService
    {
        [Header("Settings")]
        [SerializeField] private bool defaultEnabled = true;

        [Header("Runtime Debug")]
        [SerializeField] private bool vibrationEnabled = true;
        [SerializeField] private bool hasVibrator = true;

        private bool _initialized;
        private bool _capabilityResolved;

        public bool IsVibrationEnabled => vibrationEnabled;
        public bool CanVibrate => CanVibrateInternal(requireEnabled: true);

        public void Init()
        {
            EnsureInitialized();
        }

        public void SetVibrationEnabled(bool enabled)
        {
            EnsureInitialized();
            vibrationEnabled = enabled;
        }

        public void PlayButtonTap()
        {
            PlayLightImpact(requireEnabled: true);
        }

        public void PlayToggle()
        {
            PlayLightImpact(requireEnabled: true);
        }

        public void PlayTogglePreview()
        {
            PlayLightImpact(requireEnabled: false);
        }

        public void PlayMoveSuccess()
        {
            PlayMediumImpact(requireEnabled: true);
        }

        public void PlayReject()
        {
            if (!CanVibrateInternal(requireEnabled: true))
            {
                return;
            }

            if (Application.platform == RuntimePlatform.IPhonePlayer)
            {
                global::Vibration.VibrateIOS(global::NotificationFeedbackStyle.Warning);
                return;
            }

            if (Application.platform == RuntimePlatform.Android)
            {
                _ = global::Vibration.VibrateNope();
                return;
            }

            global::Vibration.Vibrate();
        }

        public void PlayBoosterSelect()
        {
            PlayMediumImpact(requireEnabled: true);
        }

        public void PlayWin()
        {
            if (!CanVibrateInternal(requireEnabled: true))
            {
                return;
            }

            if (Application.platform == RuntimePlatform.IPhonePlayer)
            {
                global::Vibration.VibrateIOS(global::NotificationFeedbackStyle.Success);
                return;
            }

            if (Application.platform == RuntimePlatform.Android)
            {
                global::Vibration.VibratePeek();
                return;
            }

            global::Vibration.Vibrate();
        }

        public void PlayLose()
        {
            if (!CanVibrateInternal(requireEnabled: true))
            {
                return;
            }

            if (Application.platform == RuntimePlatform.IPhonePlayer)
            {
                global::Vibration.VibrateIOS(global::NotificationFeedbackStyle.Error);
                return;
            }

            if (Application.platform == RuntimePlatform.Android)
            {
                _ = global::Vibration.VibrateNope();
                return;
            }

            global::Vibration.Vibrate();
        }

        private void EnsureInitialized()
        {
            if (_initialized)
            {
                return;
            }

            vibrationEnabled = defaultEnabled;
            global::Vibration.Init();
            RefreshCapabilityCache();
            _initialized = true;
        }

        public void PlayLightImpact(bool requireEnabled = true)
        {
            if (!CanVibrateInternal(requireEnabled))
            {
                return;
            }

            if (Application.platform == RuntimePlatform.IPhonePlayer)
            {
                global::Vibration.VibrateIOS_SelectionChanged();
                return;
            }

            if (Application.platform == RuntimePlatform.Android)
            {
                global::Vibration.VibratePop();
                return;
            }

            global::Vibration.Vibrate();
        }

        public void PlayMediumImpact(bool requireEnabled = true)
        {
            if (!CanVibrateInternal(requireEnabled))
            {
                return;
            }

            if (Application.platform == RuntimePlatform.IPhonePlayer)
            {
                global::Vibration.VibrateIOS(global::ImpactFeedbackStyle.Medium);
                return;
            }

            if (Application.platform == RuntimePlatform.Android)
            {
                global::Vibration.VibratePeek();
                return;
            }

            global::Vibration.Vibrate();
        }

        private bool CanVibrateInternal(bool requireEnabled)
        {
            EnsureInitialized();

            if (!Application.isMobilePlatform)
            {
                return false;
            }

            if (requireEnabled && !vibrationEnabled)
            {
                return false;
            }

            return ResolveHasVibrator();
        }

        private bool ResolveHasVibrator()
        {
            if (_capabilityResolved)
            {
                return hasVibrator;
            }

            RefreshCapabilityCache();
            return hasVibrator;
        }

        private void RefreshCapabilityCache()
        {
            _capabilityResolved = true;

            if (!Application.isMobilePlatform)
            {
                hasVibrator = false;
                return;
            }

            try
            {
                hasVibrator = global::Vibration.HasVibrator();
            }
            catch
            {
                hasVibrator = false;
            }
        }

        private void OnApplicationPause(bool pauseStatus)
        {
            if (!_initialized)
            {
                return;
            }

            global::Vibration.SetActive(!pauseStatus);
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            if (!_initialized)
            {
                return;
            }

            global::Vibration.SetActive(hasFocus);
        }
    }
}
