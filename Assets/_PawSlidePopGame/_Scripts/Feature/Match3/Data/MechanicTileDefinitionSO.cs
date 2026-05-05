using _PawSlidePopGame._Scripts.Feature.Match3.Core.Enum;
using _PawSlidePopGame._Scripts.Feature.Match3.View;
using UnityEngine;

namespace _PawSlidePopGame._Scripts.Feature.Match3.Data
{
    [CreateAssetMenu(fileName = "MechanicTile", menuName = "_PawSlidePopGame/Match3/Tiles/Mechanic Tile")]
    public class MechanicTileDefinitionSO : TileDefinitionSO
    {
        [Header("View")]
        [SerializeField] private MechanicTileView tileViewPrefab;
        [Tooltip("Placeholder until dedicated mechanic logic types are introduced.")]
        [SerializeField] private TileLogicType mechanicLogicType = TileLogicType.None;

        public override Match3TileView TileViewPrefab => tileViewPrefab;
        public override TileKind TileKind => TileKind.Mechanic;
        public override TileLogicType LogicType => mechanicLogicType;

        protected override void OnValidate()
        {
            base.OnValidate();
            MigrateLegacyTileViewPrefab(ref tileViewPrefab);
        }
    }
}
