using _PawSlidePopGame._Scripts.Feature.Match3.Core.Enum;
using UnityEngine;

namespace _PawSlidePopGame._Scripts.Feature.Match3.Data
{
    [CreateAssetMenu(fileName = "NormalAnimalTile", menuName = "_PawSlidePopGame/Match3/Tiles/Normal Animal Tile")]
    public class NormalAnimalTileDefinitionSO : TileDefinitionSO
    {
        [SerializeField] private AnimalTileId animalId = AnimalTileId.Cat;

        public AnimalTileId AnimalId => animalId;
        public override TileKind TileKind => TileKind.Normal;
        public override TileLogicType LogicType => TileLogicType.NormalAnimal;
    }
}
