using _PawSlidePopGame._Scripts.Feature.Match3.Core;
using _PawSlidePopGame._Scripts.Feature.Match3.Model.Board;
using _PawSlidePopGame._Scripts.Feature.Match3.Presentation;

namespace _PawSlidePopGame._Scripts.Feature.Match3.Logic.Base
{
    public abstract class BaseBoosterLogic : ITileLogic
    {
        public virtual bool CanBeMoved() => true;
        public virtual bool CanFall() => true;
        public virtual bool CanMatch() => false;

        public virtual void OnMatched(BoardModel board, CellModel cell, BoardFxContext fxContext) { }

        public virtual void OnExploded(BoardModel board, CellModel cell, BoardFxContext fxContext)
        {
            OnActivated(board, cell, fxContext);
        }

        public abstract void OnActivated(BoardModel board, CellModel cell, BoardFxContext fxContext);
    }
}
