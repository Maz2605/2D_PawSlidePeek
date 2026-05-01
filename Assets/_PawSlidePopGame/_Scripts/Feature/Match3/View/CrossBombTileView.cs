using System.Collections;
using UnityEngine;

namespace _PawSlidePopGame._Scripts.Feature.Match3.View
{
    public class CrossBombTileView : SpecialTileView
    {
        [Header("Cross Bomb FX")]
        [SerializeField] private Color crossActivateColor = new Color(1f, 0.9f, 0.45f, 1f);
        [SerializeField] private float crossActivatePunchMultiplier = 1.85f;

        public override IEnumerator PlayActivateAsync()
        {
            yield return PlayPulseTintAsync(
                Mathf.Max(0.05f, DamageDuration),
                DamagePunchScale * crossActivatePunchMultiplier,
                crossActivateColor,
                TileShadowState.Active,
                false);
        }
    }
}
