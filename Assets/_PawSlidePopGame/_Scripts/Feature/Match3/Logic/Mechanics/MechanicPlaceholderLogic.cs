using _PawSlidePopGame._Scripts.Feature.Match3.Core;
using _PawSlidePopGame._Scripts.Feature.Match3.Model.Board;
using _PawSlidePopGame._Scripts.Feature.Match3.Presentation;

namespace _PawSlidePopGame._Scripts.Feature.Match3.Logic.Mechanics
{
    public class MechanicPlaceholderLogic : ITileLogic
    {
        public bool CanBeMoved() => false;
        public bool CanFall() => false;
        public bool CanMatch() => false;

        public void OnMatched(BoardModel board, CellModel cell, BoardFxContext fxContext)
        {
        }

        public void OnExploded(BoardModel board, CellModel cell, BoardFxContext fxContext)
        {
        }

        public void OnActivated(BoardModel board, CellModel cell, BoardFxContext fxContext)
        {
        }
    }
}
