using System.Collections.Generic;
using _PawSlidePopGame._Scripts.Feature.Match3.Model.Board;
using _PawSlidePopGame._Scripts.Feature.Match3.Model.Entities;

namespace _PawSlidePopGame._Scripts.Feature.Match3.Logic.Match
{
    public class BoardMatchFinder : IBoardMatchRule
    {
        public HashSet<CellModel> FindMatchedCells(BoardModel board)
        {
            HashSet<CellModel> matchedCells = new HashSet<CellModel>();

            for (int y = 0; y < board.Height; y++)
            {
                int runStartX = 0;
                while (runStartX < board.Width)
                {
                    int runLength = GetHorizontalRunLength(board, runStartX, y);
                    if (runLength >= 3)
                    {
                        for (int x = runStartX; x < runStartX + runLength; x++)
                        {
                            matchedCells.Add(board.GetCell(x, y));
                        }
                    }

                    runStartX += runLength > 0 ? runLength : 1;
                }
            }

            for (int x = 0; x < board.Width; x++)
            {
                int runStartY = 0;
                while (runStartY < board.Height)
                {
                    int runLength = GetVerticalRunLength(board, x, runStartY);
                    if (runLength >= 3)
                    {
                        for (int y = runStartY; y < runStartY + runLength; y++)
                        {
                            matchedCells.Add(board.GetCell(x, y));
                        }
                    }

                    runStartY += runLength > 0 ? runLength : 1;
                }
            }

            return matchedCells;
        }

        private static int GetHorizontalRunLength(BoardModel board, int startX, int y)
        {
            TileModel seed = board.GetCell(startX, y)?.CurrentTile;
            if (!IsMatchable(seed))
            {
                return 0;
            }

            int length = 1;
            for (int x = startX + 1; x < board.Width; x++)
            {
                TileModel candidate = board.GetCell(x, y)?.CurrentTile;
                if (!seed.IsMatchableWith(candidate))
                {
                    break;
                }

                length++;
            }

            return length;
        }

        private static int GetVerticalRunLength(BoardModel board, int x, int startY)
        {
            TileModel seed = board.GetCell(x, startY)?.CurrentTile;
            if (!IsMatchable(seed))
            {
                return 0;
            }

            int length = 1;
            for (int y = startY + 1; y < board.Height; y++)
            {
                TileModel candidate = board.GetCell(x, y)?.CurrentTile;
                if (!seed.IsMatchableWith(candidate))
                {
                    break;
                }

                length++;
            }

            return length;
        }

        private static bool IsMatchable(TileModel tile)
        {
            return tile != null && tile.CanMatch();
        }
    }
}
