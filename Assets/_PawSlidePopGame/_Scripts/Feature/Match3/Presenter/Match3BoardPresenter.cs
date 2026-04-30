using _PawSlidePopGame._Scripts.Feature.Match3.Logic.Move;
using _PawSlidePopGame._Scripts.Feature.Match3.Model.Board;
using _PawSlidePopGame._Scripts.Feature.Match3.Presentation;
using _PawSlidePopGame._Scripts.Feature.Match3.View;
using UnityEngine;

namespace _PawSlidePopGame._Scripts.Feature.Match3.Presenter
{
    public class Match3BoardPresenter : MonoBehaviour
    {
        [SerializeField] private Match3GameManager gameManager;
        [SerializeField] private Match3BoardView boardView;
        [SerializeField] private Match3InputController inputController;

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
        }

        private void OnEnable()
        {
            if (gameManager != null)
            {
                gameManager.OnBoardInitialized += HandleBoardInitialized;
            }

            if (inputController != null)
            {
                inputController.OnMoveRequested += HandleMoveRequested;
                inputController.OnPreviewStarted += HandlePreviewStarted;
                inputController.OnPreviewUpdated += HandlePreviewUpdated;
                inputController.OnPreviewCleared += HandlePreviewCleared;
            }
        }

        private void Start()
        {
            if (gameManager != null && gameManager.IsInitialized)
            {
                HandleBoardInitialized(gameManager.Board);
            }
        }

        private void OnDisable()
        {
            if (gameManager != null)
            {
                gameManager.OnBoardInitialized -= HandleBoardInitialized;
            }

            if (inputController != null)
            {
                inputController.OnMoveRequested -= HandleMoveRequested;
                inputController.OnPreviewStarted -= HandlePreviewStarted;
                inputController.OnPreviewUpdated -= HandlePreviewUpdated;
                inputController.OnPreviewCleared -= HandlePreviewCleared;
            }
        }

        private void HandleBoardInitialized(BoardModel board)
        {
            if (boardView == null || gameManager == null)
            {
                return;
            }

            boardView.Bind(board);

            if (inputController != null)
            {
                inputController.Bind(boardView);
            }
        }

        private void HandleMoveRequested(BoardMoveRequest request)
        {
            if (_isAnimatingMove)
            {
                return;
            }

            if (_movePlaybackRoutine != null)
            {
                StopCoroutine(_movePlaybackRoutine);
            }

            _movePlaybackRoutine = StartCoroutine(PlayMoveRoutine(request));
        }

        private System.Collections.IEnumerator PlayMoveRoutine(BoardMoveRequest request)
        {
            _isAnimatingMove = true;
            inputController?.SetInputLocked(true);
            boardView?.ClearPreview();
            boardView?.SetIdleEnabled(false);

            BoardMoveExecutionResult executionResult = gameManager != null
                ? gameManager.ExecuteMove(request)
                : new BoardMoveExecutionResult();

            if (executionResult.IsApplied && boardView != null)
            {
                yield return StartCoroutine(boardView.PlayMoveExecution(executionResult));
                boardView.SyncToBoardState();
            }

            if (boardView != null)
            {
                boardView.SetIdleEnabled(true);
            }

            inputController?.SetInputLocked(false);
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
    }
}
