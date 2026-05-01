using _PawSlidePopGame._Scripts.Feature.Match3.Logic.Base;
using _PawSlidePopGame._Scripts.Feature.Match3.Model.Board;
using _PawSlidePopGame._Scripts.Feature.Match3.Presentation;

namespace _PawSlidePopGame._Scripts.Feature.Match3.Logic.Booster
{
    public class CrossBombLogic : BaseBoosterLogic
    {
        public override void OnActivated(BoardModel board, CellModel cell, BoardFxContext fxContext)
        {
            BoosterExplosionUtility.ExplodeCross(board, cell, fxContext, 30);
        }
    }
}
