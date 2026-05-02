using UnityEngine;
using AGNIDAWN.Core;

namespace AGNIDAWN.Player
{
    /// <summary>
    /// Sits on the Player GameObject. Listens for OnBoonApplyRequest from EventBus
    /// and applies stat modifications to PlayerController, HealthSystem, AstraController.
    ///
    /// This lives in AGNIDAWN.Player (not Core) so it can safely reference all
    /// Player components without creating a circular dependency.
    /// Linear: FAI-7
    /// </summary>
    [RequireComponent(typeof(PlayerController))]
    [RequireComponent(typeof(HealthSystem))]
    [RequireComponent(typeof(AstraController))]
    public class BoonApplier : MonoBehaviour
    {
        private PlayerController  _player;
        private HealthSystem      _health;
        private AstraController   _astra;

        private void Awake()
        {
            _player = GetComponent<PlayerController>();
            _health = GetComponent<HealthSystem>();
            _astra  = GetComponent<AstraController>();
        }

        private void OnEnable()
            => EventBus.On<BoonData, GameObject>("OnBoonApplyRequest", OnBoonApplyRequest);

        private void OnDisable()
            => EventBus.Off<BoonData, GameObject>("OnBoonApplyRequest", OnBoonApplyRequest);

        // ──────────────────────────────────────────────────────────────────

        private void OnBoonApplyRequest(BoonData boon, GameObject target)
        {
            // Only apply to our own GameObject
            if (target != gameObject) return;

            // Movement
            if (_player != null)
            {
                if (boon.movespeedBonus    != 0f)
                    _player.MoveSpeedMult      += boon.movespeedBonus;
                if (boon.dashCooldownBonus != 0f)
                    _player.DashCooldownMult    = Mathf.Max(0.1f,
                        _player.DashCooldownMult - boon.dashCooldownBonus);
            }

            // Health
            if (_health != null)
            {
                if (boon.maxHealthBonus    != 0f) _health.AddMaxHealth(boon.maxHealthBonus);
                if (boon.healingMultBonus  != 0f) _health.HealingMult += boon.healingMultBonus;
                if (boon.damageMultBonus   != 0f)
                    _health.DamageReductionMult = Mathf.Max(0f,
                        _health.DamageReductionMult - boon.damageMultBonus);
            }

            // Astra / weapons
            if (_astra != null)
            {
                if (boon.damageMultBonus   != 0f) _astra.GlobalDamageMult   += boon.damageMultBonus;
                if (boon.cooldownMultBonus != 0f)
                    _astra.GlobalCooldownMult = Mathf.Max(0.1f,
                        _astra.GlobalCooldownMult - boon.cooldownMultBonus);
            }

            Debug.Log($"[BoonApplier] Applied '{boon.displayName}' to {gameObject.name}");
        }
    }
}
