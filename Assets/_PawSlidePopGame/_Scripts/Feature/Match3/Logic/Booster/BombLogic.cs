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
            BoosterExplosionUtility.ExplodeArea(board, cell, 1, fxContext, 25);
        }
    }
}
