using UnityEngine;

namespace AGNIDAWN.Player
{
    /// <summary>
    /// Oval shadow that stays fixed at ground level in Y, follows the player's X.
    /// Attach to the shadow GameObject. Call Init() from GameBootstrap.
    ///
    /// Shadow Y never moves — it represents Agni's contact point on the stone floor.
    /// Opacity scales slightly with player's height above ground for depth cue.
    /// </summary>
    public class ShadowFollow : MonoBehaviour
    {
        private Transform     _target;
        private float         _groundY;
        private SpriteRenderer _sr;

        // Height above ground at which shadow opacity = 0
        private const float FADE_HEIGHT = 4f;
        private const float MIN_ALPHA   = 0.10f;
        private const float MAX_ALPHA   = 0.32f;

        /// <summary>
        /// Called by GameBootstrap after both player and ground are set up.
        /// </summary>
        public void Init(Transform player, float groundY)
        {
            _target  = player;
            _groundY = groundY;
            _sr      = GetComponent<SpriteRenderer>();

            // Lock Z and Y immediately
            transform.position = new Vector3(player.position.x, _groundY, 0f);
        }

        private void LateUpdate()
        {
            if (_target == null) return;

            // Follow X; Y stays on the floor
            transform.position = new Vector3(_target.position.x, _groundY, 0f);

            // Fade out as player rises above ground
            if (_sr != null)
            {
                float height = Mathf.Max(0f, _target.position.y - _groundY);
                float t      = Mathf.Clamp01(height / FADE_HEIGHT);
                float alpha  = Mathf.Lerp(MAX_ALPHA, MIN_ALPHA, t);
                var   c      = _sr.color;
                _sr.color    = new Color(c.r, c.g, c.b, alpha);
            }
        }
    }
}
