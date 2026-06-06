using UnityEngine;

namespace _PawSlidePopGame._Scripts.Gameplay.Meta.MapManager
{
    [CreateAssetMenu(fileName = "MapSectionConfig", menuName = "_PawSlidePopGame/Map/Section Config")]
    public sealed class MapSectionConfigSO : ScriptableObject
    {
        [SerializeField] private Sprite backgroundSprite;
        [SerializeField, Min(1)] private int weight = 1;

        public Sprite BackgroundSprite => backgroundSprite;
        public int Weight => Mathf.Max(1, weight);
    }
}
