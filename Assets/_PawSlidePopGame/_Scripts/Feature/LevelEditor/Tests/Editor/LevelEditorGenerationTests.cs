using System.IO;
using System.Linq;
using _PawSlidePopGame._Scripts.Data.LevelProvider;
using _PawSlidePopGame._Scripts.Feature.LevelEditor.Core.Services;
using _PawSlidePopGame._Scripts.Feature.LevelEditor.Temporary;
using _PawSlidePopGame._Scripts.Feature.LevelEditor.Validation;
using _PawSlidePopGame._Scripts.Feature.Match3.Data;
using NUnit.Framework;
using UnityEditor;

namespace _PawSlidePopGame._Scripts.Feature.LevelEditor.Tests.Editor
{
    public class LevelEditorGenerationTests
    {
        private Match3TileDatabaseSO _database;

        [SetUp]
        public void SetUp()
        {
            _database = AssetDatabase.LoadAssetAtPath<Match3TileDatabaseSO>("Assets/_PawSlidePopGame/_Data/Tiles/Database.asset");
        }

        [Test]
        public void Generate_Level001_MatchesRequestedSpec()
        {
            LevelGenerationService generator = new LevelGenerationService();
            var result = generator.Generate(TempLevel001GenerationProfile.CreateRequest(), _database);

            Assert.That(result.success, Is.True, result.message);
            Assert.That(result.levelData.width, Is.EqualTo(8));
            Assert.That(result.levelData.height, Is.EqualTo(8));
            Assert.That(result.levelData.movesLimit, Is.EqualTo(26));
            Assert.That(result.levelData.targets.Any(target => target.tileId == 105 && target.requiredCount == 10), Is.True);
            Assert.That(result.levelData.targets.Any(target => target.tileId == 301 && target.requiredCount == 5), Is.True);
            Assert.That(result.levelData.spawnableTileIds.SequenceEqual(new[] { 101, 102, 103, 104, 105, 106, 107, 108, 109 }), Is.True);
        }

        [Test]
        public void Generate_Level001_HasExactlyFiveIceAndNoForbiddenBaseTiles()
        {
            LevelGenerationService generator = new LevelGenerationService();
            var result = generator.Generate(TempLevel001GenerationProfile.CreateRequest(), _database);

            Assert.That(result.levelData.overlayLayout.Count(tileId => tileId == 301), Is.EqualTo(5));
            Assert.That(result.levelData.tileLayout.Any(tileId => tileId is 151 or 152 or 153 or 154 or 155 or 201 or 231 or 301 or 302 or 303), Is.False);
            Assert.That(result.levelData.tileLayout.Distinct().Count(tileId => tileId > 0), Is.EqualTo(9));
        }

        [Test]
        public void Generate_Level001_PassesValidatorAndHasNoInitialMatches()
        {
            LevelGenerationService generator = new LevelGenerationService();
            var result = generator.Generate(TempLevel001GenerationProfile.CreateRequest(), _database);
            Match3LevelDataValidationResult validation = Match3LevelDataValidator.Validate(result.levelData, _database);

            Assert.That(validation.IsValid, Is.True);
            Assert.That(result.hasInitialMatches, Is.False);
        }

        [Test]
        public void TempGenerator_Exports_Level001Json()
        {
            string path = LevelPathUtility.GetAbsoluteAssetPath("Level_001");
            if (!string.IsNullOrEmpty(path) && File.Exists(path))
            {
                File.Delete(path);
                AssetDatabase.Refresh();
            }

            Assert.That(TempLevel001Generator.TryGenerateAndExport(_database), Is.True);
            Assert.That(File.Exists(LevelPathUtility.GetAbsoluteAssetPath("Level_001")), Is.True);
        }

        [Test]
        public void Exported_Level001Json_CanBeLoadedBackByJsonProvider()
        {
            Assert.That(TempLevel001Generator.TryGenerateAndExport(_database), Is.True);

            JsonLevelDataProvider provider = new JsonLevelDataProvider();
            Assert.That(provider.TryGetLevelDocument("Level_001", out LevelSaveData document), Is.True);
            Assert.That(document, Is.Not.Null);
            Assert.That(document.levelId, Is.EqualTo("Level_001"));
            Assert.That(document.levelData, Is.Not.Null);
            Assert.That(document.levelData.width, Is.EqualTo(8));
            Assert.That(document.levelData.height, Is.EqualTo(8));
            Assert.That(document.levelData.targets.Any(target => target.tileId == 105 && target.requiredCount == 10), Is.True);
            Assert.That(document.levelData.targets.Any(target => target.tileId == 301 && target.requiredCount == 5), Is.True);
        }
    }
}
