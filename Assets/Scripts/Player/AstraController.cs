using System.Collections.Generic;
using UnityEngine;
using AGNIDAWN.Core;

namespace AGNIDAWN.Player
{
    /// <summary>
    /// Manages the player's equipped Astras (divine weapons).
    /// Handles firing, cooldowns, upgrades, and synergy detection.
    /// Up to 6 Astra slots, each fires independently on its own cooldown.
    /// Linear: FAI-7
    /// </summary>
    public class AstraController : MonoBehaviour
    {
        // ── Data ───────────────────────────────────────────────────────────
        [System.Serializable]
        public class AstraSlot
        {
            public AstraData data;
            public int       level         = 1;
            public float     cooldownTimer = 0f;
            public bool      IsReady       => cooldownTimer <= 0f;
        }

        // ── Inspector ──────────────────────────────────────────────────────
        [Header("Slots")]
        [SerializeField] private int maxSlots = 6;

        [Header("Fire Point")]
        [SerializeField] private Transform firePoint;

        // ── State ──────────────────────────────────────────────────────────
        private readonly List<AstraSlot> _slots = new List<AstraSlot>();

        // ── Boon modifiers ─────────────────────────────────────────────────
        public float GlobalDamageMult      { get; set; } = 1f;
        public float GlobalCooldownMult    { get; set; } = 1f;
        public float GlobalProjectileSpeed { get; set; } = 1f;
        public int   GlobalPiercingBonus   { get; set; } = 0;

        // ──────────────────────────────────────────────────────────────────
        #region Unity Lifecycle

        private void OnEnable()
        {
            EventBus.On(GameManager.EVT_GAME_PAUSE,  OnPause);
            EventBus.On(GameManager.EVT_GAME_RESUME, OnResume);
        }

        private void OnDisable()
        {
            EventBus.Off(GameManager.EVT_GAME_PAUSE,  OnPause);
            EventBus.Off(GameManager.EVT_GAME_RESUME, OnResume);
        }

        private void Update()
        {
            if (!GameManager.Instance.IsRunning) return;

            for (int i = 0; i < _slots.Count; i++)
            {
                var slot = _slots[i];
                if (slot.data == null) continue;

                // Tick cooldown
                if (slot.cooldownTimer > 0f)
                    slot.cooldownTimer -= Time.deltaTime * (1f / GlobalCooldownMult);

                // Auto-fire when ready
                if (slot.IsReady)
                    FireAstra(slot);
            }
        }

        #endregion

        // ──────────────────────────────────────────────────────────────────
        #region Public API

        /// Equip a new Astra into the next available slot. Returns true if equipped.
        public bool EquipAstra(AstraData data)
        {
            // Check if already equipped — upgrade instead
            for (int i = 0; i < _slots.Count; i++)
            {
                if (_slots[i].data != null && _slots[i].data.astraId == data.astraId)
                {
                    UpgradeAstra(i);
                    return true;
                }
            }

            if (_slots.Count >= maxSlots)
            {
                Debug.LogWarning("[AstraController] All slots full.");
                return false;
            }

            _slots.Add(new AstraSlot { data = data, level = 1 });
            EventBus.Emit<AstraData>("OnAstraEquipped", data);
            return true;
        }

        /// Upgrade an existing slot by index
        public void UpgradeAstra(int slotIndex)
        {
            if (slotIndex < 0 || slotIndex >= _slots.Count) return;
            var slot = _slots[slotIndex];
            slot.level = Mathf.Min(slot.level + 1, slot.data.maxLevel);
            EventBus.Emit<AstraData, int>("OnAstraUpgraded", slot.data, slot.level);
        }

        /// Remove an Astra from a slot
        public void RemoveAstra(int slotIndex)
        {
            if (slotIndex < 0 || slotIndex >= _slots.Count) return;
            var data = _slots[slotIndex].data;
            _slots.RemoveAt(slotIndex);
            EventBus.Emit<AstraData>("OnAstraRemoved", data);
        }

        public List<AstraSlot> GetSlots()  => _slots;
        public int             SlotCount   => _slots.Count;
        public bool            HasSlotFree => _slots.Count < maxSlots;

        #endregion

        // ──────────────────────────────────────────────────────────────────
        #region Private — Firing

        private void FireAstra(AstraSlot slot)
        {
            if (slot.data == null) return;

            // Find nearest enemy for targeting
            GameObject target = FindNearestEnemy();
            if (target == null && slot.data.requiresTarget) return;

            Vector2 direction = target != null
                ? ((Vector2)(target.transform.position - firePoint.position)).normalized
                : GetFacingDirection();

            // Get pooled projectile
            var proj = ObjectPool.Instance?.Get(
                $"Bullet_{slot.data.astraId}",
                slot.data.projectilePrefab,
                firePoint.position,
                Quaternion.LookRotation(Vector3.forward, direction)
            );

            if (proj != null && proj.TryGetComponent<Projectile>(out var p))
            {
                float damage = slot.data.GetDamageAtLevel(slot.level) * GlobalDamageMult;
                float speed  = slot.data.projectileSpeed * GlobalProjectileSpeed;
                int   pierce = slot.data.piercing + GlobalPiercingBonus;
                p.Init(damage, speed, pierce, direction, $"Bullet_{slot.data.astraId}");
            }

            float cd = slot.data.GetCooldownAtLevel(slot.level);
            slot.cooldownTimer = cd;

            EventBus.Emit<AstraData>("OnAstraFired", slot.data);
        }

        private GameObject FindNearestEnemy()
        {
            var enemies = GameObject.FindGameObjectsWithTag("Enemy");
            GameObject nearest = null;
            float minDist = float.MaxValue;
            Vector2 pos = transform.position;

            foreach (var e in enemies)
            {
                float d = Vector2.SqrMagnitude((Vector2)e.transform.position - pos);
                if (d < minDist) { minDist = d; nearest = e; }
            }
            return nearest;
        }

        private Vector2 GetFacingDirection()
        {
            if (TryGetComponent<PlayerController>(out var pc))
            {
                var vel = pc.GetVelocity();
                return vel.sqrMagnitude > 0.01f ? vel.normalized : Vector2.up;
            }
            return Vector2.up;
        }

        #endregion

        // ──────────────────────────────────────────────────────────────────
        #region Event Handlers
        private void OnPause()  { }
        private void OnResume() { }
        #endregion
    }
}
