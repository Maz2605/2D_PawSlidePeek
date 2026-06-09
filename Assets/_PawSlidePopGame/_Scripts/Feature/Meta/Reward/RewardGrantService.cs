using System.Collections.Generic;
using _PawSlidePopGame._Scripts.Gameplay.Meta.EconomyManager;
using _PawSlidePopGame._Scripts.Gameplay.Meta.Inventory;
using UnityEngine;

namespace _PawSlidePopGame._Scripts.Feature.Meta.Reward
{
    /// <summary>
    /// Dịch vụ trao thưởng dùng chung cho toàn game (Wheel, Shop, Star Rewards, v.v.).
    /// Không kéo theo singleton – chỉ là static utility class.
    /// </summary>
    public static class RewardGrantService
    {
        /// <summary>Trao một phần thưởng đơn lẻ cho người chơi.</summary>
        /// <param name="reward">Asset cấu hình phần thưởng.</param>
        /// <param name="reason">Lý do trao thưởng (dùng cho analytics/log).</param>
        /// <returns>true nếu trao thành công.</returns>
        public static bool TryGrant(RewardEntrySO reward, string reason = "grant")
        {
            if (reward == null || reward.Amount <= 0)
            {
                return false;
            }
            switch (reward.RewardKind)
            {
                case RewardKind.Coins:
                    if (EconomyManager.Instance == null)
                    {
                        Debug.LogWarning("[RewardGrantService] EconomyManager.Instance is null.");
                        return false;
                    }
                    EconomyManager.Instance.AddCoins(reward.Amount, reason);
                    return true;
                case RewardKind.Booster:
                    if (reward.BoosterDefinition == null)
                    {
                        Debug.LogWarning($"[RewardGrantService] Reward '{reward.RewardId}' là loại Booster nhưng không có BoosterDefinitionSO nào được gán.");
                        return false;
                    }
                    if (BoosterInventory.Instance == null)
                    {
                        Debug.LogWarning("[RewardGrantService] BoosterInventory.Instance is null.");
                        return false;
                    }
                    BoosterInventory.Instance.AddBooster(reward.BoosterDefinition, reward.Amount, reason);
                    return true;
                case RewardKind.Heart:
                    if (HeartManager.Instance == null)
                    {
                        Debug.LogWarning("[RewardGrantService] HeartManager.Instance is null.");
                        return false;
                    }

                    HeartManager.Instance.AddHearts(reward.Amount);
                    return true;

                case RewardKind.InfiniteHeart:
                    if (HeartManager.Instance == null)
                    {
                        Debug.LogWarning("[RewardGrantService] HeartManager.Instance is null.");
                        return false;
                    }

                    // reward.Amount is minutes of infinite hearts. Convert to seconds.
                    HeartManager.Instance.AddInfiniteHearts(reward.Amount * 60);
                    return true;

                default:
                    Debug.LogWarning($"[RewardGrantService] Loại phần thưởng chưa được hỗ trợ: {reward.RewardKind}");
                    return false;
            }
        }

        /// <summary>
        /// Trao nhiều phần thưởng cùng lúc (dùng cho gói đặc biệt / bundles).
        /// </summary>
        /// <param name="rewards">Danh sách asset phần thưởng.</param>
        /// <param name="reason">Lý do trao thưởng.</param>
        /// <returns>true nếu TẤT CẢ phần thưởng đều được trao thành công.</returns>
        public static bool TryGrantMultiple(IEnumerable<RewardEntrySO> rewards, string reason = "grant")
        {
            if (rewards == null)
            {
                return false;
            }

            bool allSucceeded = true;
            foreach (RewardEntrySO reward in rewards)
            {
                if (!TryGrant(reward, reason))
                {
                    allSucceeded = false;
                }
            }

            return allSucceeded;
        }
    }
}
