using System;
using _PawSlidePopGame._Scripts.Data.LevelProvider;
using _PawSlidePopGame._Scripts.Data.SaveSystem;

namespace _PawSlidePopGame._Scripts.Gameplay.Meta.MapManager
{
    public sealed class LevelProgressRepository
    {
        public const string DefaultSaveKey = "level_progress";

        private static readonly Lazy<LevelProgressRepository> LazyInstance =
            new Lazy<LevelProgressRepository>(() => new LevelProgressRepository(DefaultSaveKey));

        private readonly string _saveKey;
        private LevelProgressSaveData _data;

        public static LevelProgressRepository Instance => LazyInstance.Value;
        public LevelProgressSaveData Data => _data ??= Load();

        public LevelProgressRepository(string saveKey)
        {
            _saveKey = string.IsNullOrWhiteSpace(saveKey) ? DefaultSaveKey : saveKey;
        }

        public void Reload()
        {
            _data = Load();
        }

        public LevelProgressEntryData GetProgress(string levelId)
        {
            string sanitizedLevelId = LevelPathUtility.SanitizeLevelId(levelId);
            if (string.IsNullOrWhiteSpace(sanitizedLevelId))
            {
                return null;
            }

            LevelProgressSaveData data = Data;
            for (int i = 0; i < data.levels.Count; i++)
            {
                LevelProgressEntryData entry = data.levels[i];
                if (entry != null && string.Equals(entry.levelId, sanitizedLevelId, StringComparison.OrdinalIgnoreCase))
                {
                    return entry;
                }
            }

            return null;
        }

        public bool RecordLevelResult(string levelId, int reachedStars, int score)
        {
            string sanitizedLevelId = LevelPathUtility.SanitizeLevelId(levelId);
            if (string.IsNullOrWhiteSpace(sanitizedLevelId))
            {
                return false;
            }

            LevelProgressEntryData entry = GetOrCreateProgress(sanitizedLevelId);
            int sanitizedStars = Math.Max(0, reachedStars);
            int sanitizedScore = Math.Max(0, score);
            bool changed = false;

            if (sanitizedStars > entry.bestStars)
            {
                entry.bestStars = sanitizedStars;
                changed = true;
            }

            if (sanitizedScore > entry.bestScore)
            {
                entry.bestScore = sanitizedScore;
                changed = true;
            }

            if (!entry.isCompleted && sanitizedStars > 0)
            {
                entry.isCompleted = true;
                changed = true;
            }

            if (changed)
            {
                Save();
            }

            return changed;
        }

        public void Save()
        {
            Data.Sanitize();
            SaveSystem.Save(_saveKey, Data);
        }

        public void DeleteSave()
        {
            _data = new LevelProgressSaveData();
            SaveSystem.DeleteFile(_saveKey);
        }

        private LevelProgressEntryData GetOrCreateProgress(string sanitizedLevelId)
        {
            LevelProgressEntryData existing = GetProgress(sanitizedLevelId);
            if (existing != null)
            {
                return existing;
            }

            LevelProgressEntryData entry = new LevelProgressEntryData
            {
                levelId = sanitizedLevelId
            };
            Data.levels.Add(entry);
            return entry;
        }

        private LevelProgressSaveData Load()
        {
            LevelProgressSaveData loaded = SaveSystem.Load<LevelProgressSaveData>(_saveKey) ?? new LevelProgressSaveData();
            loaded.Sanitize();
            return loaded;
        }
    }
}
