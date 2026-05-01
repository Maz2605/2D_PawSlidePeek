using System.Collections;
using UnityEngine;

namespace _PawSlidePopGame._Scripts.Feature.Match3.View
{
    public class SpecialTileView : Match3TileView
    {
        [Header("Special FX")]
        [SerializeField] private Color specialActivateColor = new Color(1f, 0.88f, 0.55f, 1f);
        [SerializeField] private Color specialTargetColor = new Color(1f, 0.94f, 0.72f, 1f);
        [SerializeField] private Color specialCreateColor = new Color(1f, 0.98f, 0.84f, 1f);
        [SerializeField] private float specialActivatePunchMultiplier = 1.45f;
        [SerializeField] private float specialTargetPunchMultiplier = 1.65f;
        [SerializeField] private float specialCreatePunchMultiplier = 1.55f;

        public override IEnumerator PlayActivateAsync()
        {
            yield return PlayPulseTintAsync(
                Mathf.Max(0.05f, DamageDuration),
                DamagePunchScale * specialActivatePunchMultiplier,
                specialActivateColor,
                TileShadowState.Active,
                false);
        }

        public override IEnumerator PlayTargetSelectionAsync()
        {
            yield return PlayPulseTintAsync(
                Mathf.Max(0.05f, TargetPulseDuration),
                DamagePunchScale * specialTargetPunchMultiplier,
                specialTargetColor,
                TileShadowState.Active,
                true);
        }

        public override IEnumerator PlaySpecialCreateAsync()
        {
            yield return PlayPulseTintAsync(
                Mathf.Max(0.05f, SpecialCreateDuration),
                DamagePunchScale * specialCreatePunchMultiplier,
                specialCreateColor,
                TileShadowState.Active,
                false);
        }
    }
}
