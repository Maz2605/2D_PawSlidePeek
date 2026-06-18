using _PawSlidePopGame._Scripts.Data.Events;
using _PawSlidePopGame._Scripts.Data.Events.Payloads;
using _PawSlidePopGame.Scripts.DesignPattern.ObserverPattern;
using UnityEngine;

namespace _PawSlidePopGame._Scripts.Core.Vibration
{
    [DisallowMultipleComponent]
    public sealed class VibrationController : MonoBehaviour
    {
        [SerializeField] private bool dontDestroyOnLoad = true;

        private bool _eventsBound;

        private void Awake()
        {
            if (dontDestroyOnLoad)
            {
                DontDestroyOnLoad(gameObject);
            }
        }

        private void OnEnable()
        {
            if (_eventsBound)
            {
                return;
            }

            EventManager<FeedbackEvent>.AddListener(
                FeedbackEvent.UiButtonTap,
                HandleUiButtonTap);
            EventManager<FeedbackEvent>.AddListener(
                FeedbackEvent.VibrationPreviewRequested,
                HandleVibrationPreviewRequested);
            EventManager<FeedbackEvent>.AddListener(
                FeedbackEvent.MoveSuccess,
                HandleMoveSuccessFeedback);
            EventManager<FeedbackEvent>.AddListener(
                FeedbackEvent.MoveReject,
                HandleMoveRejectFeedback);
            EventManager<FeedbackEvent>.AddListener(
                FeedbackEvent.BoosterSelect,
                HandleBoosterSelectFeedback);
            EventManager<FeedbackEvent>.AddListener(
                FeedbackEvent.BoosterReject,
                HandleBoosterRejectFeedback);


            _eventsBound = true;
        }

        private void OnDisable()
        {
            if (!_eventsBound)
            {
                return;
            }

            EventManager<FeedbackEvent>.RemoveListener(
                FeedbackEvent.UiButtonTap,
                HandleUiButtonTap);
            EventManager<FeedbackEvent>.RemoveListener(
                FeedbackEvent.VibrationPreviewRequested,
                HandleVibrationPreviewRequested);
            EventManager<FeedbackEvent>.RemoveListener(
                FeedbackEvent.MoveSuccess,
                HandleMoveSuccessFeedback);
            EventManager<FeedbackEvent>.RemoveListener(
                FeedbackEvent.MoveReject,
                HandleMoveRejectFeedback);
            EventManager<FeedbackEvent>.RemoveListener(
                FeedbackEvent.BoosterSelect,
                HandleBoosterSelectFeedback);
            EventManager<FeedbackEvent>.RemoveListener(
                FeedbackEvent.BoosterReject,
                HandleBoosterRejectFeedback);

            _eventsBound = false;
        }

        private void HandleUiButtonTap()
        {
            PlayButtonTap();
        }

        private void HandleVibrationPreviewRequested()
        {
            PlayTogglePreview();
        }

        private void HandleMoveSuccessFeedback()
        {
            PlayMoveSuccess();
        }

        private void HandleMoveRejectFeedback()
        {
            PlayReject();
        }

        private void HandleBoosterSelectFeedback()
        {
            PlayBoosterSelect();
        }

        private void HandleBoosterRejectFeedback()
        {
            PlayReject();
        }

        public void PlayButtonTap()
        {
            VibrationManager.Instance?.PlayButtonTap();
        }

        public void PlayTogglePreview()
        {
            VibrationManager.Instance?.PlayTogglePreview();
        }

        public void PlayMoveSuccess()
        {
            VibrationManager.Instance?.PlayMoveSuccess();
        }

        public void PlayReject()
        {
            VibrationManager.Instance?.PlayReject();
        }

        public void PlayBoosterSelect()
        {
            VibrationManager.Instance?.PlayBoosterSelect();
        }

        public void PlayWin()
        {
            VibrationManager.Instance?.PlayWin();
        }

        public void PlayLose()
        {
            VibrationManager.Instance?.PlayLose();
        }

    }
}
