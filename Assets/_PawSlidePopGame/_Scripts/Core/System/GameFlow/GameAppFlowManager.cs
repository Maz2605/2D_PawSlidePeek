using System;
using _PawSlidePopGame._Scripts.Core.System.DesignPattern.Singleton;
using _PawSlidePopGame._Scripts.Data.Events;
using _PawSlidePopGame._Scripts.Data.Events.Payloads;
using _PawSlidePopGame._Scripts.Feature.Match3.Flow;
using _PawSlidePopGame._Scripts.Feature.Match3.Presenter;
using _PawSlidePopGame._Scripts.UI.Manager;
using _PawSlidePopGame._Scripts.UI.Popups;
using _PawSlidePopGame._Scripts.UI.Screens;
using _PawSlidePopGame._Scripts.UI.Screens.Gameplay;
using _PawSlidePopGame.Scripts.DesignPattern.ObserverPattern;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace _PawSlidePopGame._Scripts.Core.System.GameFlow
{
    [DisallowMultipleComponent]
    public sealed class GameAppFlowManager : Singleton<GameAppFlowManager>
    {
        [SerializeField] private string gameplaySceneName = "GameplayScene";
        [SerializeField] private string gameplaySessionRootName = "===Gameplay===";
        [SerializeField] private string tempLevelId = "Level_001";

        private bool _isInitialized;
        private GameFlowManager _gameFlowManager;
        private Match3GameManager _gameManager;
        private Match3LevelManager _levelManager;
        private Match3BoardPresenter _boardPresenter;
        private Transform _gameplaySessionRoot;

        public GameAppState CurrentAppState { get; private set; } = GameAppState.None;
        public string CurrentLevelId { get; private set; }

        protected override void Awake()
        {
            DontDestroyOnLoadEnabled = false;
            base.Awake();
        }

        public void Initialize()
        {
            if (_isInitialized)
            {
                return;
            }

            _isInitialized = true;
            SceneManager.sceneLoaded += HandleSceneLoaded;
            EventManager<LogicGameEvent>.AddListener<InGameSubStateChangedPayload>(
                LogicGameEvent.InGameSubStateChanged,
                HandleInGameSubStateChanged);

            SetAppState(GameAppState.Bootstrapping);
            TryResolveGameplayFlowManager();
        }

        public void EnterMainMenu()
        {
            if (!EnsureUiManager())
            {
                return;
            }

            ExitGameplaySession();
            UIManager.Instance.ClearAllPopups();
            UIManager.Instance.ShowScreen<GameMenuScreen>(ScreenID.GameMenuScreen);
            SetAppState(GameAppState.MainMenu);
        }

        public bool StartGameplaySession(string levelId = null)
        {
            if (CurrentAppState == GameAppState.EnteringGameplay)
            {
                return false;
            }

            if (!EnsureUiManager())
            {
                return false;
            }

            if (!TryResolveGameplayFlowManager())
            {
                Debug.LogError("[GameAppFlowManager] Missing GameFlowManager in active scene.", this);
                return false;
            }

            if (!TryResolveGameplaySessionRoot())
            {
                Debug.LogError("[GameAppFlowManager] Missing gameplay session root in active scene.", this);
                return false;
            }

            CurrentLevelId = string.IsNullOrWhiteSpace(levelId) ? tempLevelId : levelId;

            ExitGameplaySession();
            if (!TryResolveLevelManager())
            {
                Debug.LogError("[GameAppFlowManager] Missing Match3LevelManager in active scene.", this);
                return false;
            }

            _levelManager.SetRequestedLevelId(CurrentLevelId);
            if (!_levelManager.TryLoadCurrentLevel(out var levelData))
            {
                Debug.LogError($"[GameAppFlowManager] Failed to load level '{CurrentLevelId}'.", this);
                return false;
            }

            _gameManager?.SetLevelData(levelData);
            SetAppState(GameAppState.EnteringGameplay);
            SetGameplaySessionActive(true);
            UIManager.Instance.ClearAllPopups();
            UIManager.Instance.ShowScreen<GameplayScreen>(ScreenID.GameplayScreen);
            _gameFlowManager.EnterGameplay();

            if (_gameFlowManager.CurrentInGameSubState == InGameSubState.PlayerTurn)
            {
                SetAppState(GameAppState.Gameplay);
            }

            return true;
        }

        public bool RequestStartLevel(string levelId = null)
        {
            return StartGameplaySession(levelId);
        }

        public void ExitGameplaySession()
        {
            TryResolveGameplayFlowManager();
            TryResolveGameplaySessionRoot();

            if (_gameFlowManager != null)
            {
                _gameFlowManager.ExitGameplay();
            }

            CloseGameplayPopups();
            _boardPresenter?.ResetPresentation();
            _gameManager?.ResetGame();
            _levelManager?.ClearLoadedLevel();
            SetGameplaySessionActive(false);
        }

        public void RestartGameplay()
        {
            StartGameplaySession(CurrentLevelId);
        }

        protected override void OnDestroy()
        {
            if (_isInitialized)
            {
                SceneManager.sceneLoaded -= HandleSceneLoaded;
                EventManager<LogicGameEvent>.RemoveListener<InGameSubStateChangedPayload>(
                    LogicGameEvent.InGameSubStateChanged,
                    HandleInGameSubStateChanged);
            }

            base.OnDestroy();
        }

        private void HandleSceneLoaded(Scene scene, LoadSceneMode loadMode)
        {
            if (!string.Equals(scene.name, gameplaySceneName, StringComparison.Ordinal))
            {
                return;
            }

            TryResolveGameplayFlowManager();
            TryResolveGameplaySessionRoot();
            EnterMainMenu();
        }

        private void HandleInGameSubStateChanged(InGameSubStateChangedPayload payload)
        {
            if (CurrentAppState == GameAppState.EnteringGameplay &&
                payload.Current == InGameSubState.PlayerTurn)
            {
                SetAppState(GameAppState.Gameplay);
            }
        }

        private bool TryResolveGameplayFlowManager()
        {
            _gameFlowManager = FindFirstObjectByType<GameFlowManager>(FindObjectsInactive.Include);
            _gameManager = _gameFlowManager != null
                ? _gameFlowManager.GetComponent<Match3GameManager>()
                : FindFirstObjectByType<Match3GameManager>(FindObjectsInactive.Include);
            _levelManager = FindFirstObjectByType<Match3LevelManager>(FindObjectsInactive.Include);
            _boardPresenter = FindFirstObjectByType<Match3BoardPresenter>(FindObjectsInactive.Include);
            return _gameFlowManager != null;
        }

        private bool TryResolveLevelManager()
        {
            if (_levelManager != null)
            {
                return true;
            }

            _levelManager = FindFirstObjectByType<Match3LevelManager>(FindObjectsInactive.Include);
            return _levelManager != null;
        }

        private bool TryResolveGameplaySessionRoot()
        {
            if (_gameplaySessionRoot != null)
            {
                return true;
            }

            GameObject rootObject = GameObject.Find(gameplaySessionRootName);
            if (rootObject != null)
            {
                _gameplaySessionRoot = rootObject.transform;
                return true;
            }

            if (_gameFlowManager != null)
            {
                _gameplaySessionRoot = _gameFlowManager.transform.root;
            }

            return _gameplaySessionRoot != null;
        }

        private bool EnsureUiManager()
        {
            if (UIManager.Instance != null)
            {
                return true;
            }

            Debug.LogError("[GameAppFlowManager] Missing UIManager instance.", this);
            return false;
        }

        private void SetGameplaySessionActive(bool isActive)
        {
            if (_gameplaySessionRoot == null)
            {
                return;
            }

            if (_gameplaySessionRoot.gameObject.activeSelf == isActive)
            {
                return;
            }

            _gameplaySessionRoot.gameObject.SetActive(isActive);
        }

        private void CloseGameplayPopups()
        {
            if (UIManager.Instance == null)
            {
                return;
            }

            UIManager.Instance.ClosePopup(PopupID.PausePopup);
            UIManager.Instance.ClosePopup(PopupID.WinPopup);
            UIManager.Instance.ClosePopup(PopupID.LosePopup);
        }

        private void SetAppState(GameAppState nextState)
        {
            if (CurrentAppState == nextState)
            {
                return;
            }

            GameAppState previousState = CurrentAppState;
            CurrentAppState = nextState;
            EventManager<LogicGameEvent>.Post(
                LogicGameEvent.GameAppStateChanged,
                new GameAppStateChangedPayload(previousState, CurrentAppState));
        }
    }
}
