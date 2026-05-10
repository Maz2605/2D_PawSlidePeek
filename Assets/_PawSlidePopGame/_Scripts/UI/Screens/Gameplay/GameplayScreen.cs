using _PawSlidePopGame._Scripts.Feature.Match3.Flow;
using _PawSlidePopGame._Scripts.UI.Base;
using _PawSlidePopGame._Scripts.UI.Components.HUD;
using UnityEngine;
using UnityEngine.UI;

namespace _PawSlidePopGame._Scripts.UI.Screens.Gameplay
{
    public class GameplayScreen : BaseScreen
    {
        [SerializeField] private TopHUDPresenter topHUDPresenter;
        [SerializeField] private BottomHUDPresenter bottomHUDPresenter;
        [SerializeField] private Button pauseButton;

        protected override void OnBeforeShow()
        {
            BindButton(pauseButton, HandlePausePressed);
        }

        private void HandlePausePressed()
        {
            GameFlowManager.Instance?.PauseGameplay();
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
