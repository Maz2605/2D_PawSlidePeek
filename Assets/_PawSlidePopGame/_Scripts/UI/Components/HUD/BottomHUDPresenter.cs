using _PawSlidePopGame._Scripts.Core.System.GameFlow;
using _PawSlidePopGame._Scripts.Data.Events;
using _PawSlidePopGame._Scripts.Data.Events.Payloads;
using _PawSlidePopGame._Scripts.Feature.Match3.Flow;
using _PawSlidePopGame.Scripts.DesignPattern.ObserverPattern;
using UnityEngine;
using DG.Tweening;

namespace _PawSlidePopGame._Scripts.UI.Components.HUD
{
    public class BottomHUDPresenter : MonoBehaviour
    {
        [SerializeField] private ChargedAbilityView chargedAbilityView;
        private bool _inputEnabled = true;

        private Vector3 _originalLocalPos;
        private bool _hasOriginalLocalPos;

        private void Awake()
        {
            EnsureView();
            CacheOriginalPosition();
        }

        private void CacheOriginalPosition()
        {
            if (!_hasOriginalLocalPos)
            {
                _originalLocalPos = transform.localPosition;
                _hasOriginalLocalPos = true;
            }
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
            EventManager<LogicGameEvent>.AddListener<GameplayHudSnapshot>(LogicGameEvent.GameplayHudInitialized, HandleHudInitialized);
            EventManager<LogicGameEvent>.AddListener<GameplayHudSnapshot>(LogicGameEvent.GameplayHudStateChanged, HandleHudSnapshot);
            EventManager<LogicGameEvent>.AddListener<InGameSubStateChangedPayload>(
                LogicGameEvent.InGameSubStateChanged,
                HandleSubStateChanged);

            if (chargedAbilityView != null)
            {
                chargedAbilityView.OnUseRequested += HandleChargedUseRequested;
                chargedAbilityView.OnCancelRequested += HandleChargedCancelRequested;
            }

            SetInputEnabled(GameFlowManager.Instance != null &&
                            GameFlowManager.IsInteractiveGameplaySubState(GameFlowManager.Instance.CurrentInGameSubState));
        }

        private void OnDisable()
        {
            EventManager<LogicGameEvent>.RemoveListener<GameplayHudSnapshot>(LogicGameEvent.GameplayHudInitialized, HandleHudInitialized);
            EventManager<LogicGameEvent>.RemoveListener<GameplayHudSnapshot>(LogicGameEvent.GameplayHudStateChanged, HandleHudSnapshot);
            EventManager<LogicGameEvent>.RemoveListener<InGameSubStateChangedPayload>(
                LogicGameEvent.InGameSubStateChanged,
                HandleSubStateChanged);

            if (chargedAbilityView != null)
            {
                chargedAbilityView.OnUseRequested -= HandleChargedUseRequested;
                chargedAbilityView.OnCancelRequested -= HandleChargedCancelRequested;
            }
        }

        public void ResetView()
        {
            _inputEnabled = true;

            if (_hasOriginalLocalPos)
            {
                transform.DOKill();
                transform.localPosition = _originalLocalPos;
            }

            chargedAbilityView?.ResetView();
        }

        private void HandleHudSnapshot(GameplayHudSnapshot snapshot)
        {
            chargedAbilityView?.SetData(snapshot?.chargedAbility);
            chargedAbilityView?.SetInputEnabled(_inputEnabled);
        }

        private void HandleChargedUseRequested()
        {
            GameFlowManager.Instance?.EnterChargedPlacementMode();
        }

        private void HandleChargedCancelRequested()
        {
            GameFlowManager.Instance?.CancelChargedAbilityMode();
        }

        private void HandleSubStateChanged(InGameSubStateChangedPayload payload)
        {
            SetInputEnabled(GameFlowManager.IsInteractiveGameplaySubState(payload.Current));
        }

        public void SetInputEnabled(bool enabled)
        {
            _inputEnabled = enabled;
            chargedAbilityView?.SetInputEnabled(enabled);
        }

        private void HandleHudInitialized(GameplayHudSnapshot snapshot)
        {
            HandleHudSnapshot(snapshot);
            PlayIntroAnimation();
        }

        public void PlayIntroAnimation()
        {
            CacheOriginalPosition();
            transform.DOKill();
            transform.localPosition = _originalLocalPos - new Vector3(0f, 400f, 0f);
            transform.DOLocalMove(_originalLocalPos, 0.8f)
                .SetEase(Ease.OutBack)
                .SetLink(gameObject);

            BoosterWidget boosterWidget = GetComponentInChildren<BoosterWidget>(true);
            if (boosterWidget != null)
            {
                boosterWidget.PlayIntroAnimation();
            }
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
