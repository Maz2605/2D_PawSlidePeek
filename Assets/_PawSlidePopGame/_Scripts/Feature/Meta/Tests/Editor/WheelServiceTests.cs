using System;
using System.Collections.Generic;
using _PawSlidePopGame._Scripts.Feature.Meta.Wheel;
using NUnit.Framework;
using UnityEditor;
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
                CreateReward("zero", WheelRewardKind.Coins, 10, 0, true),
                CreateReward("disabled", WheelRewardKind.Coins, 10, 10, false),
                CreateReward("valid", WheelRewardKind.Coins, 10, 1, true)
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

        [Test]
        public void JsonProvider_RejectsEmptyRewardConfig()
        {
            TextAsset json = new TextAsset("{\"rewards\":[]}");

            bool success = WheelJsonConfigProvider.TryCreateSnapshot(json, null, out WheelConfigSnapshot snapshot, out string error);

            Assert.That(success, Is.False);
            Assert.That(snapshot, Is.Null);
            Assert.That(error, Is.Not.Empty);
        }

        private static WheelRewardEntryData CreateReward(
            string id,
            WheelRewardKind kind,
            int amount,
            int weight,
            bool enabled)
        {
            WheelRewardEntryData reward = new WheelRewardEntryData();
            SerializedObject serializedObject = new SerializedObject(ScriptableObject.CreateInstance<WheelRewardEntryDataProxy>());
            WheelRewardEntryDataProxy proxy = (WheelRewardEntryDataProxy)serializedObject.targetObject;
            proxy.reward = reward;

            SerializedObject rewardObject = new SerializedObject(proxy);
            SerializedProperty rewardProperty = rewardObject.FindProperty("reward");
            rewardProperty.FindPropertyRelative("rewardId").stringValue = id;
            rewardProperty.FindPropertyRelative("rewardKind").enumValueIndex = (int)kind;
            rewardProperty.FindPropertyRelative("amount").intValue = amount;
            rewardProperty.FindPropertyRelative("weight").intValue = weight;
            rewardProperty.FindPropertyRelative("enabled").boolValue = enabled;
            rewardObject.ApplyModifiedPropertiesWithoutUndo();

            reward.Sanitize();
            UnityEngine.Object.DestroyImmediate(proxy);
            return reward;
        }

        private sealed class WheelRewardEntryDataProxy : ScriptableObject
        {
            public WheelRewardEntryData reward;
        }
    }
}
