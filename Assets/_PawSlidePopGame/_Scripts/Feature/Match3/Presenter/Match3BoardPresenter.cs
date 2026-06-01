using _PawSlidePopGame._Scripts.Core.System.GameFlow;
using _PawSlidePopGame._Scripts.Data.Events;
using _PawSlidePopGame._Scripts.Data.Events.Payloads;
using _PawSlidePopGame._Scripts.Feature.Match3.Boosters;
using _PawSlidePopGame._Scripts.Feature.Match3.Flow;
using _PawSlidePopGame._Scripts.Feature.Match3.Logic.Move;
using _PawSlidePopGame._Scripts.Feature.Match3.Model.Board;
using _PawSlidePopGame._Scripts.Feature.Match3.Model.Entities;
using _PawSlidePopGame._Scripts.Feature.Match3.Presentation;
using _PawSlidePopGame._Scripts.Feature.Match3.View;
using _PawSlidePopGame.Scripts.DesignPattern.ObserverPattern;
using UnityEngine;

namespace _PawSlidePopGame._Scripts.Feature.Match3.Presenter
{
    public class Match3BoardPresenter : MonoBehaviour
    {
        [SerializeField] private Match3GameManager gameManager;
        [SerializeField] private Match3BoardView boardView;
        [SerializeField] private Match3InputController inputController;
        [SerializeField] private BoosterController boosterController;

        private Coroutine _movePlaybackRoutine;
        private bool _isAnimatingMove;

        private void Awake()
        {
            if (gameManager == null)
            {
                gameManager = GetComponent<Match3GameManager>();
            }

            if (boardView == null)
            {
                boardView = GetComponentInChildren<Match3BoardView>();
            }

            if (inputController == null)
            {
                inputController = GetComponent<Match3InputController>();
            }

            if (boosterController == null)
            {
                boosterController = FindFirstObjectByType<BoosterController>(FindObjectsInactive.Include);
            }
        }

        private void OnEnable()
        {
            if (gameManager != null)
            {
                gameManager.OnBoardInitialized += HandleBoardInitialized;
            }

            if (boardView != null)
            {
                boardView.OnTileClearPlaybackStarted += HandleTileClearPlaybackStarted;
                boardView.OnScoreGainPlaybackStarted += HandleScoreGainPlaybackStarted;
            }

            if (inputController != null)
            {
                inputController.OnMoveRequested += HandleMoveRequested;
                inputController.OnTileTapped += HandleTileTapped;
                inputController.OnPreviewStarted += HandlePreviewStarted;
                inputController.OnPreviewUpdated += HandlePreviewUpdated;
                inputController.OnPreviewCleared += HandlePreviewCleared;
            }

            if (boosterController != null)
            {
                boosterController.OnExecutionRequested += HandleBoosterExecutionRequested;
                boosterController.OnActiveBoosterChanged += HandleActiveBoosterChanged;
            }

            EventManager<LogicGameEvent>.AddListener<InGameSubStateChangedPayload>(
                LogicGameEvent.InGameSubStateChanged,
                HandleSubStateChanged);
        }

        private void Start()
        {
            if (gameManager != null && gameManager.IsInitialized)
            {
                HandleBoardInitialized(gameManager.Board);
            }

            if (GameFlowManager.Instance != null)
            {
                ApplyInputState(GameFlowManager.Instance.CurrentInGameSubState);
            }
        }

        private void OnDisable()
        {
            if (gameManager != null)
            {
                gameManager.OnBoardInitialized -= HandleBoardInitialized;
            }

            if (boardView != null)
            {
                boardView.OnTileClearPlaybackStarted -= HandleTileClearPlaybackStarted;
                boardView.OnScoreGainPlaybackStarted -= HandleScoreGainPlaybackStarted;
            }

            if (inputController != null)
            {
                inputController.OnMoveRequested -= HandleMoveRequested;
                inputController.OnTileTapped -= HandleTileTapped;
                inputController.OnPreviewStarted -= HandlePreviewStarted;
                inputController.OnPreviewUpdated -= HandlePreviewUpdated;
                inputController.OnPreviewCleared -= HandlePreviewCleared;
            }

            if (boosterController != null)
            {
                boosterController.OnExecutionRequested -= HandleBoosterExecutionRequested;
                boosterController.OnActiveBoosterChanged -= HandleActiveBoosterChanged;
            }

            EventManager<LogicGameEvent>.RemoveListener<InGameSubStateChangedPayload>(
                LogicGameEvent.InGameSubStateChanged,
                HandleSubStateChanged);
        }

        private void HandleBoardInitialized(BoardModel board)
        {
            if (boardView == null || gameManager == null)
            {
                return;
            }

            boardView.Bind(board, gameManager.LevelData, gameManager.TileDatabase);

            if (inputController != null)
            {
                inputController.Bind(boardView);
            }
        }

        public void ResetPresentation()
        {
            if (_movePlaybackRoutine != null)
            {
                StopCoroutine(_movePlaybackRoutine);
                _movePlaybackRoutine = null;
            }

            _isAnimatingMove = false;
            inputController?.SetInputMode(Match3InputController.BoardInputMode.Disabled);
            inputController?.Bind(null);
            boardView?.ClearPreview();
            boardView?.SetIdleEnabled(false);
            boardView?.ClearBoardVisuals();
        }

        private void HandleMoveRequested(BoardMoveRequest request)
        {
            if (_isAnimatingMove || GameFlowManager.Instance == null)
            {
                return;
            }

            if (boosterController != null && boosterController.TryHandleLineSwipe(request))
            {
                return;
            }

            if (_movePlaybackRoutine != null)
            {
                StopCoroutine(_movePlaybackRoutine);
            }

            _movePlaybackRoutine = StartCoroutine(PlayExecutionRoutine(() => GameFlowManager.Instance != null
                ? GameFlowManager.Instance.RequestMove(request)
                : new BoardMoveExecutionResult()));
        }

        private void HandleTileTapped(CellModel cell)
        {
            if (_isAnimatingMove || cell == null || GameFlowManager.Instance == null)
            {
                return;
            }

            if (boosterController != null && boosterController.TryHandleTileTap(cell))
            {
                return;
            }

            if (GameFlowManager.Instance.IsInChargedPlacementMode)
            {
                if (!GameFlowManager.Instance.CanPlaceChargedBoosterAt(cell.X, cell.Y))
                {
                    return;
                }

                if (_movePlaybackRoutine != null)
                {
                    StopCoroutine(_movePlaybackRoutine);
                }

                int placementX = cell.X;
                int placementY = cell.Y;
                _movePlaybackRoutine = StartCoroutine(PlayExecutionRoutine(() => GameFlowManager.Instance != null
                    ? GameFlowManager.Instance.RequestChargedPlacement(placementX, placementY)
                    : new BoardMoveExecutionResult()));
                return;
            }

            if (GameFlowManager.Instance.IsInChargedComboMode)
            {
                if (!GameFlowManager.Instance.CanConfirmChargedComboAt(cell.X, cell.Y))
                {
                    GameFlowManager.Instance.CancelChargedAbilityMode();
                    return;
                }

                if (_movePlaybackRoutine != null)
                {
                    StopCoroutine(_movePlaybackRoutine);
                }

                int comboX = cell.X;
                int comboY = cell.Y;
                _movePlaybackRoutine = StartCoroutine(PlayExecutionRoutine(() => GameFlowManager.Instance != null
                    ? GameFlowManager.Instance.RequestChargedComboActivation(comboX, comboY)
                    : new BoardMoveExecutionResult()));
                return;
            }

            if (cell.Tile == null || !cell.CanTileActivate())
            {
                return;
            }

            if (cell.Tile.LogicType == Core.Enum.TileLogicType.ChargedSweepBooster &&
                GameFlowManager.Instance.TryEnterChargedComboMode(cell.X, cell.Y))
            {
                return;
            }

            if (_movePlaybackRoutine != null)
            {
                StopCoroutine(_movePlaybackRoutine);
            }

            int x = cell.X;
            int y = cell.Y;
            _movePlaybackRoutine = StartCoroutine(PlayExecutionRoutine(() => GameFlowManager.Instance != null
                ? GameFlowManager.Instance.RequestTileActivation(x, y)
                : new BoardMoveExecutionResult()));
        }

        private void HandleBoosterExecutionRequested(System.Func<BoardMoveExecutionResult> executeAction)
        {
            if (_isAnimatingMove || executeAction == null)
            {
                return;
            }

            if (_movePlaybackRoutine != null)
            {
                StopCoroutine(_movePlaybackRoutine);
            }

            _movePlaybackRoutine = StartCoroutine(PlayExecutionRoutine(executeAction));
        }

        private void HandleActiveBoosterChanged(BoosterDefinitionSO boosterDefinition)
        {
            if (_isAnimatingMove)
            {
                return;
            }

            boardView?.ClearPreview();
        }

        private System.Collections.IEnumerator PlayExecutionRoutine(System.Func<BoardMoveExecutionResult> executeAction)
        {
            _isAnimatingMove = true;
            inputController?.SetInputLocked(true);
            boardView?.ClearPreview();
            boardView?.SetIdleEnabled(false);

            BoardMoveExecutionResult executionResult = executeAction != null
                ? executeAction.Invoke()
                : new BoardMoveExecutionResult();

            if (executionResult.IsApplied && boardView != null)
            {
                yield return StartCoroutine(boardView.PlayMoveExecution(executionResult));
                boardView.SyncToBoardState();
            }

            GameFlowManager.Instance?.NotifyResolutionPlaybackComplete(executionResult);
            _isAnimatingMove = false;
            _movePlaybackRoutine = null;
        }

        private void HandlePreviewStarted(CellModel cell)
        {
            if (_isAnimatingMove)
            {
                return;
            }

            boardView?.ShowPressPreview(cell);
        }

        private void HandlePreviewUpdated(BoardLinePreview preview)
        {
            if (_isAnimatingMove)
            {
                return;
            }

            boardView?.ShowLinePreview(preview);
        }

        private void HandlePreviewCleared()
        {
            if (_isAnimatingMove)
            {
                return;
            }

            boardView?.ClearPreview();
        }

        private void HandleTileClearPlaybackStarted(TileClearOp clearOp, Vector3 worldPosition)
        {
            GameFlowManager.Instance?.NotifyTileClearedDuringPlayback(clearOp, worldPosition);
        }

        private void HandleScoreGainPlaybackStarted(ScoreGainOp scoreGainOp)
        {
            GameFlowManager.Instance?.NotifyScoreGainedDuringPlayback(scoreGainOp);
        }

        private void HandleSubStateChanged(InGameSubStateChangedPayload payload)
        {
            ApplyInputState(payload.Current);
        }

        private void ApplyInputState(InGameSubState subState)
        {
            Match3InputController.BoardInputMode inputMode = Match3InputController.BoardInputMode.Disabled;
            bool idleEnabled = false;

            switch (subState)
            {
                case InGameSubState.PlayerTurn:
                    inputMode = Match3InputController.BoardInputMode.Normal;
                    idleEnabled = true;
                    break;
                case InGameSubState.TargetingChargedPlacement:
                case InGameSubState.TargetingChargedCombo:
                    inputMode = Match3InputController.BoardInputMode.TapOnly;
                    idleEnabled = true;
                    break;
            }

            inputController?.SetInputMode(inputMode);
            boardView?.SetIdleEnabled(idleEnabled);
            boardView?.ClearPreview();

            if (subState == InGameSubState.TargetingChargedPlacement)
            {
                boardView?.ShowPlacementCandidates(GameFlowManager.Instance != null
                    ? GameFlowManager.Instance.GetChargedPlacementCandidates()
                    : null);
            }
            else if (subState == InGameSubState.TargetingChargedCombo)
            {
                boardView?.ShowChargedComboSelection(
                    GameFlowManager.Instance != null ? GameFlowManager.Instance.GetChargedComboSourceCell() : null,
                    GameFlowManager.Instance != null ? GameFlowManager.Instance.GetChargedComboPartnerCells() : null);
            }
        }
    }
}

