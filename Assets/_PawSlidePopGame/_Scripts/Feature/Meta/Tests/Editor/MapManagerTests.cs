using System.Collections.Generic;
using System.Reflection;
using _PawSlidePopGame._Scripts.Gameplay.Meta.MapManager;
using _PawSlidePopGame._Scripts.UI.Screens.SubScreens;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;

namespace _PawSlidePopGame._Scripts.Feature.Meta.Tests.Editor
{
    public sealed class MapManagerTests
    {
        private readonly List<Object> _createdObjects = new List<Object>();

        [TearDown]
        public void TearDown()
        {
            for (int i = _createdObjects.Count - 1; i >= 0; i--)
            {
                if (_createdObjects[i] != null)
                {
                    Object.DestroyImmediate(_createdObjects[i]);
                }
            }

            _createdObjects.Clear();
        }

        [Test]
        public void BuildLayout_LoopsSectionPrefabsByOrder()
        {
            MapManager manager = new MapManager();
            MapSectionView sectionA = CreateSectionPrefab("SectionA", new Vector2(0f, 100f), new Vector2(0f, 300f));
            MapSectionView sectionB = CreateSectionPrefab("SectionB", new Vector2(0f, 120f), new Vector2(0f, 320f), new Vector2(0f, 520f));
            MapWorldConfigSO config = CreateConfig(sectionA, sectionB);
            List<string> levelIds = new List<string> { "Level_001", "Level_002", "Level_003", "Level_004", "Level_005", "Level_006", "Level_007" };

            MapManager.MapLayoutData layout = manager.BuildLayout(levelIds, config, 7, 1);

            Assert.That(layout.Sections.Count, Is.EqualTo(4));
            Assert.That(layout.Sections[0].Prefab, Is.SameAs(sectionA));
            Assert.That(layout.Sections[1].Prefab, Is.SameAs(sectionB));
            Assert.That(layout.Sections[2].Prefab, Is.SameAs(sectionA));
            Assert.That(layout.Sections[3].Prefab, Is.SameAs(sectionB));
        }

        [Test]
        public void BuildLayout_UsesAnchorCapacityPerSection()
        {
            MapManager manager = new MapManager();
            MapSectionView sectionA = CreateSectionPrefab("SectionA", new Vector2(0f, 100f), new Vector2(0f, 300f));
            MapSectionView sectionB = CreateSectionPrefab("SectionB", new Vector2(0f, 120f), new Vector2(0f, 320f), new Vector2(0f, 520f));
            MapWorldConfigSO config = CreateConfig(sectionA, sectionB);
            List<string> levelIds = new List<string> { "Level_001", "Level_002", "Level_003", "Level_004", "Level_005", "Level_006" };

            MapManager.MapLayoutData layout = manager.BuildLayout(levelIds, config, 6, 1);

            Assert.That(layout.Entries.Count, Is.EqualTo(6));
            Assert.That(layout.Sections[0].EntryCount, Is.EqualTo(2));
            Assert.That(layout.Sections[1].EntryCount, Is.EqualTo(3));
            Assert.That(layout.Sections[2].EntryCount, Is.EqualTo(1));
        }

        [Test]
        public void BuildLayout_ResolvesAnchoredPositionFromSectionAndAnchor()
        {
            MapManager manager = new MapManager();
            MapSectionView sectionA = CreateSectionPrefab("SectionA", new Vector2(-50f, 140f), new Vector2(25f, 360f));
            MapSectionView sectionB = CreateSectionPrefab("SectionB", new Vector2(75f, 180f), new Vector2(10f, 460f));
            MapWorldConfigSO config = CreateConfig(sectionA, sectionB);
            List<string> levelIds = new List<string> { "Level_001", "Level_002", "Level_003" };

            MapManager.MapLayoutData layout = manager.BuildLayout(levelIds, config, 3, 3);
            MapLevelEntry thirdEntry = layout.Entries[2];

            float expectedSectionY = config.BottomPadding + sectionA.SectionHeight + config.SectionSpacing;
            Vector2 expectedPosition = new Vector2(75f, expectedSectionY + 180f);

            Assert.That(thirdEntry.SectionIndex, Is.EqualTo(1));
            Assert.That(thirdEntry.SlotIndexInSection, Is.EqualTo(0));
            Assert.That(thirdEntry.AnchoredPosition, Is.EqualTo(expectedPosition));
            Assert.That(thirdEntry.State, Is.EqualTo(MapLevelState.Current));
        }

        [Test]
        public void BuildLayout_WithFourStarProgress_ReportsPerfectState()
        {
            string saveKey = $"map_manager_progress_test_{System.Guid.NewGuid()}";
            LevelProgressRepository repository = new LevelProgressRepository(saveKey);
            repository.DeleteSave();
            repository.RecordLevelResult("Level_001", 4, 1500);

            MapManager manager = new MapManager();
            MapSectionView sectionA = CreateSectionPrefab("SectionA", new Vector2(0f, 140f));
            MapWorldConfigSO config = CreateConfig(sectionA);
            MapManager.MapLayoutData layout = manager.BuildLayout(
                new List<string> { "Level_001" },
                config,
                1,
                2,
                repository);

            Assert.That(layout.Entries[0].BestStars, Is.EqualTo(4));
            Assert.That(layout.Entries[0].BestScore, Is.EqualTo(1500));
            Assert.That(layout.Entries[0].State, Is.EqualTo(MapLevelState.Perfect));

            repository.DeleteSave();
        }

        [Test]
        public void CalculateContentHeight_CoversLastNode()
        {
            MapManager manager = new MapManager();
            MapSectionView sectionA = CreateSectionPrefab("SectionA", new Vector2(0f, 120f), new Vector2(0f, 320f));
            MapWorldConfigSO config = CreateConfig(sectionA);
            List<string> levelIds = new List<string> { "Level_001", "Level_002", "Level_003" };

            MapManager.MapLayoutData layout = manager.BuildLayout(levelIds, config, 3, 1);
            float height = manager.CalculateContentHeight(layout, config);

            Assert.That(height, Is.GreaterThan(layout.Entries[layout.Entries.Count - 1].AnchoredPosition.y));
        }

        private MapWorldConfigSO CreateConfig(params MapSectionView[] sectionPrefabs)
        {
            MapWorldConfigSO config = ScriptableObject.CreateInstance<MapWorldConfigSO>();
            _createdObjects.Add(config);
            SetPrivateField(config, "maxVisibleLevels", 100);
            SetPrivateField(config, "sectionSpacing", 60f);
            SetPrivateField(config, "bottomPadding", 160f);
            SetPrivateField(config, "topPadding", 220f);
            SetPrivateField(config, "sectionPrefabs", new List<MapSectionView>(sectionPrefabs));
            return config;
        }

        private MapSectionView CreateSectionPrefab(string name, params Vector2[] anchorPositions)
        {
            GameObject root = new GameObject(name, typeof(RectTransform), typeof(MapSectionView));
            _createdObjects.Add(root);
            RectTransform rootRect = root.GetComponent<RectTransform>();
            rootRect.sizeDelta = new Vector2(1080f, 1100f);

            GameObject background = new GameObject("Background", typeof(RectTransform), typeof(Image));
            background.transform.SetParent(root.transform, false);

            GameObject nodeRootObject = new GameObject("NodeRoot", typeof(RectTransform));
            nodeRootObject.transform.SetParent(root.transform, false);
            RectTransform nodeRoot = nodeRootObject.GetComponent<RectTransform>();
            nodeRoot.anchorMin = new Vector2(0.5f, 0f);
            nodeRoot.anchorMax = new Vector2(0.5f, 0f);
            nodeRoot.pivot = new Vector2(0.5f, 0f);
            nodeRoot.anchoredPosition = Vector2.zero;
            nodeRoot.sizeDelta = new Vector2(0f, 0f);

            List<RectTransform> anchors = new List<RectTransform>();
            for (int i = 0; i < anchorPositions.Length; i++)
            {
                GameObject anchorObject = new GameObject($"Anchor_{i:00}", typeof(RectTransform));
                anchorObject.transform.SetParent(nodeRootObject.transform, false);
                RectTransform anchorRect = anchorObject.GetComponent<RectTransform>();
                anchorRect.anchorMin = new Vector2(0.5f, 0f);
                anchorRect.anchorMax = new Vector2(0.5f, 0f);
                anchorRect.pivot = new Vector2(0.5f, 0.5f);
                anchorRect.anchoredPosition = anchorPositions[i];
                anchors.Add(anchorRect);
            }

            MapSectionView sectionView = root.GetComponent<MapSectionView>();
            SetPrivateField(sectionView, "backgroundImage", background.GetComponent<Image>());
            SetPrivateField(sectionView, "nodeRoot", nodeRoot);
            SetPrivateField(sectionView, "nodeAnchors", anchors);
            return sectionView;
        }

        private static void SetPrivateField(object target, string fieldName, object value)
        {
            FieldInfo fieldInfo = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(fieldInfo, Is.Not.Null, $"Field '{fieldName}' was not found on {target.GetType().Name}.");
            fieldInfo.SetValue(target, value);
        }
    }
}
