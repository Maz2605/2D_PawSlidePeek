using UnityEngine;

namespace _PawSlidePopGame._Scripts.Data.Config
{
    [CreateAssetMenu(fileName = "AudioConfig", menuName = "PawSlidePop/Core/Audio Config")]
    public class PawSlidePopAudioConfig : ScriptableObject
    {
        public AudioSource backgroundMusic;
        public AudioSource foregroundMusic;
        
        public AudioSource sfxSlide;
        
    }
}