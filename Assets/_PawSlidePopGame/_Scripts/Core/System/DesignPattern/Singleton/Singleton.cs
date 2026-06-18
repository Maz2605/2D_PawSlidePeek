using UnityEngine;

namespace _PawSlidePopGame._Scripts.Core.System.DesignPattern.Singleton
{
    public class Singleton<T> : MonoBehaviour where T : MonoBehaviour
    {
        private static T _instance;
        private static readonly object Lock = new object();
        private static bool _applicationIsQuitting = false;

        public static bool DontDestroyOnLoadEnabled { get; set; } = true;

        public static T Instance
        {
            get
            {
                // Tránh tạo "Ghost Object" khi Editor đang tắt
                if (_applicationIsQuitting)
                {
                    Debug.LogWarning($"[Singleton] Instance '{typeof(T)}' already destroyed on application quit. Won't create again - returning null.");
                    return null;
                }

                lock (Lock)
                {
                    if (_instance == null)
                    {
                        T[] instances = Object.FindObjectsByType<T>(FindObjectsInactive.Include, FindObjectsSortMode.None);
                        if (instances.Length > 0)
                        {
                            _instance = instances[0];
                            if (instances.Length > 1)
                            {
                                Debug.LogError($"[Singleton] Something went really wrong - there are two instances of {typeof(T)}");
                            }
                        }

                        if (_instance == null)
                        {
                            GameObject singleton = new GameObject();
                            _instance = singleton.AddComponent<T>();
                            singleton.name = "[Singleton] " + typeof(T);

                            if (DontDestroyOnLoadEnabled && Application.isPlaying)
                                DontDestroyOnLoad(singleton);
                        }
                    }
                }
                return _instance;
            }
        }

        protected virtual void Awake()
        {
            if (_applicationIsQuitting) return;

            lock (Lock)
            {
                if (_instance == null)
                {
                    _instance = this as T;
                    if (DontDestroyOnLoadEnabled && transform.parent == null && Application.isPlaying)
                    {
                        DontDestroyOnLoad(gameObject);
                    }
                }
                else if (_instance != this)
                {
                    if (GetComponents<Component>().Length > 2)
                    {
                        Destroy(this);
                    }
                    else
                    {
                        Destroy(gameObject);
                    }
                }
            }
        }

        protected virtual void OnApplicationQuit()
        {
            _applicationIsQuitting = true;
        }

        public static void ResetQuittingFlag()
        {
            _applicationIsQuitting = false;
        }

        protected virtual void OnDestroy()
        {
            if (_instance == this)
            {
                _instance = null;
            }
        }
    }
}