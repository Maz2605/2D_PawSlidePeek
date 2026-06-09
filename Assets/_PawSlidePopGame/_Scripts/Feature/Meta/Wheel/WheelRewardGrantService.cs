using _PawSlidePopGame._Scripts.Feature.Meta.Reward;

namespace _PawSlidePopGame._Scripts.Feature.Meta.Wheel
{
    /// <summary>
    /// Wrapper trao thưởng dành riêng cho Wheel – delegate sang RewardGrantService dùng chung.
    /// </summary>
    public static class WheelRewardGrantService
    {
        public static bool TryGrant(WheelRewardEntryData reward, string reason = "wheel_spin")
        {
            return reward != null && RewardGrantService.TryGrant(reward.Reward, reason);
        }
    }
}
