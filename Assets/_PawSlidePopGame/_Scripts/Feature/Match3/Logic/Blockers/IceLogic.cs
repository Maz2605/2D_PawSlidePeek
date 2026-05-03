using _PawSlidePopGame._Scripts.Feature.Match3.Logic.Base;
using _PawSlidePopGame._Scripts.Feature.Match3.Model.Board;
using _PawSlidePopGame._Scripts.Feature.Match3.Presentation;

namespace _PawSlidePopGame._Scripts.Feature.Match3.Logic.Blockers
{
    public class IceLogic : BaseBlockerLogic
    {
        public override void OnExploded(BoardModel board, CellModel cell, BoardFxContext fxContext)
        {
            if (cell.CurrentTile == null)
            {
                return;
            }

            var tile = cell.CurrentTile;
            int previousHp = tile.CurrentHP;
            tile.TakeDamage(1);
            fxContext?.RecordDamage(tile, cell, previousHp, tile.CurrentHP, tile.IsDead);
            if (tile.IsDead)
            {
                fxContext?.RecordClear(tile, cell, false);
                cell.ClearTile();
                board.AddScore(50);
                fxContext?.RecordScore(tile, cell, 50, board.CurrentScore);
            }
        }
    }
}
