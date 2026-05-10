using UnityEngine;

namespace _PawSlidePopGame._Scripts.Feature.Match3.Data
{
    [CreateAssetMenu(fileName = "Match3LevelDefinition", menuName = "_PawSlidePopGame/Match3/Level Definition")]
    public class Match3LevelDefinitionSO : ScriptableObject
    {
        [SerializeField] private Match3LevelData levelData = new Match3LevelData();

        public Match3LevelData LevelData => levelData;
    }
}

