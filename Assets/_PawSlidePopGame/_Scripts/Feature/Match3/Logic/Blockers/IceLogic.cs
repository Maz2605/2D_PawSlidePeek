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

            int previousHp = cell.CurrentTile.CurrentHP;
            cell.CurrentTile.TakeDamage(1);
            fxContext?.RecordDamage(cell.CurrentTile, cell, previousHp, cell.CurrentTile.CurrentHP, cell.CurrentTile.IsDead);
            if (cell.CurrentTile.IsDead)
            {
                fxContext?.RecordClear(cell.CurrentTile, cell, false);
                cell.ClearTile();
                board.AddScore(50);
            }
        }
    }
}
