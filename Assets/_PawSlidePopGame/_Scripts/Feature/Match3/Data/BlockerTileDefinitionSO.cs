using _PawSlidePopGame._Scripts.Feature.Match3.Core.Enum;
using _PawSlidePopGame._Scripts.Feature.Match3.View;
using UnityEngine;

namespace _PawSlidePopGame._Scripts.Feature.Match3.Data
{
    [CreateAssetMenu(fileName = "BlockerTile", menuName = "_PawSlidePopGame/Match3/Tiles/Blocker Tile")]
    public class BlockerTileDefinitionSO : TileDefinitionSO
    {
        [Header("View")]
        [SerializeField] private BlockerTileView tileViewPrefab;
        [SerializeField] private TileLogicType blockerLogicType = TileLogicType.IceBlocker;
        [Min(1)]
        [SerializeField] private int defaultHP = 1;

        public override Match3TileView TileViewPrefab => tileViewPrefab;
        public override TileKind TileKind => TileKind.Blocker;
        public override TileLogicType LogicType => blockerLogicType;
        public override int DefaultHP => defaultHP;

        protected override void OnValidate()
        {
            base.OnValidate();
            MigrateLegacyTileViewPrefab(ref tileViewPrefab);

            if (defaultHP < 1)
            {
                defaultHP = 1;
            }

            if (blockerLogicType == TileLogicType.None || blockerLogicType == TileLogicType.NormalAnimal || blockerLogicType == TileLogicType.BombBooster)
            {
                blockerLogicType = TileLogicType.IceBlocker;
            }
        }
    }
}
