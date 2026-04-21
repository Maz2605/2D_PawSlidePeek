namespace _PawSlidePopGame.Scripts.DesignPattern.ObjectPooling
{
    public interface IPoolable
    {
        void OnSpawn();
        void OnDespawn();
    }
}