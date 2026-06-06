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
        [SerializeField] private BoosterWidgetInteractionMode interactionMode = BoosterWidgetInteractionMode.GameplayController;

        private readonly Dictionary<string, BoosterButtonView> _viewsByBoosterId = new Dictionary<string, BoosterButtonView>();
        private readonly List<BoosterButtonView> _managedViews = new List<BoosterButtonView>();
        private readonly List<BoosterButtonView> _tempViewsList = new List<BoosterButtonView>();

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

            if (rebuildOnEnable || _managedViews.Count == 0)
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
                RegisterAndBindView(view, definition);
            }
        }

        private void ReuseExistingChildrenAndBind()
        {
            GetImmediateChildViews(_tempViewsList);
            int existingIndex = 0;

            for (int i = 0; i < boosterDefinitions.Count; i++)
            {
                BoosterDefinitionSO definition = boosterDefinitions[i];
                if (definition == null || string.IsNullOrWhiteSpace(definition.BoosterId))
                {
                    continue;
                }

                BoosterButtonView view = existingIndex < _tempViewsList.Count
                    ? _tempViewsList[existingIndex]
                    : CreateMissingView();

                existingIndex++;
                if (view == null)
                {
                    continue;
                }

                view.gameObject.SetActive(true);
                RegisterAndBindView(view, definition);
            }

            for (int i = existingIndex; i < _tempViewsList.Count; i++)
            {
                if (_tempViewsList[i] != null)
                {
                    _tempViewsList[i].gameObject.SetActive(false);
                }
            }

            _tempViewsList.Clear();
        }

        private BoosterButtonView CreateMissingView()
        {
            if (buttonPrefab == null)
            {
                return null;
            }

            return Instantiate(buttonPrefab, contentRoot);
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

            if (interactionMode == BoosterWidgetInteractionMode.GameplayController && boosterController != null)
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
            return interactionMode == BoosterWidgetInteractionMode.GameplayController &&
                   boosterController != null &&
                   boosterController.ActiveBooster != null &&
                   boosterController.ActiveBooster.BoosterId == definition.BoosterId;
        }

        private void Subscribe()
        {
            var inventory = BoosterInventory.Instance;
            if (inventory != null)
            {
                inventory.OnBoosterCountChanged -= HandleBoosterCountChanged;
                inventory.OnBoosterCountChanged += HandleBoosterCountChanged;
            }

            if (interactionMode == BoosterWidgetInteractionMode.GameplayController && boosterController != null)
            {
                boosterController.OnActiveBoosterChanged -= HandleActiveBoosterChanged;
                boosterController.OnActiveBoosterChanged += HandleActiveBoosterChanged;
            }
        }

        private void Unsubscribe()
        {
            var inventory = BoosterInventory.Instance;
            if (inventory != null)
            {
                inventory.OnBoosterCountChanged -= HandleBoosterCountChanged;
            }

            if (boosterController != null)
            {
                boosterController.OnActiveBoosterChanged -= HandleActiveBoosterChanged;
            }
        }

        public void SetInteractionMode(BoosterWidgetInteractionMode mode)
        {
            if (interactionMode == mode)
            {
                return;
            }

            Unsubscribe();
            interactionMode = mode;
            Subscribe();
            RefreshAll();
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
            GetImmediateChildViews(_tempViewsList);
            for (int i = 0; i < _tempViewsList.Count; i++)
            {
                BoosterButtonView view = _tempViewsList[i];
                if (view == null)
                {
                    continue;
                }

                if (view == buttonPrefab)
                {
                    view.gameObject.SetActive(false);
                    continue;
                }

                DestroyView(view);
            }
            _tempViewsList.Clear();
        }

        private void GetImmediateChildViews(List<BoosterButtonView> results)
        {
            results.Clear();
            int childCount = contentRoot.childCount;
            for (int i = 0; i < childCount; i++)
            {
                if (contentRoot.GetChild(i).TryGetComponent<BoosterButtonView>(out var view))
                {
                    results.Add(view);
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

        public enum BoosterWidgetInteractionMode
        {
            GameplayController = 0,
            SelectionOnly = 1
        }
    }
}
