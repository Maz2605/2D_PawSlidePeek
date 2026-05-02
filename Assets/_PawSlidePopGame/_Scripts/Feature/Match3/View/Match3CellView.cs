using _PawSlidePopGame.Scripts.DesignPattern.ObjectPooling;
using UnityEngine;

namespace _PawSlidePopGame._Scripts.Feature.Match3.View
{
    public class Match3CellView : MonoBehaviour, IPoolable
    {
        [SerializeField] private Transform tileAnchor;
        [SerializeField] private SpriteRenderer fallbackRenderer;

        private Quaternion _initialLocalRotation;
        private Vector3 _initialLocalScale;

        public Transform TileAnchor => tileAnchor != null ? tileAnchor : transform;
        public SpriteRenderer FallbackRenderer => fallbackRenderer;

        private void Awake()
        {
            EnsureRuntimeDefaults();
        }

        public void Initialize(_PawSlidePopGame._Scripts.Feature.Match3.Model.Board.CellModel cell)
        {
            if (tileAnchor == null)
            {
                tileAnchor = transform;
            }
        }

        public Vector3 GetTileAnchorLocalPosition(Transform relativeTo)
        {
            if (relativeTo == null)
            {
                return TileAnchor.position;
            }

            return relativeTo.InverseTransformPoint(TileAnchor.position);
        }

        public void OnSpawn()
        {
            EnsureRuntimeDefaults();
            ResetRuntimeState();
        }

        public void OnDespawn()
        {
            ResetRuntimeState();
        }

        private void EnsureRuntimeDefaults()
        {
            _initialLocalRotation = transform.localRotation;
            _initialLocalScale = transform.localScale;

            if (tileAnchor == null)
            {
                tileAnchor = transform;
            }
        }

        private void ResetRuntimeState()
        {
            transform.localPosition = Vector3.zero;
            transform.localRotation = _initialLocalRotation;
            transform.localScale = _initialLocalScale;
        }
    }
}
