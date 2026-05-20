using _PawSlidePopGame._Scripts.Core.System.GameFlow;
using _PawSlidePopGame._Scripts.Data.Events;
using _PawSlidePopGame._Scripts.Data.Events.Payloads;
using _PawSlidePopGame._Scripts.Feature.Match3.Flow;
using _PawSlidePopGame.Scripts.DesignPattern.ObserverPattern;
using DG.Tweening;
using UnityEngine;

namespace _PawSlidePopGame._Scripts.UI.Components.HUD
{
    public class TopHUDPresenter : MonoBehaviour
    {
        [SerializeField] private MovesCounterView movesCounterView;
        [SerializeField] private TargetListView targetListView;
        [SerializeField] private LevelProgressView levelProgressView;
        [SerializeField] private CanvasGroup canvasGroup;

        private GameplayHudSnapshot _currentSnapshot;

        private void Awake()
        {
            if (canvasGroup == null)
            {
                canvasGroup = GetComponent<CanvasGroup>();
                if (canvasGroup == null)
                {
                    canvasGroup = gameObject.AddComponent<CanvasGroup>();
                }
            }

        }

        private void OnValidate()
        {
            if (movesCounterView == null)
            {
                movesCounterView = GetComponentInChildren<MovesCounterView>(true);
            }

            if (targetListView == null)
            {
                targetListView = GetComponentInChildren<TargetListView>(true);
            }

            if (levelProgressView == null)
            {
                levelProgressView = GetComponentInChildren<LevelProgressView>(true);
            }

            if (canvasGroup == null)
            {
                canvasGroup = GetComponent<CanvasGroup>();
            }
        }

        private void OnEnable()
        {
            EventManager<LogicGameEvent>.AddListener<GameplayHudSnapshot>(LogicGameEvent.GameplayHudInitialized, HandleHudSnapshot);
            EventManager<LogicGameEvent>.AddListener<GameplayHudSnapshot>(LogicGameEvent.GameplayHudStateChanged, HandleHudSnapshot);
            EventManager<LogicGameEvent>.AddListener<RemainingMovesChangedPayload>(LogicGameEvent.GameplayMovesChanged, HandleMovesChanged);
            EventManager<LogicGameEvent>.AddListener<ScoreChangedPayload>(LogicGameEvent.GameplayScoreChanged, HandleScoreChanged);
            EventManager<LogicGameEvent>.AddListener<InGameSubStateChangedPayload>(LogicGameEvent.InGameSubStateChanged, HandleSubStateChanged);
            EventManager<VisualGameEvent>.AddListener<TargetProgressChangedPayload>(VisualGameEvent.TopHudTargetProgressFx, HandleTargetProgressFx);
            EventManager<VisualGameEvent>.AddListener<StarReachedPayload>(VisualGameEvent.TopHudStarReachedFx, HandleStarReachedFx);
            EventManager<VisualGameEvent>.AddListener(VisualGameEvent.TopHudTargetsCompletedFx, HandleTargetsCompletedFx);
        }

        private void OnDisable()
        {
            EventManager<LogicGameEvent>.RemoveListener<GameplayHudSnapshot>(LogicGameEvent.GameplayHudInitialized, HandleHudSnapshot);
            EventManager<LogicGameEvent>.RemoveListener<GameplayHudSnapshot>(LogicGameEvent.GameplayHudStateChanged, HandleHudSnapshot);
            EventManager<LogicGameEvent>.RemoveListener<RemainingMovesChangedPayload>(LogicGameEvent.GameplayMovesChanged, HandleMovesChanged);
            EventManager<LogicGameEvent>.RemoveListener<ScoreChangedPayload>(LogicGameEvent.GameplayScoreChanged, HandleScoreChanged);
            EventManager<LogicGameEvent>.RemoveListener<InGameSubStateChangedPayload>(LogicGameEvent.InGameSubStateChanged, HandleSubStateChanged);
            EventManager<VisualGameEvent>.RemoveListener<TargetProgressChangedPayload>(VisualGameEvent.TopHudTargetProgressFx, HandleTargetProgressFx);
            EventManager<VisualGameEvent>.RemoveListener<StarReachedPayload>(VisualGameEvent.TopHudStarReachedFx, HandleStarReachedFx);
            EventManager<VisualGameEvent>.RemoveListener(VisualGameEvent.TopHudTargetsCompletedFx, HandleTargetsCompletedFx);
        }

        public void ResetView()
        {
            _currentSnapshot = null;

            if (canvasGroup != null)
            {
                canvasGroup.DOKill();
                canvasGroup.alpha = 1f;
            }

            movesCounterView?.ResetView();
            targetListView?.ResetView();
            levelProgressView?.ResetView();
        }

        private void HandleHudSnapshot(GameplayHudSnapshot snapshot)
        {
            if (snapshot == null)
            {
                return;
            }

            _currentSnapshot = snapshot.Clone();
            movesCounterView?.SetValue(_currentSnapshot.remainingMoves);
            targetListView?.SetTargets(_currentSnapshot.targets);
            levelProgressView?.SetData(_currentSnapshot);
        }

        private void HandleSubStateChanged(InGameSubStateChangedPayload payload)
        {
            bool paused = payload.Current == InGameSubState.Paused;
            canvasGroup.DOKill();
            canvasGroup.DOFade(paused ? 0.85f : 1f, 0.15f)
                .SetLink(gameObject, LinkBehaviour.KillOnDisable);
        }

        private void HandleTargetProgressFx(TargetProgressChangedPayload payload)
        {
            UpdateSnapshotTargetProgress(payload);
            targetListView?.PlayProgressFx(payload);
        }

        private void HandleStarReachedFx(StarReachedPayload payload)
        {
            levelProgressView?.PlayStarReachedFx(payload);
        }

        private void HandleTargetsCompletedFx()
        {
            targetListView?.PlayCompletedFx();
        }

        private void HandleMovesChanged(RemainingMovesChangedPayload payload)
        {
            if (_currentSnapshot == null)
            {
                return;
            }

            _currentSnapshot.remainingMoves = payload.CurrentMoves;
            movesCounterView?.SetValue(payload.CurrentMoves);
            movesCounterView?.PlayValueChangedFx();
        }

        private void HandleScoreChanged(ScoreChangedPayload payload)
        {
            if (_currentSnapshot == null)
            {
                return;
            }

            _currentSnapshot.currentScore = payload.CurrentScore;
            _currentSnapshot.reachedStars = GameplayHudSnapshotBuilder.CountReachedStars(payload.CurrentScore, _currentSnapshot.starScoreThresholds);
            levelProgressView?.PlayScoreChangedFx(payload);
        }

        private void UpdateSnapshotTargetProgress(TargetProgressChangedPayload payload)
        {
            if (_currentSnapshot?.targets == null)
            {
                return;
            }

            for (int i = 0; i < _currentSnapshot.targets.Count; i++)
            {
                TargetProgressData target = _currentSnapshot.targets[i];
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
    }
}
