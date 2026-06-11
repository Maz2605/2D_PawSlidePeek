using _PawSlidePopGame._Scripts.Core.System.GameFlow;
using _PawSlidePopGame._Scripts.Data.Config;
using _PawSlidePopGame._Scripts.Data.Events;
using _PawSlidePopGame._Scripts.Data.Events.Payloads;
using _PawSlidePopGame._Scripts.Feature.Match3.Core.Enum;
using _PawSlidePopGame._Scripts.Feature.Match3.Boosters;
using _PawSlidePopGame._Scripts.Feature.Match3.Presentation;
using _PawSlidePopGame.Scripts.DesignPattern.ObserverPattern;
using UnityEngine;

namespace _PawSlidePopGame._Scripts.Core.Audio
{
    [DisallowMultipleComponent]
    public sealed class AudioController : MonoBehaviour
    {
        [Header("Config")]
        [SerializeField] private PawSlidePopAudioConfig audioConfig;

        [Header("Runtime")]
        [SerializeField] private bool playMusicOnEnable = true;
        [SerializeField] private bool dontDestroyOnLoad = true;

        [Header("Match 3 Tuning")]
        [SerializeField] private float matchSfxCooldown = 0.06f;
        [SerializeField] private int bigMatchScoreThreshold = 50;
        [SerializeField] private int comboScoreThreshold = 90;

        private bool _eventsBound;
        private float _lastMatchSfxTime = -999f;

        private void Awake()
        {
            if (dontDestroyOnLoad)
            {
                DontDestroyOnLoad(gameObject);
            }
        }

        private void OnEnable()
        {
            if (_eventsBound)
            {
                return;
            }

            EventManager<LogicGameEvent>.AddListener<GameAppStateChangedPayload>(
                LogicGameEvent.GameAppStateChanged,
                HandleGameAppStateChanged);
            EventManager<LogicGameEvent>.AddListener<GameplayHudSnapshot>(
                LogicGameEvent.GameplayWon,
                HandleGameplayWon);
            EventManager<LogicGameEvent>.AddListener<GameplayHudSnapshot>(
                LogicGameEvent.GameplayLost,
                HandleGameplayLost);
            EventManager<LogicGameEvent>.AddListener<ScoreChangedPayload>(
                LogicGameEvent.GameplayScoreChanged,
                HandleGameplayScoreChanged);
            EventManager<FeedbackEvent>.AddListener(
                FeedbackEvent.UiButtonTap,
                HandleUiButtonTap);
            EventManager<FeedbackEvent>.AddListener(
                FeedbackEvent.MoveSuccess,
                HandleMoveSuccessFeedback);
            EventManager<FeedbackEvent>.AddListener(
                FeedbackEvent.MoveReject,
                HandleMoveRejectFeedback);
            EventManager<FeedbackEvent>.AddListener(
                FeedbackEvent.BoosterSelect,
                HandleBoosterSelectFeedback);
            EventManager<FeedbackEvent>.AddListener(
                FeedbackEvent.BoosterReject,
                HandleBoosterRejectFeedback);
            EventManager<FeedbackEvent>.AddListener<BoosterFeedbackPayload>(
                FeedbackEvent.BoosterUse,
                HandleBoosterUseFeedback);
            EventManager<FeedbackEvent>.AddListener<SpecialTileFeedbackPayload>(
                FeedbackEvent.SpecialTileActivated,
                HandleSpecialTileActivatedFeedback);
            EventManager<FeedbackEvent>.AddListener<SpecialTileFeedbackPayload>(
                FeedbackEvent.SpecialTileCreated,
                HandleSpecialTileCreatedFeedback);
            EventManager<VisualGameEvent>.AddListener<TargetCollectedFxPayload>(
                VisualGameEvent.TopHudTargetCollectedFx,
                HandleTargetCollectedFx);
            EventManager<VisualGameEvent>.AddListener(
                VisualGameEvent.TopHudTargetsCompletedFx,
                HandleTargetsCompletedFx);

            _eventsBound = true;

            if (playMusicOnEnable)
            {
                SyncCurrentMusicState();
            }
        }

        private void OnDisable()
        {
            if (!_eventsBound)
            {
                return;
            }

            EventManager<LogicGameEvent>.RemoveListener<GameAppStateChangedPayload>(
                LogicGameEvent.GameAppStateChanged,
                HandleGameAppStateChanged);
            EventManager<LogicGameEvent>.RemoveListener<GameplayHudSnapshot>(
                LogicGameEvent.GameplayWon,
                HandleGameplayWon);
            EventManager<LogicGameEvent>.RemoveListener<GameplayHudSnapshot>(
                LogicGameEvent.GameplayLost,
                HandleGameplayLost);
            EventManager<LogicGameEvent>.RemoveListener<ScoreChangedPayload>(
                LogicGameEvent.GameplayScoreChanged,
                HandleGameplayScoreChanged);
            EventManager<FeedbackEvent>.RemoveListener(
                FeedbackEvent.UiButtonTap,
                HandleUiButtonTap);
            EventManager<FeedbackEvent>.RemoveListener(
                FeedbackEvent.MoveSuccess,
                HandleMoveSuccessFeedback);
            EventManager<FeedbackEvent>.RemoveListener(
                FeedbackEvent.MoveReject,
                HandleMoveRejectFeedback);
            EventManager<FeedbackEvent>.RemoveListener(
                FeedbackEvent.BoosterSelect,
                HandleBoosterSelectFeedback);
            EventManager<FeedbackEvent>.RemoveListener(
                FeedbackEvent.BoosterReject,
                HandleBoosterRejectFeedback);
            EventManager<FeedbackEvent>.RemoveListener<BoosterFeedbackPayload>(
                FeedbackEvent.BoosterUse,
                HandleBoosterUseFeedback);
            EventManager<FeedbackEvent>.RemoveListener<SpecialTileFeedbackPayload>(
                FeedbackEvent.SpecialTileActivated,
                HandleSpecialTileActivatedFeedback);
            EventManager<FeedbackEvent>.RemoveListener<SpecialTileFeedbackPayload>(
                FeedbackEvent.SpecialTileCreated,
                HandleSpecialTileCreatedFeedback);
            EventManager<VisualGameEvent>.RemoveListener<TargetCollectedFxPayload>(
                VisualGameEvent.TopHudTargetCollectedFx,
                HandleTargetCollectedFx);
            EventManager<VisualGameEvent>.RemoveListener(
                VisualGameEvent.TopHudTargetsCompletedFx,
                HandleTargetsCompletedFx);
            _eventsBound = false;
        }

        public void PlayButtonTap()
        {
            AudioManager.Instance?.PlayUISound(UISoundType.ClickNormal);
        }

        public void PlayBackButton()
        {
            AudioManager.Instance?.PlayUISound(UISoundType.ClickBack);
        }

        public void PlayConfirmButton()
        {
            AudioManager.Instance?.PlayUISound(UISoundType.ClickConfirm);
        }

        public void PlayCancelButton()
        {
            AudioManager.Instance?.PlayUISound(UISoundType.ClickCancel);
        }

        public void PlayMainMenuMusic()
        {
            PlayMusic(audioConfig != null ? audioConfig.Music.mainMenuBackground : null);
        }

        public void PlayGameplayMusic()
        {
            PlayMusic(audioConfig != null ? audioConfig.Music.gameplayBackground : null);
        }

        public void PlayWinMusic()
        {
            PlayMusic(audioConfig != null ? audioConfig.Music.resultWinBackground : null);
        }

        public void PlayLoseMusic()
        {
            PlayMusic(audioConfig != null ? audioConfig.Music.resultLoseBackground : null);
        }

        public void PlayLevelStart()
        {
            PlaySfx(audioConfig != null ? audioConfig.Match3Sfx.levelStart : null);
        }

        public void PlayMoveSuccess()
        {
            PlaySfx(audioConfig != null ? audioConfig.Match3Sfx.swapSuccess : null);
        }

        public void PlayMoveReject()
        {
            PlaySfx(audioConfig != null ? audioConfig.Match3Sfx.swapInvalid : null);
        }

        public void PlayTileSlide()
        {
            PlaySfx(audioConfig != null ? audioConfig.Match3Sfx.tileSlide : null);
        }

        public void PlayTileDrop()
        {
            PlaySfx(audioConfig != null ? audioConfig.Match3Sfx.tileDrop : null);
        }

        public void PlayMatchBasic()
        {
            PlaySfx(audioConfig != null ? audioConfig.Match3Sfx.matchBasic : null);
        }

        public void PlayMatchBig()
        {
            PlaySfx(audioConfig != null ? audioConfig.Match3Sfx.matchBig : null);
        }

        public void PlayBoosterSelect()
        {
            PlaySfx(audioConfig != null ? audioConfig.Match3Sfx.boosterSelect : null);
        }

        public void PlayBoosterReject()
        {
            PlaySfx(audioConfig != null ? audioConfig.Match3Sfx.boosterReject : null);
        }

        public void PlayBoosterUse(BoosterType boosterType = BoosterType.None)
        {
            if (audioConfig == null)
            {
                return;
            }

            AudioClip clip = boosterType == BoosterType.Shuffle
                ? audioConfig.Match3Sfx.shuffle
                : audioConfig.Match3Sfx.boosterUse;

            PlaySfx(clip);
        }

        public void PlaySpecialCreated()
        {
            PlaySfx(audioConfig != null ? audioConfig.Match3Sfx.specialCreated : null);
        }

        public void PlaySpecialActivated()
        {
            PlaySfx(audioConfig != null ? audioConfig.Match3Sfx.specialActivated : null);
        }

        public void PlayExplosion()
        {
            PlaySfx(audioConfig != null ? audioConfig.Match3Sfx.explosion : null);
        }

        public void PlayTargetCollected()
        {
            PlaySfx(audioConfig != null ? audioConfig.Match3Sfx.targetCollected : null);
        }

        public void PlayObjectiveCompleted()
        {
            PlaySfx(audioConfig != null ? audioConfig.Match3Sfx.objectiveCompleted : null);
        }

        public void PlayCombo()
        {
            PlaySfx(audioConfig != null ? audioConfig.Match3Sfx.combo : null);
        }

        public void PlayLevelWin()
        {
            PlaySfx(audioConfig != null ? audioConfig.Match3Sfx.levelWin : null);
        }

        public void PlayLevelLose()
        {
            PlaySfx(audioConfig != null ? audioConfig.Match3Sfx.levelLose : null);
        }

        public void HandleTileActivated(TileLogicType logicType)
        {
            if (!IsSpecialLogicType(logicType))
            {
                return;
            }

            if (IsExplosiveLogicType(logicType))
            {
                PlayExplosion();
                return;
            }

            PlaySpecialActivated();
        }

        public void HandleSpecialCreated(TileLogicType logicType)
        {
            if (!IsSpecialLogicType(logicType))
            {
                return;
            }

            PlaySpecialCreated();
        }

        private void HandleGameAppStateChanged(GameAppStateChangedPayload payload)
        {
            switch (payload.Current)
            {
                case GameAppState.MainMenu:
                    PlayMainMenuMusic();
                    break;
                case GameAppState.EnteringGameplay:
                    PlayGameplayMusic();
                    break;
                case GameAppState.Gameplay:
                    if (payload.Previous == GameAppState.EnteringGameplay)
                    {
                        PlayLevelStart();
                    }
                    break;
            }
        }

        private void HandleGameplayWon(GameplayHudSnapshot _)
        {
            PlayWinMusic();
            PlayLevelWin();
        }

        private void HandleGameplayLost(GameplayHudSnapshot _)
        {
            PlayLoseMusic();
            PlayLevelLose();
        }

        private void HandleUiButtonTap()
        {
            PlayButtonTap();
        }

        private void HandleMoveSuccessFeedback()
        {
            PlayMoveSuccess();
        }

        private void HandleMoveRejectFeedback()
        {
            PlayMoveReject();
        }

        private void HandleBoosterSelectFeedback()
        {
            PlayBoosterSelect();
        }

        private void HandleBoosterRejectFeedback()
        {
            PlayBoosterReject();
        }

        private void HandleBoosterUseFeedback(BoosterFeedbackPayload payload)
        {
            PlayBoosterUse(payload.BoosterType);
        }

        private void HandleSpecialTileActivatedFeedback(SpecialTileFeedbackPayload payload)
        {
            HandleTileActivated(payload.LogicType);
        }

        private void HandleSpecialTileCreatedFeedback(SpecialTileFeedbackPayload payload)
        {
            HandleSpecialCreated(payload.LogicType);
        }

        private void HandleGameplayScoreChanged(ScoreChangedPayload payload)
        {
            if (payload.Delta <= 0 || !CanPlayMatchSfx())
            {
                return;
            }

            if (payload.Delta >= comboScoreThreshold)
            {
                PlayCombo();
            }
            else if (payload.Delta >= bigMatchScoreThreshold)
            {
                PlayMatchBig();
            }
            else
            {
                PlayMatchBasic();
            }
        }

        private void HandleTargetCollectedFx(TargetCollectedFxPayload _)
        {
            PlayTargetCollected();
        }

        private void HandleTargetsCompletedFx()
        {
            PlayObjectiveCompleted();
        }

        private static void PlayMusic(AudioClip clip)
        {
            if (clip == null)
            {
                return;
            }

            AudioManager.Instance?.PlayMusic(clip);
        }

        private static void PlaySfx(AudioClip clip, float volumeScale = 1f, float pitchVariation = 0f)
        {
            if (clip == null)
            {
                return;
            }

            AudioManager.Instance?.PlaySfx(clip, volumeScale, pitchVariation);
        }

        private bool CanPlayMatchSfx()
        {
            float now = Time.unscaledTime;
            if (now - _lastMatchSfxTime < matchSfxCooldown)
            {
                return false;
            }

            _lastMatchSfxTime = now;
            return true;
        }

        private static bool IsSpecialLogicType(TileLogicType logicType)
        {
            return logicType != TileLogicType.None && logicType != TileLogicType.NormalAnimal;
        }

        private static bool IsExplosiveLogicType(TileLogicType logicType)
        {
            switch (logicType)
            {
                case TileLogicType.BombBooster:
                case TileLogicType.CrossBomb:
                case TileLogicType.SquareBomb:
                case TileLogicType.AreaBombMedium:
                case TileLogicType.AreaBombLarge:
                case TileLogicType.ChargedSweepBooster:
                case TileLogicType.BoardClearBomb:
                    return true;
                default:
                    return false;
            }
        }

        private void SyncCurrentMusicState()
        {
            GameAppFlowManager flowManager = FindFirstObjectByType<GameAppFlowManager>(FindObjectsInactive.Include);
            if (flowManager == null)
            {
                PlayMainMenuMusic();
                return;
            }

            switch (flowManager.CurrentAppState)
            {
                case GameAppState.EnteringGameplay:
                case GameAppState.Gameplay:
                    PlayGameplayMusic();
                    break;
                case GameAppState.MainMenu:
                case GameAppState.Bootstrapping:
                case GameAppState.None:
                default:
                    PlayMainMenuMusic();
                    break;
            }
        }
    }
}
