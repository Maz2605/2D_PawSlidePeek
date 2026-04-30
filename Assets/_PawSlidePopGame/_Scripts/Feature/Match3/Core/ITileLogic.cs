using _PawSlidePopGame._Scripts.Feature.Match3.Model.Board;
using _PawSlidePopGame._Scripts.Feature.Match3.Presentation;

namespace _PawSlidePopGame._Scripts.Feature.Match3.Core
{
    public interface ITileLogic
    {
        bool CanBeMoved();
        bool CanFall();
        bool CanMatch();

        void OnMatched(BoardModel board, CellModel cell, BoardFxContext fxContext);
        void OnExploded(BoardModel board, CellModel cell, BoardFxContext fxContext);
        void OnActivated(BoardModel board, CellModel cell, BoardFxContext fxContext);
    }
}
