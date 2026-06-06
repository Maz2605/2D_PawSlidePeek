using System;
using System.Collections.Generic;
using _PawSlidePopGame._Scripts.Core.System.DesignPattern.Singleton;
using _PawSlidePopGame._Scripts.UI.Base;
using _PawSlidePopGame._Scripts.UI.Popups;
using _PawSlidePopGame._Scripts.UI.Screens;
using _PawSlidePopGame._Scripts.UI.TopLevels;
using UnityEngine;
using UnityEngine.UI;

namespace _PawSlidePopGame._Scripts.UI.Manager
{
    [Serializable]
    public struct ScreenConfig
    {
        public ScreenID id;
        public BaseScreen prefab; 
    }
    [Serializable]
    public struct PopupConfig
    {
        public PopupID id;
        public BasePopup prefab;
    }

   public class UIManager : Singleton<UIManager>
    {
        [Header("--- UI Roots ---")]
        [SerializeField] private Transform screenRoot; 
        [SerializeField] private Transform popupRoot;  
        [SerializeField] private Transform topRoot;    

        [Header("--- Screen Configs (Layer 1) ---")]
        [SerializeField] private List<ScreenConfig> screenConfigs = new List<ScreenConfig>();

        [Header("--- Popup Configs (Layer 2) ---")]
        [SerializeField] private List<PopupConfig> popupConfigs = new List<PopupConfig>();
        
        [Header("--- Top UI Prefabs (Layer 3) ---")]
        [SerializeField] private ToastNotification toastPrefab;
        [SerializeField] private LoadingScreen loadingScreenPrefab;

        // --- Caches ---
        private Dictionary<ScreenID, BaseScreen> _screenPrefabDict = new Dictionary<ScreenID, BaseScreen>();
        private Dictionary<PopupID, BasePopup> _popupPrefabDict = new Dictionary<PopupID, BasePopup>();
        
        private Dictionary<ScreenID, BaseScreen> _screenCache = new Dictionary<ScreenID, BaseScreen>();
        private Dictionary<PopupID, BasePopup> _popupCache = new Dictionary<PopupID, BasePopup>();
        
        // --- Flow State ---
        private Stack<BasePopup> _popupStack = new Stack<BasePopup>();
        private BaseScreen _currentScreen; 

        private ToastNotification _toastInstance;
        private LoadingScreen _loadingInstance;

        // protected override void Awake()
        // {
        //     base.Awake();
        //     InitPrefabDictionaries();
        // }

        public void Init()
        {
            AssignUILayerToRoots();
            AssignUICameraToRoots();
            InitPrefabDictionaries();
            InitTopUI();
        }

        // private void Start()
        // {
        //     InitTopUI();
        // }

        private void InitPrefabDictionaries()
        {
            foreach (var config in popupConfigs)
            {
                if (config.prefab != null && !_popupPrefabDict.ContainsKey(config.id))
                    _popupPrefabDict.Add(config.id, config.prefab);
            }

            foreach (var config in screenConfigs)
            {
                if (config.prefab != null && !_screenPrefabDict.ContainsKey(config.id))
                    _screenPrefabDict.Add(config.id, config.prefab);
            }
        }
        
        private void InitTopUI()
        {
            if (loadingScreenPrefab != null && _loadingInstance == null)
            {
                _loadingInstance = Instantiate(loadingScreenPrefab, topRoot);
                _loadingInstance.gameObject.SetActive(false);
            }

            if (toastPrefab != null && _toastInstance == null)
            {
                _toastInstance = Instantiate(toastPrefab, topRoot);
                _toastInstance.gameObject.SetActive(false);
            }
        }

        private void AssignUILayerToRoots()
        {
            int uiLayer = LayerMask.NameToLayer("UI");
            if (uiLayer < 0)
            {
                return;
            }

            ApplyLayerRecursively(screenRoot, uiLayer);
            ApplyLayerRecursively(popupRoot, uiLayer);
            ApplyLayerRecursively(topRoot, uiLayer);
        }

        private void AssignUICameraToRoots()
        {
            Camera uiCamera = FindUICamera();
            if (uiCamera == null)
            {
                return;
            }

            AssignUICamera(screenRoot, uiCamera);
            AssignUICamera(popupRoot, uiCamera);
            AssignUICamera(topRoot, uiCamera);
        }

        private static void AssignUICamera(Transform root, Camera uiCamera)
        {
            if (root == null || uiCamera == null)
            {
                return;
            }

            Canvas canvas = root.GetComponent<Canvas>();
            if (canvas == null)
            {
                return;
            }

            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = uiCamera;
            canvas.planeDistance = 100f;
        }

        private static Camera FindUICamera()
        {
            GameObject cameraObject = GameObject.Find("UICamera");
            if (cameraObject != null)
            {
                Camera namedCamera = cameraObject.GetComponent<Camera>();
                if (namedCamera != null)
                {
                    return namedCamera;
                }
            }

            Camera[] cameras = FindObjectsByType<Camera>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            for (int i = 0; i < cameras.Length; i++)
            {
                if (cameras[i] != null && cameras[i].name.Contains("UI"))
                {
                    return cameras[i];
                }
            }

            return null;
        }

        private static void ApplyLayerRecursively(Transform root, int layer)
        {
            if (root == null)
            {
                return;
            }

            root.gameObject.layer = layer;
            for (int i = 0; i < root.childCount; i++)
            {
                ApplyLayerRecursively(root.GetChild(i), layer);
            }
        }

        
        public T ShowScreen<T>(ScreenID id, Action onOpened = null) where T : BaseScreen
        {
            if (_currentScreen != null && _currentScreen.gameObject.activeInHierarchy)
            {
                _currentScreen.Hide();
            }

            if (!_screenCache.TryGetValue(id, out BaseScreen instance) || instance == null)
            {
                if (!_screenPrefabDict.TryGetValue(id, out BaseScreen prefab))
                {
                    string resourcePath = $"UI/Screens/{id}";
                    prefab = Resources.Load<BaseScreen>(resourcePath);
                    
                    if (prefab == null)
                    {
                        Debug.LogError($"[UIManager] Lỗi: Không tìm thấy Screen Prefab cho ID '{id}'!");
                        return null;
                    }
                    _screenPrefabDict[id] = prefab;
                }

                instance = Instantiate(prefab, screenRoot);
                _screenCache[id] = instance;
            }

            instance.transform.SetAsLastSibling();
            instance.Show(onOpened);
            _currentScreen = instance;

            return instance as T;
        }

        public T ShowPopup<T>(PopupID id, Action<T> beforeShow = null, Action onOpened = null) where T : BasePopup
        {
            if (!_popupCache.TryGetValue(id, out BasePopup instance) || instance == null)
            {
                if (!_popupPrefabDict.TryGetValue(id, out BasePopup prefab))
                {
                    string resourcePath = $"UI/Popups/{id}";
                    prefab = Resources.Load<BasePopup>(resourcePath);
                    
                    if (prefab == null)
                    {
                        Debug.LogError($"[UIManager] Lỗi: Không tìm thấy Popup Prefab cho ID '{id}'!");
                        return null;
                    }
                    _popupPrefabDict[id] = prefab;
                }

                instance = Instantiate(prefab, popupRoot);
                _popupCache[id] = instance;
            }

            T typedInstance = instance as T;
            if (typedInstance == null)
            {
                Debug.LogError($"[UIManager] Popup '{id}' không phải kiểu '{typeof(T).Name}'.");
                return null;
            }

            PushPopupToTop(instance);
            instance.transform.SetAsLastSibling();
            beforeShow?.Invoke(typedInstance);
            instance.Show(onOpened);
            return typedInstance;
        }

        public void CloseTopPopup()
        {
            if (_popupStack.Count > 0)
            {
                BasePopup topPopup = _popupStack.Pop();
                if (topPopup != null && topPopup.gameObject.activeInHierarchy)
                {
                    topPopup.Hide(); 
                }
            }
        }

        public bool ClosePopup(PopupID id)
        {
            if (!_popupCache.TryGetValue(id, out BasePopup popup) || popup == null)
            {
                return false;
            }

            bool wasVisible = popup.gameObject.activeInHierarchy;
            RemovePopupFromStack(popup);

            if (wasVisible)
            {
                popup.Hide();
            }

            return wasVisible;
        }

        public bool IsPopupVisible(PopupID id)
        {
            return _popupCache.TryGetValue(id, out BasePopup popup) &&
                   popup != null &&
                   popup.gameObject.activeInHierarchy;
        }

        public void ClearAllPopups()
        {
            while (_popupStack.Count > 0)
            {
                BasePopup popup = _popupStack.Pop();
                if (popup != null) popup.Hide();
            }
        }
        
        public void HideCurrentScreen()
        {
            if (_currentScreen != null && _currentScreen.gameObject.activeInHierarchy)
            {
                _currentScreen.Hide();
                _currentScreen = null;
            }
        }

        public void ShowToast(string message, float duration = -1f)
        {
            if (_toastInstance)
            {
                _toastInstance.transform.SetAsLastSibling();
                _toastInstance.ShowToast(message, duration);
            }
        }

        public bool ShowLoading(Action onCovered = null)
        {
            if (_loadingInstance == null)
            {
                onCovered?.Invoke();
                return false;
            }

            _loadingInstance.ShowLoading(onCovered);
            return true;
        }

        public void HideLoading() => _loadingInstance?.HideLoading();

        private void PushPopupToTop(BasePopup popup)
        {
            if (popup == null)
            {
                return;
            }

            RemovePopupFromStack(popup);
            _popupStack.Push(popup);
        }

        private void RemovePopupFromStack(BasePopup popup)
        {
            if (popup == null || _popupStack.Count == 0)
            {
                return;
            }

            Stack<BasePopup> buffer = new Stack<BasePopup>();
            while (_popupStack.Count > 0)
            {
                BasePopup current = _popupStack.Pop();
                if (current != popup)
                {
                    buffer.Push(current);
                }
            }

            while (buffer.Count > 0)
            {
                _popupStack.Push(buffer.Pop());
            }
        }
    }
}
