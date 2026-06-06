using System;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace _PawSlidePopGame._Scripts.Feature.LevelEditor.UI
{
    public enum LevelEditorBoardCellAction
    {
        Paint = 0,
        Erase = 1
    }

    public sealed class LevelEditorBoardCellView : MonoBehaviour, IPointerDownHandler, IPointerEnterHandler, IPointerUpHandler
    {
        private const float LayerTweenDuration = 0.12f;
        private const float SelectionTweenDuration = 0.1f;
        private const float PlayableTweenDuration = 0.1f;

        [SerializeField] private TMP_Text coordinateText;
        [SerializeField] private TMP_Text valueText;
        [SerializeField] private Image underlayImage;
        [SerializeField] private Image itemImage;
        [SerializeField] private Image overlayImage;
        [SerializeField] private Image selectedHighlight;
        [SerializeField] private Graphic playableGraphic;
        [SerializeField] private GameObject emptyStateRoot;
        [SerializeField] private Color playableColor = Color.white;
        [SerializeField] private Color disabledColor = new Color(0.35f, 0.35f, 0.35f, 1f);

        private static bool _isDraggingAction;
        private static LevelEditorBoardCellAction _dragAction;
        private int _x;
        private int _y;
        private bool _hasBound;
        private bool _lastSelected;
        private bool _lastPlayable;
        private Sprite _lastUnderlaySprite;
        private Sprite _lastItemSprite;
        private Sprite _lastOverlaySprite;
        private Action<int, int, LevelEditorBoardCellAction> _onActionRequested;

        public void Bind(
            int x,
            int y,
            Sprite underlaySprite,
            Sprite itemSprite,
            Sprite overlaySprite,
            bool selected,
            bool playable,
            Action<int, int, LevelEditorBoardCellAction> onActionRequested)
        {
            _x = x;
            _y = y;
            _onActionRequested = onActionRequested;

            if (coordinateText != null)
            {
                coordinateText.text = $"({x}, {y})";
            }

            if (valueText != null)
            {
                valueText.text = string.Empty;
            }

            bool animate = Application.isPlaying && _hasBound;
            SetLayerImage(underlayImage, underlaySprite, _lastUnderlaySprite, animate);
            SetLayerImage(itemImage, itemSprite, _lastItemSprite, animate);
            SetLayerImage(overlayImage, overlaySprite, _lastOverlaySprite, animate);
            SetSelected(selected, animate && selected != _lastSelected);
            SetPlayable(playable, animate && playable != _lastPlayable);

            if (emptyStateRoot != null)
            {
                emptyStateRoot.SetActive(playable && underlaySprite == null && itemSprite == null && overlaySprite == null);
            }

            _lastUnderlaySprite = underlaySprite;
            _lastItemSprite = itemSprite;
            _lastOverlaySprite = overlaySprite;
            _lastSelected = selected;
            _lastPlayable = playable;
            _hasBound = true;
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left &&
                eventData.button != PointerEventData.InputButton.Right)
            {
                return;
            }

            _dragAction = eventData.button == PointerEventData.InputButton.Right
                ? LevelEditorBoardCellAction.Erase
                : LevelEditorBoardCellAction.Paint;
            _isDraggingAction = true;
            RequestAction(_dragAction);
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (_isDraggingAction && Mouse.current != null)
            {
                bool isButtonStillPressed = _dragAction == LevelEditorBoardCellAction.Erase
                    ? Mouse.current.rightButton.isPressed
                    : Mouse.current.leftButton.isPressed;
                if (!isButtonStillPressed)
                {
                    _isDraggingAction = false;
                }
            }

            if (_isDraggingAction)
            {
                RequestAction(_dragAction);
            }
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if ((eventData.button == PointerEventData.InputButton.Left && _dragAction == LevelEditorBoardCellAction.Paint) ||
                (eventData.button == PointerEventData.InputButton.Right && _dragAction == LevelEditorBoardCellAction.Erase))
            {
                _isDraggingAction = false;
            }
        }

        private void OnDisable()
        {
            _isDraggingAction = false;
            KillTweens();
        }

        private void OnDestroy()
        {
            KillTweens();
        }

        private void RequestAction(LevelEditorBoardCellAction action)
        {
            _onActionRequested?.Invoke(_x, _y, action);
        }

        private static void SetLayerImage(Image image, Sprite nextSprite, Sprite previousSprite, bool animate)
        {
            if (image == null)
            {
                return;
            }

            image.DOKill(false);
            image.transform.DOKill(false);

            if (!animate)
            {
                image.sprite = nextSprite;
                image.enabled = nextSprite != null;
                SetGraphicAlpha(image, nextSprite != null ? 1f : 0f);
                image.transform.localScale = Vector3.one;
                return;
            }

            if (nextSprite == previousSprite)
            {
                if (nextSprite != null)
                {
                    image.sprite = nextSprite;
                    image.enabled = true;
                    SetGraphicAlpha(image, 1f);
                    image.transform.localScale = Vector3.one;
                }
                return;
            }

            if (nextSprite == null)
            {
                image.DOFade(0f, LayerTweenDuration).SetEase(Ease.OutQuad);
                image.transform.DOScale(0.85f, LayerTweenDuration)
                    .SetEase(Ease.OutQuad)
                    .OnComplete(() =>
                    {
                        image.sprite = null;
                        image.enabled = false;
                        image.transform.localScale = Vector3.one;
                    });
                return;
            }

            image.sprite = nextSprite;
            image.enabled = true;
            SetGraphicAlpha(image, 0f);
            image.transform.localScale = Vector3.one * 0.85f;
            image.DOFade(1f, LayerTweenDuration).SetEase(Ease.OutQuad);
            image.transform.DOScale(1f, LayerTweenDuration).SetEase(Ease.OutBack);
        }

        private void SetSelected(bool selected, bool animate)
        {
            if (selectedHighlight == null)
            {
                return;
            }

            selectedHighlight.DOKill(false);
            selectedHighlight.transform.DOKill(false);

            if (!animate)
            {
                selectedHighlight.enabled = selected;
                SetGraphicAlpha(selectedHighlight, selected ? 1f : 0f);
                selectedHighlight.transform.localScale = Vector3.one;
                return;
            }

            if (selected)
            {
                selectedHighlight.enabled = true;
                SetGraphicAlpha(selectedHighlight, 0f);
                selectedHighlight.transform.localScale = Vector3.one * 0.95f;
                selectedHighlight.DOFade(1f, SelectionTweenDuration).SetEase(Ease.OutQuad);
                selectedHighlight.transform.DOScale(1f, SelectionTweenDuration).SetEase(Ease.OutBack);
                return;
            }

            selectedHighlight.DOFade(0f, SelectionTweenDuration)
                .SetEase(Ease.OutQuad)
                .OnComplete(() => selectedHighlight.enabled = false);
        }

        private void SetPlayable(bool playable, bool animate)
        {
            if (playableGraphic == null)
            {
                return;
            }

            playableGraphic.DOKill(false);
            Color targetColor = playable ? playableColor : disabledColor;
            if (animate)
            {
                playableGraphic.DOColor(targetColor, PlayableTweenDuration).SetEase(Ease.OutQuad);
            }
            else
            {
                playableGraphic.color = targetColor;
            }
        }

        private void KillTweens()
        {
            KillGraphicTween(underlayImage);
            KillGraphicTween(itemImage);
            KillGraphicTween(overlayImage);
            KillGraphicTween(selectedHighlight);
            KillGraphicTween(playableGraphic);
        }

        private static void KillGraphicTween(Graphic graphic)
        {
            if (graphic == null)
            {
                return;
            }

            graphic.DOKill(false);
            graphic.transform.DOKill(false);
        }

        private static void SetGraphicAlpha(Graphic graphic, float alpha)
        {
            Color color = graphic.color;
            color.a = alpha;
            graphic.color = color;
        }
    }
}
