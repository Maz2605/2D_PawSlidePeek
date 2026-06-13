using System.Collections;
using UnityEngine;

namespace _PawSlidePopGame._Scripts.Feature.Match3.View
{
    public class AreaBombTileView : SpecialTileView
    {
        [Header("Area Bomb FX")]
        [SerializeField] private Color areaActivateColor = new Color(1f, 0.86f, 0.62f, 1f);
        [SerializeField] private Color areaCreateColor = new Color(1f, 0.95f, 0.78f, 1f);
        [SerializeField] private float areaActivatePunchMultiplier = 1.7f;
        [SerializeField] private float areaCreatePunchMultiplier = 1.75f;

        public override IEnumerator PlayActivateAsync()
        {
            yield return PlayPulseTintAsync(
                Mathf.Max(0.05f, DamageDuration),
                DamagePunchScale * areaActivatePunchMultiplier,
                areaActivateColor,
                TileShadowState.Active,
                false);
        }

        public override IEnumerator PlaySpecialCreateAsync(float speedMultiplier = 1f)
        {
            yield return PlayPulseTintAsync(
                Mathf.Max(0.02f, SpecialCreateDuration / speedMultiplier),
                DamagePunchScale * areaCreatePunchMultiplier,
                areaCreateColor,
                TileShadowState.Active,
                false);
        }
    }
}

