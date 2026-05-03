using UnityEngine;
using UnityEngine.UI;
using TMPro;
using AGNIDAWN.Core;

namespace AGNIDAWN.UI
{
    /// <summary>
    /// HUDController — In-game heads-up display.
    ///
    /// Elements:
    ///   - Health bar (stylised as lotus petals)
    ///   - Agni Dial (5-tier fire indicator with tier name)
    ///   - Survival timer (counts up to 20:00)
    ///   - XP bar (fills toward next level)
    ///   - Level indicator
    ///   - Enemy kill counter
    ///   - Shard counter
    ///   - Boss warning banner (flashes on boss approach)
    ///
    /// All data comes from EventBus — no direct references to Player or GameManager.
    /// Linear: FAI-14
    /// </summary>
    public class HUDController : BaseUIPanel
    {
        // ── Health ─────────────────────────────────────────────────────────
        [Header("Health")]
        [SerializeField] private Slider      healthSlider;
        [SerializeField] private Image       healthFill;
        [SerializeField] private TextMeshProUGUI healthText;
        [SerializeField] private Color       healthFullColor   = new Color(1f, 0.4f, 0.1f);   // deep orange
        [SerializeField] private Color       healthLowColor    = new Color(0.8f, 0.05f, 0.05f); // dark red

        // ── Agni Dial ──────────────────────────────────────────────────────
        [Header("Agni Dial")]
        [SerializeField] private Slider      agniSlider;
        [SerializeField] private Image       agniFill;
        [SerializeField] private TextMeshProUGUI agniTierNameText;
        [SerializeField] private Image       agniFlameIcon;

        // Tier names mirrored from AgniKund (UI assembly can't reference Gameplay)
        private static readonly string[] TIER_NAMES =
        {
            "",             // 0 — unused
            "MRITYUPRAYA",  // 1 — near death
            "KSHEEN",       // 2 — weakening
            "SADHARAN",     // 3 — normal
            "PRABHAVA",     // 4 — strong
            "MAHAAGNI"      // 5 — maximum
        };

        // Tier colours: Tier1=dying red → Tier5=divine gold
        private static readonly Color[] TIER_COLORS =
        {
            Color.clear,
            new Color(0.5f, 0.0f, 0.0f), // Tier 1 — MRITYUPRAYA — dark red
            new Color(0.8f, 0.3f, 0.0f), // Tier 2 — KSHEEN — dim orange
            new Color(1.0f, 0.6f, 0.1f), // Tier 3 — SADHARAN — flame orange
            new Color(1.0f, 0.9f, 0.2f), // Tier 4 — PRABHAVA — bright yellow
            new Color(1.0f, 1.0f, 0.7f), // Tier 5 — MAHAAGNI — divine white-gold
        };

        // ── Timer ──────────────────────────────────────────────────────────
        [Header("Timer")]
        [SerializeField] private TextMeshProUGUI timerText;
        [SerializeField] private Color           timerNormalColor  = Color.white;
        [SerializeField] private Color           timerUrgentColor  = new Color(1f, 0.3f, 0.3f);

        // ── XP / Level ─────────────────────────────────────────────────────
        [Header("XP / Level")]
        [SerializeField] private Slider      xpSlider;
        [SerializeField] private TextMeshProUGUI levelText;

        // ── Kill / Shard Counters ──────────────────────────────────────────
        [Header("Counters")]
        [SerializeField] private TextMeshProUGUI killCountText;
        [SerializeField] private TextMeshProUGUI shardCountText;

        // ── Boss Warning ───────────────────────────────────────────────────
        [Header("Boss Warning")]
        [SerializeField] private GameObject  bossWarningBanner;
        [SerializeField] private TextMeshProUGUI bossWarningText;
        private float _bossWarningTimer;
        private const float BOSS_WARNING_DURATION = 3.5f;

        // ── State ──────────────────────────────────────────────────────────
        private float _currentHP;
        private float _maxHP         = 100f;
        private float _agniHP        = 1f;
        private int   _currentTier   = 5;
        private int   _kills         = 0;
        private int   _shards        = 0;
        private int   _currentLevel  = 1;
        private float _currentXP     = 0f;
        private float _xpToNextLevel = 100f;

        // ──────────────────────────────────────────────────────────────────
        #region Panel Lifecycle

        protected override void OnShow()
        {
            EventBus.On<float, GameObject>("OnPlayerDamagedWithSource", OnPlayerDamaged);
            EventBus.On<float>("OnPlayerHealed",           OnPlayerHealed);
            EventBus.On<float, float>("OnAgniAuraChanged", OnAgniChanged);
            EventBus.On<int, int>("OnAgniTierChanged",     OnAgniTierChanged);
            EventBus.On<float, Vector2>("OnXPDropped",     OnXPGained);
            EventBus.On<string>("OnEnemyDied",             OnEnemyDied);
            EventBus.On<int>("OnShardCollected",           OnShardCollected);
            EventBus.On<string>("OnBossSpawned",           OnBossSpawned);

            // Reset counters
            _kills  = 0;
            _shards = 0;
            _currentLevel = 1;
            _currentXP    = 0f;
            UpdateKillText();
            UpdateShardText();
            UpdateLevelText();

            if (bossWarningBanner != null)
                bossWarningBanner.SetActive(false);
        }

        protected override void OnHide()
        {
            EventBus.Off<float, GameObject>("OnPlayerDamagedWithSource", OnPlayerDamaged);
            EventBus.Off<float>("OnPlayerHealed",           OnPlayerHealed);
            EventBus.Off<float, float>("OnAgniAuraChanged", OnAgniChanged);
            EventBus.Off<int, int>("OnAgniTierChanged",     OnAgniTierChanged);
            EventBus.Off<float, Vector2>("OnXPDropped",     OnXPGained);
            EventBus.Off<string>("OnEnemyDied",             OnEnemyDied);
            EventBus.Off<int>("OnShardCollected",           OnShardCollected);
            EventBus.Off<string>("OnBossSpawned",           OnBossSpawned);
        }

        protected override void Update()
        {
            base.Update();
            UpdateTimer();
            TickBossWarning();
        }

        #endregion

        // ──────────────────────────────────────────────────────────────────
        #region Event Handlers

        private void OnPlayerDamaged(float dmg, GameObject src)
        {
            _currentHP = Mathf.Max(0f, _currentHP - dmg);
            RefreshHealthBar();
        }

        private void OnPlayerHealed(float amount)
        {
            _currentHP = Mathf.Min(_maxHP, _currentHP + amount);
            RefreshHealthBar();
        }

        private void OnAgniChanged(float speedBuff, float damageBuff)
        {
            // Agni HP is tracked from AgniKund's HealthPercent event
        }

        private void OnAgniTierChanged(int newTier, int prevTier)
        {
            _currentTier = newTier;
            RefreshAgniDial();
        }

        private void OnXPGained(float xp, Vector2 pos)
        {
            _currentXP += xp;
            while (_currentXP >= _xpToNextLevel)
            {
                _currentXP      -= _xpToNextLevel;
                _currentLevel++;
                _xpToNextLevel   = 100f * Mathf.Pow(1.4f, _currentLevel - 1);
            }
            RefreshXPBar();
        }

        private void OnEnemyDied(string enemyType)
        {
            _kills++;
            UpdateKillText();
        }

        private void OnShardCollected(int amount)
        {
            _shards += amount;
            UpdateShardText();
        }

        private void OnBossSpawned(string bossId)
        {
            ShowBossWarning(bossId);
        }

        #endregion

        // ──────────────────────────────────────────────────────────────────
        #region UI Refresh

        private void RefreshHealthBar()
        {
            float pct = _maxHP > 0 ? _currentHP / _maxHP : 0f;
            if (healthSlider != null) healthSlider.value = pct;
            if (healthFill   != null) healthFill.color   = Color.Lerp(healthLowColor, healthFullColor, pct);
            if (healthText   != null) healthText.text     = $"{Mathf.CeilToInt(_currentHP)} / {Mathf.CeilToInt(_maxHP)}";
        }

        private void RefreshAgniDial()
        {
            if (agniSlider     != null) agniSlider.value    = _agniHP;
            if (agniFill       != null) agniFill.color      = _currentTier >= 1 && _currentTier <= 5
                                                              ? TIER_COLORS[_currentTier]
                                                              : Color.white;
            if (agniTierNameText != null)
                agniTierNameText.text = (_currentTier >= 1 && _currentTier <= 5)
                                      ? TIER_NAMES[_currentTier]
                                      : "";
        }

        private void RefreshXPBar()
        {
            float pct = _xpToNextLevel > 0 ? _currentXP / _xpToNextLevel : 0f;
            if (xpSlider != null) xpSlider.value = pct;
            UpdateLevelText();
        }

        private void UpdateTimer()
        {
            if (GameManager.Instance == null || !GameManager.Instance.IsRunning) return;

            float elapsed  = GameManager.Instance.ElapsedTime;
            float minutes  = Mathf.Floor(elapsed / 60f);
            float seconds  = elapsed % 60f;
            bool  urgent   = elapsed >= 18f * 60f; // last 2 minutes

            if (timerText != null)
            {
                timerText.text  = $"{minutes:00}:{seconds:00}";
                timerText.color = urgent ? timerUrgentColor : timerNormalColor;
            }
        }

        private void UpdateKillText()
        {
            if (killCountText != null) killCountText.text = $"{_kills}";
        }

        private void UpdateShardText()
        {
            if (shardCountText != null) shardCountText.text = $"{_shards}";
        }

        private void UpdateLevelText()
        {
            if (levelText != null) levelText.text = $"LVL {_currentLevel}";
        }

        private void ShowBossWarning(string bossId)
        {
            if (bossWarningBanner == null) return;
            bossWarningBanner.SetActive(true);
            _bossWarningTimer = BOSS_WARNING_DURATION;

            string bossName = bossId switch
            {
                "Ravana"      => "RAVANA APPROACHES",
                "Mahishasura" => "MAHISHASURA RISES",
                "Kali"        => "KALI DESCENDS",
                "Vritra"      => "VRITRA — THE FINAL DARKNESS",
                _             => bossId.ToUpper() + " APPROACHES"
            };

            if (bossWarningText != null) bossWarningText.text = bossName;
        }

        private void TickBossWarning()
        {
            if (bossWarningBanner == null || !bossWarningBanner.activeSelf) return;
            _bossWarningTimer -= Time.deltaTime;
            if (_bossWarningTimer <= 0f)
                bossWarningBanner.SetActive(false);
        }

        #endregion
    }
}
