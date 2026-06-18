using System;
using _PawSlidePopGame._Scripts.Data.Audio;
using _PawSlidePopGame._Scripts.Data.Events;
using DG.Tweening;
using _PawSlidePopGame.Scripts.DesignPattern.ObserverPattern;
using UnityEngine;
using UnityEngine.UI;

namespace _PawSlidePopGame._Scripts.UI.Base
{
    public class BaseHUD : MonoBehaviour
    {
        protected void BindButton(Button btn, Action onClickAction)
        {
            if (btn == null) return;
            
            btn.onClick?.RemoveAllListeners();
            btn.onClick?.AddListener(() =>
            {
                bool hasButtonSound = btn.TryGetComponent<UIButtonSound>(out var _);
                Debug.Log($"[BaseHUD] BindButton clicked: '{btn.name}' on HUD '{gameObject.name}'. hasUIButtonSound={hasButtonSound}");
                
                if (!hasButtonSound)
                {
                    Debug.Log("[BaseHUD] Posting FeedbackEvent.UiButtonTap");
                    EventManager<FeedbackEvent>.Post(FeedbackEvent.UiButtonTap);
                }
                
                btn.transform.DOKill();
                btn.transform.localScale = Vector3.one;
                btn.transform
                    .DOPunchScale(Vector3.one * -0.1f, 0.15f, 5) 
                    .SetUpdate(true)
                    .SetLink(btn.gameObject, LinkBehaviour.KillOnDisable)
                    .OnComplete(() => onClickAction?.Invoke());
            });
        }
    }
}
