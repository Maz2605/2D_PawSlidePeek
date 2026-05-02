using DG.Tweening;
using TMPro;
using UnityEngine;

namespace _PawSlidePopGame._Scripts.UI.Components.HUD
{
    public class MovesCounterView : MonoBehaviour
    {
        [SerializeField] private TMP_Text movesAmountText;

        private void OnValidate()
        {
            if (movesAmountText == null)
            {
                TMP_Text[] labels = GetComponentsInChildren<TMP_Text>(true);
                for (int i = 0; i < labels.Length; i++)
                {
                    if (labels[i] != null && labels[i].name.Contains("Amount"))
                    {
                        movesAmountText = labels[i];
                        break;
                    }
                }
            }
        }

        public void SetValue(int moves)
        {
            if (movesAmountText == null)
            {
                return;
            }

            movesAmountText.text = moves.ToString();
        }

        public void PlayValueChangedFx()
        {
            transform.DOKill();
            transform.localScale = Vector3.one;
            transform.DOPunchScale(Vector3.one * 0.08f, 0.2f, 4)
                .SetLink(gameObject, LinkBehaviour.KillOnDisable);
        }
    }
}
