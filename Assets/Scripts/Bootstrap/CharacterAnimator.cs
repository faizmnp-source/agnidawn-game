using System.Collections.Generic;
using UnityEngine;

namespace AGNIDAWN.Bootstrap
{
    /// <summary>
    /// Code-driven frame animator for painted character sprites.
    /// No AnimatorController required — fully zero-prefab compatible.
    /// </summary>
    public class CharacterAnimator : MonoBehaviour
    {
        // ── Public settings ──────────────────────────────────────────────────
        [Tooltip("Frames per second for the current animation.")]
        public float FrameRate = 8f;

        [Tooltip("When true, switches between idle/walk based on Rigidbody2D.linearVelocity.")]
        public bool AutoVelocitySwitch = true;

        // ── State ────────────────────────────────────────────────────────────
        private readonly Dictionary<string, Sprite[]> _animations = new();
        private SpriteRenderer _sr;
        private Rigidbody2D   _rb;

        private string _currentAnim = "";
        private int    _frameIdx;
        private float  _timer;

        // ── Registration ─────────────────────────────────────────────────────
        public void RegisterAnimation(string animName, Sprite[] frames)
        {
            if (frames == null || frames.Length == 0) return;
            _animations[animName] = frames;
        }

        // ── Playback ──────────────────────────────────────────────────────────
        public void Play(string animName)
        {
            if (_currentAnim == animName) return;
            if (!_animations.ContainsKey(animName)) return;
            _currentAnim = animName;
            _frameIdx    = 0;
            _timer       = 0f;
            ApplyFrame();
        }

        /// <summary>Flip sprite on X axis (face left/right).</summary>
        public void FlipX(bool flip)
        {
            if (_sr != null) _sr.flipX = flip;
        }

        // ── Unity lifecycle ───────────────────────────────────────────────────
        private void Awake()
        {
            _sr = GetComponent<SpriteRenderer>();
            _rb = GetComponent<Rigidbody2D>();
        }

        private void Update()
        {
            // Auto velocity-driven state switch
            if (AutoVelocitySwitch && _rb != null && _animations.Count >= 2)
            {
                bool moving = _rb.linearVelocity.sqrMagnitude > 0.04f; // ~0.2 u/s threshold
                string target = moving ? "walk" : "idle";
                if (target != _currentAnim) Play(target);

                // Face movement direction
                if (moving && _sr != null)
                    _sr.flipX = _rb.linearVelocity.x < -0.05f;
            }

            // Advance frame
            if (!_animations.TryGetValue(_currentAnim, out var frames) || frames.Length == 0)
                return;

            _timer += Time.deltaTime;
            float interval = 1f / Mathf.Max(1f, FrameRate);
            if (_timer >= interval)
            {
                _timer  -= interval;
                _frameIdx = (_frameIdx + 1) % frames.Length;
                ApplyFrame();
            }
        }

        private void ApplyFrame()
        {
            if (_sr == null) _sr = GetComponent<SpriteRenderer>();
            if (_sr == null || !_animations.TryGetValue(_currentAnim, out var frames)) return;
            if (_frameIdx < frames.Length && frames[_frameIdx] != null)
                _sr.sprite = frames[_frameIdx];
        }
    }
}
