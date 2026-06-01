using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using _PawSlidePopGame._Scripts.Data.LevelProvider;

namespace _PawSlidePopGame._Scripts.Feature.LevelEditor.UI
{
    public sealed class LevelEditorLevelSelectorWidget : MonoBehaviour
    {
        [SerializeField] private string levelPrefix = "Level_";
        [SerializeField] private TMP_InputField levelSuffixInput;
        [SerializeField] private TMP_Dropdown levelDropdown;
        [SerializeField] private TMP_Text selectedLevelText;
        [SerializeField] private TMP_InputField searchInput;

        private readonly ILevelCatalogProvider _catalogProvider = new ResourcesLevelCatalogProvider();
        private readonly List<string> _displayedLevelIds = new List<string>();

        private string _currentLevelId = "Level_001";
        private bool _suppressCallbacks;

        public string CurrentLevelId => string.IsNullOrWhiteSpace(_currentLevelId) ? "Level_001" : _currentLevelId;

        public void Bind()
        {
            if (levelSuffixInput != null)
            {
                levelSuffixInput.onValueChanged.RemoveListener(HandleSuffixChanged);
                levelSuffixInput.onValueChanged.AddListener(HandleSuffixChanged);
            }

            if (searchInput != null)
            {
                searchInput.onValueChanged.RemoveListener(HandleSearchChanged);
                searchInput.onValueChanged.AddListener(HandleSearchChanged);
            }

            if (levelDropdown != null)
            {
                levelDropdown.onValueChanged.RemoveListener(HandleDropdownSelectionChanged);
                levelDropdown.onValueChanged.AddListener(HandleDropdownSelectionChanged);
            }

            Refresh();
        }

        public void Refresh()
        {
            SetLevelId(CurrentLevelId);
        }

        public void SetLevelId(string levelId)
        {
            string sanitizedLevelId = LevelPathUtility.SanitizeLevelId(levelId);
            if (string.IsNullOrWhiteSpace(sanitizedLevelId))
            {
                sanitizedLevelId = "Level_001";
            }

            _currentLevelId = sanitizedLevelId;

            _suppressCallbacks = true;
            try
            {
                if (levelSuffixInput != null)
                {
                    levelSuffixInput.SetTextWithoutNotify(ExtractSuffix(_currentLevelId));
                }

                SyncSelectedText();
                RebuildDropdownOptions(string.Empty);
                SelectDropdownValue(_currentLevelId);
            }
            finally
            {
                _suppressCallbacks = false;
            }
        }

        private void HandleSuffixChanged(string suffix)
        {
            if (_suppressCallbacks)
            {
                return;
            }

            _currentLevelId = BuildLevelId(suffix);
            SyncSelectedText();
            RebuildDropdownOptions(GetActiveFilter());
        }

        private void HandleSearchChanged(string _)
        {
            if (_suppressCallbacks)
            {
                return;
            }

            RebuildDropdownOptions(GetActiveFilter());
        }

        private void HandleDropdownSelectionChanged(int selectedIndex)
        {
            if (_suppressCallbacks || selectedIndex < 0 || selectedIndex >= _displayedLevelIds.Count)
            {
                return;
            }

            SetLevelId(_displayedLevelIds[selectedIndex]);
        }

        private void RebuildDropdownOptions(string filter)
        {
            if (levelDropdown == null)
            {
                return;
            }

            _displayedLevelIds.Clear();
            _displayedLevelIds.AddRange(_catalogProvider.GetLevelIds(filter));

            if (_displayedLevelIds.Count == 0)
            {
                _displayedLevelIds.Add(CurrentLevelId);
            }

            levelDropdown.ClearOptions();
            levelDropdown.AddOptions(_displayedLevelIds);
            SyncSelectedText();
        }

        private void SelectDropdownValue(string levelId)
        {
            if (levelDropdown == null)
            {
                return;
            }

            int optionIndex = _displayedLevelIds.FindIndex(option => string.Equals(option, levelId, StringComparison.OrdinalIgnoreCase));
            if (optionIndex < 0)
            {
                optionIndex = 0;
            }

            levelDropdown.SetValueWithoutNotify(optionIndex);
            levelDropdown.RefreshShownValue();
            SyncSelectedText();
        }

        private void SyncSelectedText()
        {
            if (selectedLevelText != null)
            {
                selectedLevelText.text = CurrentLevelId;
            }
        }

        private string GetActiveFilter()
        {
            if (searchInput != null && !string.IsNullOrWhiteSpace(searchInput.text))
            {
                return searchInput.text.Trim();
            }

            if (levelSuffixInput != null && !string.IsNullOrWhiteSpace(levelSuffixInput.text))
            {
                return levelSuffixInput.text.Trim();
            }

            return string.Empty;
        }

        private string BuildLevelId(string suffix)
        {
            string trimmedSuffix = string.IsNullOrWhiteSpace(suffix) ? string.Empty : suffix.Trim();
            if (trimmedSuffix.StartsWith(levelPrefix, StringComparison.OrdinalIgnoreCase))
            {
                trimmedSuffix = trimmedSuffix.Substring(levelPrefix.Length);
            }

            string fullLevelId = $"{levelPrefix}{trimmedSuffix}";
            return LevelPathUtility.SanitizeLevelId(fullLevelId);
        }

        private string ExtractSuffix(string levelId)
        {
            if (string.IsNullOrWhiteSpace(levelId))
            {
                return string.Empty;
            }

            if (levelId.StartsWith(levelPrefix, StringComparison.OrdinalIgnoreCase))
            {
                return levelId.Substring(levelPrefix.Length);
            }

            return levelId;
        }
    }
}
