using System;
using System.Collections.Generic;
using _PawSlidePopGame._Scripts.Feature.Match3.Boosters;
using _PawSlidePopGame._Scripts.Gameplay.Meta.Inventory;
using UnityEngine;

namespace _PawSlidePopGame._Scripts.UI.Components.HUD
{
    [DisallowMultipleComponent]
    public sealed class BoosterWidget : MonoBehaviour
    {
        [Header("Refs")]
        [SerializeField] private BoosterController boosterController;
        [SerializeField] private Transform contentRoot;
        [SerializeField] private BoosterButtonView buttonPrefab;

        [Header("Data")]
        [SerializeField] private List<BoosterDefinitionSO> boosterDefinitions = new List<BoosterDefinitionSO>();
        [SerializeField] private bool rebuildOnEnable = true;
        [SerializeField] private ButtonBuildMode buildMode = ButtonBuildMode.ReuseExistingChildren;

        private readonly Dictionary<string, BoosterButtonView> _viewsByBoosterId = new Dictionary<string, BoosterButtonView>();
        private readonly List<BoosterButtonView> _spawnedViews = new List<BoosterButtonView>();
        private readonly List<BoosterButtonView> _managedViews = new List<BoosterButtonView>();

        public event Action<BoosterDefinitionSO> OnLockedBoosterClicked;
        public event Action<BoosterDefinitionSO> OnBoosterClicked;

        private void Awake()
        {
            ResolveBoosterController();
        }

        private void OnEnable()
        {
            ResolveBoosterController();
            Subscribe();

            if (BoosterInventory.Instance != null)
            {
                BoosterInventory.Instance.RegisterDefinitions(boosterDefinitions);
            }

            if (rebuildOnEnable || _spawnedViews.Count == 0)
            {
                Rebuild();
            }
            else
            {
                RefreshAll();
            }
        }

        private void OnDisable()
        {
            Unsubscribe();
        }

        public void Rebuild()
        {
            ClearManagedState();

            if (contentRoot == null)
            {
                return;
            }

            if (buildMode == ButtonBuildMode.ClearAndSpawnFromPrefab)
            {
                ClearChildButtonViews();
                _spawnedViews.Clear();
                SpawnAndBindAll();
                return;
            }

            ReuseExistingChildrenAndBind();
        }

        private void SpawnAndBindAll()
        {
            if (buttonPrefab == null)
            {
                return;
            }

            for (int i = 0; i < boosterDefinitions.Count; i++)
            {
                BoosterDefinitionSO definition = boosterDefinitions[i];
                if (definition == null || string.IsNullOrWhiteSpace(definition.BoosterId))
                {
                    continue;
                }

                BoosterButtonView view = Instantiate(buttonPrefab, contentRoot);
                view.gameObject.SetActive(true);
                _spawnedViews.Add(view);
                RegisterAndBindView(view, definition);
            }
        }

        private void ReuseExistingChildrenAndBind()
        {
            BoosterButtonView[] existingViews = contentRoot.GetComponentsInChildren<BoosterButtonView>(true);
            int existingIndex = 0;

            for (int i = 0; i < boosterDefinitions.Count; i++)
            {
                BoosterDefinitionSO definition = boosterDefinitions[i];
                if (definition == null || string.IsNullOrWhiteSpace(definition.BoosterId))
                {
                    continue;
                }

                BoosterButtonView view = existingIndex < existingViews.Length
                    ? existingViews[existingIndex]
                    : CreateMissingView();

                existingIndex++;
                if (view == null)
                {
                    continue;
                }

                view.gameObject.SetActive(true);
                RegisterAndBindView(view, definition);
            }

            for (int i = existingIndex; i < existingViews.Length; i++)
            {
                if (existingViews[i] != null)
                {
                    existingViews[i].gameObject.SetActive(false);
                }
            }
        }

        private BoosterButtonView CreateMissingView()
        {
            if (buttonPrefab == null)
            {
                return null;
            }

            BoosterButtonView view = Instantiate(buttonPrefab, contentRoot);
            _spawnedViews.Add(view);
            return view;
        }

        private void RegisterAndBindView(BoosterButtonView view, BoosterDefinitionSO definition)
        {
            if (view == null || definition == null)
            {
                return;
            }

            _managedViews.Add(view);
            _viewsByBoosterId[definition.BoosterId] = view;
            BindView(view, definition);
        }

        public void RefreshAll()
        {
            for (int i = 0; i < boosterDefinitions.Count; i++)
            {
                BoosterDefinitionSO definition = boosterDefinitions[i];
                if (definition != null)
                {
                    Refresh(definition.BoosterId);
                }
            }
        }

        public void Refresh(string boosterId)
        {
            if (string.IsNullOrWhiteSpace(boosterId) || !_viewsByBoosterId.TryGetValue(boosterId, out BoosterButtonView view))
            {
                return;
            }

            BoosterDefinitionSO definition = view.Definition;
            if (definition == null)
            {
                return;
            }

            view.SetState(GetCount(definition), IsSelected(definition));
        }

        private void BindView(BoosterButtonView view, BoosterDefinitionSO definition)
        {
            if (view == null)
            {
                return;
            }

            view.Bind(definition, GetCount(definition), IsSelected(definition), HandleBoosterButtonClicked);
        }

        private void HandleBoosterButtonClicked(BoosterDefinitionSO definition)
        {
            if (definition == null)
            {
                return;
            }

            OnBoosterClicked?.Invoke(definition);

            if (!definition.IsUnlocked)
            {
                OnLockedBoosterClicked?.Invoke(definition);
                return;
            }

            if (boosterController != null)
            {
                boosterController.TrySelectBooster(definition.BoosterType);
            }
        }

        private void HandleBoosterCountChanged(string boosterId, int previousCount, int currentCount, string reason)
        {
            Refresh(boosterId);
        }

        private void HandleActiveBoosterChanged(BoosterDefinitionSO activeBooster)
        {
            RefreshAll();
        }

        private int GetCount(BoosterDefinitionSO definition)
        {
            return BoosterInventory.Instance != null ? BoosterInventory.Instance.GetCount(definition) : 0;
        }

        private bool IsSelected(BoosterDefinitionSO definition)
        {
            return boosterController != null &&
                   boosterController.ActiveBooster != null &&
                   boosterController.ActiveBooster.BoosterId == definition.BoosterId;
        }

        private void Subscribe()
        {
            if (BoosterInventory.Instance != null)
            {
                BoosterInventory.Instance.OnBoosterCountChanged -= HandleBoosterCountChanged;
                BoosterInventory.Instance.OnBoosterCountChanged += HandleBoosterCountChanged;
            }

            if (boosterController != null)
            {
                boosterController.OnActiveBoosterChanged -= HandleActiveBoosterChanged;
                boosterController.OnActiveBoosterChanged += HandleActiveBoosterChanged;
            }
        }

        private void Unsubscribe()
        {
            if (BoosterInventory.Instance != null)
            {
                BoosterInventory.Instance.OnBoosterCountChanged -= HandleBoosterCountChanged;
            }

            if (boosterController != null)
            {
                boosterController.OnActiveBoosterChanged -= HandleActiveBoosterChanged;
            }
        }

        private void ResolveBoosterController()
        {
            if (boosterController == null)
            {
                boosterController = FindFirstObjectByType<BoosterController>(FindObjectsInactive.Include);
            }
        }

        private void ClearManagedState()
        {
            _managedViews.Clear();
            _viewsByBoosterId.Clear();
        }

        private void ClearChildButtonViews()
        {
            BoosterButtonView[] childViews = contentRoot.GetComponentsInChildren<BoosterButtonView>(true);
            for (int i = 0; i < childViews.Length; i++)
            {
                if (childViews[i] == null)
                {
                    continue;
                }

                if (childViews[i] == buttonPrefab)
                {
                    childViews[i].gameObject.SetActive(false);
                    continue;
                }

                if (childViews[i] != null)
                {
                    DestroyView(childViews[i]);
                }
            }
        }

        private static void DestroyView(BoosterButtonView view)
        {
            if (Application.isPlaying)
            {
                Destroy(view.gameObject);
            }
            else
            {
                DestroyImmediate(view.gameObject);
            }
        }

        private enum ButtonBuildMode
        {
            ReuseExistingChildren = 0,
            ClearAndSpawnFromPrefab = 1
        }
    }
}
