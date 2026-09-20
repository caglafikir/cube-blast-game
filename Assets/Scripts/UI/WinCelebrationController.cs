using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace DreamGames.Match.UI
{
    /// <summary>Plays a win sequence: a white flash, confetti burst, a springy banner pop-in, then a second confetti wave.</summary>
    public class WinCelebrationController : MonoBehaviour
    {
        [SerializeField] private GameObject root;
        [SerializeField] private Image overlayImage;
        [SerializeField] private ParticleSystem celebrationParticles;
        [SerializeField] private Transform popupBanner;
        [SerializeField] private float displayDuration = 1.8f;

        private Color overlayRestColor;

        private void Awake()
        {
            if (root != null) root.SetActive(false);
            if (overlayImage != null) overlayRestColor = overlayImage.color;
        }

        public void Play(Action onComplete)
        {
            if (root != null) root.SetActive(true);
            if (popupBanner != null) popupBanner.localScale = Vector3.zero;
            StartCoroutine(PlayRoutine(onComplete));
        }

        private IEnumerator PlayRoutine(Action onComplete)
        {
            yield return FlashRoutine();

            celebrationParticles?.Play();

            if (popupBanner != null)
                yield return Utils.TweenUtil.ScaleInBouncy(this, popupBanner, Vector3.one, 0.45f);

            yield return new WaitForSeconds(0.3f);
            celebrationParticles?.Play();

            yield return new WaitForSeconds(displayDuration);
            onComplete?.Invoke();
        }

        private IEnumerator FlashRoutine()
        {
            if (overlayImage == null) yield break;

            const float duration = 0.18f;
            Color flash = new Color(1f, 1f, 1f, 0.55f);
            float t = 0f;
            while (t < duration)
            {
                t += Time.deltaTime;
                overlayImage.color = Color.Lerp(flash, overlayRestColor, Utils.TweenUtil.EaseOutCubic(Mathf.Clamp01(t / duration)));
                yield return null;
            }
            overlayImage.color = overlayRestColor;
        }
    }
}
