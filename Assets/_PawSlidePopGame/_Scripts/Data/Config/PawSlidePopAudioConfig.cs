using UnityEngine;

namespace _PawSlidePopGame._Scripts.Data.Config
{
    [System.Serializable]
    public sealed class PawSlidePopMusicLibrary
    {
        [Header("Background Music")]
        public AudioClip mainMenuBackground;
        public AudioClip gameplayBackground;
        public AudioClip resultWinBackground;
        public AudioClip resultLoseBackground;
    }

    [System.Serializable]
    public sealed class PawSlidePopMatch3SfxLibrary
    {
        [Header("Level Flow")]
        public AudioClip levelStart;
        public AudioClip levelWin;
        public AudioClip levelLose;

        [Header("Board Interaction")]
        public AudioClip swapSuccess;
        public AudioClip swapInvalid;
        public AudioClip tileSlide;
        public AudioClip tileDrop;

        [Header("Match Feedback")]
        public AudioClip matchBasic;
        public AudioClip matchBig;
        public AudioClip combo;
        public AudioClip targetCollected;
        public AudioClip objectiveCompleted;

        [Header("Special And Booster")]
        public AudioClip specialCreated;
        public AudioClip specialActivated;
        public AudioClip boosterSelect;
        public AudioClip boosterUse;
        public AudioClip boosterReject;
        public AudioClip shuffle;
        public AudioClip explosion;
    }

    [CreateAssetMenu(fileName = "AudioConfig", menuName = "PawSlidePop/Core/Audio Config")]
    public sealed class PawSlidePopAudioConfig : ScriptableObject
    {
        [Header("Music")]
        [SerializeField] private PawSlidePopMusicLibrary music = new PawSlidePopMusicLibrary();

        [Header("Match 3 SFX")]
        [SerializeField] private PawSlidePopMatch3SfxLibrary match3Sfx = new PawSlidePopMatch3SfxLibrary();

        public PawSlidePopMusicLibrary Music => music;
        public PawSlidePopMatch3SfxLibrary Match3Sfx => match3Sfx;
    }
}
