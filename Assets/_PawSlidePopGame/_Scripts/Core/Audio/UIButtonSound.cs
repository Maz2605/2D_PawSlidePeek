using _PawSlidePopGame._Scripts.Core.Audio;
using UnityEngine;
using UnityEngine.EventSystems;

namespace _PawSlidePopGame._Scripts.Data.Audio
{
    public class UIButtonSound : MonoBehaviour, IPointerClickHandler
    {
        public UISoundType soundType = UISoundType.ClickNormal;
        

        public AudioClip customClip; 
        [Range(0f, 1f)] public float volumeScale = 1f;

        public void OnPointerClick(PointerEventData eventData)
        {
            PlaySound();
        }

        public void PlaySound()
        {
            if (AudioManager.Instance == null) return;

            if (soundType == UISoundType.Custom)
            {
                // Logic Custom: Play file riêng
                if (customClip != null) 
                    AudioManager.Instance.PlaySfx(customClip, volumeScale);
            }
            else
            {
                // Logic Standard: Gọi qua Enum
                AudioManager.Instance.PlayUISound(soundType);
            }
        }
    }
}