using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

namespace _PawSlidePopGame._Scripts.Gameplay.Meta.MapManager
{
    public class MapManager
    {
        private static readonly int[] LanePattern = { 0, -1, 1, -2, 2 };

        public IReadOnlyList<MapLevelEntry> BuildEntries(
            IReadOnlyList<string> levelIds,
            MapWorldConfigSO config,
            int highestUnlockedLevelNumber = int.MaxValue,
            int currentLevelNumber = 1,
            LevelProgressRepository progressRepository = null)
        {
            List<MapLevelEntry> entries = new List<MapLevelEntry>();
            if (levelIds == null || levelIds.Count == 0)
            {
                return entries;
            }

            int maxVisibleLevels = config != null ? config.MaxVisibleLevels : levelIds.Count;
            int count = Mathf.Min(levelIds.Count, maxVisibleLevels);
            System.Random random = new System.Random(config != null ? config.Seed : 0);
            float levelSpacing = config != null ? config.LevelSpacing : 170f;
            float horizontalLimit = config != null ? config.HorizontalLimit : 260f;
            float horizontalJitter = config != null ? config.HorizontalJitter : 70f;
            float bottomPadding = config != null ? config.BottomPadding : 160f;
            int levelsPerSection = config != null ? config.LevelsPerSection : 8;

            int previousLane = 0;
            for (int i = 0; i < count; i++)
            {
                string levelId = levelIds[i];
                int displayNumber = TryParseLevelNumber(levelId, out int parsedNumber) ? parsedNumber : i + 1;
                int lane = PickLane(random, previousLane);
                previousLane = lane;

                float laneX = horizontalLimit <= 0f ? 0f : Mathf.Lerp(-horizontalLimit, horizontalLimit, (lane + 2) / 4f);
                float jitter = horizontalJitter <= 0f ? 0f : (float)((random.NextDouble() * 2d) - 1d) * horizontalJitter;
                float x = Mathf.Clamp(laneX + jitter, -horizontalLimit, horizontalLimit);
                float y = bottomPadding + (i * levelSpacing);
                int sectionIndex = Mathf.Max(0, i / levelsPerSection);
                LevelProgressEntryData progress = progressRepository?.GetProgress(levelId);
                int bestStars = progress != null ? progress.bestStars : 0;
                int bestScore = progress != null ? progress.bestScore : 0;
                bool isHardLevel = config != null && config.IsHardLevel(displayNumber);
                MapLevelState state = ResolveState(displayNumber, highestUnlockedLevelNumber, currentLevelNumber, bestStars, isHardLevel);

                entries.Add(new MapLevelEntry(
                    levelId,
                    displayNumber,
                    sectionIndex,
                    new Vector2(x, y),
                    state,
                    bestStars,
                    bestScore,
                    isHardLevel));
            }

            return entries;
        }

        public float CalculateContentHeight(IReadOnlyList<MapLevelEntry> entries, MapWorldConfigSO config)
        {
            float sectionHeight = config != null ? config.SectionHeight : 1100f;
            float topPadding = config != null ? config.TopPadding : 220f;
            float bottomPadding = config != null ? config.BottomPadding : 160f;
            if (entries == null || entries.Count == 0)
            {
                return sectionHeight;
            }

            MapLevelEntry lastEntry = entries[entries.Count - 1];
            int sectionCount = Mathf.Max(1, lastEntry.SectionIndex + 1);
            float nodeHeight = lastEntry.AnchoredPosition.y + topPadding;
            return Mathf.Max(sectionHeight * sectionCount, nodeHeight + bottomPadding);
        }

        public static bool TryParseLevelNumber(string levelId, out int levelNumber)
        {
            levelNumber = 0;
            if (string.IsNullOrWhiteSpace(levelId))
            {
                return false;
            }

            int lastSeparatorIndex = levelId.LastIndexOf('_');
            string numberText = lastSeparatorIndex >= 0 && lastSeparatorIndex < levelId.Length - 1
                ? levelId.Substring(lastSeparatorIndex + 1)
                : levelId;

            return int.TryParse(numberText, NumberStyles.Integer, CultureInfo.InvariantCulture, out levelNumber) &&
                   levelNumber > 0;
        }

        private static int PickLane(System.Random random, int previousLane)
        {
            int lane = previousLane;
            for (int attempt = 0; attempt < 6; attempt++)
            {
                lane = LanePattern[random.Next(0, LanePattern.Length)];
                if (lane != previousLane)
                {
                    break;
                }
            }

            return lane;
        }

        private static MapLevelState ResolveState(
            int displayNumber,
            int highestUnlockedLevelNumber,
            int currentLevelNumber,
            int bestStars,
            bool isHardLevel)
        {
            if (displayNumber > highestUnlockedLevelNumber)
            {
                return MapLevelState.Locked;
            }

            if (bestStars >= 4)
            {
                return MapLevelState.Perfect;
            }

            if (bestStars > 0)
            {
                return MapLevelState.Completed;
            }

            if (isHardLevel)
            {
                return MapLevelState.Hard;
            }

            if (displayNumber == currentLevelNumber)
            {
                return MapLevelState.Current;
            }

            return MapLevelState.Unplayed;
        }
    }
}
