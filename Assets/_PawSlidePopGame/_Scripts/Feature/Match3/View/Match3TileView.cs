using System.Collections;
using DG.Tweening;
using _PawSlidePopGame._Scripts.Feature.Match3.Data;
using _PawSlidePopGame._Scripts.Feature.Match3.Model.Entities;
using UnityEngine;

namespace _PawSlidePopGame._Scripts.Feature.Match3.View
{
    public class Match3TileView : MonoBehaviour
    {
        [Header("Renderers")]
        [SerializeField] private SpriteRenderer bodyRenderer;
        [SerializeField] private SpriteRenderer shadowRenderer;

        [Header("Sprites")]
        [SerializeField] private Sprite openSprite;
        [SerializeField] private Sprite closedSprite;
        [SerializeField] private bool useDefinitionIconAsFallback = true;

        [Header("Shadow")]
        [SerializeField] private Color previewShadowColor = new Color(1f, 1f, 1f, 0.6f);
        [SerializeField] private Color activeShadowColor = new Color(1f, 1f, 1f, 0.9f);

        [Header("Idle Blink")]
        [SerializeField] private bool enableIdleBlink = true;
        [SerializeField] private Vector2 blinkIntervalRange = new Vector2(2.5f, 5f);
        [SerializeField] private float closedEyesDuration = 0.08f;

        [Header("DOTween Idle")]
        [SerializeField] private bool enableIdlePulse;
        [SerializeField] private float pulseScale = 1.03f;
        [SerializeField] private float pulseDuration = 1.2f;
        [SerializeField] private float pulseLoopDelay = 1.8f;

        [Header("FX")]
        [SerializeField] private float clearDuration = 0.2f;
        [SerializeField] private float damageDuration = 0.18f;
        [SerializeField] private float landingDuration = 0.16f;
        [SerializeField] private float clearScale = 0.75f;
        [SerializeField] private float damagePunchScale = 0.1f;
        [SerializeField] private float landingPunchScale = 0.08f;

        private TileModel _tile;
        private TileDefinitionSO _definition;
        private bool _isIdleEnabled;
        private Coroutine _blinkRoutine;
        private Tween _pulseTween;
        private Vector3 _initialScale;
        private Color _bodyBaseColor = Color.white;
        private Color _shadowBaseColor = Color.white;
        private TileShadowState _shadowState;

        public int TileInstanceId => _tile != null ? _tile.InstanceId : 0;
        public TileModel Tile => _tile;

        protected virtual void Awake()
        {
            _initialScale = transform.localScale;
            if (bodyRenderer != null)
            {
                _bodyBaseColor = bodyRenderer.color;
            }

            if (shadowRenderer != null)
            {
                _shadowBaseColor = shadowRenderer.color;
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

        public virtual void Bind(TileModel tile, TileDefinitionSO definition)
        {
            _tile = tile;
            _definition = definition;

            if (bodyRenderer != null)
            {
                bodyRenderer.color = _bodyBaseColor;
            }

            if (shadowRenderer != null)
            {
                shadowRenderer.color = _shadowBaseColor;
            }

            ApplyOpenSprite();
            SetShadowState(TileShadowState.Off);
        }

        public IEnumerator PlayActivateAsync()
        {
            StopIdle();
            SetShadowState(TileShadowState.Active);
            KillMotionTweens();

            float duration = Mathf.Max(0.05f, damageDuration);
            Sequence sequence = DOTween.Sequence().SetLink(gameObject);
            sequence.Join(transform.DOPunchScale(Vector3.one * (damagePunchScale * 1.25f), duration, 4, 0.7f));

            if (bodyRenderer != null)
            {
                sequence.Join(bodyRenderer.DOColor(new Color(1f, 0.95f, 0.7f, 1f), duration * 0.5f).SetLoops(2, LoopType.Yoyo));
            }

            yield return sequence.WaitForCompletion();

            if (bodyRenderer != null)
            {
                bodyRenderer.color = _bodyBaseColor;
            }
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

        public void SetShadowState(TileShadowState state)
        {
            _shadowState = state;
            if (shadowRenderer == null || shadowRenderer.sprite == null)
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

        public IEnumerator PlayClearAsync()
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

        public IEnumerator PlayDamageAsync(int currentHp, int previousHp)
        {
            StopIdle();
            SetShadowState(TileShadowState.Active);
            KillMotionTweens();

            float duration = Mathf.Max(0.05f, damageDuration);
            Sequence sequence = DOTween.Sequence().SetLink(gameObject);
            sequence.Join(transform.DOPunchScale(Vector3.one * damagePunchScale, duration, 4, 0.8f));

            if (bodyRenderer != null)
            {
                Color flashColor = previousHp > currentHp ? new Color(1f, 0.75f, 0.75f, 1f) : new Color(1f, 1f, 1f, 1f);
                sequence.Join(bodyRenderer.DOColor(flashColor, duration * 0.5f).SetLoops(2, LoopType.Yoyo));
            }

            yield return sequence.WaitForCompletion();

            if (bodyRenderer != null)
            {
                bodyRenderer.color = _bodyBaseColor;
            }

            SetShadowState(TileShadowState.Off);
        }

        public IEnumerator PlayMoveAsync(Vector3 targetLocalPosition, float duration)
        {
            StopIdle();
            SetShadowState(TileShadowState.Active);
            KillMotionTweens();

            Tween moveTween = transform
                .DOLocalMove(targetLocalPosition, Mathf.Max(0.05f, duration))
                .SetEase(Ease.InOutQuad)
                .SetLink(gameObject);

            yield return moveTween.WaitForCompletion();
        }

        public IEnumerator PlaySpawnFallAsync(Vector3 startLocalPosition, Vector3 targetLocalPosition, float duration)
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

        public IEnumerator PlayLandAsync(float intensity = 1f)
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

        private void ResetVisualState()
        {
            if (bodyRenderer != null)
            {
                bodyRenderer.color = _bodyBaseColor;
            }

            if (shadowRenderer != null)
            {
                shadowRenderer.color = _shadowBaseColor;
            }

            transform.localScale = _initialScale;
            SetShadowState(TileShadowState.Off);
        }

        private void StopIdle()
        {
            _isIdleEnabled = false;
            StopBlinkLoop();
            KillPulseTween();
        }

        private void StartIdleLoop()
        {
            StopBlinkLoop();
            StartIdlePulse();

            if (!_isIdleEnabled || !enableIdleBlink || bodyRenderer == null || closedSprite == null)
            {
                return;
            }

            _blinkRoutine = StartCoroutine(BlinkLoop());
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

                bodyRenderer.sprite = closedSprite;
                yield return new WaitForSeconds(Mathf.Max(0.02f, closedEyesDuration));

                if (bodyRenderer == null)
                {
                    yield break;
                }

                ApplyOpenSprite();
            }
        }

        private void ApplyOpenSprite()
        {
            if (bodyRenderer == null)
            {
                return;
            }

            Sprite spriteToUse = openSprite;
            if (spriteToUse == null && useDefinitionIconAsFallback && _definition != null)
            {
                spriteToUse = _definition.Icon;
            }

            bodyRenderer.sprite = spriteToUse;
        }

        private float GetNextBlinkDelay()
        {
            float min = Mathf.Max(0.25f, blinkIntervalRange.x);
            float max = Mathf.Max(min, blinkIntervalRange.y);
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

        private void KillMotionTweens()
        {
            DOTween.Kill(transform, false);

            if (bodyRenderer != null)
            {
                DOTween.Kill(bodyRenderer, false);
            }

            if (shadowRenderer != null)
            {
                DOTween.Kill(shadowRenderer, false);
            }

            _pulseTween = null;
        }

        private void KillAllTweens()
        {
            KillMotionTweens();
            KillPulseTween();
        }

        protected virtual void OnValidate()
        {
            if (blinkIntervalRange.x < 0.25f)
            {
                blinkIntervalRange.x = 0.25f;
            }

            if (blinkIntervalRange.y < blinkIntervalRange.x)
            {
                blinkIntervalRange.y = blinkIntervalRange.x;
            }

            if (closedEyesDuration < 0.02f)
            {
                closedEyesDuration = 0.02f;
            }

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

            if (bodyRenderer == null)
            {
                bodyRenderer = GetComponent<SpriteRenderer>();
            }

            if (shadowRenderer != null)
            {
                shadowRenderer.enabled = false;
            }
        }
    }
}
