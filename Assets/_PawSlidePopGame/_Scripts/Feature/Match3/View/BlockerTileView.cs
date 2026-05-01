using System.Collections;
using UnityEngine;

namespace _PawSlidePopGame._Scripts.Feature.Match3.View
{
    public class BlockerTileView : Match3TileView
    {
        [Header("Blocker FX")]
        [SerializeField] private Color blockerDamageColor = new Color(0.82f, 0.92f, 1f, 1f);
        [SerializeField] private float blockerDamagePunchMultiplier = 1.15f;

        public override IEnumerator PlayDamageAsync(int currentHp, int previousHp)
        {
            yield return PlayPulseTintAsync(
                Mathf.Max(0.05f, DamageDuration),
                DamagePunchScale * blockerDamagePunchMultiplier,
                blockerDamageColor,
                TileShadowState.Active,
                true);
        }
    }
}
