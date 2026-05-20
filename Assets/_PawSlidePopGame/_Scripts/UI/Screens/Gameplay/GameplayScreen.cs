using _PawSlidePopGame._Scripts.Feature.Match3.Flow;
using _PawSlidePopGame._Scripts.Core.System.GameFlow;
using _PawSlidePopGame._Scripts.Data.Events;
using _PawSlidePopGame._Scripts.Data.Events.Payloads;
using _PawSlidePopGame._Scripts.UI.Base;
using _PawSlidePopGame._Scripts.UI.Components.HUD;
using _PawSlidePopGame.Scripts.DesignPattern.ObserverPattern;
using UnityEngine;
using UnityEngine.UI;

namespace _PawSlidePopGame._Scripts.UI.Screens.Gameplay
{
    public class GameplayScreen : BaseScreen
    {
        [SerializeField] private TopHUDPresenter topHUDPresenter;
        [SerializeField] private BottomHUDPresenter bottomHUDPresenter;
        [SerializeField] private Button pauseButton;

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
            RefreshPauseButtonState(GameFlowManager.Instance != null
                ? GameFlowManager.Instance.CurrentInGameSubState
                : InGameSubState.None);
        }

        protected override void OnBeforeHide()
        {
            topHUDPresenter?.ResetView();
            bottomHUDPresenter?.ResetView();
            RefreshPauseButtonState(InGameSubState.None);
        }

        private void HandlePausePressed()
        {
            GameFlowManager.Instance?.PauseGameplay();
        }

        private void HandleSubStateChanged(InGameSubStateChangedPayload payload)
        {
            RefreshPauseButtonState(payload.Current);
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
