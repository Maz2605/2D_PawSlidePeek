using System;
using _PawSlidePopGame._Scripts.Feature.Match3.Model.Entities;

namespace _PawSlidePopGame._Scripts.Feature.Match3.Model.Board
{
    public class CellModel
    {
        public int X { get; }
        public int Y { get; }
        public bool IsPlayable { get; }
        public TileModel CurrentTile { get; private set; }
        public Action<TileModel> OnTileChanged;

        public CellModel(int x, int y, bool isPlayable)
        {
            X = x;
            Y = y;
            IsPlayable = isPlayable;
        }

        public void SetTile(TileModel newTile)
        {
            CurrentTile = newTile;
            OnTileChanged?.Invoke(CurrentTile);
        }

        public void ClearTile()
        {
            CurrentTile = null;
            OnTileChanged?.Invoke(null);
        }

        public bool IsEmpty()
        {
            return CurrentTile == null && IsPlayable;
        }

        public bool HasTile()
        {
            return CurrentTile != null;
        }
    }
}
