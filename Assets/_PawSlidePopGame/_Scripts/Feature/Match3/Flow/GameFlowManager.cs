using System.Collections.Generic;
using _PawSlidePopGame._Scripts.Core.System.DesignPattern.Singleton;
using _PawSlidePopGame._Scripts.Core.System.GameFlow;
using _PawSlidePopGame._Scripts.Data.Events;
using _PawSlidePopGame._Scripts.Data.Events.Payloads;
using _PawSlidePopGame._Scripts.Feature.Match3.Logic.Move;
using _PawSlidePopGame._Scripts.Feature.Match3.Presentation;
using _PawSlidePopGame._Scripts.Feature.Match3.Presenter;
using _PawSlidePopGame._Scripts.UI.Manager;
using _PawSlidePopGame._Scripts.UI.Screens;
using _PawSlidePopGame._Scripts.UI.Screens.Gameplay;
using _PawSlidePopGame.Scripts.DesignPattern.ObserverPattern;
using UnityEngine;

namespace _PawSlidePopGame._Scripts.Feature.Match3.Flow
{
    [DisallowMultipleComponent]
    public class GameFlowManager : Singleton<GameFlowManager>
    {
        [SerializeField] private Match3GameManager gameManager;
        [SerializeField] private bool autoStartFlow = true;

        private Match3ObjectiveTracker _objectiveTracker;
        private GameplayHudSnapshot _lastSnapshot;
        private InGameSubState _resumeSubState = InGameSubState.PlayerTurn;

        public GameState CurrentGameState { get; private set; } = GameState.None;
        public InGameSubState CurrentInGameSubState { get; private set; } = InGameSubState.None;
        public GameplayHudSnapshot LastSnapshot => _lastSnapshot;

        public bool CanAcceptGameplayCommands =>
            CurrentGameState == GameState.Gameplay &&
            CurrentInGameSubState == InGameSubState.PlayerTurn &&
            gameManager != null &&
            gameManager.IsInitialized;

        protected override void Awake()
        {
            DontDestroyOnLoadEnabled = false;
            base.Awake();

            if (gameManager == null)
            {
                gameManager = GetComponent<Match3GameManager>();
            }
        }

        private void Start()
        {
            if (autoStartFlow)
            {
                EnterGameplay();
            }
        }

        public void EnterGameplay()
        {
            if (gameManager == null)
            {
                Debug.LogError("[GameFlowManager] Missing Match3GameManager reference.", this);
                return;
            }

            SetGameState(GameState.Gameplay);
            SetInGameSubState(InGameSubState.Bootstrapping);
            UIManager.Instance?.ShowScreen<GameplayScreen>(ScreenID.Gameplay);
            BeginBoardPreparation();
        }

        public void BeginBoardPreparation()
        {
            if (gameManager == null)
            {
                return;
            }

            SetInGameSubState(InGameSubState.PreparingBoard);
            gameManager.InitializeGame();
            _objectiveTracker = new Match3ObjectiveTracker(gameManager.LevelData, gameManager.TileDatabase);
            PublishHudInitialized();
            EnterPlayerTurn();
        }

        public BoardMoveExecutionResult RequestMove(BoardMoveRequest request)
        {
            if (!CanAcceptGameplayCommands)
            {
                return new BoardMoveExecutionResult();
            }

            SetInGameSubState(InGameSubState.ResolvingBoard);
            int previousMoves = _lastSnapshot != null ? _lastSnapshot.remainingMoves : gameManager.Board.RemainingMoves;
            BoardMoveExecutionResult executionResult = gameManager.ExecuteMove(request);
            if (executionResult.IsAccepted)
            {
                PublishMovesChanged(previousMoves, gameManager.Board.RemainingMoves);
            }

            return executionResult;
        }

        public BoardMoveExecutionResult RequestTileActivation(int x, int y)
        {
            if (!CanAcceptGameplayCommands)
            {
                return new BoardMoveExecutionResult();
            }

            SetInGameSubState(InGameSubState.ResolvingBoard);
            int previousMoves = _lastSnapshot != null ? _lastSnapshot.remainingMoves : gameManager.Board.RemainingMoves;
            BoardMoveExecutionResult executionResult = gameManager.ExecuteTileActivation(x, y);
            if (executionResult.IsAccepted)
            {
                PublishMovesChanged(previousMoves, gameManager.Board.RemainingMoves);
            }

            return executionResult;
        }

        public void NotifyTileClearedDuringPlayback(TileClearOp clearOp, Vector3 worldPosition)
        {
            if (CurrentInGameSubState != InGameSubState.ResolvingBoard || clearOp == null || _objectiveTracker == null || _lastSnapshot == null)
            {
                return;
            }

            if (!_objectiveTracker.TryApplyClearOp(clearOp, out TargetProgressChangedPayload targetChange))
            {
                return;
            }

            UpdateSnapshotTargetProgress(targetChange);
            EventManager<VisualGameEvent>.Post(VisualGameEvent.TopHudTargetProgressFx, targetChange);
            EventManager<VisualGameEvent>.Post(
                VisualGameEvent.TopHudTargetCollectedFx,
                new TargetCollectedFxPayload(
                    clearOp.TileId,
                    clearOp.TileInstanceId,
                    clearOp.Cell,
                    worldPosition,
                    targetChange.PreviousCount,
                    targetChange.CurrentCount,
                    targetChange.RequiredCount,
                    targetChange.JustCompleted));

            if (_objectiveTracker.TryConsumeTargetsCompletedFx())
            {
                EventManager<VisualGameEvent>.Post(VisualGameEvent.TopHudTargetsCompletedFx);
            }
        }

        public void NotifyScoreGainedDuringPlayback(ScoreGainOp scoreGainOp)
        {
            if (CurrentInGameSubState != InGameSubState.ResolvingBoard || scoreGainOp == null || _lastSnapshot == null)
            {
                return;
            }

            int previousScore = _lastSnapshot.currentScore;
            if (scoreGainOp.RunningScore <= previousScore)
            {
                return;
            }

            _lastSnapshot.currentScore = scoreGainOp.RunningScore;
            EventManager<LogicGameEvent>.Post(
                LogicGameEvent.GameplayScoreChanged,
                new ScoreChangedPayload(previousScore, _lastSnapshot.currentScore, scoreGainOp.Amount));

            int previousStars = _lastSnapshot.reachedStars;
            int currentStars = GameplayHudSnapshotBuilder.CountReachedStars(_lastSnapshot.currentScore, _lastSnapshot.starScoreThresholds);
            _lastSnapshot.reachedStars = currentStars;

            for (int starIndex = previousStars + 1; starIndex <= currentStars; starIndex++)
            {
                EventManager<VisualGameEvent>.Post(
                    VisualGameEvent.TopHudStarReachedFx,
                    new StarReachedPayload(starIndex, _lastSnapshot.currentScore));
            }
        }

        public void NotifyResolutionPlaybackComplete(BoardMoveExecutionResult executionResult)
        {
            if (CurrentInGameSubState != InGameSubState.ResolvingBoard)
            {
                return;
            }

            ReconcileRuntimeSnapshot();
            SetInGameSubState(InGameSubState.CheckingResult);
            EvaluateBoardResult();
        }

        public void PauseGameplay()
        {
            if (CurrentGameState != GameState.Gameplay || CurrentInGameSubState == InGameSubState.Paused)
            {
                return;
            }

            _resumeSubState = CurrentInGameSubState == InGameSubState.None
                ? InGameSubState.PlayerTurn
                : CurrentInGameSubState;

            SetInGameSubState(InGameSubState.Paused);
            EventManager<VisualGameEvent>.Post(VisualGameEvent.GameplayPausedFx);
        }

        public void ResumeGameplay()
        {
            if (CurrentInGameSubState != InGameSubState.Paused)
            {
                return;
            }

            InGameSubState targetState = _resumeSubState == InGameSubState.None
                ? InGameSubState.PlayerTurn
                : _resumeSubState;

            SetInGameSubState(targetState);
            EventManager<VisualGameEvent>.Post(VisualGameEvent.GameplayResumedFx);
        }

        public void EnterVictory()
        {
            SetGameState(GameState.Result);
            SetInGameSubState(InGameSubState.Victory);
            EventManager<LogicGameEvent>.Post(LogicGameEvent.GameplayWon, _lastSnapshot?.Clone());
        }

        public void EnterDefeat()
        {
            SetGameState(GameState.Result);
            SetInGameSubState(InGameSubState.Defeat);
            EventManager<LogicGameEvent>.Post(LogicGameEvent.GameplayLost, _lastSnapshot?.Clone());
        }

        private void EnterPlayerTurn()
        {
            SetInGameSubState(InGameSubState.PlayerTurn);
        }

        private void EvaluateBoardResult()
        {
            if (_objectiveTracker != null && _objectiveTracker.AreAllTargetsComplete)
            {
                EnterVictory();
                return;
            }

            if (gameManager?.Board != null && gameManager.Board.RemainingMoves <= 0)
            {
                EnterDefeat();
                return;
            }

            EnterPlayerTurn();
        }

        private void PublishHudInitialized()
        {
            _lastSnapshot = GameplayHudSnapshotBuilder.Build(gameManager.LevelData, gameManager.Board, _objectiveTracker);
            EventManager<LogicGameEvent>.Post(LogicGameEvent.GameplayHudInitialized, _lastSnapshot.Clone());
        }

        private bool SetGameState(GameState nextState)
        {
            if (CurrentGameState == nextState)
            {
                return false;
            }

            if (!IsValidGameStateTransition(CurrentGameState, nextState))
            {
                Debug.LogWarning($"[GameFlowManager] Invalid GameState transition {CurrentGameState} -> {nextState}.", this);
                return false;
            }

            GameState previous = CurrentGameState;
            CurrentGameState = nextState;
            EventManager<LogicGameEvent>.Post(
                LogicGameEvent.GameStateChanged,
                new GameStateChangedPayload(previous, CurrentGameState));
            return true;
        }

        private bool SetInGameSubState(InGameSubState nextState)
        {
            if (CurrentInGameSubState == nextState)
            {
                return false;
            }

            if (!IsValidInGameSubStateTransition(CurrentInGameSubState, nextState))
            {
                Debug.LogWarning($"[GameFlowManager] Invalid InGameSubState transition {CurrentInGameSubState} -> {nextState}.", this);
                return false;
            }

            InGameSubState previous = CurrentInGameSubState;
            CurrentInGameSubState = nextState;
            EventManager<LogicGameEvent>.Post(
                LogicGameEvent.InGameSubStateChanged,
                new InGameSubStateChangedPayload(previous, CurrentInGameSubState));
            return true;
        }

        private static bool IsValidGameStateTransition(GameState currentState, GameState nextState)
        {
            switch (currentState)
            {
                case GameState.None:
                    return nextState == GameState.Bootstrapping || nextState == GameState.Gameplay || nextState == GameState.LoadingScene;
                case GameState.Bootstrapping:
                    return nextState == GameState.LoadingScene || nextState == GameState.Gameplay;
                case GameState.LoadingScene:
                    return nextState == GameState.Gameplay || nextState == GameState.Result;
                case GameState.Gameplay:
                    return nextState == GameState.Gameplay || nextState == GameState.Result || nextState == GameState.LoadingScene;
                case GameState.Result:
                    return nextState == GameState.LoadingScene || nextState == GameState.Gameplay;
                case GameState.Paused:
                    return nextState == GameState.Gameplay || nextState == GameState.Result;
                default:
                    return false;
            }
        }

        private static bool IsValidInGameSubStateTransition(InGameSubState currentState, InGameSubState nextState)
        {
            switch (currentState)
            {
                case InGameSubState.None:
                    return nextState == InGameSubState.Bootstrapping;
                case InGameSubState.Bootstrapping:
                    return nextState == InGameSubState.PreparingBoard || nextState == InGameSubState.Paused;
                case InGameSubState.PreparingBoard:
                    return nextState == InGameSubState.PlayerTurn || nextState == InGameSubState.Defeat || nextState == InGameSubState.Paused;
                case InGameSubState.PlayerTurn:
                    return nextState == InGameSubState.ResolvingBoard || nextState == InGameSubState.Paused || nextState == InGameSubState.Victory || nextState == InGameSubState.Defeat;
                case InGameSubState.ResolvingBoard:
                    return nextState == InGameSubState.CheckingResult || nextState == InGameSubState.Paused;
                case InGameSubState.CheckingResult:
                    return nextState == InGameSubState.PlayerTurn || nextState == InGameSubState.Victory || nextState == InGameSubState.Defeat || nextState == InGameSubState.Paused;
                case InGameSubState.Victory:
                case InGameSubState.Defeat:
                    return nextState == InGameSubState.Bootstrapping;
                case InGameSubState.Paused:
                    return nextState != InGameSubState.None;
                default:
                    return false;
            }
        }

        private void PublishMovesChanged(int previousMoves, int currentMoves)
        {
            if (_lastSnapshot == null || previousMoves == currentMoves)
            {
                return;
            }

            _lastSnapshot.remainingMoves = currentMoves;
            EventManager<LogicGameEvent>.Post(
                LogicGameEvent.GameplayMovesChanged,
                new RemainingMovesChangedPayload(previousMoves, currentMoves));
        }

        private void UpdateSnapshotTargetProgress(TargetProgressChangedPayload payload)
        {
            if (_lastSnapshot?.targets == null)
            {
                return;
            }

            for (int i = 0; i < _lastSnapshot.targets.Count; i++)
            {
                TargetProgressData target = _lastSnapshot.targets[i];
                if (target == null || target.tileId != payload.TileId)
                {
                    continue;
                }

                target.currentCount = payload.CurrentCount;
                target.requiredCount = payload.RequiredCount;
                target.isCompleted = payload.CurrentCount >= payload.RequiredCount;
                return;
            }
        }

        private void ReconcileRuntimeSnapshot()
        {
            if (gameManager == null)
            {
                return;
            }

            _lastSnapshot = GameplayHudSnapshotBuilder.Build(gameManager.LevelData, gameManager.Board, _objectiveTracker);
        }
    }
}
