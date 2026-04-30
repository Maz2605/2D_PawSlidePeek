using System.Collections.Generic;
using _PawSlidePopGame._Scripts.Feature.Match3.Logic.Base;
using _PawSlidePopGame._Scripts.Feature.Match3.Model.Board;
using _PawSlidePopGame._Scripts.Feature.Match3.Presentation;

namespace _PawSlidePopGame._Scripts.Feature.Match3.Logic.Booster
{
    public class BombLogic : BaseBoosterLogic
    {
        public override void OnActivated(BoardModel board, CellModel cell, BoardFxContext fxContext)
        {
            if (cell == null || cell.CurrentTile == null)
            {
                return;
            }

            fxContext?.RecordActivate(cell.CurrentTile, cell);
            fxContext?.RecordClear(cell.CurrentTile, cell, false);
            cell.ClearTile();
            board.AddScore(25);

            List<CellModel> neighbors = board.GetNeighbors(cell.X, cell.Y, 1);
            for (int i = 0; i < neighbors.Count; i++)
            {
                CellModel neighbor = neighbors[i];
                if (neighbor.CurrentTile != null)
                {
                    neighbor.CurrentTile.Explode(board, neighbor, fxContext);
                }
            }
        }
    }
}
