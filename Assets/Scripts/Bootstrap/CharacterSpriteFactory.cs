using System.Collections.Generic;
using UnityEngine;

namespace AGNIDAWN.Bootstrap
{
    /// <summary>
    /// Loads per-character painted sprite frames from Resources/Characters/
    /// and wires up a CharacterAnimator.  Zero-prefab, zero-ScriptableObject.
    ///
    /// Assets must be imported as textureType:8 (Sprite) in their .meta files.
    /// File naming: Resources/Characters/{Name}_{idle|walk}_{0..N}.png
    /// Uses Resources.Load<Sprite> — no runtime Sprite.Create needed.
    /// </summary>
    public static class CharacterSpriteFactory
    {
        // ── Frame counts per character (idle count, walk count)
        private static readonly Dictionary<string, (int Idle, int Walk)> CharInfo =
            new()
            {
                { "Agni",     (2, 2) },
                { "Asura",    (8, 8) },
                { "Naga",     (3, 3) },
                { "Pisacha",  (2, 2) },
                { "Vetala",   (3, 3) },
                { "Rakshasa", (3, 3) },
            };

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>
        /// Adds / configures SpriteRenderer + CharacterAnimator on <paramref name="go"/>,
        /// loads all Sprite frames and starts idle animation.
        /// Returns null on load failure (caller should use a fallback).
        /// </summary>
        public static CharacterAnimator Setup(GameObject go, string charName, int sortingOrder = 5)
        {
            if (!CharInfo.TryGetValue(charName, out var info))
            {
                Debug.LogWarning($"[CharacterSpriteFactory] Unknown character: {charName}");
                return null;
            }

            // ── Load idle frames (at least the first must exist) ──────────────
            var idleSprites = LoadSprites(charName, "idle", info.Idle);
            if (idleSprites == null || idleSprites.Length == 0)
            {
                Debug.LogWarning(
                    $"[CharacterSpriteFactory] No idle sprites loaded for {charName}. " +
                    $"Ensure Assets/Resources/Characters/{charName}_idle_0.png is imported " +
                    $"as textureType:8 (Sprite).");
                return null;
            }

            var walkSprites = LoadSprites(charName, "walk", info.Walk);

            // ── Configure SpriteRenderer ──────────────────────────────────────
            var sr = go.GetComponent<SpriteRenderer>();
            if (sr == null) sr = go.AddComponent<SpriteRenderer>();
            sr.sprite       = idleSprites[0];
            sr.sortingOrder = sortingOrder;
            sr.color        = Color.white;

            // ── Wire CharacterAnimator ────────────────────────────────────────
            var anim = go.AddComponent<CharacterAnimator>();
            anim.RegisterAnimation("idle", idleSprites);
            anim.RegisterAnimation("walk",
                (walkSprites != null && walkSprites.Length > 0) ? walkSprites : idleSprites);
            anim.Play("idle");
            anim.AutoVelocitySwitch = true;

            return anim;
        }

        // ── Private helpers ───────────────────────────────────────────────────

        private static Sprite[] LoadSprites(string charName, string animName, int count)
        {
            var result   = new List<Sprite>(count);
            Sprite last  = null;

            for (int i = 0; i < count; i++)
            {
                string path   = $"Characters/{charName}_{animName}_{i}";
                var    sprite = Resources.Load<Sprite>(path);

                if (sprite == null)
                {
                    Debug.LogWarning($"[CharacterSpriteFactory] Missing sprite: {path}");
                    if (last != null) result.Add(last);   // pad with last good frame
                    continue;
                }

                result.Add(sprite);
                last = sprite;
            }

            return result.ToArray();
        }
    }
}
