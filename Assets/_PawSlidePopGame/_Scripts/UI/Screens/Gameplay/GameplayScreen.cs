using _PawSlidePopGame._Scripts.Feature.Match3.Flow;
using _PawSlidePopGame._Scripts.Core.System.GameFlow;
using _PawSlidePopGame._Scripts.Data.Events;
using _PawSlidePopGame._Scripts.Data.Events.Payloads;
using _PawSlidePopGame._Scripts.Feature.Match3.Presenter;
using _PawSlidePopGame._Scripts.UI.Base;
using _PawSlidePopGame._Scripts.UI.Components.HUD;
using _PawSlidePopGame.Scripts.DesignPattern.ObserverPattern;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace _PawSlidePopGame._Scripts.UI.Screens.Gameplay
{
    public class GameplayScreen : BaseScreen
    {
        [SerializeField] private TopHUDPresenter topHUDPresenter;
        [SerializeField] private BottomHUDPresenter bottomHUDPresenter;
        [SerializeField] private Button pauseButton;
        [SerializeField] private Button skipSugarCrushButton;

        [Header("FX Prefabs")]
        [SerializeField] private FloatingScoreText floatingScoreTextPrefab;
        public FloatingScoreText FloatingScoreTextPrefab => floatingScoreTextPrefab;

        private GameplayPausePopupPresenter _pausePopupPresenter;

        protected override void Awake()
        {
            base.Awake();
            EnsurePausePopupPresenter();
        }

        private void OnEnable()
        {
            EventManager<LogicGameEvent>.AddListener<InGameSubStateChangedPayload>(
                LogicGameEvent.InGameSubStateChanged,
                HandleSubStateChanged);

            RefreshPauseButtonState(GameFlowManager.Instance != null
                ? GameFlowManager.Instance.CurrentInGameSubState
                : InGameSubState.None);
        }

        private void OnDisable()
        {
            EventManager<LogicGameEvent>.RemoveListener<InGameSubStateChangedPayload>(
                LogicGameEvent.InGameSubStateChanged,
                HandleSubStateChanged);
        }

        protected override void OnBeforeShow()
        {
            topHUDPresenter?.ResetView();
            bottomHUDPresenter?.ResetView();
            BindButton(pauseButton, HandlePausePressed);
            BindButton(skipSugarCrushButton, HandleSkipSugarCrushPressed);
            if (skipSugarCrushButton != null)
            {
                skipSugarCrushButton.gameObject.SetActive(false);
            }
            RefreshPauseButtonState(GameFlowManager.Instance != null
                ? GameFlowManager.Instance.CurrentInGameSubState
                : InGameSubState.None);
        }

        protected override void OnBeforeHide()
        {
            topHUDPresenter?.ResetView();
            bottomHUDPresenter?.ResetView();
            if (skipSugarCrushButton != null)
            {
                skipSugarCrushButton.transform.DOKill();
                var cg = skipSugarCrushButton.GetComponent<CanvasGroup>();
                if (cg != null) cg.DOKill();
                skipSugarCrushButton.gameObject.SetActive(false);
            }
            RefreshPauseButtonState(InGameSubState.None);
        }

        private void HandlePausePressed()
        {
            GameFlowManager.Instance?.PauseGameplay();
        }

        private void HandleSkipSugarCrushPressed()
        {
            var boardPresenter = FindFirstObjectByType<Match3BoardPresenter>(FindObjectsInactive.Include);
            if (boardPresenter != null)
            {
                boardPresenter.SkipSugarCrush();
            }
            if (skipSugarCrushButton != null)
            {
                skipSugarCrushButton.transform.DOKill();
                var cg = skipSugarCrushButton.GetComponent<CanvasGroup>();
                if (cg != null) cg.DOKill();
                skipSugarCrushButton.gameObject.SetActive(false);
            }
        }

        private void HandleSubStateChanged(InGameSubStateChangedPayload payload)
        {
            RefreshPauseButtonState(payload.Current);
            if (payload.Current == InGameSubState.Victory)
            {
                int moves = GameFlowManager.Instance != null ? GameFlowManager.Instance.LastSnapshot?.remainingMoves ?? 0 : 0;
                if (skipSugarCrushButton != null)
                {
                    bool show = moves > 0;
                    skipSugarCrushButton.gameObject.SetActive(show);
                    if (show)
                    {
                        skipSugarCrushButton.transform.DOKill();
                        skipSugarCrushButton.transform.localScale = Vector3.zero;

                        var cg = skipSugarCrushButton.GetComponent<CanvasGroup>();
                        if (cg == null)
                        {
                            cg = skipSugarCrushButton.gameObject.AddComponent<CanvasGroup>();
                        }
                        cg.DOKill();
                        cg.alpha = 1f;

                        // Entry animation (Scale from 0 to 1 with Back ease)
                        skipSugarCrushButton.transform.DOScale(Vector3.one, 0.35f)
                            .SetEase(Ease.OutBack)
                            .SetLink(skipSugarCrushButton.gameObject);

                        // Pulse (blinking) alpha effect
                        cg.DOFade(0.7f, 0.5f)
                          .SetEase(Ease.InOutSine)
                          .SetLoops(-1, LoopType.Yoyo)
                          .SetLink(skipSugarCrushButton.gameObject);
                    }
                    else
                    {
                        skipSugarCrushButton.transform.DOKill();
                        var cg = skipSugarCrushButton.GetComponent<CanvasGroup>();
                        if (cg != null) cg.DOKill();
                    }
                }
            }
            else
            {
                if (skipSugarCrushButton != null)
                {
                    skipSugarCrushButton.transform.DOKill();
                    var cg = skipSugarCrushButton.GetComponent<CanvasGroup>();
                    if (cg != null) cg.DOKill();
                    skipSugarCrushButton.gameObject.SetActive(false);
                }
            }
        }

        private void RefreshPauseButtonState(InGameSubState subState)
        {
            if (pauseButton == null)
            {
                return;
            }

            pauseButton.interactable = GameFlowManager.IsInteractiveGameplaySubState(subState);
        }

        private void EnsurePausePopupPresenter()
        {
            _pausePopupPresenter = GetComponent<GameplayPausePopupPresenter>();
            if (_pausePopupPresenter == null)
            {
                _pausePopupPresenter = gameObject.AddComponent<GameplayPausePopupPresenter>();
            }
        }

        private void OnValidate()
        {
            if (topHUDPresenter == null)
            {
                topHUDPresenter = GetComponentInChildren<TopHUDPresenter>(true);
            }

            if (bottomHUDPresenter == null)
            {
                bottomHUDPresenter = GetComponentInChildren<BottomHUDPresenter>(true);
            }
        }
    }
}
