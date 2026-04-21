using System;

namespace _PawSlidePopGame._Scripts.Gameplay.InGame.Tiles.Data
{
    [Serializable]
    public struct TileData
    {
        public int id;
        public TileCategory category;

        public bool IsMatchableWith(TileData other)
        {
            if (this.category != TileCategory.Normal || other.category != TileCategory.Normal)
                return false;

            return this.id == other.id;
        }
    }
}