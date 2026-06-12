using System.Collections;
using DG.Tweening;
using _PawSlidePopGame._Scripts.Feature.Match3.Data;
using _PawSlidePopGame._Scripts.Feature.Match3.Model.Entities;
using _PawSlidePopGame.Scripts.DesignPattern.ObjectPooling;
using UnityEngine;

namespace _PawSlidePopGame._Scripts.Feature.Match3.View
{
    public class Match3TileView : MonoBehaviour, IPoolable
    {
        [Header("Renderers")]
        [SerializeField] private SpriteRenderer bodyRenderer;
        [SerializeField] private SpriteRenderer shadowRenderer;

        [Header("Sprites")]
        [SerializeField] private bool useDefinitionIconAsFallback = true;

        [Header("Shadow")]
        [SerializeField] private Color previewShadowColor = new Color(1f, 1f, 1f, 0.6f);
        [SerializeField] private Color activeShadowColor = new Color(1f, 1f, 1f, 0.9f);

        [Header("DOTween Idle")]
        [SerializeField] private bool enableIdlePulse;
        [SerializeField] private float pulseScale = 1.03f;
        [SerializeField] private float pulseDuration = 1.2f;
        [SerializeField] private float pulseLoopDelay = 1.8f;

        [Header("FX")]
        [SerializeField] private float clearDuration = 0.2f;
        [SerializeField] private float damageDuration = 0.18f;
        [SerializeField] private float landingDuration = 0.16f;
        [SerializeField] private float targetPulseDuration = 0.18f;
        [SerializeField] private float specialCreateDuration = 0.22f;
        [SerializeField] private float clearScale = 0.75f;
        [SerializeField] private float damagePunchScale = 0.1f;
        [SerializeField] private float landingPunchScale = 0.08f;

        [Header("Movement FX")]
        [SerializeField] private Ease slideEase = Ease.OutCubic;
        [SerializeField] private float slideOvershoot = 1.2f;
        [SerializeField] private float slideStretchAmount = 0.08f;
        [SerializeField] private float slideBounceDuration = 0.15f;

        private TileModel _tile;
        private BoardContentDefinitionSO _definition;
        private bool _isIdleEnabled;
        private Coroutine _blinkRoutine;
        private Tween _pulseTween;
        private Vector3 _initialScale;
        private Vector3 _initialShadowScale = Vector3.one;
        private Quaternion _initialLocalRotation;
        private Color _bodyBaseColor = Color.white;
        private Color _shadowBaseColor = Color.white;
        private TileShadowState _shadowState;
        private bool _isScaledUp;

        public int TileInstanceId => _tile != null ? _tile.InstanceId : 0;
        public TileModel Tile => _tile;
        protected BoardContentDefinitionSO Definition => _definition;
        protected SpriteRenderer BodyRenderer => bodyRenderer;
        protected SpriteRenderer ShadowRenderer => shadowRenderer;
        protected Vector3 InitialScale => _initialScale;
        protected Color BodyBaseColor => _bodyBaseColor;
        protected Color ShadowBaseColor => _shadowBaseColor;
        protected float DamageDuration => damageDuration;
        protected float TargetPulseDuration => targetPulseDuration;
        protected float SpecialCreateDuration => specialCreateDuration;
        protected float ClearDuration => clearDuration;
        protected float DamagePunchScale => damagePunchScale;
        protected float LandingDuration => landingDuration;
        protected float LandingPunchScale => landingPunchScale;
        protected float ClearScale => clearScale;
        protected TileShadowState ShadowState => _shadowState;
        protected virtual bool EnableIdleBlink => false;
        protected virtual Vector2 BlinkIntervalRange => new Vector2(2.5f, 5f);
        protected virtual float ClosedEyesDuration => 0.08f;
        protected virtual Sprite OpenSprite => null;
        protected virtual Sprite ClosedSprite => null;

        protected virtual void Awake()
        {
            CacheInitialTransformState();
            _initialScale = transform.localScale;
            if (bodyRenderer != null)
            {
                _bodyBaseColor = bodyRenderer.color;
            }

            if (shadowRenderer != null)
            {
                _shadowBaseColor = shadowRenderer.color;
                _initialShadowScale = shadowRenderer.transform.localScale;
            }
        }

        protected virtual void OnEnable()
        {
            if (_isIdleEnabled)
            {
                StartIdleLoop();
            }
        }

        protected virtual void OnDisable()
        {
            StopIdle();
            ResetVisualState();
        }

        protected virtual void OnDestroy()
        {
            StopIdle();
            KillAllTweens();
        }

        public void OnSpawn()
        {
            CacheInitialTransformState();
            ResetVisualState();
        }

        public void OnDespawn()
        {
            StopIdle();
            KillAllTweens();
            ResetVisualState();
            _tile = null;
            _definition = null;
        }

        public virtual void Bind(TileModel tile, BoardContentDefinitionSO definition)
        {
            _tile = tile;
            _definition = definition;

            RestoreBodyColor();
            RestoreShadowColor();
            ApplyOpenSprite();
            SetShadowState(TileShadowState.Off);
        }

        public virtual IEnumerator PlayActivateAsync()
        {
            yield return PlayPulseTintAsync(
                Mathf.Max(0.05f, damageDuration),
                damagePunchScale * 1.25f,
                new Color(1f, 0.95f, 0.7f, 1f),
                TileShadowState.Active,
                false);
        }

        public virtual IEnumerator PlayTargetSelectionAsync()
        {
            yield return PlayPulseTintAsync(
                Mathf.Max(0.05f, targetPulseDuration),
                damagePunchScale * 1.5f,
                new Color(1f, 0.9f, 0.65f, 1f),
                TileShadowState.Active,
                true);
        }

        public virtual IEnumerator PlaySpecialCreateAsync()
        {
            yield return PlayPulseTintAsync(
                Mathf.Max(0.05f, specialCreateDuration),
                damagePunchScale * 1.35f,
                new Color(1f, 0.98f, 0.8f, 1f),
                TileShadowState.Active,
                false);
        }

        public void SnapToLocalPosition(Vector3 localPosition)
        {
            KillMotionTweens();
            transform.localPosition = localPosition;
        }

        public virtual void SetIdleEnabled(bool isEnabled)
        {
            _isIdleEnabled = isEnabled;

            if (!_isIdleEnabled)
            {
                StopIdle();
                ApplyOpenSprite();
                return;
            }

            StartIdleLoop();
        }

        public virtual void SetShadowState(TileShadowState state)
        {
            _shadowState = state;
            if (shadowRenderer == null)
            {
                return;
            }

            SyncShadowSpriteIfNeeded();
            if (shadowRenderer.sprite == null)
            {
                return;
            }

            switch (state)
            {
                case TileShadowState.Preview:
                    shadowRenderer.enabled = true;
                    shadowRenderer.color = previewShadowColor;
                    break;
                case TileShadowState.Active:
                    shadowRenderer.enabled = true;
                    shadowRenderer.color = activeShadowColor;
                    break;
                default:
                    shadowRenderer.enabled = false;
                    shadowRenderer.color = _shadowBaseColor;
                    break;
            }
        }

        public virtual void SetSelectedScale(bool isSelected)
        {
            if (isSelected)
            {
                if (!_isScaledUp)
                {
                    _isScaledUp = true;
                    // Stop idle animations without clearing _isIdleEnabled flag
                    StopBlinkLoop();
                    KillPulseTween();

                    // Scale up tile slightly (1.12x of _initialScale)
                    transform.DOScale(_initialScale * 1.12f, 0.15f)
                        .SetEase(Ease.OutBack)
                        .SetLink(gameObject);

                    // Scale up shadow to the same level/factor as the tile (1.12x of its initial scale)
                    if (shadowRenderer != null)
                    {
                        shadowRenderer.transform.DOScale(_initialShadowScale * 1.12f, 0.15f)
                            .SetEase(Ease.OutBack)
                            .SetLink(shadowRenderer.gameObject);
                    }
                }
            }
            else
            {
                if (_isScaledUp)
                {
                    _isScaledUp = false;
                    // Scale tile back to normal
                    transform.DOScale(_initialScale, 0.15f)
                        .SetEase(Ease.OutQuad)
                        .SetLink(gameObject)
                        .OnComplete(() =>
                        {
                            // Resume idle if still enabled on this tile
                            if (_isIdleEnabled && !_isScaledUp)
                            {
                                StartIdleLoop();
                            }
                        });

                    // Scale shadow back to normal
                    if (shadowRenderer != null)
                    {
                        shadowRenderer.transform.DOScale(_initialShadowScale, 0.15f)
                            .SetEase(Ease.OutQuad)
                            .SetLink(shadowRenderer.gameObject);
                    }
                }
            }
        }

        public void ApplyDimmedState(bool isDimmed)
        {
            if (bodyRenderer == null)
            {
                return;
            }

            DOTween.Kill(bodyRenderer, false);
            if (isDimmed)
            {
                Color dimmedColor = new Color(_bodyBaseColor.r * 0.45f, _bodyBaseColor.g * 0.45f, _bodyBaseColor.b * 0.45f, _bodyBaseColor.a);
                bodyRenderer.DOColor(dimmedColor, 0.15f)
                    .SetEase(Ease.OutQuad)
                    .SetLink(gameObject);
            }
            else
            {
                bodyRenderer.DOColor(_bodyBaseColor, 0.15f)
                    .SetEase(Ease.OutQuad)
                    .SetLink(gameObject);
            }
        }

        public virtual IEnumerator PlayClearAsync()
        {
            StopIdle();
            SetShadowState(TileShadowState.Off);
            KillMotionTweens();

            Sequence sequence = DOTween.Sequence().SetLink(gameObject);
            sequence.Join(transform.DOScale(_initialScale * clearScale, Mathf.Max(0.05f, clearDuration)).SetEase(Ease.InBack));

            if (bodyRenderer != null)
            {
                sequence.Join(bodyRenderer.DOFade(0f, Mathf.Max(0.05f, clearDuration)));
            }

            if (shadowRenderer != null)
            {
                sequence.Join(shadowRenderer.DOFade(0f, Mathf.Max(0.05f, clearDuration * 0.8f)));
            }

            yield return sequence.WaitForCompletion();
        }

        public virtual IEnumerator PlayDamageAsync(int currentHp, int previousHp)
        {
            Color flashColor = previousHp > currentHp
                ? new Color(1f, 0.75f, 0.75f, 1f)
                : new Color(1f, 1f, 1f, 1f);

            yield return PlayPulseTintAsync(
                Mathf.Max(0.05f, damageDuration),
                damagePunchScale,
                flashColor,
                TileShadowState.Active,
                true);
        }

        public virtual IEnumerator PlayMoveAsync(Vector3 targetLocalPosition, float duration)
        {
            StopIdle();
            SetShadowState(TileShadowState.Active);
            KillMotionTweens();

            float safeDuration = Mathf.Max(0.05f, duration);

            // Lướt mượt với easing snappy - không scale để tránh tile tràn ra ngoài board
            yield return transform
                .DOLocalMove(targetLocalPosition, safeDuration)
                .SetEase(Ease.OutCubic)
                .SetLink(gameObject)
                .WaitForCompletion();
        }

        /// <summary>
        /// Ẩn tile ngay lập tức khi bắt đầu wrap-around (không tween ra ngoài board).
        /// </summary>
        public void HideForWrapExit()
        {
            StopIdle();
            KillMotionTweens();
            SetShadowState(TileShadowState.Off);

            if (bodyRenderer != null)
            {
                bodyRenderer.enabled = false;
            }
        }

        /// <summary>
        /// Hiện tile ngay lập tức sau khi đã snap về vị trí đúng ở biên đối diện.
        /// </summary>
        public void ShowAfterWrapEntry()
        {
            RestoreBodyColor();
            if (bodyRenderer != null)
            {
                bodyRenderer.enabled = true;
            }
        }


        public virtual IEnumerator PlaySpawnFallAsync(Vector3 startLocalPosition, Vector3 targetLocalPosition, float duration)
        {
            StopIdle();
            KillMotionTweens();
            transform.localPosition = startLocalPosition;
            transform.localScale = _initialScale * 0.94f;
            SetShadowState(TileShadowState.Active);

            if (bodyRenderer != null)
            {
                bodyRenderer.color = new Color(_bodyBaseColor.r, _bodyBaseColor.g, _bodyBaseColor.b, 0f);
            }

            Sequence sequence = DOTween.Sequence().SetLink(gameObject);
            sequence.Join(transform.DOLocalMove(targetLocalPosition, Mathf.Max(0.05f, duration)).SetEase(Ease.OutQuad));
            sequence.Join(transform.DOScale(_initialScale, Mathf.Max(0.05f, duration)).SetEase(Ease.OutBack));

            if (bodyRenderer != null)
            {
                sequence.Join(bodyRenderer.DOFade(1f, Mathf.Max(0.05f, duration * 0.75f)));
            }

            yield return sequence.WaitForCompletion();
        }

        public virtual IEnumerator PlayLandAsync(float intensity = 1f)
        {
            StopIdle();
            float duration = Mathf.Max(0.05f, landingDuration);
            float clampedIntensity = Mathf.Max(0.25f, intensity);
            Vector3 punch = new Vector3(landingPunchScale * clampedIntensity, -landingPunchScale * 0.6f * clampedIntensity, 0f);

            Tween tween = transform
                .DOPunchScale(punch, duration, 4, 0.75f)
                .SetLink(gameObject);

            yield return tween.WaitForCompletion();
            transform.localScale = _initialScale;
            SetShadowState(_shadowState == TileShadowState.Preview ? TileShadowState.Preview : TileShadowState.Off);
        }

        protected IEnumerator PlayPulseTintAsync(
            float duration,
            float punchAmount,
            Color flashColor,
            TileShadowState shadowState = TileShadowState.Active,
            bool resetShadowOff = false,
            int vibrato = 4,
            float elasticity = 0.75f)
        {
            StopIdle();
            SetShadowState(shadowState);
            KillMotionTweens();

            Sequence sequence = DOTween.Sequence().SetLink(gameObject);
            sequence.Join(transform.DOPunchScale(Vector3.one * punchAmount, duration, vibrato, elasticity));

            if (bodyRenderer != null)
            {
                sequence.Join(bodyRenderer.DOColor(flashColor, duration * 0.5f).SetLoops(2, LoopType.Yoyo));
            }

            yield return sequence.WaitForCompletion();
            RestoreBodyColor();

            if (resetShadowOff)
            {
                SetShadowState(TileShadowState.Off);
            }
        }

        protected void ApplyBodyColor(Color color)
        {
            if (bodyRenderer != null)
            {
                bodyRenderer.color = color;
            }
        }

        protected void RestoreBodyColor()
        {
            if (bodyRenderer != null)
            {
                bodyRenderer.color = _bodyBaseColor;
            }
        }

        protected void RestoreShadowColor()
        {
            if (shadowRenderer != null)
            {
                shadowRenderer.color = _shadowBaseColor;
            }
        }

        protected void StopIdleForFx()
        {
            StopIdle();
        }

        protected void KillMotionTweensForFx()
        {
            KillMotionTweens();
        }

        protected void ApplyOpenSprite()
        {
            if (bodyRenderer == null)
            {
                return;
            }

            Sprite spriteToUse = OpenSprite;
            if (spriteToUse == null && useDefinitionIconAsFallback && _definition != null)
            {
                spriteToUse = _definition.Icon;
            }

            bodyRenderer.sprite = spriteToUse;
            SyncShadowSpriteIfNeeded();
        }

        public void ResetVisualState()
        {
            _isScaledUp = false;
            RestoreBodyColor();
            RestoreShadowColor();
            ApplyOpenSprite();
            transform.localRotation = _initialLocalRotation;
            transform.localScale = _initialScale;
            if (shadowRenderer != null)
            {
                shadowRenderer.transform.localScale = _initialShadowScale;
            }
            SetShadowState(TileShadowState.Off);
        }

        protected void StopIdle()
        {
            _isIdleEnabled = false;
            StopBlinkLoop();
            KillPulseTween();
        }

        protected void StartIdleLoop()
        {
            StopBlinkLoop();
            StartIdlePulse();

            if (!_isIdleEnabled || !EnableIdleBlink || bodyRenderer == null || ClosedSprite == null)
            {
                return;
            }

            _blinkRoutine = StartCoroutine(BlinkLoop());
        }

        protected void KillMotionTweens()
        {
            DOTween.Kill(transform, false);

            if (bodyRenderer != null)
            {
                DOTween.Kill(bodyRenderer, false);
            }

            if (shadowRenderer != null)
            {
                DOTween.Kill(shadowRenderer, false);
                DOTween.Kill(shadowRenderer.transform, false);
            }

            _pulseTween = null;
        }

        protected virtual void OnValidate()
        {
            if (pulseScale < 1f)
            {
                pulseScale = 1f;
            }

            if (pulseDuration < 0.05f)
            {
                pulseDuration = 0.05f;
            }

            if (pulseLoopDelay < 0f)
            {
                pulseLoopDelay = 0f;
            }

            if (clearDuration < 0.05f)
            {
                clearDuration = 0.05f;
            }

            if (damageDuration < 0.05f)
            {
                damageDuration = 0.05f;
            }

            if (landingDuration < 0.05f)
            {
                landingDuration = 0.05f;
            }

            if (targetPulseDuration < 0.05f)
            {
                targetPulseDuration = 0.05f;
            }

            if (specialCreateDuration < 0.05f)
            {
                specialCreateDuration = 0.05f;
            }

            if (bodyRenderer == null)
            {
                bodyRenderer = GetComponent<SpriteRenderer>();
            }

            if (shadowRenderer != null)
            {
                shadowRenderer.enabled = false;
            }
        }

        private void StopBlinkLoop()
        {
            if (_blinkRoutine != null)
            {
                StopCoroutine(_blinkRoutine);
                _blinkRoutine = null;
            }
        }

        private IEnumerator BlinkLoop()
        {
            while (_isIdleEnabled && bodyRenderer != null)
            {
                float waitTime = GetNextBlinkDelay();
                if (waitTime > 0f)
                {
                    yield return new WaitForSeconds(waitTime);
                }

                if (!_isIdleEnabled || bodyRenderer == null)
                {
                    yield break;
                }

                bodyRenderer.sprite = ClosedSprite;
                SyncShadowSpriteIfNeeded();
                yield return new WaitForSeconds(Mathf.Max(0.02f, ClosedEyesDuration));

                if (bodyRenderer == null)
                {
                    yield break;
                }

                ApplyOpenSprite();
            }
        }

        private float GetNextBlinkDelay()
        {
            float min = Mathf.Max(0.25f, BlinkIntervalRange.x);
            float max = Mathf.Max(min, BlinkIntervalRange.y);
            return Random.Range(min, max);
        }

        private void StartIdlePulse()
        {
            KillPulseTween();
            transform.localScale = _initialScale;

            if (!_isIdleEnabled || !enableIdlePulse)
            {
                return;
            }

            float clampedScale = Mathf.Max(1f, pulseScale);
            float clampedDuration = Mathf.Max(0.05f, pulseDuration);
            float clampedDelay = Mathf.Max(0f, pulseLoopDelay);

            _pulseTween = transform
                .DOScale(_initialScale * clampedScale, clampedDuration)
                .SetEase(Ease.InOutSine)
                .SetLoops(-1, LoopType.Yoyo)
                .SetDelay(Random.Range(0f, clampedDelay))
                .SetLink(gameObject);
        }

        private void KillPulseTween()
        {
            if (_pulseTween != null && _pulseTween.IsActive())
            {
                _pulseTween.Kill();
            }

            _pulseTween = null;
            transform.localScale = _initialScale;
        }

        private void KillAllTweens()
        {
            KillMotionTweens();
            KillPulseTween();
        }

        private void SyncShadowSpriteIfNeeded()
        {
            if (shadowRenderer == null || shadowRenderer.sprite != null || bodyRenderer == null)
            {
                return;
            }

            shadowRenderer.sprite = bodyRenderer.sprite;
        }

        private void CacheInitialTransformState()
        {
            _initialScale = transform.localScale;
            _initialLocalRotation = transform.localRotation;
        }
    }
}

