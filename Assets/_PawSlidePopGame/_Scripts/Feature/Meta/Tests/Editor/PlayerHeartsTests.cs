using System;
using _PawSlidePopGame._Scripts.Core.System.DesignPattern.Singleton;
using _PawSlidePopGame._Scripts.Gameplay.Meta.EconomyManager;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace _PawSlidePopGame._Scripts.Feature.Meta.Tests.Editor
{
    public sealed class PlayerHeartsTests
    {
        private string _testSaveKey;

        [SetUp]
        public void Setup()
        {
            Singleton<HeartManager>.ResetQuittingFlag();
            _testSaveKey = $"player_economy_hearts_test_{Guid.NewGuid()}";
            
            // Set save key for repository manually or let it use the test key
            // Note: Since repository is lazy loaded, we need to make sure we clean it up.
            // But we can also test the manager directly by setting values in the repository data.
            PlayerEconomyRepository repository = new PlayerEconomyRepository(_testSaveKey);
            repository.DeleteSave();
            
            // Use custom repository instance if needed, but the HeartManager uses PlayerEconomyRepository.Instance.
            // Let's back up the current save in PlayerEconomyRepository.Instance to be safe,
            // or just use the default instance and restore it after testing.
            PlayerEconomyRepository.Instance.Reload();
            PlayerEconomyRepository.Instance.Data.infiniteHeartsEndUtc = string.Empty;
            PlayerEconomyRepository.Instance.Save();
            
            // Disable DontDestroyOnLoad for tests to avoid editor errors
            Singleton<HeartManager>.DontDestroyOnLoadEnabled = false;
        }

        [TearDown]
        public void TearDown()
        {
            Singleton<HeartManager>.ResetQuittingFlag();
            if (HeartManager.Instance != null)
            {
                Object.DestroyImmediate(HeartManager.Instance.gameObject);
            }
            
            // Clear the test save key
            PlayerEconomyRepository repository = new PlayerEconomyRepository(_testSaveKey);
            repository.DeleteSave();
            
            // Restore default repository
            PlayerEconomyRepository.Instance.Data.infiniteHeartsEndUtc = string.Empty;
            PlayerEconomyRepository.Instance.Save();
            PlayerEconomyRepository.Instance.Reload();
        }

        [Test]
        public void SpendHeart_ReducesHeartCountByOne()
        {
            PlayerEconomyRepository.Instance.Data.hearts = 5;
            PlayerEconomyRepository.Instance.Data.lastHeartRegenTime = string.Empty;
            PlayerEconomyRepository.Instance.Save();

            int initialHearts = HeartManager.Instance.Hearts;
            Assert.That(initialHearts, Is.EqualTo(5));

            bool success = HeartManager.Instance.TrySpendHeart();
            Assert.That(success, Is.True);
            Assert.That(HeartManager.Instance.Hearts, Is.EqualTo(4));
            Assert.That(string.IsNullOrEmpty(PlayerEconomyRepository.Instance.Data.lastHeartRegenTime), Is.False);
        }

        [Test]
        public void SpendHeartWhenEmpty_ReturnsFalse()
        {
            PlayerEconomyRepository.Instance.Data.hearts = 0;
            PlayerEconomyRepository.Instance.Data.lastHeartRegenTime = DateTime.UtcNow.ToString("o");
            PlayerEconomyRepository.Instance.Save();

            bool success = HeartManager.Instance.TrySpendHeart();
            Assert.That(success, Is.False);
            Assert.That(HeartManager.Instance.Hearts, Is.EqualTo(0));
        }

        [Test]
        public void HeartRegeneration_UnderLimit_RegeneratesCorrectly()
        {
            PlayerEconomyRepository.Instance.Data.hearts = 3;
            // Set regen start time to 35 minutes ago (should recover 1 heart)
            DateTime thirtyFiveMinsAgo = DateTime.UtcNow.AddMinutes(-35);
            PlayerEconomyRepository.Instance.Data.lastHeartRegenTime = thirtyFiveMinsAgo.ToString("o");
            PlayerEconomyRepository.Instance.Save();

            // Verify elapsed time regeneration
            int currentHearts = HeartManager.Instance.Hearts;
            Assert.That(currentHearts, Is.EqualTo(4));
            
            // lastHeartRegenTime should advance by exactly 30 minutes (1800 seconds)
            DateTime parsedRegenTime = DateTime.Parse(PlayerEconomyRepository.Instance.Data.lastHeartRegenTime).ToUniversalTime();
            double secondsDifference = (parsedRegenTime - thirtyFiveMinsAgo).TotalSeconds;
            Assert.That(secondsDifference, Is.EqualTo(1800).Within(1));
        }

        [Test]
        public void HeartRegeneration_CapsAtMax()
        {
            PlayerEconomyRepository.Instance.Data.hearts = 4;
            // Set regen start time to 75 minutes ago (would recover 2 hearts, but cap at 5)
            DateTime seventyFiveMinsAgo = DateTime.UtcNow.AddMinutes(-75);
            PlayerEconomyRepository.Instance.Data.lastHeartRegenTime = seventyFiveMinsAgo.ToString("o");
            PlayerEconomyRepository.Instance.Save();

            int currentHearts = HeartManager.Instance.Hearts;
            Assert.That(currentHearts, Is.EqualTo(5));
            Assert.That(string.IsNullOrEmpty(PlayerEconomyRepository.Instance.Data.lastHeartRegenTime), Is.True);
        }

        [Test]
        public void MatchCrashOrForceQuit_DeductsHeartOnInitialization()
        {
            PlayerEconomyRepository.Instance.Data.hearts = 5;
            PlayerEconomyRepository.Instance.Data.isMatchActive = true;
            PlayerEconomyRepository.Instance.Data.lastHeartRegenTime = string.Empty;
            PlayerEconomyRepository.Instance.Save();

            // Initializing system should deduct 1 heart since isMatchActive is true
            HeartManager.Instance.InitializeHeartSystem();

            Assert.That(HeartManager.Instance.Hearts, Is.EqualTo(4));
            Assert.That(HeartManager.Instance.IsMatchActive, Is.False);
            Assert.That(string.IsNullOrEmpty(PlayerEconomyRepository.Instance.Data.lastHeartRegenTime), Is.False);
        }

        [Test]
        public void InfiniteHearts_BypassesDeductionAndReportsMax()
        {
            PlayerEconomyRepository.Instance.Data.hearts = 3;
            PlayerEconomyRepository.Instance.Data.infiniteHeartsEndUtc = DateTime.UtcNow.AddMinutes(30).ToString("o");
            PlayerEconomyRepository.Instance.Save();

            Assert.That(HeartManager.Instance.IsInfiniteHeartsActive, Is.True);
            Assert.That(HeartManager.Instance.Hearts, Is.EqualTo(HeartManager.MaxHearts));
            Assert.That(HeartManager.Instance.SecondsUntilNextHeart, Is.EqualTo(0));

            bool success = HeartManager.Instance.TrySpendHeart();
            Assert.That(success, Is.True);
            // Verify hearts count remains unchanged in data
            Assert.That(PlayerEconomyRepository.Instance.Data.hearts, Is.EqualTo(3));
        }

        [Test]
        public void AddInfiniteHearts_StacksCorrectly()
        {
            PlayerEconomyRepository.Instance.Data.infiniteHeartsEndUtc = string.Empty;
            PlayerEconomyRepository.Instance.Save();

            // Add 10 minutes
            HeartManager.Instance.AddInfiniteHearts(600);
            DateTime firstEnd = DateTime.Parse(PlayerEconomyRepository.Instance.Data.infiniteHeartsEndUtc).ToUniversalTime();
            double diff = (firstEnd - DateTime.UtcNow).TotalSeconds;
            Assert.That(diff, Is.EqualTo(600).Within(3));

            // Stack another 10 minutes (600 seconds)
            HeartManager.Instance.AddInfiniteHearts(600);
            DateTime secondEnd = DateTime.Parse(PlayerEconomyRepository.Instance.Data.infiniteHeartsEndUtc).ToUniversalTime();
            double finalDiff = (secondEnd - DateTime.UtcNow).TotalSeconds;
            Assert.That(finalDiff, Is.EqualTo(1200).Within(3));
        }
    }
}
