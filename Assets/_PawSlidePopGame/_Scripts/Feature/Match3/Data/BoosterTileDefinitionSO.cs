using _PawSlidePopGame._Scripts.Feature.Match3.Core.Enum;
using UnityEngine;

namespace _PawSlidePopGame._Scripts.Feature.Match3.Data
{
    [CreateAssetMenu(fileName = "BoosterTile", menuName = "_PawSlidePopGame/Match3/Tiles/Booster Tile")]
    public class BoosterTileDefinitionSO : TileDefinitionSO
    {
        [SerializeField] private TileLogicType boosterLogicType = TileLogicType.BombBooster;

        public override TileKind TileKind => TileKind.Booster;
        public override TileLogicType LogicType => boosterLogicType;

        protected override void OnValidate()
        {
            base.OnValidate();

            if (boosterLogicType == TileLogicType.None || boosterLogicType == TileLogicType.NormalAnimal || boosterLogicType == TileLogicType.IceBlocker)
            {
                boosterLogicType = TileLogicType.BombBooster;
            }
        }
    }
}
