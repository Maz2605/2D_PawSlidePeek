using System;
using System.Collections.Generic;

namespace _PawSlidePopGame._Scripts.Feature.Match3.Data
{
    [Serializable]
    public class Match3LevelData
    {
        public string levelID;
        public int width = 8;
        public int height = 8;
        public int movesLimit = 30;
        public int[] gridLayout;
        public bool[] playableMask;
        public List<int> spawnableTileIds = new List<int>();

        public int CellCount => Math.Max(0, width) * Math.Max(0, height);

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
    }
}
