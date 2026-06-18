using _PawSlidePopGame._Scripts.Core.Audio;
using _PawSlidePopGame._Scripts.Core.Vibration;
using UnityEngine;
using UnityEngine.EventSystems;

namespace _PawSlidePopGame._Scripts.Data.Audio
{
    public class UIButtonSound : MonoBehaviour, IPointerClickHandler
    {
        public UISoundType soundType = UISoundType.ClickNormal;
        
        public AudioClip customClip; 
        [Range(0f, 1f)] public float volumeScale = 1f;

        [Header("Vibration")]
        public bool playVibration = true;

        public void OnPointerClick(PointerEventData eventData)
        {
            PlaySound();
        }

        public void PlaySound()
        {
            if (AudioManager.Instance == null)
            {
                return;
            }

            if (soundType == UISoundType.Custom)
            {
                if (customClip != null) 
                {
                    AudioManager.Instance.PlaySfx(customClip, volumeScale);
                }
            }
            else
            {
                AudioManager.Instance.PlayUISound(soundType);
            }

            if (playVibration && VibrationManager.Instance != null)
            {
                VibrationManager.Instance.PlayButtonTap();
            }
        }
    }
}