using _PawSlidePopGame._Scripts.Core.System.GameFlow;
using _PawSlidePopGame._Scripts.UI.Base;
using _PawSlidePopGame._Scripts.UI.Manager;
using _PawSlidePopGame._Scripts.UI.Popups;
using UnityEngine;

namespace _PawSlidePopGame._Scripts.Utils.DevTest
{
    public class DevHUD : MonoBehaviour
    {
        public void PlayWinPopup()
        {
            UIManager.Instance.ShowPopup<WinPopup>();
        }

        public void PlayLosePopup()
        {
            UIManager.Instance.ShowPopup<LosePopup>();
        }

        public void PlayPauseButton()
        {
            UIManager.Instance.ShowPopup<PausePopup>();
        }

        public void ShowGameMenuScreen()
        {
            GameAppFlowManager.Instance?.EnterMainMenu();
        }
    }
}
