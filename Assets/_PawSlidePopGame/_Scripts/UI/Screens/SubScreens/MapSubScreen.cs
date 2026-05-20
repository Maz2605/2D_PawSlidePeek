using _PawSlidePopGame._Scripts.Core.System.GameFlow;
using _PawSlidePopGame._Scripts.UI.Base;
using UnityEngine;
using UnityEngine.UI;

namespace _PawSlidePopGame._Scripts.UI.Screens.SubScreens
{
    public class MapSubScreen : BaseSubScreen
    {
        [SerializeField] private Button btnLevel;
        [SerializeField] private string tempLevelId = "Level_001";

        public override void Init()
        {
            if (isInitialized)
            {
                return;
            }

            base.Init();
            TryAutoBindReferences();
            BindButton(btnLevel, HandleLevelPressed);
        }

        private void HandleLevelPressed()
        {
            if (!GameAppFlowManager.Instance.RequestStartLevel(tempLevelId))
            {
                Debug.LogWarning("[MapSubScreen] Failed to request gameplay start.", this);
            }
        }

        private void OnValidate()
        {
            TryAutoBindReferences();
        }

        private void TryAutoBindReferences()
        {
            if (btnLevel != null)
            {
                return;
            }

            Button[] buttons = GetComponentsInChildren<Button>(true);
            for (int i = 0; i < buttons.Length; i++)
            {
                if (buttons[i] != null && buttons[i].name == "BtnLevel")
                {
                    btnLevel = buttons[i];
                    return;
                }
            }
        }
    }
}
