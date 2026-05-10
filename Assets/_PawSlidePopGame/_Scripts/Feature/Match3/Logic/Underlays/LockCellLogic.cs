using _PawSlidePopGame._Scripts.Feature.Match3.Core.Enum;
using _PawSlidePopGame._Scripts.Feature.Match3.Core.Interfaces;
using _PawSlidePopGame._Scripts.Feature.Match3.Model.Board;
using _PawSlidePopGame._Scripts.Feature.Match3.Model.Entities;
using _PawSlidePopGame._Scripts.Feature.Match3.Presentation;

namespace _PawSlidePopGame._Scripts.Feature.Match3.Logic.Underlays
{
    public sealed class LockCellLogic : IUnderlayLogic
    {
        public bool CanTileEnter(BoardModel board, CellModel cell, TileModel tile) => cell?.Underlay == null || cell.Underlay.CurrentHP <= 0;
        public bool CanTileExit(BoardModel board, CellModel cell, TileModel tile) => cell?.Underlay == null || cell.Underlay.CurrentHP <= 0;
        public bool LocksLine(BoardModel board, CellModel cell, MoveAxis axis) => cell?.Underlay != null && cell.Underlay.CurrentHP > 0;
        public bool BlocksMatch(BoardModel board, CellModel cell, TileModel tile) => false;
        public void OnTileEntered(BoardModel board, CellModel cell, TileModel tile, BoardFxContext fxContext) { }
        public void OnTileExited(BoardModel board, CellModel cell, TileModel tile, BoardFxContext fxContext) { }
        public void OnMatched(BoardModel board, CellModel cell, UnderlayContentModel tile, BoardFxContext fxContext) { }
        public void OnExploded(BoardModel board, CellModel cell, UnderlayContentModel tile, BoardFxContext fxContext) { }
    }
}

