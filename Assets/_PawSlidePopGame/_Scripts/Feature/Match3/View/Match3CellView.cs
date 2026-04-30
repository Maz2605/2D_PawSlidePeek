using UnityEngine;

namespace _PawSlidePopGame._Scripts.Feature.Match3.View
{
    public class Match3CellView : MonoBehaviour
    {
        [SerializeField] private Transform tileAnchor;
        [SerializeField] private SpriteRenderer fallbackRenderer;

        public Transform TileAnchor => tileAnchor != null ? tileAnchor : transform;
        public SpriteRenderer FallbackRenderer => fallbackRenderer;

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
    }
}
