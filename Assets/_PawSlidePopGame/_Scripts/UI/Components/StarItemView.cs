using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace _PawSlidePopGame._Scripts.UI.Components
{
    public enum StarVisualState
    {
        Locked,         // Chưa tới: Màu xám/rỗng, KHÔNG shadow
        ReachedNormal,  // Đạt mốc: Màu vàng, CÓ shadow
        ReachedMax      // Đạt mốc cuối: Màu xanh, CÓ shadow
    }

    public class StarItemView : MonoBehaviour
    {
        [Header("--- UI References ---")]
        [SerializeField] private Image starImage;
        [SerializeField] private Image shadowImage; 
        
        [Header("--- Sprites ---")]
        [SerializeField] private Sprite lockedSprite; // Hình lúc chưa đạt
        [SerializeField] private Sprite yellowSprite; // Hình sao vàng
        [SerializeField] private Sprite greenSprite;  // Hình sao xanh (mốc cuối)

        [Header("--- VFX Hooks ---")]
        [SerializeField] private ParticleSystem unlockParticlePrefab;

        [Header("--- Animation Settings ---")]
        [SerializeField] private float animDuration = 0.5f;

        private ParticleSystem _cachedParticleInstance;
        private StarVisualState _currentState = StarVisualState.Locked;

        private void Awake()
        {
            ResolveReferences();
        }

        private void OnValidate()
        {
            ResolveReferences();
        }

        public void SetState(StarVisualState state, bool instant = true)
        {
            if (!ResolveReferences())
            {
                return;
            }

            _currentState = state;
            starImage.DOKill();
            
            // Xử lý logic đổi hình và bật tắt bóng đổ
            ApplyVisualState(state);

            if (instant)
            {
                starImage.transform.localScale = state == StarVisualState.Locked ? Vector3.one : Vector3.one; 
                // Có thể em muốn sao xám mặc định scale = 1, tùy thiết kế. Ở đây anh để mặc định hiện hình xám.
            }
        }

        public void PlayUnlockFx(StarVisualState newState, float delay = 0f)
        {
            if (!ResolveReferences())
            {
                return;
            }

            _currentState = newState;
            starImage.transform.DOKill();

            Sequence seq = DOTween.Sequence();
            
            // Nếu có delay (chờ slider chạy tới), thì nhét vào sequence
            if (delay > 0)
            {
                seq.AppendInterval(delay);
            }

            // Ngay trước khi bung anim, đổi hình sang Vàng hoặc Xanh
            seq.AppendCallback(() => ApplyVisualState(newState));

            // Hiệu ứng Pop & Spin
            seq.Append(starImage.transform.DOScale(1.3f, animDuration * 0.6f).SetEase(Ease.OutBack))
               .Join(starImage.transform.DORotate(new Vector3(0, 0, -360f), animDuration * 0.6f, RotateMode.FastBeyond360).SetEase(Ease.InOutElastic))
               .Append(starImage.transform.DOScale(1f, animDuration * 0.4f).SetEase(Ease.InOutQuad))
               .SetLink(starImage.gameObject, LinkBehaviour.KillOnDisable);

            // Play hạt sau khi delay
            DOVirtual.DelayedCall(delay, PlayParticles).SetLink(gameObject, LinkBehaviour.KillOnDisable);
        }

        private void ApplyVisualState(StarVisualState state)
        {
            if (!ResolveReferences())
            {
                return;
            }

            switch (state)
            {
                case StarVisualState.Locked:
                    starImage.sprite = lockedSprite;
                    if (shadowImage != null) shadowImage.enabled = false;
                    break;
                case StarVisualState.ReachedNormal:
                    starImage.sprite = yellowSprite;
                    if (shadowImage != null) shadowImage.enabled = true;
                    break;
                case StarVisualState.ReachedMax:
                    starImage.sprite = greenSprite;
                    if (shadowImage != null) shadowImage.enabled = true;
                    break;
            }
        }

        private bool ResolveReferences()
        {
            if (starImage != null)
            {
                return true;
            }

            Image[] images = GetComponentsInChildren<Image>(true);
            for (int i = 0; i < images.Length; i++)
            {
                Image image = images[i];
                if (image == null)
                {
                    continue;
                }

                if (starImage == null && image.name == "ImgStar")
                {
                    starImage = image;
                }
                else if (shadowImage == null && image.name == "ImgShadow")
                {
                    shadowImage = image;
                }
            }

            if (starImage == null)
            {
                for (int i = 0; i < images.Length; i++)
                {
                    if (images[i] != null)
                    {
                        starImage = images[i];
                        break;
                    }
                }
            }

            if (starImage == null)
            {
                Debug.LogWarning($"[StarItemView] Missing starImage reference on '{name}'.", this);
                return false;
            }

            return true;
        }

        private void PlayParticles()
        {
            if (unlockParticlePrefab == null) return;
            if (_cachedParticleInstance == null)
            {
                _cachedParticleInstance = Instantiate(unlockParticlePrefab, transform);
                _cachedParticleInstance.transform.localPosition = Vector3.zero;
            }
            _cachedParticleInstance.Play();
        }
    }
}
