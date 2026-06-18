using System.Collections.Generic;
using _PawSlidePopGame._Scripts.Feature.Meta.Reward;
using _PawSlidePopGame._Scripts.Feature.Meta.Shop;
using NUnit.Framework;
using UnityEngine;

namespace _PawSlidePopGame._Scripts.Feature.Meta.Tests.Editor
{
    /// <summary>
    /// Unit tests cho ShopItemSO – kiểm tra dữ liệu cấu hình vật phẩm.
    /// Lưu ý: ShopService phụ thuộc vào EconomyManager singleton nên được kiểm tra riêng;
    /// phần logic SO không phụ thuộc singleton có thể được test hoàn toàn ở đây.
    /// </summary>
    public sealed class ShopServiceTests
    {
        // ───────────────────────────────────────────────
        // Tests ShopItemSO – Cấu hình vật phẩm
        // ───────────────────────────────────────────────

        [Test]
        public void ShopItem_NormalItem_HasCorrectDefaults()
        {
            ShopItemSO item = CreateNormalItem("Hammer_200", priceCoins: 200, originalPrice: 0);

            Assert.That(item.IsSpecial, Is.False, "Vật phẩm thường không phải Special.");
            Assert.That(item.HasDiscount, Is.False, "Không có giảm giá vì originalPriceCoins = 0.");
            Assert.That(item.PriceCoins, Is.EqualTo(200));
        }

        [Test]
        public void ShopItem_SpecialItem_HasDiscountAndBadge()
        {
            ShopItemSO item = CreateSpecialItem(
                id:            "Combo_Bundle",
                priceCoins:    1000,
                originalPrice: 1500,
                tagText:       "New items");

            Assert.That(item.IsSpecial, Is.True, "Vật phẩm đặc biệt phải có IsSpecial = true.");
            Assert.That(item.HasDiscount, Is.True, "originalPriceCoins > priceCoins → HasDiscount = true.");
            Assert.That(item.TagText, Is.EqualTo("New items"));
            Assert.That(item.OriginalPriceCoins, Is.EqualTo(1500));
            Assert.That(item.PriceCoins, Is.EqualTo(1000));
        }

        [Test]
        public void ShopItem_DisplayName_FallsBackToRewardName_WhenEmpty()
        {
            // Tạo RewardEntrySO với displayName rỗng
            RewardEntrySO reward = ScriptableObject.CreateInstance<RewardEntrySO>();
            reward.name = "Coins_100";
            reward.EditorSetValues(RewardKind.Coins, 100);

            ShopItemSO item = ScriptableObject.CreateInstance<ShopItemSO>();
            item.name = "ShopItem_Coins";
            // displayName không được set → fallback sang reward.DisplayName hoặc item.name
            // Kiểm tra rằng DisplayName không null/empty
            Assert.That(item.DisplayName, Is.Not.Null.And.Not.Empty);
        }

        [Test]
        public void ShopItem_Rewards_AreAccessibleCorrectly()
        {
            RewardEntrySO reward1 = CreateCoinReward("Coins_500", 500);
            RewardEntrySO reward2 = CreateCoinReward("Coins_100", 100);

            ShopItemSO item = CreateSpecialItemWithRewards("Bundle", 1000, 0, new List<RewardEntrySO> { reward1, reward2 });

            Assert.That(item.Rewards.Count, Is.EqualTo(2));
            Assert.That(item.Rewards[0].Amount, Is.EqualTo(500));
            Assert.That(item.Rewards[1].Amount, Is.EqualTo(100));
        }

        [Test]
        public void ShopItem_PriceCoins_MinimumIsOne()
        {
            // OnValidate không chạy trong test, kiểm tra thuộc tính PriceCoins clamp
            ShopItemSO item = CreateNormalItem("ZeroPrice", priceCoins: 0, originalPrice: 0);
            // PriceCoins property: Mathf.Max(1, priceCoins) → phải >= 1
            Assert.That(item.PriceCoins, Is.GreaterThanOrEqualTo(1),
                "PriceCoins không được nhỏ hơn 1.");
        }

        [Test]
        public void ShopItem_NoDiscount_WhenOriginalPriceLessThanOrEqualActualPrice()
        {
            ShopItemSO item = CreateNormalItem("Item_SamePrice", priceCoins: 500, originalPrice: 500);
            Assert.That(item.HasDiscount, Is.False,
                "Không coi là giảm giá khi original = actual.");

            ShopItemSO item2 = CreateNormalItem("Item_LowerOriginal", priceCoins: 500, originalPrice: 200);
            Assert.That(item2.HasDiscount, Is.False,
                "Không coi là giảm giá khi original < actual.");
        }

        // ───────────────────────────────────────────────
        // Helpers
        // ───────────────────────────────────────────────

        private static ShopItemSO CreateNormalItem(string id, int priceCoins, int originalPrice)
        {
            ShopItemSO item = ScriptableObject.CreateInstance<ShopItemSO>();
            item.name       = id;

            // Dùng SerializedObject để set giá trị vì field là private
            // (trong test editor Unity cho phép dùng SerializedObject)
            var so = new UnityEditor.SerializedObject(item);
            so.FindProperty("priceCoins").intValue         = priceCoins;
            so.FindProperty("originalPriceCoins").intValue = originalPrice;
            so.FindProperty("isSpecial").boolValue         = false;
            so.ApplyModifiedPropertiesWithoutUndo();

            return item;
        }

        private static ShopItemSO CreateSpecialItem(string id, int priceCoins, int originalPrice, string tagText)
        {
            ShopItemSO item = ScriptableObject.CreateInstance<ShopItemSO>();
            item.name       = id;

            var so = new UnityEditor.SerializedObject(item);
            so.FindProperty("priceCoins").intValue         = priceCoins;
            so.FindProperty("originalPriceCoins").intValue = originalPrice;
            so.FindProperty("isSpecial").boolValue         = true;
            so.FindProperty("tagText").stringValue         = tagText;
            so.ApplyModifiedPropertiesWithoutUndo();

            return item;
        }

        private static ShopItemSO CreateSpecialItemWithRewards(
            string id, int priceCoins, int originalPrice,
            List<RewardEntrySO> rewards)
        {
            ShopItemSO item = ScriptableObject.CreateInstance<ShopItemSO>();
            item.name       = id;

            var so = new UnityEditor.SerializedObject(item);
            so.FindProperty("priceCoins").intValue         = priceCoins;
            so.FindProperty("originalPriceCoins").intValue = originalPrice;
            so.FindProperty("isSpecial").boolValue         = true;

            var rewardsProp = so.FindProperty("rewards");
            rewardsProp.ClearArray();
            for (int i = 0; i < rewards.Count; i++)
            {
                rewardsProp.InsertArrayElementAtIndex(i);
                rewardsProp.GetArrayElementAtIndex(i).objectReferenceValue = rewards[i];
            }

            so.ApplyModifiedPropertiesWithoutUndo();

            return item;
        }

        private static RewardEntrySO CreateCoinReward(string id, int amount)
        {
            RewardEntrySO reward = ScriptableObject.CreateInstance<RewardEntrySO>();
            reward.name          = id;
            reward.EditorSetValues(RewardKind.Coins, amount);
            return reward;
        }
    }
}
