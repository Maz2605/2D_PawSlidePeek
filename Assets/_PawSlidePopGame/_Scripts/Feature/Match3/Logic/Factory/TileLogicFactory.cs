using _PawSlidePopGame._Scripts.Feature.Match3.Core;
using _PawSlidePopGame._Scripts.Feature.Match3.Core.Enum;
using _PawSlidePopGame._Scripts.Feature.Match3.Data;
using _PawSlidePopGame._Scripts.Feature.Match3.Logic.Blockers;
using _PawSlidePopGame._Scripts.Feature.Match3.Logic.Booster;
using _PawSlidePopGame._Scripts.Feature.Match3.Logic.Mechanics;
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

            if (definition.TileKind == TileKind.Mechanic)
            {
                return new MechanicPlaceholderLogic();
            }

            switch (definition.LogicType)
            {
                case TileLogicType.BombBooster:
                    return new BombLogic();
                case TileLogicType.CrossBomb:
                    return new CrossBombLogic();
                case TileLogicType.SquareBomb:
                    return new SquareBombLogic();
                case TileLogicType.AreaBombMedium:
                    return new AreaBombMediumLogic();
                case TileLogicType.AreaBombLarge:
                    return new AreaBombLargeLogic();
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
