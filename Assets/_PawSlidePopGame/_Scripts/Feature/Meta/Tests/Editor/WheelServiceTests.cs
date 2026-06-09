using System;
using System.Collections.Generic;
using _PawSlidePopGame._Scripts.Feature.Meta.Wheel;
using _PawSlidePopGame._Scripts.Feature.Meta.Reward;
using NUnit.Framework;
using UnityEngine;

namespace _PawSlidePopGame._Scripts.Feature.Meta.Tests.Editor
{
    public sealed class WheelServiceTests
    {
        [Test]
        public void SelectRewardIndex_SkipsZeroWeightAndDisabledRewards()
        {
            List<WheelRewardEntryData> rewards = new List<WheelRewardEntryData>
            {
                CreateReward("zero", RewardKind.Coins, 10, 0, true),
                CreateReward("disabled", RewardKind.Coins, 10, 10, false),
                CreateReward("valid", RewardKind.Coins, 10, 1, true)
            };

            int selectedIndex = WheelSpinService.SelectRewardIndex(rewards, 0.99f);

            Assert.That(selectedIndex, Is.EqualTo(2));
        }

        [Test]
        public void CalculateTargetZAngle_LandsInsideExpectedSegment()
        {
            float targetAngle = WheelSpinService.CalculateTargetZAngle(
                rewardIndex: 2,
                rewardCount: 8,
                currentZAngle: 0f,
                minimumFullTurns: 5,
                landingPaddingDegrees: 4f,
                random01: 0.5f,
                pointerAngleDegrees: 90f,
                direction: WheelSpinDirection.Clockwise);

            float finalWheelAngle = Mathf.Repeat(targetAngle, 360f);
            float rewardCenterAngleAfterSpin = Mathf.Repeat(0f + finalWheelAngle, 360f);

            Assert.That(rewardCenterAngleAfterSpin, Is.InRange(89.9f, 90.1f));
            Assert.That(targetAngle, Is.GreaterThanOrEqualTo(5 * 360f));
        }

        [Test]
        public void CalculateTargetZAngle_CanSpinCounterClockwise()
        {
            float targetAngle = WheelSpinService.CalculateTargetZAngle(
                rewardIndex: 2,
                rewardCount: 8,
                currentZAngle: 0f,
                minimumFullTurns: 5,
                landingPaddingDegrees: 4f,
                random01: 0.5f,
                pointerAngleDegrees: 90f,
                direction: WheelSpinDirection.CounterClockwise);

            float finalWheelAngle = Mathf.Repeat(targetAngle, 360f);
            float rewardCenterAngleAfterSpin = Mathf.Repeat(0f + finalWheelAngle, 360f);

            Assert.That(targetAngle, Is.LessThanOrEqualTo(-5 * 360f));
            Assert.That(rewardCenterAngleAfterSpin, Is.InRange(89.9f, 90.1f));
        }

        [Test]
        public void StateRepository_CooldownBlocksUntilWindowExpires()
        {
            string saveKey = $"wheel_state_test_{Guid.NewGuid()}";
            WheelStateRepository repository = new WheelStateRepository(saveKey)
            {
                UtcNowOverride = new DateTime(2026, 6, 6, 0, 0, 0, DateTimeKind.Utc)
            };
            repository.DeleteSave();

            Assert.That(repository.CanFreeSpin(60), Is.True);

            repository.MarkFreeSpinUsed();

            repository.UtcNowOverride = new DateTime(2026, 6, 6, 0, 0, 30, DateTimeKind.Utc);
            Assert.That(repository.CanFreeSpin(60), Is.False);
            Assert.That(repository.GetRemainingCooldownSeconds(60), Is.EqualTo(30).Within(0.1f));

            repository.UtcNowOverride = new DateTime(2026, 6, 6, 0, 1, 0, DateTimeKind.Utc);
            Assert.That(repository.CanFreeSpin(60), Is.True);

            repository.DeleteSave();
        }

        [Test]
        public void StateRepository_ResetFreeSpinCooldown_AllowsSpinImmediately()
        {
            string saveKey = $"wheel_state_test_{Guid.NewGuid()}";
            WheelStateRepository repository = new WheelStateRepository(saveKey)
            {
                UtcNowOverride = new DateTime(2026, 6, 6, 0, 0, 0, DateTimeKind.Utc)
            };
            repository.DeleteSave();

            repository.MarkFreeSpinUsed();
            Assert.That(repository.CanFreeSpin(86400), Is.False);

            repository.ResetFreeSpinCooldown();

            Assert.That(repository.CanFreeSpin(86400), Is.True);
            repository.DeleteSave();
        }

        private static WheelRewardEntryData CreateReward(
            string id,
            RewardKind kind,
            int amount,
            int weight,
            bool enabled)
        {
            var rewardSO = ScriptableObject.CreateInstance<RewardEntrySO>();
            rewardSO.name = id;
            // boosterDefinition = null vì test này dùng Coins/Heart không cần Booster
            rewardSO.EditorSetValues(kind, amount, boosterDef: null);

            WheelRewardEntryData reward = new WheelRewardEntryData();
            reward.EditorSetup(rewardSO, weight, enabled);
            reward.Sanitize();
            return reward;
        }
    }
}
