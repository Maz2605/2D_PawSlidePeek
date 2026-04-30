namespace _PawSlidePopGame._Scripts.Data.LevelProvider
{
    public interface ILevelDataProvider
    {
        LevelSaveData GetLevelData(string levelId);
    }
}