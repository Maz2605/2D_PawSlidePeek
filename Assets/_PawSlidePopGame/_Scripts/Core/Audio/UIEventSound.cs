using _PawSlidePopGame._Scripts.Data.Audio;
using UnityEngine;

namespace _PawSlidePopGame._Scripts.Core.Audio
{
    public class UIEventSound : MonoBehaviour
    {
        [SerializeField] private UISoundType openSound = UISoundType.None;
        [SerializeField] private UISoundType closeSound = UISoundType.None;

        [SerializeField] private AudioClip customOpenClip;
        [SerializeField] private AudioClip customCloseClip;

        private void OnEnable()
        {
            PlaySound(openSound, customOpenClip);
        }

        private void OnDisable()
        {
            if (gameObject.scene.isLoaded) 
            {
                PlaySound(closeSound, customCloseClip);
            }
        }

        private void PlaySound(UISoundType type, AudioClip customClip)
        {
            if (AudioManager.Instance == null) return;

            if (customClip != null)
            {
                AudioManager.Instance.PlaySfx(customClip);
                return;
            }

            if (type != UISoundType.None)
            {
                AudioManager.Instance.PlayUISound(type);
            }
        }
    }
}