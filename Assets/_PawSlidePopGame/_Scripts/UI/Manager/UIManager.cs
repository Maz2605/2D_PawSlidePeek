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
   public class UIManager : Singleton<UIManager>
    {
        [Header("--- UI Roots ---")]
        [SerializeField] private Transform screenRoot; 
        [SerializeField] private Transform popupRoot;  
        [SerializeField] private Transform topRoot;    

        [Header("--- Screen Prefabs (Layer 1) ---")]
        [SerializeField] private List<BaseScreen> screenPrefabs = new List<BaseScreen>();

        [Header("--- Popup Prefabs (Layer 2) ---")]
        [SerializeField] private List<BasePopup> popupPrefabs = new List<BasePopup>();
        
        [Header("--- Top UI Prefabs (Layer 3) ---")]
        [SerializeField] private ToastNotification toastPrefab;
        [SerializeField] private LoadingScreen loadingScreenPrefab;

        // --- Caches ---
        private Dictionary<Type, BaseScreen> _screenPrefabDict = new Dictionary<Type, BaseScreen>();
        private Dictionary<Type, BasePopup> _popupPrefabDict = new Dictionary<Type, BasePopup>();
        
        private Dictionary<Type, BaseScreen> _screenCache = new Dictionary<Type, BaseScreen>();
        private Dictionary<Type, BasePopup> _popupCache = new Dictionary<Type, BasePopup>();
        
        // --- Flow State ---
        private Stack<BasePopup> _popupStack = new Stack<BasePopup>();
        private BaseScreen _currentScreen; 

        private ToastNotification _toastInstance;
        private LoadingScreen _loadingInstance;

        protected override void Awake()
        {
            base.Awake();
            UnityEngine.SceneManagement.SceneManager.sceneLoaded += OnSceneLoaded;
        }

        protected override void OnDestroy()
        {
            UnityEngine.SceneManagement.SceneManager.sceneLoaded -= OnSceneLoaded;
            base.OnDestroy();
        }

        private void OnSceneLoaded(UnityEngine.SceneManagement.Scene scene, UnityEngine.SceneManagement.LoadSceneMode mode)
        {
            AssignUICameraToRoots();
        }

        public void Init()
        {
            AssignUILayerToRoots();
            AssignUICameraToRoots();
            InitPrefabDictionaries();
            InitTopUI();
            PrewarmUI();
        }

        private void PrewarmUI()
        {
            // 1. Scan and cache already existing UI components under root objects to prevent double instantiations
            if (screenRoot != null)
            {
                var existingScreens = screenRoot.GetComponentsInChildren<BaseScreen>(true);
                foreach (var screen in existingScreens)
                {
                    if (screen != null)
                    {
                        Type type = screen.GetType();
                        if (!_screenCache.ContainsKey(type))
                        {
                            _screenCache[type] = screen;
                        }
                    }
                }
            }

            if (popupRoot != null)
            {
                var existingPopups = popupRoot.GetComponentsInChildren<BasePopup>(true);
                foreach (var popup in existingPopups)
                {
                    if (popup != null)
                    {
                        Type type = popup.GetType();
                        if (!_popupCache.ContainsKey(type))
                        {
                            _popupCache[type] = popup;
                        }
                    }
                }
            }

            // 2. Instantiate and prewarm only the missing configurations
            if (screenPrefabs != null)
            {
                foreach (var prefab in screenPrefabs)
                {
                    if (prefab != null)
                    {
                        Type type = prefab.GetType();
                        if (!_screenCache.ContainsKey(type))
                        {
                            BaseScreen instance = Instantiate(prefab, screenRoot);
                            if (instance != null)
                            {
                                instance.gameObject.SetActive(false);
                                _screenCache[type] = instance;
                            }
                        }
                    }
                }
            }

            if (popupPrefabs != null)
            {
                foreach (var prefab in popupPrefabs)
                {
                    if (prefab != null)
                    {
                        Type type = prefab.GetType();
                        if (!_popupCache.ContainsKey(type))
                        {
                            BasePopup instance = Instantiate(prefab, popupRoot);
                            if (instance != null)
                            {
                                instance.gameObject.SetActive(false);
                                _popupCache[type] = instance;
                            }
                        }
                    }
                }
            }
        }

        // private void Start()
        // {
        //     InitTopUI();
        // }

        private void InitPrefabDictionaries()
        {
            foreach (var prefab in popupPrefabs)
            {
                if (prefab != null)
                {
                    Type type = prefab.GetType();
                    if (!_popupPrefabDict.ContainsKey(type))
                        _popupPrefabDict.Add(type, prefab);
                }
            }

            foreach (var prefab in screenPrefabs)
            {
                if (prefab != null)
                {
                    Type type = prefab.GetType();
                    if (!_screenPrefabDict.ContainsKey(type))
                        _screenPrefabDict.Add(type, prefab);
                }
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

        
        public T ShowScreen<T>(Action onOpened = null) where T : BaseScreen
        {
            if (_currentScreen != null && _currentScreen.gameObject.activeInHierarchy)
            {
                _currentScreen.Hide();
            }

            Type type = typeof(T);

            if (!_screenCache.TryGetValue(type, out BaseScreen instance) || instance == null)
            {
                if (!_screenPrefabDict.TryGetValue(type, out BaseScreen prefab))
                {
                    string resourcePath = $"UI/Screens/{type.Name}";
                    prefab = Resources.Load<T>(resourcePath);
                    
                    if (prefab == null)
                    {
                        Debug.LogError($"[UIManager] Lỗi: Không tìm thấy Screen Prefab cho Type '{type.Name}' tại '{resourcePath}'!");
                        return null;
                    }
                    _screenPrefabDict[type] = prefab;
                }

                instance = Instantiate(prefab, screenRoot);
                _screenCache[type] = instance;
            }

            instance.transform.SetAsLastSibling();
            instance.Show(onOpened);
            _currentScreen = instance;

            return instance as T;
        }

        public T ShowPopup<T>(Action<T> beforeShow = null, Action onOpened = null) where T : BasePopup
        {
            Type type = typeof(T);

            if (!_popupCache.TryGetValue(type, out BasePopup instance) || instance == null)
            {
                if (!_popupPrefabDict.TryGetValue(type, out BasePopup prefab))
                {
                    string resourcePath = $"UI/Popups/{type.Name}";
                    prefab = Resources.Load<T>(resourcePath);
                    
                    if (prefab == null)
                    {
                        Debug.LogError($"[UIManager] Lỗi: Không tìm thấy Popup Prefab cho Type '{type.Name}' tại '{resourcePath}'!");
                        return null;
                    }
                    _popupPrefabDict[type] = prefab;
                }

                instance = Instantiate(prefab, popupRoot);
                _popupCache[type] = instance;
            }

            T typedInstance = instance as T;
            if (typedInstance == null)
            {
                Debug.LogError($"[UIManager] Popup không phải kiểu '{typeof(T).Name}'.");
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

        public bool ClosePopup<T>() where T : BasePopup
        {
            Type type = typeof(T);
            if (!_popupCache.TryGetValue(type, out BasePopup popup) || popup == null)
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

        public bool IsPopupVisible<T>() where T : BasePopup
        {
            return _popupCache.TryGetValue(typeof(T), out BasePopup popup) &&
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
