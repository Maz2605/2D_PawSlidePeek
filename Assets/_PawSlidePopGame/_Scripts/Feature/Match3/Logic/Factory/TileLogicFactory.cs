using _PawSlidePopGame._Scripts.Feature.Match3.Core;
using _PawSlidePopGame._Scripts.Feature.Match3.Core.Enum;
using _PawSlidePopGame._Scripts.Feature.Match3.Data;
using _PawSlidePopGame._Scripts.Feature.Match3.Logic.Blockers;
using _PawSlidePopGame._Scripts.Feature.Match3.Logic.Booster;
using _PawSlidePopGame._Scripts.Feature.Match3.Logic.Tiles;

namespace _PawSlidePopGame._Scripts.Feature.Match3.Logic.Factory
{
    public static class TileLogicFactory
    {
        public static ITileLogic CreateLogic(TileDefinitionSO definition)
        {
            if (definition == null)
            {
                return null;
            }

            switch (definition.LogicType)
            {
                case TileLogicType.BombBooster:
                    return new BombLogic();
                case TileLogicType.IceBlocker:
                    return new IceLogic();
                case TileLogicType.NormalAnimal:
                case TileLogicType.None:
                default:
                    return new NormalAnimalTileLogic();
            }
        }
    }
}
