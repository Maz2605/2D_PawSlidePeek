using UnityEngine;

namespace _PawSlidePopGame._Scripts.Feature.Match3.Core.Enum
{
    public enum TileLogicType
    {
        None = 0,
        NormalAnimal = 1,
        [InspectorName("Area Bomb Small (3x3)")]
        BombBooster = 2,
        IceBlocker = 3,
        [InspectorName("Cross Bomb (Row + Column)")]
        CrossBomb = 4,
        [InspectorName("Square Bomb (Random 3x3)")]
        SquareBomb = 5,
        [InspectorName("Area Bomb Medium (Diamond)")]
        AreaBombMedium = 6,
        [InspectorName("Area Bomb Large (5x5)")]
        AreaBombLarge = 7,
        
        //Blocker
        BubbleBlocker = 8,
    }
}
