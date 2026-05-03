using UnityEngine;

namespace AGNIDAWN.Core
{
    /// <summary>
    /// Generates procedural Sprite objects at runtime for programmer-art placeholders.
    /// Used by Phase 15 bootstraps so the game has visible content without Unity assets.
    /// </summary>
    public static class SpriteFactory
    {
        // ── Circle ─────────────────────────────────────────────────────────────
        public static Sprite CreateCircle(Color color, int radius = 64)
        {
            int size = radius * 2;
            var tex  = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;

            var pixels = new Color32[size * size];
            float r    = radius - 1f;
            Vector2 center = new Vector2(radius, radius);

            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float dist = Vector2.Distance(new Vector2(x, y), center);
                float alpha = Mathf.Clamp01(r - dist + 0.5f); // anti-aliased edge
                pixels[y * size + x] = new Color(color.r, color.g, color.b, alpha);
            }

            tex.SetPixels32(pixels);
            tex.Apply();

            return Sprite.Create(tex, new Rect(0, 0, size, size),
                new Vector2(0.5f, 0.5f), radius);
        }

        // ── Rounded square ─────────────────────────────────────────────────────
        public static Sprite CreateSquare(Color color, int size = 64, float cornerRadius = 6f)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;

            var pixels = new Color32[size * size];
            float half = size * 0.5f;

            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float px = Mathf.Abs(x - half);
                float py = Mathf.Abs(y - half);
                float inner = half - cornerRadius;

                float dist = 0f;
                if (px > inner && py > inner)
                    dist = Vector2.Distance(new Vector2(px, py), new Vector2(inner, inner));

                float alpha = Mathf.Clamp01((half - cornerRadius - dist) + 0.5f);
                if (px < half && py < half) alpha = 1f; // inside
                pixels[y * size + x] = new Color(color.r, color.g, color.b, alpha);
            }

            tex.SetPixels32(pixels);
            tex.Apply();

            return Sprite.Create(tex, new Rect(0, 0, size, size),
                new Vector2(0.5f, 0.5f), size * 0.5f);
        }

        // ── Ring (for Agni Kund border) ─────────────────────────────────────────
        public static Sprite CreateRing(Color color, int radius = 64, float thickness = 6f)
        {
            int size = radius * 2;
            var tex  = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;

            var pixels = new Color32[size * size];
            Vector2 center = new Vector2(radius, radius);
            float outer = radius - 1f;
            float inner = outer - thickness;

            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float dist = Vector2.Distance(new Vector2(x, y), center);
                float a = Mathf.Clamp01(outer - dist + 0.5f)
                        - Mathf.Clamp01(inner - dist + 0.5f);
                pixels[y * size + x] = new Color(color.r, color.g, color.b, a);
            }

            tex.SetPixels32(pixels);
            tex.Apply();

            return Sprite.Create(tex, new Rect(0, 0, size, size),
                new Vector2(0.5f, 0.5f), radius);
        }

        // ── Solid white 1×1 (for UI fills) ─────────────────────────────────────
        public static Sprite CreateWhitePixel()
        {
            var tex = new Texture2D(1, 1);
            tex.SetPixel(0, 0, Color.white);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f));
        }
    }
}
