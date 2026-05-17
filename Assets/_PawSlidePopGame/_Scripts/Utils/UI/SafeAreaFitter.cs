using UnityEngine;

namespace _PawSlidePopGame._Scripts.Utils.UI
{
    /// <summary>
    /// Tự động co RectTransform sao cho vừa khít với vùng an toàn (Safe Area) của thiết bị.
    /// Tránh UI bị lẹm vào Tai thỏ (Notch), Dynamic Island, hoặc thanh điều hướng ảo.
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    [ExecuteAlways] 
    public class SafeAreaFitter : MonoBehaviour
    {
        private RectTransform _rectTransform;
        private Rect _lastSafeArea = new Rect(0, 0, 0, 0);
        private Canvas _canvas;

        private void Awake()
        {
            CacheReferences();
            ApplySafeArea(true);
        }

        private void OnEnable()
        {
            CacheReferences();
            ApplySafeArea();
        }

        /// <summary>
        /// Bắt sự kiện khi màn hình xoay hoặc thay đổi kích thước.
        /// Tối ưu hiệu năng: Thay thế hoàn toàn cho việc gọi trong Update().
        /// </summary>
        private void OnRectTransformDimensionsChange()
        {
            ApplySafeArea();
        }

#if UNITY_EDITOR
        private void Update()
        {
            if (!Application.isPlaying)
            {
                ApplySafeArea();
            }
        }
#endif

        private void CacheReferences()
        {
            if (_rectTransform == null)
            {
                _rectTransform = GetComponent<RectTransform>();
            }

            if (_canvas == null)
            {
                _canvas = GetComponentInParent<Canvas>();
            }
        }

        private void ApplySafeArea(bool force = false)
        {
            CacheReferences();
            if (_rectTransform == null || _canvas == null)
            {
                return;
            }

            if (Screen.width <= 0 || Screen.height <= 0)
            {
                return;
            }

            Rect safeArea = Screen.safeArea;
            if (!IsFinite(safeArea) || safeArea.width <= 0f || safeArea.height <= 0f)
            {
                return;
            }

            if (!force && safeArea == _lastSafeArea)
            {
                return;
            }

            _lastSafeArea = safeArea;
            SanitizeRectTransform();

            Vector2 anchorMin = safeArea.position;
            Vector2 anchorMax = safeArea.position + safeArea.size;

            anchorMin.x /= Screen.width;
            anchorMin.y /= Screen.height;
            anchorMax.x /= Screen.width;
            anchorMax.y /= Screen.height;

            if (!IsFinite(anchorMin) || !IsFinite(anchorMax))
            {
                return;
            }

            _rectTransform.anchorMin = anchorMin;
            _rectTransform.anchorMax = anchorMax;
        }

        private void SanitizeRectTransform()
        {
            if (!IsFinite(_rectTransform.anchoredPosition))
            {
                _rectTransform.anchoredPosition = Vector2.zero;
            }

            if (!IsFinite(_rectTransform.sizeDelta))
            {
                _rectTransform.sizeDelta = Vector2.zero;
            }

            if (!IsFinite(_rectTransform.offsetMin))
            {
                _rectTransform.offsetMin = Vector2.zero;
            }

            if (!IsFinite(_rectTransform.offsetMax))
            {
                _rectTransform.offsetMax = Vector2.zero;
            }

            if (!IsFinite(_rectTransform.localPosition))
            {
                _rectTransform.localPosition = Vector3.zero;
            }
        }

        private static bool IsFinite(Rect rect)
        {
            return IsFinite(rect.position) && IsFinite(rect.size);
        }

        private static bool IsFinite(Vector2 value)
        {
            return IsFinite(value.x) && IsFinite(value.y);
        }

        private static bool IsFinite(Vector3 value)
        {
            return IsFinite(value.x) && IsFinite(value.y) && IsFinite(value.z);
        }

        private static bool IsFinite(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }
    }
}
