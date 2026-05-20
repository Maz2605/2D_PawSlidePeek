using System;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace _PawSlidePopGame._Scripts.UI.Components.NavigationBar
{
    [Serializable]
    public class NavTab
    {
        public MainTabID tabID;
        public Button btn;
        public RectTransform rectTransform;

        [Header("Core Target")]
        public RectTransform iconTarget;
        public RectTransform contentRoot;
        public Graphic backgroundGraphic;

        [Header("Optional Visual Hooks")]
        public CanvasGroup contentGroup;
        public Graphic[] tintTargets;
        public RectTransform glowTarget;
        public Graphic glowGraphic;

        [HideInInspector] public Sequence animSeq;
        [NonSerialized] public TabPoseCache poseCache;
    }

    [Serializable]
    public class TabPoseCache
    {
        public Vector2 contentAnchoredPosition;
        public Vector3 contentScale;
        public Vector3 contentEulerAngles;
    }

    [RequireComponent(typeof(RectTransform))]
    public class NavigationBar : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        [Header("--- UI References ---")]
        [SerializeField] private RectTransform sliderBackground;
        
        [Header("--- Tabs Setup ---")]
        [SerializeField] private NavTab[] tabs;

        [Header("--- Candy Crush Feel (Juicy) ---")]
        [SerializeField] private float sliderMoveDuration = 0.25f;
        [SerializeField] private float sliderOvershoot = 1.15f; // Độ nảy của thanh trượt
        [SerializeField] private float activeContentScale = 1.25f; // Phóng to hơn bình thường
        [SerializeField] private Ease bounceEase = Ease.OutBack; // Tạo độ nảy chuẩn casual
        
        [Header("--- Colors & Alpha ---")]
        [SerializeField] private float activeShadowAlpha = 1f;
        [SerializeField] private float inactiveShadowAlpha = 0f;
        [SerializeField] private Color activeBackgroundColor = new(1f, 0.86f, 0.32f, 1f);
        [SerializeField] private Color inactiveBackgroundColor = new(1f, 1f, 1f, 0.65f);
        [SerializeField] private float iconSelectedAlpha = 1f;
        [SerializeField] private float iconInactiveAlpha = 0.6f;

        [Header("--- Drag Setting ---")]
        [SerializeField] private float baseSwipeThreshold = 60f; // Nhạy hơn một chút
        
        // Mở cổng Event để Manager bên ngoài bắt và chạy Âm thanh / Rung (Haptic)
        public event Action<MainTabID> OnTabClicked;
        public event Action OnDragInteraction; 

        public MainTabID CurrentTab => _currentTab;
        private MainTabID _currentTab = (MainTabID)(-1);
        private RectTransform _containerRect;
        private float _startDragX;
        private int _currentDragPointerId = -999;
        private bool _isInitialized;

        private Tweener _sliderPosTween;
        private Sequence _sliderScaleSequence;

        public void Init()
        {
            if (_isInitialized) return;

            _containerRect = GetComponent<RectTransform>();

            if (tabs == null || tabs.Length == 0) return;

            foreach (NavTab tab in tabs)
            {
                if (tab.contentRoot == null) tab.contentRoot = tab.iconTarget;
                
                CacheTabPose(tab);
                SetTabVisualInstant(tab, false);

                MainTabID id = tab.tabID;
                tab.btn.onClick.RemoveAllListeners();
                
                // Hiệu ứng "Punch" nhẹ khi chạm vào nút trước khi chuyển tab
                tab.btn.onClick.AddListener(() =>
                {
                    if (_currentDragPointerId == -999)
                    {
                        tab.contentRoot.DOPunchScale(new Vector3(-0.1f, -0.1f, 0), 0.15f, 1, 0.5f)
                            .OnComplete(() => ChangeTab(id));
                        
                        OnDragInteraction?.Invoke(); // Trigger âm thanh "Click" nhỏ
                    }
                });
            }

            _isInitialized = true;
        }

        public void ChangeTab(MainTabID tabID, bool instant = false)
        {
            if (_currentTab == tabID && !instant) return;

            bool isNew = _currentTab != tabID;
            _currentTab = tabID;

            AnimateSliderToTab(tabID, instant);

            foreach (NavTab tab in tabs)
            {
                if (tab != null && tab.contentRoot != null)
                {
                    AnimateTabSelection(tab, tab.tabID == tabID, instant);
                }
            }

            if (isNew)
            {
                OnTabClicked?.Invoke(tabID); // Nơi gọi tiếng "Pop" to và Rung Haptic
            }
        }

        // Tối ưu GC: Thay Array.Find bằng vòng lặp for truyền thống
        private NavTab GetTabByID(MainTabID id)
        {
            for (int i = 0; i < tabs.Length; i++)
            {
                if (tabs[i].tabID == id) return tabs[i];
            }
            return null;
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (!_isInitialized || _currentDragPointerId != -999) return;
            
            _currentDragPointerId = eventData.pointerId;
            _sliderPosTween?.Kill();
            
            RectTransformUtility.ScreenPointToLocalPointInRectangle(_containerRect, eventData.position, eventData.pressEventCamera, out Vector2 startPt);
            _startDragX = startPt.x;
        }

        public void OnDrag(PointerEventData eventData)
        {
            // Đã loại bỏ logic update Graphic mỗi frame ở đây để cứu Performance CPU/Canvas
            if (eventData.pointerId != _currentDragPointerId) return;

            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(_containerRect, eventData.position, eventData.pressEventCamera, out Vector2 localPt))
            {
                float clampedX = Mathf.Clamp(localPt.x, GetTabTargetX(tabs[0]), GetTabTargetX(tabs[tabs.Length - 1]));
                if (sliderBackground != null)
                {
                    sliderBackground.anchoredPosition = new Vector2(clampedX, sliderBackground.anchoredPosition.y);
                }
            }
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (eventData.pointerId != _currentDragPointerId) return;
            
            _currentDragPointerId = -999;
            
            RectTransformUtility.ScreenPointToLocalPointInRectangle(_containerRect, eventData.position, eventData.pressEventCamera, out Vector2 endPt);
            float deltaX = endPt.x - _startDragX;

            MainTabID targetID = _currentTab;

            // Xử lý vuốt để chuyển tab
            if (Mathf.Abs(deltaX) > baseSwipeThreshold)
            {
                int currentIndex = GetTabIndex(_currentTab);
                int direction = deltaX > 0 ? 1 : -1;
                int targetIndex = Mathf.Clamp(currentIndex + direction, 0, tabs.Length - 1);
                targetID = tabs[targetIndex].tabID;
            }

            ChangeTab(targetID, false);
        }

        private int GetTabIndex(MainTabID id)
        {
            for (int i = 0; i < tabs.Length; i++)
            {
                if (tabs[i].tabID == id) return i;
            }
            return 0;
        }

        private float GetTabTargetX(NavTab tab)
        {
            return _containerRect.InverseTransformPoint(tab.rectTransform.position).x;
        }

        private void AnimateSliderToTab(MainTabID tabID, bool instant)
        {
            NavTab targetTab = GetTabByID(tabID);
            if (targetTab == null || sliderBackground == null) return;

            float targetX = GetTabTargetX(targetTab);
            _sliderPosTween?.Kill();
            _sliderScaleSequence?.Kill();

            if (instant)
            {
                sliderBackground.anchoredPosition = new Vector2(targetX, sliderBackground.anchoredPosition.y);
                sliderBackground.localScale = Vector3.one;
                return;
            }

            // DOTween: Kéo background chạy sang với độ nảy nhẹ
            _sliderPosTween = sliderBackground.DOAnchorPosX(targetX, sliderMoveDuration)
                .SetEase(Ease.OutCubic);
            
            // DOTween: Squash & Stretch cho background lúc trượt
            _sliderScaleSequence = DOTween.Sequence();
            _sliderScaleSequence
                .Append(sliderBackground.DOScale(new Vector3(1.1f, 0.9f, 1f), sliderMoveDuration * 0.5f))
                .Append(sliderBackground.DOScale(Vector3.one, sliderMoveDuration * 0.5f).SetEase(bounceEase));
        }

        private void AnimateTabSelection(NavTab tab, bool selected, bool instant)
        {
            tab.animSeq?.Kill();
            
            Vector3 targetScale = tab.poseCache.contentScale * (selected ? activeContentScale : 1f);
            Color targetBgColor = selected ? activeBackgroundColor : inactiveBackgroundColor;
            float targetAlpha = selected ? iconSelectedAlpha : iconInactiveAlpha;

            if (instant)
            {
                SetTabVisualInstant(tab, selected);
                return;
            }

            tab.animSeq = DOTween.Sequence();
            
            // Animation nảy lên mạnh mẽ (Candy Crush feel)
            tab.animSeq.Join(tab.contentRoot.DOScale(targetScale, sliderMoveDuration).SetEase(selected ? bounceEase : Ease.OutQuad));
            
            // Thay đổi màu sắc nền
            if (tab.backgroundGraphic != null)
            {
                tab.animSeq.Join(tab.backgroundGraphic.DOColor(targetBgColor, sliderMoveDuration));
            }

            // Alpha cho Icon
            if (tab.contentGroup != null)
            {
                tab.animSeq.Join(tab.contentGroup.DOFade(targetAlpha, sliderMoveDuration));
            }
        }

        private void CacheTabPose(NavTab tab)
        {
            tab.poseCache = new TabPoseCache
            {
                contentAnchoredPosition = tab.contentRoot.anchoredPosition,
                contentScale = tab.contentRoot.localScale,
                contentEulerAngles = tab.contentRoot.localEulerAngles
            };
        }

        private void SetTabVisualInstant(NavTab tab, bool selected)
        {
            tab.contentRoot.localScale = tab.poseCache.contentScale * (selected ? activeContentScale : 1f);
            if (tab.backgroundGraphic != null) tab.backgroundGraphic.color = selected ? activeBackgroundColor : inactiveBackgroundColor;
            if (tab.contentGroup != null) tab.contentGroup.alpha = selected ? iconSelectedAlpha : iconInactiveAlpha;
        }

        private void OnDestroy()
        {
            _sliderPosTween?.Kill();
            _sliderScaleSequence?.Kill();
            foreach (NavTab tab in tabs) tab?.animSeq?.Kill();
        }
    }
}