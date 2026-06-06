using System;
using _PawSlidePopGame._Scripts.Feature.Match3.Boosters;
using UnityEngine;

namespace _PawSlidePopGame._Scripts.Feature.Meta.Wheel
{
    [Serializable]
    public sealed class WheelRewardEntryData
    {
        [SerializeField] private string rewardId;
        [SerializeField] private WheelRewardKind rewardKind = WheelRewardKind.Coins;
        [SerializeField] private int amount = 1;
        [SerializeField] private int weight = 1;
        [SerializeField] private bool enabled = true;
        [SerializeField] private string displayName;
        [SerializeField] private Sprite icon;
        [SerializeField] private BoosterDefinitionSO boosterDefinition;
        [SerializeField] private string boosterIdOverride;

        public string RewardId => rewardId;
        public WheelRewardKind RewardKind => rewardKind;
        public int Amount => Mathf.Max(0, amount);
        public int Weight => Mathf.Max(0, weight);
        public bool Enabled => enabled;
        public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? BuildFallbackDisplayName() : displayName;
        public Sprite Icon => icon;
        public BoosterDefinitionSO BoosterDefinition => boosterDefinition;
        public string BoosterId => boosterDefinition != null ? boosterDefinition.BoosterId : boosterIdOverride;

        public bool CanSpin()
        {
            if (!enabled || Weight <= 0 || Amount <= 0)
            {
                return false;
            }

            return rewardKind != WheelRewardKind.Booster || !string.IsNullOrWhiteSpace(BoosterId);
        }

        public void Sanitize()
        {
            amount = Mathf.Max(0, amount);
            weight = Mathf.Max(0, weight);

            if (string.IsNullOrWhiteSpace(rewardId))
            {
                rewardId = $"{rewardKind}_{amount}";
            }
        }

        internal static WheelRewardEntryData FromJson(
            WheelRewardEntryJson json,
            Func<string, BoosterDefinitionSO> boosterResolver,
            Func<string, Sprite> iconResolver)
        {
            WheelRewardEntryData entry = new WheelRewardEntryData
            {
                rewardId = json.id,
                rewardKind = json.kind,
                amount = json.amount,
                weight = json.weight,
                enabled = json.enabled,
                displayName = json.displayName,
                boosterIdOverride = json.boosterId
            };

            if (!string.IsNullOrWhiteSpace(json.boosterId) && boosterResolver != null)
            {
                entry.boosterDefinition = boosterResolver(json.boosterId);
            }

            if (!string.IsNullOrWhiteSpace(json.iconResourcesPath) && iconResolver != null)
            {
                entry.icon = iconResolver(json.iconResourcesPath);
            }

            entry.Sanitize();
            return entry;
        }

        private string BuildFallbackDisplayName()
        {
            return rewardKind switch
            {
                WheelRewardKind.Coins => $"{Amount} Coins",
                WheelRewardKind.Booster => boosterDefinition != null ? $"{boosterDefinition.DisplayName} x{Amount}" : $"{BoosterId} x{Amount}",
                WheelRewardKind.Heart => $"{Amount} Heart",
                _ => $"{rewardKind} x{Amount}"
            };
        }
    }
}
