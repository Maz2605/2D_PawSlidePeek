using UnityEngine;

namespace _PawSlidePopGame._Scripts.Gameplay.Meta.MapManager
{
    public readonly struct MapLevelEntry
    {
        public readonly string LevelId;
        public readonly int DisplayLevelNumber;
        public readonly int SectionIndex;
        public readonly Vector2 AnchoredPosition;
        public readonly MapLevelState State;
        public readonly int BestStars;
        public readonly int BestScore;
        public readonly bool IsHardLevel;

        public MapLevelEntry(
            string levelId,
            int displayLevelNumber,
            int sectionIndex,
            Vector2 anchoredPosition,
            MapLevelState state,
            int bestStars = 0,
            int bestScore = 0,
            bool isHardLevel = false)
        {
            LevelId = levelId;
            DisplayLevelNumber = displayLevelNumber;
            SectionIndex = sectionIndex;
            AnchoredPosition = anchoredPosition;
            State = state;
            BestStars = Mathf.Max(0, bestStars);
            BestScore = Mathf.Max(0, bestScore);
            IsHardLevel = isHardLevel;
        }
    }
}
