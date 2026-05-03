using System;
using System.Collections.Generic;

namespace _PawSlidePopGame._Scripts.Feature.Match3.Data
{
    [Serializable]
    public struct LevelTargetData
    {
        public int tileId;
        public int requiredCount;

        public LevelTargetData(int tileId, int requiredCount)
        {
            this.tileId = tileId;
            this.requiredCount = requiredCount;
        }
    }

    [Serializable]
    public class Match3LevelData
    {
        public string levelID;
        public int displayLevelNumber = 1;
        public int width = 8;
        public int height = 8;
        public int movesLimit = 30;
        public int[] gridLayout;
        public bool[] playableMask;
        public List<int> spawnableTileIds = new List<int>();
        public List<LevelTargetData> targets = new List<LevelTargetData>();
        public int[] starScoreThresholds = { 100, 250, 500, 1500 };

        public int CellCount => Math.Max(0, width) * Math.Max(0, height);
        public int DisplayLevelNumber => displayLevelNumber > 0 ? displayLevelNumber : 1;

        public bool HasValidGridLayout()
        {
            return gridLayout != null && gridLayout.Length == CellCount;
        }

        public bool HasPlayableMask()
        {
            return playableMask != null && playableMask.Length == CellCount;
        }

        public bool IsPlayableCell(int x, int y)
        {
            if (x < 0 || x >= width || y < 0 || y >= height)
            {
                return false;
            }

            if (!HasPlayableMask())
            {
                return true;
            }

            return playableMask[(y * width) + x];
        }

        public int GetLayoutValue(int x, int y)
        {
            if (!HasValidGridLayout() || x < 0 || x >= width || y < 0 || y >= height)
            {
                return 0;
            }

            return gridLayout[(y * width) + x];
        }

        public int[] GetSafeStarThresholds()
        {
            if (starScoreThresholds == null || starScoreThresholds.Length == 0)
            {
                return new[] { 100, 250, 500, 1500 };
            }

            int[] clone = new int[starScoreThresholds.Length];
            int lastValue = 0;
            for (int i = 0; i < starScoreThresholds.Length; i++)
            {
                lastValue = Math.Max(lastValue, Math.Max(0, starScoreThresholds[i]));
                clone[i] = lastValue;
            }

            return clone;
        }

        public List<LevelTargetData> GetValidTargets()
        {
            List<LevelTargetData> validTargets = new List<LevelTargetData>();
            if (targets == null)
            {
                return validTargets;
            }

            for (int i = 0; i < targets.Count; i++)
            {
                LevelTargetData target = targets[i];
                if (target.tileId <= 0 || target.requiredCount <= 0)
                {
                    continue;
                }

                validTargets.Add(target);
            }

            return validTargets;
        }
    }
}
