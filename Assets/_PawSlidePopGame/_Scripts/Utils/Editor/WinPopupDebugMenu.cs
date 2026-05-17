#if UNITY_EDITOR
using _PawSlidePopGame._Scripts.Data.Events;
using _PawSlidePopGame._Scripts.Data.Events.Payloads;
using _PawSlidePopGame._Scripts.Feature.Match3.Flow;
using _PawSlidePopGame.Scripts.DesignPattern.ObserverPattern;
using UnityEditor;
using UnityEngine;

namespace _PawSlidePopGame._Scripts.Utils.Editor
{
    public static class WinPopupDebugMenu
    {
        [MenuItem("Tools/Paw Slide Pop/Debug/Trigger Victory Popup")]
        private static void TriggerVictoryPopup()
        {
            if (!EditorApplication.isPlaying)
            {
                Debug.LogWarning("[WinPopupDebugMenu] Enter Play Mode before triggering the victory popup.");
                return;
            }

            if (GameFlowManager.Instance == null)
            {
                Debug.LogWarning("[WinPopupDebugMenu] GameFlowManager instance not found.");
                return;
            }

            GameplayHudSnapshot snapshot = GameFlowManager.Instance.LastSnapshot?.Clone();
            if (snapshot != null &&
                GameFlowManager.Instance.CurrentGameState == _PawSlidePopGame._Scripts.Core.System.GameFlow.GameState.Gameplay)
            {
                GameFlowManager.Instance.EnterVictory();
                return;
            }

            snapshot = new GameplayHudSnapshot
            {
                levelNumber = 1,
                currentScore = 12345,
                reachedStars = 4
            };

            EventManager<LogicGameEvent>.Post(LogicGameEvent.GameplayWon, snapshot);
        }
    }
}
#endif
