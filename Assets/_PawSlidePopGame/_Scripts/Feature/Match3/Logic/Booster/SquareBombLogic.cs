using System.Collections.Generic;
using _PawSlidePopGame._Scripts.Feature.Match3.Logic.Base;
using _PawSlidePopGame._Scripts.Feature.Match3.Model.Board;
using _PawSlidePopGame._Scripts.Feature.Match3.Presentation;

namespace _PawSlidePopGame._Scripts.Feature.Match3.Logic.Booster
{
    public class SquareBombLogic : BaseBoosterLogic
    {
        public override void OnActivated(BoardModel board, CellModel cell, BoardFxContext fxContext)
        {
            if (board == null || cell == null || cell.CurrentTile == null)
            {
                return;
            }

            List<CellModel> candidates = new List<CellModel>();
            foreach (CellModel candidate in board.GetAllCells())
            {
                if (candidate != null && candidate.IsPlayable && candidate.CurrentTile != null)
                {
                    candidates.Add(candidate);
                }
            }

            if (candidates.Count == 0)
            {
                return;
            }

            int index = fxContext?.Random != null ? fxContext.Random.Next(0, candidates.Count) : UnityEngine.Random.Range(0, candidates.Count);
            CellModel targetCell = candidates[index];
            fxContext?.RecordActivate(cell.CurrentTile, cell);
            fxContext?.RecordTargetSelection(cell.CurrentTile, targetCell);
            fxContext?.RecordClear(cell.CurrentTile, cell, false);
            cell.ClearTile();
            board.AddScore(30);

            for (int x = targetCell.X - 1; x <= targetCell.X + 1; x++)
            {
                for (int y = targetCell.Y - 1; y <= targetCell.Y + 1; y++)
                {
                    CellModel affectedCell = board.GetCell(x, y);
                    if (affectedCell?.CurrentTile != null)
                    {
                        affectedCell.CurrentTile.Explode(board, affectedCell, fxContext);
                    }
                }
            }
        }
    }
}
