using System.Collections.Generic;
using _PawSlidePopGame._Scripts.Feature.Match3.Model.Board;

namespace _PawSlidePopGame._Scripts.Feature.Match3.Logic.Match
{
    public interface IBoardMatchRule
    {
        HashSet<CellModel> FindMatchedCells(BoardModel board);
    }
}
