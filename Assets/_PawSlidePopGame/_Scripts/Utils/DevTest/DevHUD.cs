using _PawSlidePopGame._Scripts.UI.Base;
using _PawSlidePopGame._Scripts.UI.Manager;
using _PawSlidePopGame._Scripts.UI.Popups;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

namespace _PawSlidePopGame._Scripts.Utils.DevTest
{
    public class DevHUD : MonoBehaviour
    {

        public void PlayWinPopup()
        {
            UIManager.Instance.ShowPopup<BasePopup>(PopupID.WinPopup);
        }

        public void PlayLosePopup()
        {
            UIManager.Instance.ShowPopup<BasePopup>(PopupID.LosePopup);
        }

        public void PlayPauseButton()
        {
            UIManager.Instance.ShowPopup<BasePopup>(PopupID.PausePopup);
        }
    }
}