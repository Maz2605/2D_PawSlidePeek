using System.Collections.Generic;
using _PawSlidePopGame._Scripts.Gameplay.Meta.MapManager;
using NUnit.Framework;

namespace _PawSlidePopGame._Scripts.Feature.Meta.Tests.Editor
{
    public sealed class MapManagerTests
    {
        [Test]
        public void BuildEntries_WithSameInputs_ReturnsDeterministicPositions()
        {
            MapManager manager = new MapManager();
            List<string> levelIds = new List<string> { "Level_001", "Level_002", "Level_003", "Level_004" };

            IReadOnlyList<MapLevelEntry> first = manager.BuildEntries(levelIds, null, 4, 1);
            IReadOnlyList<MapLevelEntry> second = manager.BuildEntries(levelIds, null, 4, 1);

            Assert.That(first.Count, Is.EqualTo(second.Count));
            for (int i = 0; i < first.Count; i++)
            {
                Assert.That(first[i].LevelId, Is.EqualTo(second[i].LevelId));
                Assert.That(first[i].DisplayLevelNumber, Is.EqualTo(second[i].DisplayLevelNumber));
                Assert.That(first[i].AnchoredPosition, Is.EqualTo(second[i].AnchoredPosition));
            }
        }

        [Test]
        public void BuildEntries_ParsesPaddedLevelIds()
        {
            MapManager manager = new MapManager();
            List<string> levelIds = new List<string> { "Level_001", "Level_012", "Level_120" };

            IReadOnlyList<MapLevelEntry> entries = manager.BuildEntries(levelIds, null, 120, 12);

            Assert.That(entries[0].DisplayLevelNumber, Is.EqualTo(1));
            Assert.That(entries[1].DisplayLevelNumber, Is.EqualTo(12));
            Assert.That(entries[2].DisplayLevelNumber, Is.EqualTo(120));
            Assert.That(entries[1].State, Is.EqualTo(MapLevelState.Current));
        }

        [Test]
        public void BuildEntries_ForUnplayedLevel_ReportsZeroStars()
        {
            MapManager manager = new MapManager();
            List<string> levelIds = new List<string> { "Level_001" };

            IReadOnlyList<MapLevelEntry> entries = manager.BuildEntries(levelIds, null, 1, 1);

            Assert.That(entries[0].BestStars, Is.EqualTo(0));
            Assert.That(entries[0].State, Is.EqualTo(MapLevelState.Current));
        }

        [Test]
        public void BuildEntries_WithFourStarProgress_ReportsPerfectState()
        {
            string saveKey = $"map_manager_progress_test_{System.Guid.NewGuid()}";
            LevelProgressRepository repository = new LevelProgressRepository(saveKey);
            repository.DeleteSave();
            repository.RecordLevelResult("Level_001", 4, 1500);

            MapManager manager = new MapManager();
            IReadOnlyList<MapLevelEntry> entries = manager.BuildEntries(
                new List<string> { "Level_001" },
                null,
                1,
                1,
                repository);

            Assert.That(entries[0].BestStars, Is.EqualTo(4));
            Assert.That(entries[0].BestScore, Is.EqualTo(1500));
            Assert.That(entries[0].State, Is.EqualTo(MapLevelState.Perfect));

            repository.DeleteSave();
        }

        [Test]
        public void CalculateContentHeight_CoversLastNode()
        {
            MapManager manager = new MapManager();
            List<string> levelIds = new List<string> { "Level_001", "Level_002", "Level_003" };

            IReadOnlyList<MapLevelEntry> entries = manager.BuildEntries(levelIds, null, 3, 1);
            float height = manager.CalculateContentHeight(entries, null);

            Assert.That(height, Is.GreaterThan(entries[entries.Count - 1].AnchoredPosition.y));
        }
    }
}
