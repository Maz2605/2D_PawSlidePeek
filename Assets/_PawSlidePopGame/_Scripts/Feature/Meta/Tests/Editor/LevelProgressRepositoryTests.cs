using System;
using _PawSlidePopGame._Scripts.Gameplay.Meta.MapManager;
using NUnit.Framework;

namespace _PawSlidePopGame._Scripts.Feature.Meta.Tests.Editor
{
    public sealed class LevelProgressRepositoryTests
    {
        private string _saveKey;
        private LevelProgressRepository _repository;

        [SetUp]
        public void SetUp()
        {
            _saveKey = $"level_progress_test_{Guid.NewGuid()}";
            _repository = new LevelProgressRepository(_saveKey);
            _repository.DeleteSave();
        }

        [TearDown]
        public void TearDown()
        {
            _repository?.DeleteSave();
        }

        [Test]
        public void RecordLevelResult_SaveAndReload_PreservesProgress()
        {
            _repository.RecordLevelResult("Level_001", 3, 900);

            LevelProgressRepository reloaded = new LevelProgressRepository(_saveKey);
            LevelProgressEntryData progress = reloaded.GetProgress("Level_001");

            Assert.That(progress, Is.Not.Null);
            Assert.That(progress.bestStars, Is.EqualTo(3));
            Assert.That(progress.bestScore, Is.EqualTo(900));
            Assert.That(progress.isCompleted, Is.True);
        }

        [Test]
        public void RecordLevelResult_LowerReplay_DoesNotReduceBestStarsOrScore()
        {
            _repository.RecordLevelResult("Level_001", 4, 1500);
            _repository.RecordLevelResult("Level_001", 2, 500);

            LevelProgressEntryData progress = _repository.GetProgress("Level_001");

            Assert.That(progress.bestStars, Is.EqualTo(4));
            Assert.That(progress.bestScore, Is.EqualTo(1500));
        }

        [Test]
        public void RecordWinAndAdvance_UnlocksNextLevelAndMovesCurrentProgress()
        {
            _repository.EnsureInitializedProgress("Level_001");

            _repository.RecordWinAndAdvance("Level_001", 3, 900);

            Assert.That(_repository.GetHighestUnlockedLevelNumber(), Is.EqualTo(2));
            Assert.That(_repository.GetCurrentLevelId(), Is.EqualTo("Level_002"));
            Assert.That(_repository.IsLevelUnlocked("Level_002"), Is.True);
            Assert.That(_repository.IsLevelUnlocked("Level_003"), Is.False);
        }

        [Test]
        public void EnsureInitializedProgress_SaveAndReload_PreservesCurrentLevelAndUnlockBoundary()
        {
            _repository.EnsureInitializedProgress("Level_001");
            _repository.RecordWinAndAdvance("Level_001", 2, 700);

            LevelProgressRepository reloaded = new LevelProgressRepository(_saveKey);

            Assert.That(reloaded.GetCurrentLevelId(), Is.EqualTo("Level_002"));
            Assert.That(reloaded.GetHighestUnlockedLevelNumber(), Is.EqualTo(2));
        }
    }
}
