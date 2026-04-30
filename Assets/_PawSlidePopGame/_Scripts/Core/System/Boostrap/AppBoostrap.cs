using System.Collections;
using System.Collections.Generic;
using _PawSlidePopGame._Scripts.Core.Boostrap;
using _PawSlidePopGame._Scripts.Core.System.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

// Namespace chứa LoadingSceneVisual

namespace _PawSlidePopGame._Scripts.Core.System.Boostrap
{
    public class AppBootstrap : MonoBehaviour
    {
        [Header("Scene Config")]
        [SerializeField] private string nameInitScene = "LoadingScene";
        [SerializeField] private string nameMainScene = "GameplayScene";
        
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
            
            foreach (var mono in coreServices)
            {
                if (mono is IAppService service) service.Init();
            }
            yield return null; 
            SceneLoaderManager.Instance.LoadScene(nameMainScene);
        }
    }
}