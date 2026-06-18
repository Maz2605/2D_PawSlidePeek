#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using _PawSlidePopGame._Scripts.Feature.Match3.Boosters;
using _PawSlidePopGame._Scripts.Feature.Meta.Reward;
using _PawSlidePopGame._Scripts.Feature.Meta.Shop;
using _PawSlidePopGame._Scripts.Gameplay.Meta.EconomyManager;
using UnityEditor;
using UnityEngine;

namespace _PawSlidePopGame._Scripts.Feature.Meta.Shop.Editor
{
    public static class ShopDevTools
    {
        [MenuItem("Tools/Shop Dev/Add 5000 Coins")]
        public static void Add5000Coins()
        {
            if (!Application.isPlaying)
            {
                Debug.LogWarning("Vui lòng vào Play Mode để cộng Coin.");
                return;
            }

            if (EconomyManager.Instance != null)
            {
                EconomyManager.Instance.AddCoins(5000, "dev_tool");
                Debug.Log("[ShopDevTools] Đã cộng 5000 Coins thành công!");
            }
            else
            {
                Debug.LogError("[ShopDevTools] Không tìm thấy EconomyManager.Instance.");
            }
        }

        [MenuItem("Tools/Shop Dev/Refill 5 Hearts")]
        public static void Refill5Hearts()
        {
            if (!Application.isPlaying)
            {
                Debug.LogWarning("Vui lòng vào Play Mode để cộng Mạng (Hearts).");
                return;
            }

            if (HeartManager.Instance != null)
            {
                HeartManager.Instance.AddHearts(5, allowOverfill: true);
                Debug.Log("[ShopDevTools] Đã nạp đầy 5 Mạng thành công!");
            }
            else
            {
                Debug.LogError("[ShopDevTools] Không tìm thấy HeartManager.Instance.");
            }
        }

        [MenuItem("Tools/Shop Dev/Create More Test Items")]
        public static void CreateMoreTestItems()
        {
            // Đường dẫn lưu trữ test items
            string itemsFolder = "Assets/Resources/Shop/TestItems";
            if (!Directory.Exists(itemsFolder))
            {
                Directory.CreateDirectory(itemsFolder);
            }

            // Load Booster definitions có sẵn để gán vào phần thưởng
            string hammerPath = "Assets/_PawSlidePopGame/_Data/Boosters/HammerBooster.asset";
            var hammerDef = AssetDatabase.LoadAssetAtPath<BoosterDefinitionSO>(hammerPath);

            // 1. Tạo RewardEntrySO
            RewardEntrySO superCoinsReward = ScriptableObject.CreateInstance<RewardEntrySO>();
            superCoinsReward.name = "SuperCoins_1000";
            superCoinsReward.EditorSetValues(RewardKind.Coins, 1000);
            string rewardPath = Path.Combine(itemsFolder, "Reward_SuperCoins.asset");
            AssetDatabase.CreateAsset(superCoinsReward, rewardPath);

            RewardEntrySO testHammerReward = null;
            if (hammerDef != null)
            {
                testHammerReward = ScriptableObject.CreateInstance<RewardEntrySO>();
                testHammerReward.name = "TestHammer_x5";
                testHammerReward.EditorSetValues(RewardKind.Booster, 5, hammerDef);
                string hRewardPath = Path.Combine(itemsFolder, "Reward_TestHammer_x5.asset");
                AssetDatabase.CreateAsset(testHammerReward, hRewardPath);
            }

            // 2. Tạo ShopItemSO - Gói Tiền Siêu Khổng Lồ (Mega Coin Pack)
            ShopItemSO megaCoinsItem = ScriptableObject.CreateInstance<ShopItemSO>();
            megaCoinsItem.name = "Item_MegaCoins_Test";
            
            var so1 = new SerializedObject(megaCoinsItem);
            so1.FindProperty("displayName").stringValue = "Mega Coin Pack (Test)";
            so1.FindProperty("description").stringValue = "Gói Coin khổng lồ dành cho nhà phát triển kiểm thử.";
            so1.FindProperty("priceCoins").intValue = 50; // Giá cực rẻ để dễ test
            so1.FindProperty("originalPriceCoins").intValue = 1000; // Hiển thị giảm giá sâu
            so1.FindProperty("isSpecial").boolValue = true;
            so1.FindProperty("tagText").stringValue = "-95%";
            
            var rewardsProp1 = so1.FindProperty("rewards");
            rewardsProp1.ClearArray();
            rewardsProp1.InsertArrayElementAtIndex(0);
            rewardsProp1.GetArrayElementAtIndex(0).objectReferenceValue = superCoinsReward;
            so1.ApplyModifiedPropertiesWithoutUndo();

            string item1Path = Path.Combine(itemsFolder, "Item_MegaCoins_Test.asset");
            AssetDatabase.CreateAsset(megaCoinsItem, item1Path);

            // 3. Tạo ShopItemSO - Gói Búa Huỷ Diệt (Hammer Pack Test)
            ShopItemSO megaHammerItem = null;
            if (testHammerReward != null)
            {
                megaHammerItem = ScriptableObject.CreateInstance<ShopItemSO>();
                megaHammerItem.name = "Item_MegaHammer_Test";

                var so2 = new SerializedObject(megaHammerItem);
                so2.FindProperty("displayName").stringValue = "Mega Hammer Pack (Test)";
                so2.FindProperty("description").stringValue = "Mua 5 Búa đập để thỏa sức phá vỡ các khối gạch.";
                so2.FindProperty("priceCoins").intValue = 100;
                so2.FindProperty("originalPriceCoins").intValue = 500;
                so2.FindProperty("isSpecial").boolValue = true;
                so2.FindProperty("tagText").stringValue = "SUPER";

                var rewardsProp2 = so2.FindProperty("rewards");
                rewardsProp2.ClearArray();
                rewardsProp2.InsertArrayElementAtIndex(0);
                rewardsProp2.GetArrayElementAtIndex(0).objectReferenceValue = testHammerReward;
                so2.ApplyModifiedPropertiesWithoutUndo();

                string item2Path = Path.Combine(itemsFolder, "Item_MegaHammer_Test.asset");
                AssetDatabase.CreateAsset(megaHammerItem, item2Path);
            }

            // 4. Thêm các Item mới vào DefaultShopCatalog.asset
            string catalogPath = "Assets/Resources/Shop/DefaultShopCatalog.asset";
            var catalog = AssetDatabase.LoadAssetAtPath<ShopCatalogSO>(catalogPath);
            if (catalog != null)
            {
                var catalogSO = new SerializedObject(catalog);
                var itemsProp = catalogSO.FindProperty("items");

                // Thêm MegaCoinsItem
                int index = itemsProp.arraySize;
                itemsProp.InsertArrayElementAtIndex(index);
                itemsProp.GetArrayElementAtIndex(index).objectReferenceValue = megaCoinsItem;

                // Thêm MegaHammerItem
                if (megaHammerItem != null)
                {
                    index = itemsProp.arraySize;
                    itemsProp.InsertArrayElementAtIndex(index);
                    itemsProp.GetArrayElementAtIndex(index).objectReferenceValue = megaHammerItem;
                }

                catalogSO.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(catalog);
                AssetDatabase.SaveAssets();

                Debug.Log("[ShopDevTools] Tạo test items thành công và đã tự động gán vào DefaultShopCatalog!");
            }
            else
            {
                Debug.LogWarning("[ShopDevTools] Không tìm thấy DefaultShopCatalog.asset để tự động gán.");
            }

            AssetDatabase.Refresh();
        }
    }
}
#endif
