using System.Collections.Generic;
using _PawSlidePopGame._Scripts.Core.System.GameFlow;
using _PawSlidePopGame._Scripts.Data.LevelProvider;
using _PawSlidePopGame._Scripts.Gameplay.Meta.MapManager;
using _PawSlidePopGame._Scripts.UI.Base;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace _PawSlidePopGame._Scripts.UI.Screens.SubScreens
{
    public class MapSubScreen : BaseSubScreen
    {
        [Header("Fallback / Editor Binding")]
        [SerializeField] private Button btnLevel;
        [SerializeField] private string tempLevelId = "Level_001";

        [Header("Dynamic Binding Settings")]
        [SerializeField] private string levelIdPrefix = "Level_";

        [Header("Procedural Map")]
        [SerializeField] private MapWorldConfigSO mapConfig;
        [SerializeField] private ScrollRect scrollRect;
        [SerializeField] private RectTransform contentRoot;
        [SerializeField] private RectTransform backgroundRoot;
        [SerializeField] private RectTransform pathRoot;
        [SerializeField] private RectTransform nodeRoot;
        [SerializeField] private Image backgroundSectionPrefab;
        [SerializeField] private Image pathSegmentPrefab;
        [SerializeField] private MapLevelNodeView levelNodePrefab;
        [SerializeField, Min(1f)] private float pathThickness = 18f;
        [SerializeField, Min(1)] private int currentLevelNumber = 1;
        [SerializeField, Min(1)] private int highestUnlockedLevelNumber = 999999;

        private readonly List<(Button button, string levelId)> _levelButtons = new List<(Button, string)>();
        private readonly ResourcesLevelCatalogProvider _catalogProvider = new ResourcesLevelCatalogProvider();
        private readonly MapManager _mapManager = new MapManager();

        public override void Init()
        {
            if (isInitialized)
            {
                return;
            }

            base.Init();
            if (!TryRenderProceduralMap())
            {
                DiscoverAndBindLevelButtons();
            }
        }

        public override void Show()
        {
            base.Show();

            if (isInitialized && HasProceduralMapBinding())
            {
                TryRenderProceduralMap();
            }
        }

        private void DiscoverAndBindLevelButtons()
        {
            _levelButtons.Clear();

            // Find all buttons in children
            Button[] buttons = GetComponentsInChildren<Button>(true);
            foreach (Button btn in buttons)
            {
                if (btn == null) continue;

                // Check if button name starts with "BtnLevel"
                if (btn.name.StartsWith("BtnLevel"))
                {
                    string levelId = DetermineLevelId(btn);
                    if (!string.IsNullOrEmpty(levelId))
                    {
                        _levelButtons.Add((btn, levelId));
                        BindButton(btn, () => HandleLevelPressed(levelId));
                        Debug.Log($"[MapSubScreen] Successfully bound level button '{btn.name}' to level '{levelId}'", btn);
                    }
                }
            }

            // Fallback to original single button if no dynamic buttons were bound
            if (_levelButtons.Count == 0 && btnLevel != null)
            {
                string fallbackLevelId = string.IsNullOrWhiteSpace(tempLevelId) ? "Level_001" : tempLevelId;
                BindButton(btnLevel, () => HandleLevelPressed(fallbackLevelId));
                Debug.Log($"[MapSubScreen] Fallback bound single button '{btnLevel.name}' to level '{fallbackLevelId}'", btnLevel);
            }
        }

        private bool TryRenderProceduralMap()
        {
            if (!HasProceduralMapBinding())
            {
                return false;
            }

            IReadOnlyList<string> levelIds = _catalogProvider.GetLevelIds();
            if (levelIds == null || levelIds.Count == 0)
            {
                Debug.LogWarning("[MapSubScreen] Cannot render procedural map because no level JSON files were found in Resources/Levels.", this);
                return false;
            }

            IReadOnlyList<MapLevelEntry> entries = _mapManager.BuildEntries(
                levelIds,
                mapConfig,
                highestUnlockedLevelNumber,
                currentLevelNumber,
                LevelProgressRepository.Instance);
            if (entries.Count == 0)
            {
                return false;
            }

            PrepareContent(entries);
            RenderBackgrounds(entries);
            RenderPath(entries);
            RenderNodes(entries);
            ScrollToCurrentLevel(entries);
            return true;
        }

        private bool HasProceduralMapBinding()
        {
            return mapConfig != null &&
                   contentRoot != null &&
                   backgroundRoot != null &&
                   pathRoot != null &&
                   nodeRoot != null &&
                   levelNodePrefab != null;
        }

        private void PrepareContent(IReadOnlyList<MapLevelEntry> entries)
        {
            ClearChildren(backgroundRoot);
            ClearChildren(pathRoot);
            ClearChildren(nodeRoot);

            float contentHeight = _mapManager.CalculateContentHeight(entries, mapConfig);
            contentRoot.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, contentHeight);
        }

        private void RenderBackgrounds(IReadOnlyList<MapLevelEntry> entries)
        {
            int sectionCount = entries[entries.Count - 1].SectionIndex + 1;
            System.Random random = new System.Random(mapConfig.Seed);
            for (int i = 0; i < sectionCount; i++)
            {
                Image sectionImage = CreateBackgroundSection();
                if (sectionImage == null)
                {
                    continue;
                }

                MapSectionConfigSO sectionConfig = mapConfig.GetSection(i, random);
                if (sectionConfig != null && sectionConfig.BackgroundSprite != null)
                {
                    sectionImage.sprite = sectionConfig.BackgroundSprite;
                    sectionImage.enabled = true;
                }

                RectTransform rect = sectionImage.rectTransform;
                rect.SetParent(backgroundRoot, false);
                rect.anchorMin = new Vector2(0f, 0f);
                rect.anchorMax = new Vector2(1f, 0f);
                rect.pivot = new Vector2(0.5f, 0f);
                rect.anchoredPosition = new Vector2(0f, i * mapConfig.SectionHeight);
                rect.sizeDelta = new Vector2(0f, mapConfig.SectionHeight);
            }
        }

        private Image CreateBackgroundSection()
        {
            if (backgroundSectionPrefab != null)
            {
                return Instantiate(backgroundSectionPrefab);
            }

            GameObject section = new GameObject("Map Background Section", typeof(RectTransform), typeof(Image));
            Image image = section.GetComponent<Image>();
            image.raycastTarget = false;
            image.enabled = false;
            return image;
        }

        private void RenderPath(IReadOnlyList<MapLevelEntry> entries)
        {
            if (pathSegmentPrefab == null || entries.Count < 2)
            {
                return;
            }

            for (int i = 1; i < entries.Count; i++)
            {
                CreatePathSegment(entries[i - 1].AnchoredPosition, entries[i].AnchoredPosition, i);
            }
        }

        private void CreatePathSegment(Vector2 from, Vector2 to, int index)
        {
            Image segment = Instantiate(pathSegmentPrefab, pathRoot);
            segment.name = $"PathSegment_{index:000}";
            RectTransform rect = segment.rectTransform;
            Vector2 delta = to - from;
            rect.anchorMin = new Vector2(0.5f, 0f);
            rect.anchorMax = new Vector2(0.5f, 0f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = from + (delta * 0.5f);
            rect.sizeDelta = new Vector2(delta.magnitude, pathThickness);
            rect.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg);
        }

        private void RenderNodes(IReadOnlyList<MapLevelEntry> entries)
        {
            for (int i = 0; i < entries.Count; i++)
            {
                MapLevelEntry entry = entries[i];
                MapLevelNodeView node = Instantiate(levelNodePrefab, nodeRoot);
                node.name = $"MapLevelNode_{entry.DisplayLevelNumber:000}";

                RectTransform rect = node.transform as RectTransform;
                if (rect != null)
                {
                    rect.anchorMin = new Vector2(0.5f, 0f);
                    rect.anchorMax = new Vector2(0.5f, 0f);
                    rect.pivot = new Vector2(0.5f, 0.5f);
                    rect.anchoredPosition = entry.AnchoredPosition;
                }

                node.Setup(entry, HandleLevelPressed);
            }
        }

        private void ScrollToCurrentLevel(IReadOnlyList<MapLevelEntry> entries)
        {
            if (scrollRect == null || contentRoot == null || entries.Count == 0)
            {
                return;
            }

            int targetIndex = 0;
            for (int i = 0; i < entries.Count; i++)
            {
                if (entries[i].State == MapLevelState.Current)
                {
                    targetIndex = i;
                    break;
                }
            }

            float contentHeight = Mathf.Max(1f, contentRoot.rect.height);
            float normalizedFromBottom = Mathf.Clamp01(entries[targetIndex].AnchoredPosition.y / contentHeight);
            scrollRect.verticalNormalizedPosition = Mathf.Clamp01(normalizedFromBottom);
        }

        private static void ClearChildren(RectTransform root)
        {
            if (root == null)
            {
                return;
            }

            for (int i = root.childCount - 1; i >= 0; i--)
            {
                Transform child = root.GetChild(i);
                if (Application.isPlaying)
                {
                    Destroy(child.gameObject);
                }
                else
                {
                    DestroyImmediate(child.gameObject);
                }
            }
        }

        private string DetermineLevelId(Button btn)
        {
            // 1. Try to get Level Number from child Text component
            TextMeshProUGUI tmpText = btn.GetComponentInChildren<TextMeshProUGUI>(true);
            if (tmpText != null && int.TryParse(tmpText.text.Trim(), out int levelNumFromText))
            {
                return $"{levelIdPrefix}{levelNumFromText:D3}";
            }

            // 2. Fallback: Parse from button name structure: "BtnLevel", "BtnLevel (1)", "BtnLevel (2)", etc.
            string name = btn.name;
            if (name == "BtnLevel")
            {
                return $"{levelIdPrefix}001";
            }

            // Extract the number inside parenthesis, e.g. "BtnLevel (1)" -> 1
            int openParen = name.IndexOf('(');
            int closeParen = name.IndexOf(')');
            if (openParen >= 0 && closeParen > openParen)
            {
                string numStr = name.Substring(openParen + 1, closeParen - openParen - 1);
                if (int.TryParse(numStr, out int indexInParen))
                {
                    // "BtnLevel (1)" represents Level 2, "BtnLevel (2)" Level 3, etc.
                    int levelNumFromName = indexInParen + 1;
                    return $"{levelIdPrefix}{levelNumFromName:D3}";
                }
            }

            return null;
        }

        private void HandleLevelPressed(string levelId)
        {
            if (!GameAppFlowManager.Instance.RequestStartLevel(levelId))
            {
                Debug.LogWarning($"[MapSubScreen] Failed to request gameplay start for level '{levelId}'.", this);
            }
        }

        private void OnValidate()
        {
            TryAutoBindReferences();
        }

        private void TryAutoBindReferences()
        {
            if (btnLevel != null)
            {
                return;
            }

            Button[] buttons = GetComponentsInChildren<Button>(true);
            for (int i = 0; i < buttons.Length; i++)
            {
                if (buttons[i] != null && buttons[i].name == "BtnLevel")
                {
                    btnLevel = buttons[i];
                    return;
                }
            }
        }
    }
}
