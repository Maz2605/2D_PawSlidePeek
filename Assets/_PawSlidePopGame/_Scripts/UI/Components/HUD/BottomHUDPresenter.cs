using _PawSlidePopGame._Scripts.Data.Events;
using _PawSlidePopGame._Scripts.Data.Events.Payloads;
using _PawSlidePopGame._Scripts.Feature.Match3.Flow;
using _PawSlidePopGame.Scripts.DesignPattern.ObserverPattern;
using UnityEngine;

namespace _PawSlidePopGame._Scripts.UI.Components.HUD
{
    public class BottomHUDPresenter : MonoBehaviour
    {
        [SerializeField] private ChargedAbilityView chargedAbilityView;

        private void Awake()
        {
            EnsureView();
        }

        private void OnValidate()
        {
            if (chargedAbilityView == null)
            {
                chargedAbilityView = GetComponentInChildren<ChargedAbilityView>(true);
            }
        }

        private void OnEnable()
        {
            EnsureView();
            EventManager<LogicGameEvent>.AddListener<GameplayHudSnapshot>(LogicGameEvent.GameplayHudInitialized, HandleHudSnapshot);
            EventManager<LogicGameEvent>.AddListener<GameplayHudSnapshot>(LogicGameEvent.GameplayHudStateChanged, HandleHudSnapshot);

            if (chargedAbilityView != null)
            {
                chargedAbilityView.OnUseRequested += HandleChargedUseRequested;
                chargedAbilityView.OnCancelRequested += HandleChargedCancelRequested;
            }
        }

        private void OnDisable()
        {
            EventManager<LogicGameEvent>.RemoveListener<GameplayHudSnapshot>(LogicGameEvent.GameplayHudInitialized, HandleHudSnapshot);
            EventManager<LogicGameEvent>.RemoveListener<GameplayHudSnapshot>(LogicGameEvent.GameplayHudStateChanged, HandleHudSnapshot);

            if (chargedAbilityView != null)
            {
                chargedAbilityView.OnUseRequested -= HandleChargedUseRequested;
                chargedAbilityView.OnCancelRequested -= HandleChargedCancelRequested;
            }
        }

        private void HandleHudSnapshot(GameplayHudSnapshot snapshot)
        {
            chargedAbilityView?.SetData(snapshot?.chargedAbility);
        }

        private void HandleChargedUseRequested()
        {
            GameFlowManager.Instance?.EnterChargedPlacementMode();
        }

        private void HandleChargedCancelRequested()
        {
            GameFlowManager.Instance?.CancelChargedAbilityMode();
        }

        private void EnsureView()
        {
            if (chargedAbilityView != null)
            {
                return;
            }

            GameObject chargedHudObject = new GameObject("ChargedAbilityView", typeof(RectTransform));
            chargedHudObject.transform.SetParent(transform, false);
            chargedAbilityView = chargedHudObject.AddComponent<ChargedAbilityView>();
        }
    }
}
