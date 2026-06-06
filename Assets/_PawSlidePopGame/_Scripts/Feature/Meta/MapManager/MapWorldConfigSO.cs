using System.Collections.Generic;
using UnityEngine;

namespace _PawSlidePopGame._Scripts.Gameplay.Meta.MapManager
{
    [CreateAssetMenu(fileName = "MapWorldConfig", menuName = "_PawSlidePopGame/Map/World Config")]
    public sealed class MapWorldConfigSO : ScriptableObject
    {
        [Header("Generation")]
        [SerializeField] private int seed = 12817;
        [SerializeField, Min(1)] private int maxVisibleLevels = 100;
        [SerializeField, Min(1)] private int levelsPerSection = 8;
        [SerializeField] private bool markHardLevels;
        [SerializeField, Min(1)] private int hardLevelInterval = 10;

        [Header("Layout")]
        [SerializeField, Min(200f)] private float sectionHeight = 1100f;
        [SerializeField, Min(80f)] private float levelSpacing = 170f;
        [SerializeField, Min(0f)] private float horizontalLimit = 260f;
        [SerializeField, Min(0f)] private float horizontalJitter = 70f;
        [SerializeField, Min(0f)] private float bottomPadding = 160f;
        [SerializeField, Min(0f)] private float topPadding = 220f;

        [Header("Assets")]
        [SerializeField] private List<MapSectionConfigSO> sections = new List<MapSectionConfigSO>();

        public int Seed => seed;
        public int MaxVisibleLevels => Mathf.Max(1, maxVisibleLevels);
        public int LevelsPerSection => Mathf.Max(1, levelsPerSection);
        public float SectionHeight => Mathf.Max(200f, sectionHeight);
        public float LevelSpacing => Mathf.Max(80f, levelSpacing);
        public float HorizontalLimit => Mathf.Max(0f, horizontalLimit);
        public float HorizontalJitter => Mathf.Max(0f, horizontalJitter);
        public float BottomPadding => Mathf.Max(0f, bottomPadding);
        public float TopPadding => Mathf.Max(0f, topPadding);
        public IReadOnlyList<MapSectionConfigSO> Sections => sections;

        public bool IsHardLevel(int displayLevelNumber)
        {
            return markHardLevels &&
                   hardLevelInterval > 0 &&
                   displayLevelNumber > 0 &&
                   displayLevelNumber % hardLevelInterval == 0;
        }

        public MapSectionConfigSO GetSection(int sectionIndex, System.Random random)
        {
            if (sections == null || sections.Count == 0)
            {
                return null;
            }

            int totalWeight = 0;
            for (int i = 0; i < sections.Count; i++)
            {
                if (sections[i] != null)
                {
                    totalWeight += sections[i].Weight;
                }
            }

            if (totalWeight <= 0)
            {
                return sections[Mathf.Abs(sectionIndex) % sections.Count];
            }

            int roll = random.Next(0, totalWeight);
            for (int i = 0; i < sections.Count; i++)
            {
                MapSectionConfigSO section = sections[i];
                if (section == null)
                {
                    continue;
                }

                roll -= section.Weight;
                if (roll < 0)
                {
                    return section;
                }
            }

            return null;
        }
    }
}
