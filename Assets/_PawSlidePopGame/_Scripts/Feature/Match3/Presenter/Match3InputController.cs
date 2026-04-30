using System;
using _PawSlidePopGame._Scripts.Feature.Match3.Core.Enum;
using _PawSlidePopGame._Scripts.Feature.Match3.Logic.Move;
using _PawSlidePopGame._Scripts.Feature.Match3.Model.Board;
using _PawSlidePopGame._Scripts.Feature.Match3.Presentation;
using _PawSlidePopGame._Scripts.Feature.Match3.View;
using _PawSlidePopGame.Input;
using UnityEngine;

namespace _PawSlidePopGame._Scripts.Feature.Match3.Presenter
{
    public class Match3InputController : MonoBehaviour
    {
        [SerializeField] private float dragThresholdPixels = 24f;
        [SerializeField] private bool lockAxisAfterThreshold = true;

        private Match3BoardView _boardView;
        private CellModel _pressedCell;
        private Vector2 _pressScreenPosition;
        private bool _isTrackingDrag;
        private MoveAxis? _lockedAxis;
        private bool _isInputLocked;

        public event Action<BoardMoveRequest> OnMoveRequested;
        public event Action<CellModel> OnPreviewStarted;
        public event Action<BoardLinePreview> OnPreviewUpdated;
        public event Action OnPreviewCleared;

        public void Bind(Match3BoardView boardView)
        {
            _boardView = boardView;
        }

        public void SetInputLocked(bool isLocked)
        {
            _isInputLocked = isLocked;
            if (_isInputLocked)
            {
                ClearPreviewAndResetGesture();
            }
        }

        private void OnEnable()
        {
            SubscribeInputManager();
        }

        private void OnDisable()
        {
            UnsubscribeInputManager();
        }

        private void SubscribeInputManager()
        {
            if (InputManager.Instance == null)
            {
                return;
            }

            InputManager.Instance.OnTouchStart -= HandleTouchStart;
            InputManager.Instance.OnTouchMove -= HandleTouchMove;
            InputManager.Instance.OnTouchEnd -= HandleTouchEnd;
            InputManager.Instance.OnTouchStart += HandleTouchStart;
            InputManager.Instance.OnTouchMove += HandleTouchMove;
            InputManager.Instance.OnTouchEnd += HandleTouchEnd;
        }

        private void UnsubscribeInputManager()
        {
            if (InputManager.Instance == null)
            {
                return;
            }

            InputManager.Instance.OnTouchStart -= HandleTouchStart;
            InputManager.Instance.OnTouchMove -= HandleTouchMove;
            InputManager.Instance.OnTouchEnd -= HandleTouchEnd;
        }

        private void HandleTouchStart(Vector2 screenPosition)
        {
            if (_isInputLocked || _boardView == null)
            {
                return;
            }

            _pressedCell = _boardView.GetCellAtScreenPosition(screenPosition);
            if (_pressedCell == null)
            {
                ResetGesture();
                return;
            }

            _pressScreenPosition = screenPosition;
            _isTrackingDrag = true;
            _lockedAxis = null;
            OnPreviewStarted?.Invoke(_pressedCell);
        }

        private void HandleTouchMove(Vector2 screenPosition)
        {
            if (_isInputLocked || !_isTrackingDrag || _pressedCell == null)
            {
                return;
            }

            Vector2 delta = screenPosition - _pressScreenPosition;
            if (delta.magnitude < dragThresholdPixels)
            {
                return;
            }

            if (!_lockedAxis.HasValue || !lockAxisAfterThreshold)
            {
                _lockedAxis = Mathf.Abs(delta.x) >= Mathf.Abs(delta.y) ? MoveAxis.Row : MoveAxis.Column;
            }

            MoveAxis axis = _lockedAxis ?? (Mathf.Abs(delta.x) >= Mathf.Abs(delta.y) ? MoveAxis.Row : MoveAxis.Column);
            int lineIndex = axis == MoveAxis.Row ? _pressedCell.Y : _pressedCell.X;
            OnPreviewUpdated?.Invoke(new BoardLinePreview(_pressedCell, axis, lineIndex));
        }

        private void HandleTouchEnd(Vector2 screenPosition)
        {
            if (_isInputLocked || !_isTrackingDrag || _pressedCell == null)
            {
                ClearPreviewAndResetGesture();
                return;
            }

            Vector2 delta = screenPosition - _pressScreenPosition;
            if (delta.magnitude < dragThresholdPixels)
            {
                ClearPreviewAndResetGesture();
                return;
            }

            MoveAxis axis = _lockedAxis ?? (Mathf.Abs(delta.x) >= Mathf.Abs(delta.y) ? MoveAxis.Row : MoveAxis.Column);
            BoardMoveRequest request = BuildMoveRequest(axis, delta, _pressedCell);
            OnPreviewCleared?.Invoke();
            OnMoveRequested?.Invoke(request);
            ResetGesture();
        }

        private static BoardMoveRequest BuildMoveRequest(MoveAxis axis, Vector2 delta, CellModel pressedCell)
        {
            if (axis == MoveAxis.Row)
            {
                LineSlideDirection direction = delta.x >= 0f ? LineSlideDirection.Right : LineSlideDirection.Left;
                return new BoardMoveRequest(MoveAxis.Row, pressedCell.Y, direction);
            }

            LineSlideDirection verticalDirection = delta.y >= 0f ? LineSlideDirection.Up : LineSlideDirection.Down;
            return new BoardMoveRequest(MoveAxis.Column, pressedCell.X, verticalDirection);
        }

        private void ResetGesture()
        {
            _pressedCell = null;
            _isTrackingDrag = false;
            _lockedAxis = null;
        }

        private void ClearPreviewAndResetGesture()
        {
            OnPreviewCleared?.Invoke();
            ResetGesture();
        }
    }
}
