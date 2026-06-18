using _PawSlidePopGame._Scripts.Feature.Match3.Boosters;
using _PawSlidePopGame._Scripts.Gameplay.Meta.EconomyManager;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace _PawSlidePopGame._Scripts.Feature.Meta.Tests.Editor
{
    public sealed class PlayerEconomyTests
    {
        [Test]
        public void SaveDataSanitize_ClampsInvalidValues()
        {
            PlayerEconomySaveData data = new PlayerEconomySaveData
            {
                schemaVersion = 0,
                coins = -10
            };
            data.boosterCounts["hammer"] = -2;
            data.boosterCounts[string.Empty] = 5;

            data.Sanitize();

            Assert.That(data.schemaVersion, Is.EqualTo(1));
            Assert.That(data.coins, Is.EqualTo(0));
            Assert.That(data.boosterCounts["hammer"], Is.EqualTo(0));
            Assert.That(data.boosterCounts.ContainsKey(string.Empty), Is.False);
        }

        [Test]
        public void Repository_SaveAndReload_PreservesCoinsAndBoosters()
        {
            string saveKey = CreateSaveKey(nameof(Repository_SaveAndReload_PreservesCoinsAndBoosters));
            PlayerEconomyRepository repository = new PlayerEconomyRepository(saveKey);
            repository.DeleteSave();

            repository.Data.coins = 42;
            repository.Data.boosterCounts["hammer"] = 2;
            repository.Save();

            PlayerEconomyRepository reloadedRepository = new PlayerEconomyRepository(saveKey);

            Assert.That(reloadedRepository.Data.coins, Is.EqualTo(42));
            Assert.That(reloadedRepository.Data.boosterCounts["hammer"], Is.EqualTo(2));
            reloadedRepository.DeleteSave();
        }

        [Test]
        public void RegisterBoosterDefinitions_AddsMissingDefaultCountsOnly()
        {
            string saveKey = CreateSaveKey(nameof(RegisterBoosterDefinitions_AddsMissingDefaultCountsOnly));
            PlayerEconomyRepository repository = new PlayerEconomyRepository(saveKey);
            repository.DeleteSave();

            BoosterDefinitionSO hammer = CreateBoosterDefinition("hammer", 3);
            BoosterDefinitionSO shuffle = CreateBoosterDefinition("shuffle", 4);
            repository.Data.boosterCounts["hammer"] = 1;
            repository.RegisterBoosterDefinitions(new[] { hammer, shuffle });

            Assert.That(repository.Data.boosterCounts["hammer"], Is.EqualTo(1));
            Assert.That(repository.Data.boosterCounts["shuffle"], Is.EqualTo(4));
            repository.DeleteSave();
            Object.DestroyImmediate(hammer);
            Object.DestroyImmediate(shuffle);
        }

        private static BoosterDefinitionSO CreateBoosterDefinition(string boosterId, int initialCount)
        {
            BoosterDefinitionSO definition = ScriptableObject.CreateInstance<BoosterDefinitionSO>();
            SerializedObject serializedObject = new SerializedObject(definition);
            serializedObject.FindProperty("boosterId").stringValue = boosterId;
            serializedObject.FindProperty("displayName").stringValue = boosterId;
            serializedObject.FindProperty("initialCount").intValue = initialCount;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
            return definition;
        }

        private static string CreateSaveKey(string testName)
        {
            return $"player_economy_test_{testName}";
        }
    }
}
