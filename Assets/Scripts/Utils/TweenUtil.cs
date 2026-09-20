using System;
using System.Collections;
using UnityEngine;

namespace DreamGames.Match.Utils
{
    /// <summary>Minimal coroutine-based tweening helper for movement and "juice" effects, with no Physics/Animation dependency.</summary>
    public static class TweenUtil
    {
        /// <summary>Overshoots past 1 then settles back.</summary>
        public static float EaseOutBack(float p)
        {
            const float c1 = 1.70158f;
            const float c3 = c1 + 1f;
            float x = p - 1f;
            return 1f + c3 * x * x * x + c1 * x * x;
        }

        public static float EaseOutCubic(float p)
        {
            float x = 1f - p;
            return 1f - x * x * x;
        }

        public static float EaseInCubic(float p) => p * p * p;

        public static Coroutine MoveTo(MonoBehaviour runner, Transform target, Vector3 to, float duration, Action onComplete = null)
        {
            return runner.StartCoroutine(MoveRoutine(target, to, duration, onComplete));
        }

        public static Coroutine ScaleTo(MonoBehaviour runner, Transform target, Vector3 to, float duration, Action onComplete = null)
        {
            return runner.StartCoroutine(ScaleRoutine(target, to, duration, onComplete));
        }

        /// <summary>Scale-in with a springy overshoot, used for spawns/banners.</summary>
        public static Coroutine ScaleInBouncy(MonoBehaviour runner, Transform target, Vector3 to, float duration, Action onComplete = null)
        {
            return runner.StartCoroutine(ScaleInBouncyRoutine(target, to, duration, onComplete));
        }

        /// <summary>Quick "pop" feedback used for spawning special items / hints.</summary>
        public static Coroutine Punch(MonoBehaviour runner, Transform target, float strength = 1.2f, float duration = 0.18f)
        {
            return runner.StartCoroutine(PunchRoutine(target, strength, duration));
        }

        /// <summary>"Destroyed" feedback: a brief overshoot pop, then shrink to nothing while fading out.</summary>
        public static Coroutine PopOut(MonoBehaviour runner, Transform target, float duration = 0.18f, Action onComplete = null)
        {
            return runner.StartCoroutine(PopOutRoutine(target, duration, onComplete));
        }

        /// <summary>Brief camera-position shake, used for TNT/combo explosions.</summary>
        public static Coroutine Shake(MonoBehaviour runner, Transform target, float strength = 0.18f, float duration = 0.22f)
        {
            return runner.StartCoroutine(ShakeRoutine(target, strength, duration));
        }

        /// <summary>Quick horizontal-stretch/vertical-squash bounce, played when a falling item lands.</summary>
        public static Coroutine SquashLand(MonoBehaviour runner, Transform target, float strength = 0.22f, float duration = 0.14f)
        {
            return runner.StartCoroutine(SquashLandRoutine(target, strength, duration));
        }

        private static IEnumerator MoveRoutine(Transform target, Vector3 to, float duration, Action onComplete)
        {
            if (target == null) yield break;
            Vector3 from = target.position;
            float t = 0f;
            if (duration <= 0f)
            {
                target.position = to;
            }
            else
            {
                while (t < duration)
                {
                    if (target == null) yield break;
                    t += Time.deltaTime;
                    float p = Mathf.Clamp01(t / duration);
                    float eased = p * p; // ease-in cubic
                    target.position = Vector3.LerpUnclamped(from, to, eased);
                    yield return null;
                }
                target.position = to;
            }
            onComplete?.Invoke();
        }

        private static IEnumerator ScaleRoutine(Transform target, Vector3 to, float duration, Action onComplete)
        {
            if (target == null) yield break;
            Vector3 from = target.localScale;
            float t = 0f;
            while (t < duration)
            {
                if (target == null) yield break;
                t += Time.deltaTime;
                float p = Mathf.Clamp01(t / duration);
                target.localScale = Vector3.LerpUnclamped(from, to, p);
                yield return null;
            }
            target.localScale = to;
            onComplete?.Invoke();
        }

        private static IEnumerator PunchRoutine(Transform target, float strength, float duration)
        {
            if (target == null) yield break;
            Vector3 baseScale = target.localScale;
            Vector3 big = baseScale * strength;
            float half = duration * 0.5f;
            float t = 0f;
            while (t < half)
            {
                if (target == null) yield break;
                t += Time.deltaTime;
                target.localScale = Vector3.LerpUnclamped(baseScale, big, t / half);
                yield return null;
            }
            t = 0f;
            while (t < half)
            {
                if (target == null) yield break;
                t += Time.deltaTime;
                target.localScale = Vector3.LerpUnclamped(big, baseScale, t / half);
                yield return null;
            }
            target.localScale = baseScale;
        }

        private static IEnumerator ScaleInBouncyRoutine(Transform target, Vector3 to, float duration, Action onComplete)
        {
            if (target == null) yield break;
            Vector3 from = Vector3.zero;
            float t = 0f;
            while (t < duration)
            {
                if (target == null) yield break;
                t += Time.deltaTime;
                float p = EaseOutBack(Mathf.Clamp01(t / duration));
                target.localScale = Vector3.LerpUnclamped(from, to, p);
                yield return null;
            }
            target.localScale = to;
            onComplete?.Invoke();
        }

        private static IEnumerator PopOutRoutine(Transform target, float duration, Action onComplete)
        {
            if (target == null)
            {
                onComplete?.Invoke();
                yield break;
            }

            var renderers = target.GetComponentsInChildren<SpriteRenderer>();
            Vector3 baseScale = target.localScale;
            Vector3 bigScale = baseScale * 1.25f;

            float growDuration = duration * 0.35f;
            float shrinkDuration = duration - growDuration;

            float t = 0f;
            while (t < growDuration)
            {
                if (target == null) { onComplete?.Invoke(); yield break; }
                t += Time.deltaTime;
                float p = Mathf.Clamp01(t / growDuration);
                target.localScale = Vector3.LerpUnclamped(baseScale, bigScale, EaseOutCubic(p));
                yield return null;
            }

            t = 0f;
            while (t < shrinkDuration)
            {
                if (target == null) { onComplete?.Invoke(); yield break; }
                t += Time.deltaTime;
                float p = Mathf.Clamp01(t / shrinkDuration);
                float eased = EaseInCubic(p);
                target.localScale = Vector3.LerpUnclamped(bigScale, Vector3.zero, eased);
                foreach (var r in renderers)
                {
                    if (r == null) continue;
                    Color c = r.color;
                    c.a = 1f - eased;
                    r.color = c;
                }
                yield return null;
            }

            onComplete?.Invoke();
        }

        private static IEnumerator ShakeRoutine(Transform target, float strength, float duration)
        {
            if (target == null) yield break;
            Vector3 basePos = target.localPosition;
            float t = 0f;
            while (t < duration)
            {
                if (target == null) yield break;
                t += Time.deltaTime;
                float damper = 1f - Mathf.Clamp01(t / duration);
                Vector2 offset = UnityEngine.Random.insideUnitCircle * strength * damper;
                target.localPosition = basePos + new Vector3(offset.x, offset.y, 0f);
                yield return null;
            }
            target.localPosition = basePos;
        }

        private static IEnumerator SquashLandRoutine(Transform target, float strength, float duration)
        {
            if (target == null) yield break;
            Vector3 baseScale = target.localScale;
            Vector3 squashed = new Vector3(baseScale.x * (1f + strength), baseScale.y * (1f - strength), baseScale.z);
            float half = duration * 0.5f;

            float t = 0f;
            while (t < half)
            {
                if (target == null) yield break;
                t += Time.deltaTime;
                target.localScale = Vector3.LerpUnclamped(baseScale, squashed, EaseOutCubic(Mathf.Clamp01(t / half)));
                yield return null;
            }

            t = 0f;
            while (t < half)
            {
                if (target == null) yield break;
                t += Time.deltaTime;
                target.localScale = Vector3.LerpUnclamped(squashed, baseScale, EaseOutBack(Mathf.Clamp01(t / half)));
                yield return null;
            }

            target.localScale = baseScale;
        }
    }
}
