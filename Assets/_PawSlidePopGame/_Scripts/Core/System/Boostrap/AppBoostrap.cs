using System.Collections;
using System.Collections.Generic;
using _PawSlidePopGame._Scripts.Core.Boostrap;
using _PawSlidePopGame._Scripts.Core.System.SceneManagement;
using _PawSlidePopGame._Scripts.UI.Manager;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace _PawSlidePopGame._Scripts.Core.System.Boostrap
{
    public class AppBootstrap : MonoBehaviour
    {
        [Header("Scene Config")]
        [SerializeField] private string nameInitScene = "LoadingScene";
        [SerializeField] private string nameMainScene = "GameplayScene";
        [SerializeField] private GameObject uiManagerPrefab;

        [Header("Core Services")]
        [SerializeField] private List<MonoBehaviour> coreServices;

        private void Start()
        {
            if (SceneManager.GetActiveScene().name == nameInitScene)
            {
                RunInitFlow(isEditorAutoInject: false);
            }
        }

        public void RunInitFlow(bool isEditorAutoInject)
        {
            StartCoroutine(RunInitFlowRoutine(isEditorAutoInject));
        }

        private IEnumerator RunInitFlowRoutine(bool isEditorAutoInject)
        {
            DontDestroyOnLoad(gameObject);
            EnsureUIManagerExists();

            foreach (MonoBehaviour mono in coreServices)
            {
                if (mono is IAppService service)
                {
                    service.Init();
                }
            }

            yield return null;
            SceneLoaderManager.Instance.LoadScene(nameMainScene);
        }

        private void EnsureUIManagerExists()
        {
            UIManager existingUIManager = FindFirstObjectByType<UIManager>(FindObjectsInactive.Include);
            if (existingUIManager != null)
            {
                existingUIManager.Init();
                return;
            }

            GameObject prefab = uiManagerPrefab != null
                ? uiManagerPrefab
                : Resources.Load<GameObject>("UI/UIManager");

            if (prefab == null)
            {
                Debug.LogError("[AppBootstrap] Missing UIManager prefab reference.");
                return;
            }

            GameObject uiManagerInstance = Instantiate(prefab);
            uiManagerInstance.name = prefab.name;

            UIManager uiManager = uiManagerInstance.GetComponent<UIManager>();
            if (uiManager == null)
            {
                Debug.LogError("[AppBootstrap] UIManager prefab does not contain UIManager component.");
                return;
            }

            uiManager.Init();
        }
    }
}
