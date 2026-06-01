using System;
using System.Collections.Generic;
using _PawSlidePopGame._Scripts.Feature.LevelEditor.Core.Contracts;
using _PawSlidePopGame._Scripts.Feature.LevelEditor.Core.Model;
using UnityEngine;
using UnityEngine.UI;

namespace _PawSlidePopGame._Scripts.Feature.LevelEditor.UI
{
    public sealed class LevelEditorTargetWidget : MonoBehaviour
    {
        [SerializeField] private Button btnAddTargets;
        [SerializeField] private LevelEditorItemTargetView[] targetSlots;
        [SerializeField] private EditorUIManager editorUIManager;
        [SerializeField] private LevelEditorTargetPickerPopup targetPickerPopupPrefab;

        private LevelEditorUIController _service;
        private readonly Dictionary<int, LevelEditorPaletteEntryData> _targetEntryLookup = new Dictionary<int, LevelEditorPaletteEntryData>();

        public void Bind(LevelEditorUIController service)
        {
            _service = service;

            if (btnAddTargets != null)
            {
                btnAddTargets.onClick.RemoveAllListeners();
                btnAddTargets.onClick.AddListener(HandleAddTargetClicked);
            }
        }

        public void Refresh()
        {
            if (_service?.Session == null)
            {
                return;
            }

            RebuildTargetLookup(_service.GetGoalOptions());

            IReadOnlyList<LevelTargetRequirement> goals = _service.Targets;
            int slotCount = targetSlots != null ? targetSlots.Length : 0;

            for (int i = 0; i < slotCount; i++)
            {
                LevelEditorItemTargetView slot = targetSlots[i];
                if (slot == null)
                {
                    continue;
                }

                if (i < goals.Count && _targetEntryLookup.TryGetValue(goals[i].tileId, out LevelEditorPaletteEntryData entry))
                {
                    slot.Bind(
                        entry,
                        goals[i].requiredCount,
                        i,
                        HandleEditTargetRequested,
                        HandleTargetCountChanged,
                        HandleRemoveTargetRequested);
                }
                else
                {
                    slot.ShowEmpty();
                }
            }

            if (btnAddTargets != null)
            {
                btnAddTargets.gameObject.SetActive(goals.Count < slotCount);
            }
        }

        private void HandleAddTargetClicked()
        {
            if (_service?.Session == null || targetSlots == null || _service.Targets.Count >= targetSlots.Length)
            {
                return;
            }

            LevelEditorTargetPickerPopup popup = ShowPickerPopup();
            if (popup == null)
            {
                return;
            }

            popup.ShowForAdd(
                _service.GetGoalOptions(),
                tileId => _service.AddGoalWithTile(tileId, 1),
                () => editorUIManager?.ClosePopup(popup));
        }

        private void HandleEditTargetRequested(int slotIndex)
        {
            if (_service?.Session == null || slotIndex < 0 || slotIndex >= _service.Targets.Count)
            {
                return;
            }

            LevelEditorTargetPickerPopup popup = ShowPickerPopup();
            if (popup == null)
            {
                return;
            }

            popup.ShowForEdit(
                slotIndex,
                _service.Targets[slotIndex].tileId,
                _service.GetGoalOptions(),
                (index, tileId) => _service.UpdateGoalTile(index, tileId),
                () => editorUIManager?.ClosePopup(popup));
        }

        private void HandleTargetCountChanged(int slotIndex, int requiredCount)
        {
            _service?.UpdateGoalCount(slotIndex, requiredCount);
        }

        private void HandleRemoveTargetRequested(int slotIndex)
        {
            _service?.RemoveGoal(slotIndex);
        }

        private LevelEditorTargetPickerPopup ShowPickerPopup()
        {
            if (editorUIManager == null || targetPickerPopupPrefab == null)
            {
                return null;
            }

            return editorUIManager.ShowPopup(targetPickerPopupPrefab);
        }

        private void RebuildTargetLookup(IReadOnlyList<LevelEditorPaletteEntryData> options)
        {
            _targetEntryLookup.Clear();
            if (options == null)
            {
                return;
            }

            for (int i = 0; i < options.Count; i++)
            {
                _targetEntryLookup[options[i].Id] = options[i];
            }
        }
    }
}
