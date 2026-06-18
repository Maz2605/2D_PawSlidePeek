#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using _PawSlidePopGame._Scripts.Data.SaveSystem;
using _PawSlidePopGame._Scripts.Gameplay.Meta.EconomyManager;
using _PawSlidePopGame._Scripts.Gameplay.Meta.MapManager;
using _PawSlidePopGame._Scripts.Feature.Meta.Wheel;
using _PawSlidePopGame._Scripts.Feature.Match3.Boosters;
using _PawSlidePopGame._Scripts.Gameplay.Meta.Inventory;

namespace _PawSlidePopGame._Scripts.Utils.Editor
{
    public static class DevToolsMenu
    {
        [MenuItem("Tools/Paw Slide Pop/Dev/Reset All Save Data")]
        public static void ResetAllSaveData()
        {
            // 1. Delete JSON files
            string economyPath = SaveSystem.GetPath(PlayerEconomyRepository.DefaultSaveKey);
            string progressPath = SaveSystem.GetPath(LevelProgressRepository.DefaultSaveKey);
            string wheelPath = SaveSystem.GetPath(WheelStateRepository.DefaultSaveKey);

            DeleteFileAtPath(economyPath);
            DeleteFileAtPath(progressPath);
            DeleteFileAtPath(wheelPath);

            // Temp files
            DeleteFileAtPath(economyPath + ".tmp");
            DeleteFileAtPath(progressPath + ".tmp");
            DeleteFileAtPath(wheelPath + ".tmp");

            // 2. Clear PlayerPrefs
            PlayerPrefs.DeleteAll();
            PlayerPrefs.Save();

            Debug.Log("<color=green>[DevTools] Reset all save data and PlayerPrefs successfully!</color>");

            // 3. Reload repositories if playing
            if (Application.isPlaying)
            {
                PlayerEconomyRepository.Instance.Reload();
                LevelProgressRepository.Instance.Reload();
                if (BoosterInventory.Instance != null)
                {
                    BoosterInventory.Instance.Reload();
                }
                
                // Show toast
                _PawSlidePopGame._Scripts.UI.Manager.UIManager.Instance?.ShowToast("All Data Reset!");
            }
            
            AssetDatabase.Refresh();
        }

        [MenuItem("Tools/Paw Slide Pop/Dev/Unlock All Levels & Max Resources (99)")]
        public static void UnlockAllAndMaxResources()
        {
            // Setup economy data
            PlayerEconomySaveData economyData;
            if (Application.isPlaying)
            {
                economyData = PlayerEconomyRepository.Instance.Data;
            }
            else
            {
                economyData = SaveSystem.Load<PlayerEconomySaveData>(PlayerEconomyRepository.DefaultSaveKey) ?? new PlayerEconomySaveData();
            }

            economyData.coins = 9999;
            economyData.hearts = 99;

            // Fill all boosters to 99
            // First we load from resources BoosterDatabase
            var db = Resources.Load<_PawSlidePopGame._Scripts.Feature.Meta.Reward.BoosterDatabaseSO>("Configs/BoosterDatabase");
            if (db != null && db.Boosters != null)
            {
                foreach (var booster in db.Boosters)
                {
                    if (booster != null && !string.IsNullOrWhiteSpace(booster.BoosterId))
                    {
                        economyData.boosterCounts[booster.BoosterId] = 99;
                    }
                }
            }
            else
            {
                // Fallback hardcoded IDs
                string[] defaultBoosterIds = {
                    "hammer", "line_clear", "rainbow", "shuffle",
                    "pre_start_square_boom", "pre_start_cross_boom",
                    "pre_start_board_clear_boom", "pre_extra_moves_3"
                };
                foreach (var id in defaultBoosterIds)
                {
                    economyData.boosterCounts[id] = 99;
                }
            }

            // Also check all assets directly in editor mode for safety
            var boosterGUIDs = AssetDatabase.FindAssets("t:BoosterDefinitionSO");
            foreach (var guid in boosterGUIDs)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var booster = AssetDatabase.LoadAssetAtPath<BoosterDefinitionSO>(path);
                if (booster != null && !string.IsNullOrEmpty(booster.BoosterId))
                {
                    economyData.boosterCounts[booster.BoosterId] = 99;
                }
            }

            // Save economy data
            if (Application.isPlaying)
            {
                PlayerEconomyRepository.Instance.Save();
            }
            else
            {
                SaveSystem.Save(PlayerEconomyRepository.DefaultSaveKey, economyData);
            }

            // Setup level progress data
            LevelProgressSaveData progressData;
            if (Application.isPlaying)
            {
                progressData = LevelProgressRepository.Instance.Data;
            }
            else
            {
                progressData = SaveSystem.Load<LevelProgressSaveData>(LevelProgressRepository.DefaultSaveKey) ?? new LevelProgressSaveData();
            }

            progressData.highestUnlockedLevelNumber = 99;
            progressData.currentLevelId = "Level_001";

            // Save progress data
            if (Application.isPlaying)
            {
                LevelProgressRepository.Instance.Save();
            }
            else
            {
                SaveSystem.Save(LevelProgressRepository.DefaultSaveKey, progressData);
            }

            Debug.Log("<color=green>[DevTools] Unlocked all levels (highest level: 99) and maxed resources (99) successfully!</color>");

            // Reload repositories if playing
            if (Application.isPlaying)
            {
                PlayerEconomyRepository.Instance.Reload();
                LevelProgressRepository.Instance.Reload();
                if (BoosterInventory.Instance != null)
                {
                    BoosterInventory.Instance.Reload();
                }
                
                // Show toast
                _PawSlidePopGame._Scripts.UI.Manager.UIManager.Instance?.ShowToast("Unlocked & Max Resources!");
            }

            AssetDatabase.Refresh();
        }

        private static void DeleteFileAtPath(string path)
        {
            try
            {
                if (File.Exists(path))
                {
                    File.Delete(path);
                    Debug.Log($"[DevTools] Deleted: {path}");
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[DevTools] Failed to delete file at {path}: {e.Message}");
            }
        }
    }
}
#endif
