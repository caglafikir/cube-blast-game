using UnityEngine;

namespace DreamGames.Match.Utils
{
    /// <summary>
    /// Instantiates a one-shot VFX prefab (a ParticleSystem configured to play once) at a world
    /// position and auto-destroys it once it's finished, so callers don't need to manage the
    /// instance's lifetime themselves. Used to add "juice" (small bursts on ordinary blasts,
    /// bigger bursts on rocket/TNT/combo explosions) without any Physics/Animation components.
    /// </summary>
    public static class VfxSpawner
    {
        public static void Spawn(ParticleSystem prefab, Vector3 position, Transform parent = null)
        {
            if (prefab == null) return;

            ParticleSystem instance = Object.Instantiate(prefab, position, Quaternion.identity, parent);
            var main = instance.main;
            float lifetime = main.duration + main.startLifetime.constantMax;
            instance.Play();
            Object.Destroy(instance.gameObject, lifetime + 0.25f);
        }
    }
}
