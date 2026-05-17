using _PawSlidePopGame._Scripts.Data.Events;
using _PawSlidePopGame._Scripts.Data.Events.Payloads;
using _PawSlidePopGame._Scripts.Feature.Match3.Flow;
using _PawSlidePopGame._Scripts.UI.Manager;
using _PawSlidePopGame._Scripts.UI.Popups;
using _PawSlidePopGame.Scripts.DesignPattern.ObserverPattern;
using UnityEngine;

namespace _PawSlidePopGame._Scripts.UI.Screens.Gameplay
{
    public sealed class GameplayResultPopupPresenter : MonoBehaviour
    {
        private void OnEnable()
        {
            EventManager<LogicGameEvent>.AddListener<GameplayHudSnapshot>(LogicGameEvent.GameplayWon, HandleGameplayWon);
            EventManager<LogicGameEvent>.AddListener<GameplayHudSnapshot>(LogicGameEvent.GameplayLost, HandleGameplayLost);
        }

        private void OnDisable()
        {
            EventManager<LogicGameEvent>.RemoveListener<GameplayHudSnapshot>(LogicGameEvent.GameplayWon, HandleGameplayWon);
            EventManager<LogicGameEvent>.RemoveListener<GameplayHudSnapshot>(LogicGameEvent.GameplayLost, HandleGameplayLost);
        }

        private void HandleGameplayWon(GameplayHudSnapshot snapshot)
        {
            GameplayHudSnapshot resolvedSnapshot = snapshot?.Clone() ?? GameFlowManager.Instance?.LastSnapshot?.Clone();
            if (resolvedSnapshot == null)
            {
                Debug.LogWarning("[GameplayResultPopupPresenter] Missing GameplayHudSnapshot for WinPopup.");
                return;
            }

            UIManager.Instance?.ShowPopup<WinPopup>(
                PopupID.WinPopup,
                popup => popup.SetSnapshot(resolvedSnapshot));
        }

        private void HandleGameplayLost(GameplayHudSnapshot snapshot)
        {
            GameplayHudSnapshot resolvedSnapshot = snapshot?.Clone() ?? GameFlowManager.Instance?.LastSnapshot?.Clone();
            if (resolvedSnapshot == null)
            {
                Debug.LogWarning("[GameplayResultPopupPresenter] Missing GameplayHudSnapshot for LosePopup.");
                return;
            }

            UIManager.Instance?.ShowPopup<LosePopup>(
                PopupID.LosePopup,
                popup => popup.SetSnapshot(resolvedSnapshot));
        }
    }
}
