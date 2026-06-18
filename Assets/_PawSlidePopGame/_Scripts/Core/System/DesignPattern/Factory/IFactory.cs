using UnityEngine;

namespace _PawSlidePopGame._Scripts.Core.DesignPattern.Factory
{
    public interface IFactory<TData, TVisual> where TVisual : MonoBehaviour
    {
        TVisual CreateVisual(TData data, Transform parent);
        void ReturnVisual(TVisual visual);
    }
}