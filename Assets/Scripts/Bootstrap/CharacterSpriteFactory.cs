using System.Collections.Generic;
using UnityEngine;

namespace AGNIDAWN.Bootstrap
{
    /// <summary>
    /// Loads per-character painted sprite frames from Resources/Characters/
    /// and wires up a CharacterAnimator.  Zero-prefab, zero-ScriptableObject.
    ///
    /// File naming convention (in Assets/Resources/Characters/):
    ///   {Name}_idle_{0..N}.png
    ///   {Name}_walk_{0..N}.png
    /// </summary>
    public static class CharacterSpriteFactory
    {
        // ── Frame counts per character (idle count, walk count, desired world height in units)
        private static readonly Dictionary<string, (int Idle, int Walk, float WorldH)> CharInfo =
            new()
            {
                { "Agni",     (2, 2, 1.6f) },
                { "Asura",    (8, 8, 1.3f) },
                { "Naga",     (3, 3, 1.3f) },
                { "Pisacha",  (2, 2, 1.2f) },
                { "Vetala",   (3, 3, 1.1f) },
                { "Rakshasa", (3, 3, 1.3f) },
            };

        // ── Public API ───────────────────────────────────────────────────────

        /// <summary>
        /// Adds SpriteRenderer + CharacterAnimator to <paramref name="go"/>,
        /// loads all frames for <paramref name="charName"/> and starts idle.
        /// </summary>
        /// <param name="go">Target GameObject (must already exist).</param>
        /// <param name="charName">One of: Agni, Asura, Naga, Pisacha, Vetala, Rakshasa.</param>
        /// <param name="sortingOrder">SpriteRenderer sorting order.</param>
        /// <returns>The CharacterAnimator component, or null on load failure.</returns>
        public static CharacterAnimator Setup(GameObject go, string charName, int sortingOrder = 5)
        {
            if (!CharInfo.TryGetValue(charName, out var info))
            {
                Debug.LogWarning($"[CharacterSpriteFactory] Unknown character: {charName}");
                return null;
            }

            // ── Load first idle frame to compute PPU ─────────────────────────
            var firstTex = Resources.Load<Texture2D>($"Characters/{charName}_idle_0");
            if (firstTex == null)
            {
                Debug.LogWarning($"[CharacterSpriteFactory] Missing texture: Characters/{charName}_idle_0");
                return null;
            }

            // pixels-per-unit so the sprite is exactly worldH units tall
            float ppu = Mathf.Max(1f, firstTex.height / info.WorldH);

            // ── Load sprite arrays ───────────────────────────────────────────
            var idleSprites = LoadFrames(charName, "idle", info.Idle, ppu);
            var walkSprites = LoadFrames(charName, "walk", info.Walk, ppu);

            if (idleSprites == null || idleSprites.Length == 0)
            {
                Debug.LogWarning($"[CharacterSpriteFactory] No idle frames loaded for {charName}");
                return null;
            }

            // ── Configure SpriteRenderer ─────────────────────────────────────
            var sr = go.GetComponent<SpriteRenderer>();
            if (sr == null) sr = go.AddComponent<SpriteRenderer>();
            sr.sprite       = idleSprites[0];
            sr.sortingOrder = sortingOrder;
            sr.color        = Color.white; // painted art — no tinting

            // ── Wire CharacterAnimator ───────────────────────────────────────
            var anim = go.AddComponent<CharacterAnimator>();
            anim.RegisterAnimation("idle", idleSprites);

            if (walkSprites != null && walkSprites.Length > 0)
                anim.RegisterAnimation("walk", walkSprites);
            else
                anim.RegisterAnimation("walk", idleSprites); // fallback: walk = idle

            anim.Play("idle");
            anim.AutoVelocitySwitch = true;

            return anim;
        }

        // ── Private helpers ──────────────────────────────────────────────────

        private static Sprite[] LoadFrames(string charName, string anim, int count, float ppu)
        {
            var result = new List<Sprite>(count);
            Sprite fallback = null;

            for (int i = 0; i < count; i++)
            {
                string path = $"Characters/{charName}_{anim}_{i}";
                var tex = Resources.Load<Texture2D>(path);
                if (tex == null)
                {
                    if (fallback != null)
                        result.Add(fallback);   // reuse last good frame
                    continue;
                }

                tex.filterMode = FilterMode.Bilinear;
                tex.wrapMode   = TextureWrapMode.Clamp;

                var sprite = Sprite.Create(
                    tex,
                    new Rect(0, 0, tex.width, tex.height),
                    new Vector2(0.5f, 0.5f), // pivot centre
                    ppu
                );

                result.Add(sprite);
                fallback = sprite;
            }

            return result.ToArray();
        }
    }
}
