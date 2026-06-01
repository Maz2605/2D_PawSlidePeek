using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace _PawSlidePopGame._Scripts.Feature.LevelEditor.UI
{
    public sealed class LevelEditorBoardCellView : MonoBehaviour, IPointerDownHandler, IPointerEnterHandler, IPointerUpHandler
    {
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

        private static bool _isDraggingPaint;
        private int _x;
        private int _y;
        private Action<int, int> _onPaintRequested;

        public void Bind(
            int x,
            int y,
            Sprite underlaySprite,
            Sprite itemSprite,
            Sprite overlaySprite,
            bool selected,
            bool playable,
            Action<int, int> onPaintRequested)
        {
            _x = x;
            _y = y;
            _onPaintRequested = onPaintRequested;

            if (coordinateText != null)
            {
                coordinateText.text = $"({x}, {y})";
            }

            if (valueText != null)
            {
                valueText.text = string.Empty;
            }

            SetImage(underlayImage, underlaySprite);
            SetImage(itemImage, itemSprite);
            SetImage(overlayImage, overlaySprite);

            if (selectedHighlight != null)
            {
                selectedHighlight.enabled = selected;
            }

            if (playableGraphic != null)
            {
                playableGraphic.color = playable ? playableColor : disabledColor;
            }

            if (emptyStateRoot != null)
            {
                emptyStateRoot.SetActive(playable && underlaySprite == null && itemSprite == null && overlaySprite == null);
            }
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left)
            {
                return;
            }

            _isDraggingPaint = true;
            RequestPaint();
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (_isDraggingPaint && Mouse.current != null && !Mouse.current.leftButton.isPressed)
            {
                _isDraggingPaint = false;
            }

            if (_isDraggingPaint)
            {
                RequestPaint();
            }
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (eventData.button == PointerEventData.InputButton.Left)
            {
                _isDraggingPaint = false;
            }
        }

        private void OnDisable()
        {
            _isDraggingPaint = false;
        }

        private void RequestPaint()
        {
            _onPaintRequested?.Invoke(_x, _y);
        }

        private static void SetImage(Image image, Sprite sprite)
        {
            if (image == null)
            {
                return;
            }

            image.sprite = sprite;
            image.enabled = sprite != null;
        }
    }
}
