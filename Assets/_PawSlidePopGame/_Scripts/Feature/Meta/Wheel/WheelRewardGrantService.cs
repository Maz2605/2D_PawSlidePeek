using _PawSlidePopGame._Scripts.Gameplay.Meta.EconomyManager;
using _PawSlidePopGame._Scripts.Gameplay.Meta.Inventory;
using UnityEngine;

namespace _PawSlidePopGame._Scripts.Feature.Meta.Wheel
{
    public static class WheelRewardGrantService
    {
        public static bool TryGrant(WheelRewardEntryData reward, string reason = "wheel_spin")
        {
            if (reward == null || reward.Amount <= 0)
            {
                return false;
            }

            switch (reward.RewardKind)
            {
                case WheelRewardKind.Coins:
                    EconomyManager.Instance?.AddCoins(reward.Amount, reason);
                    return EconomyManager.Instance != null;

                case WheelRewardKind.Booster:
                    if (reward.BoosterDefinition != null)
                    {
                        BoosterInventory.Instance?.AddBooster(reward.BoosterDefinition, reward.Amount, reason);
                        return BoosterInventory.Instance != null;
                    }

                    if (!string.IsNullOrWhiteSpace(reward.BoosterId))
                    {
                        BoosterInventory.Instance?.AddBooster(reward.BoosterId, reward.Amount, reason);
                        return BoosterInventory.Instance != null;
                    }

                    return false;

                case WheelRewardKind.Heart:
                    HeartManager.Instance?.AddHearts(reward.Amount);
                    return HeartManager.Instance != null;

                default:
                    Debug.LogWarning($"[WheelRewardGrantService] Unsupported reward kind: {reward.RewardKind}");
                    return false;
            }
        }
    }
}
