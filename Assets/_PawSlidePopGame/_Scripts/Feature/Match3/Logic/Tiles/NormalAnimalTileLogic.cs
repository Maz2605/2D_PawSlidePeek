using _PawSlidePopGame._Scripts.Feature.Match3.Core;
using _PawSlidePopGame._Scripts.Feature.Match3.Model.Board;
using _PawSlidePopGame._Scripts.Feature.Match3.Presentation;

namespace _PawSlidePopGame._Scripts.Feature.Match3.Logic.Tiles
{
    public class NormalAnimalTileLogic : ITileLogic
    {
        public bool CanBeMoved() => true;
        public bool CanFall() => true;
        public bool CanMatch() => true;

        public void OnMatched(BoardModel board, CellModel cell, BoardFxContext fxContext)
        {
            fxContext?.RecordClear(cell?.CurrentTile, cell, true);
            cell.ClearTile();
            board.AddScore(10);
        }

        public void OnExploded(BoardModel board, CellModel cell, BoardFxContext fxContext)
        {
            fxContext?.RecordClear(cell?.CurrentTile, cell, false);
            cell.ClearTile();
            board.AddScore(10);
        }

        public void OnActivated(BoardModel board, CellModel cell, BoardFxContext fxContext)
        {
        }
    }
}
