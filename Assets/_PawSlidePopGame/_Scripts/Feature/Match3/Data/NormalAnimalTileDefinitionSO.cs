using _PawSlidePopGame._Scripts.Feature.Match3.Core.Enum;
using _PawSlidePopGame._Scripts.Feature.Match3.View;
using UnityEngine;

namespace _PawSlidePopGame._Scripts.Feature.Match3.Data
{
    [CreateAssetMenu(fileName = "NormalAnimalTile", menuName = "_PawSlidePopGame/Match3/Tiles/Normal Animal Tile")]
    public class NormalAnimalTileDefinitionSO : TileDefinitionSO
    {
        [Header("View")]
        [SerializeField] private NormalTileView tileViewPrefab;
        [SerializeField] private AnimalTileId animalId = AnimalTileId.Cat;

        public AnimalTileId AnimalId => animalId;
        public override Match3TileView TileViewPrefab => tileViewPrefab;
        public override TileKind TileKind => TileKind.Normal;
        public override TileLogicType LogicType => TileLogicType.NormalAnimal;

        protected override void OnValidate()
        {
            base.OnValidate();
            MigrateLegacyTileViewPrefab(ref tileViewPrefab);
        }
    }
}
