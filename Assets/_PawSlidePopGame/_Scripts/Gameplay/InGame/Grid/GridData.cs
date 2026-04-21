using _PawSlidePopGame._Scripts.Gameplay.InGame.Tiles.Data;

namespace _PawSlidePopGame._Scripts.Gameplay.InGame.Grid
{
    using UnityEngine;

    public class GridData
    {
        public int Width { get; private set; }
        public int Height { get; private set; }
    
        private TileData[,] board; 

        public GridData(int width, int height)
        {
            Width = width;
            Height = height;
            board = new TileData[width, height];
        }

        public void SetTile(int x, int y, TileData data)
        {
            if (IsValidPosition(x, y)) board[x, y] = data;
        }

        public TileData GetTile(int x, int y)
        {
            if (IsValidPosition(x, y)) return board[x, y];
            return new TileData { category = TileCategory.Empty }; // Trả về Empty nếu ra khỏi viền
        }

        public bool IsValidPosition(int x, int y)
        {
            return x >= 0 && x < Width && y >= 0 && y < Height;
        }
    }
}