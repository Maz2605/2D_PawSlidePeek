using System.Collections;
using UnityEngine;

namespace _PawSlidePopGame._Scripts.Feature.Match3.View
{
    public class SquareBombTileView : SpecialTileView
    {
        [Header("Square Bomb FX")]
        [SerializeField] private Color squareTargetColor = new Color(1f, 0.82f, 0.5f, 1f);
        [SerializeField] private float squareTargetPunchMultiplier = 1.9f;

        public override IEnumerator PlayTargetSelectionAsync()
        {
            yield return PlayPulseTintAsync(
                Mathf.Max(0.05f, TargetPulseDuration),
                DamagePunchScale * squareTargetPunchMultiplier,
                squareTargetColor,
                TileShadowState.Active,
                true);
        }
    }
}
