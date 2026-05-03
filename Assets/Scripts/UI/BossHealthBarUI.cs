using UnityEngine;
using UnityEngine.UI;
using TMPro;
using AGNIDAWN.Core;

namespace AGNIDAWN.UI
{
    /// <summary>
    /// BossHealthBarUI — Prominent HP bar shown during boss fights.
    ///
    /// Features:
    ///   - Boss name in Sanskrit + English
    ///   - Phase indicators (pip count = number of phase thresholds)
    ///   - Smooth lag-bar effect (red bar trails behind main HP)
    ///   - Flashes red on boss phase transition
    ///
    /// Linear: FAI-14
    /// </summary>
    public class BossHealthBarUI : BaseUIPanel
    {
        [Header("Bar")]
        [SerializeField] private Slider          mainSlider;
        [SerializeField] private Slider          lagSlider;         // trails behind
        [SerializeField] private Image           mainFill;
        [SerializeField] private float           lagSpeed = 1.2f;

        [Header("Labels")]
        [SerializeField] private TextMeshProUGUI bossNameText;
        [SerializeField] private TextMeshProUGUI phaseText;

        [Header("Phase Pips")]
        [SerializeField] private Transform       pipContainer;
        [SerializeField] private GameObject      pipPrefab;

        [Header("Colors")]
        [SerializeField] private Color           normalColor = new Color(0.85f, 0.15f, 0.15f);
        [SerializeField] private Color           phaseColor  = new Color(1.00f, 0.80f, 0.10f);

        private BaseBoss _boss;
        private float    _lagValue;
        private int      _lastPhase = -1;

        private static readonly System.Collections.Generic.Dictionary<string, string> BOSS_LABELS
            = new()
        {
            { "Ravana",      "रावण  ·  RAVANA" },
            { "Mahishasura", "महिषासुर  ·  MAHISHASURA" },
            { "Kali",        "काली  ·  KALI" },
            { "Vritra",      "वृत्र  ·  VRITRA" },
        };

        // ──────────────────────────────────────────────────────────────────
        public void SetBoss(BaseBoss boss)
        {
            _boss       = boss;
            _lagValue   = 1f;
            _lastPhase  = -1;

            if (mainSlider != null) mainSlider.value = 1f;
            if (lagSlider  != null) lagSlider.value  = 1f;

            if (bossNameText != null)
                bossNameText.text = BOSS_LABELS.TryGetValue(boss.Data.bossId, out var label)
                                  ? label : boss.Data.bossId.ToUpper();

            BuildPips(boss.Data.phaseThresholds?.Length ?? 0);
            RefreshPhaseText(0);
        }

        protected override void OnShow()
        {
            EventBus.On<string, int>("OnBossPhaseChanged", OnPhaseChanged);
        }

        protected override void OnHide()
        {
            EventBus.Off<string, int>("OnBossPhaseChanged", OnPhaseChanged);
            _boss = null;
        }

        protected override void Update()
        {
            base.Update();
            if (_boss == null || !IsVisible) return;

            float hp = _boss.HealthPercent;

            if (mainSlider != null) mainSlider.value = hp;

            // Lag bar
            if (_lagValue > hp)
            {
                _lagValue = Mathf.MoveTowards(_lagValue, hp, lagSpeed * Time.deltaTime);
                if (lagSlider != null) lagSlider.value = _lagValue;
            }
            else
            {
                _lagValue = hp;
                if (lagSlider != null) lagSlider.value = hp;
            }

            // Phase check
            if (_boss.CurrentPhase != _lastPhase)
            {
                _lastPhase = _boss.CurrentPhase;
                RefreshPhaseText(_lastPhase);
            }
        }

        // ──────────────────────────────────────────────────────────────────
        private void OnPhaseChanged(string bossId, int newPhase)
        {
            if (_boss == null || bossId != _boss.Data.bossId) return;
            if (mainFill != null) mainFill.color = phaseColor;
            Invoke(nameof(ResetFillColor), 0.8f);
            ActivatePip(newPhase - 1);
        }

        private void ResetFillColor()
        {
            if (mainFill != null) mainFill.color = normalColor;
        }

        private void RefreshPhaseText(int phase)
        {
            if (phaseText != null)
                phaseText.text = $"PHASE {phase + 1}";
        }

        private void BuildPips(int count)
        {
            if (pipContainer == null || pipPrefab == null) return;
            foreach (Transform child in pipContainer) Destroy(child.gameObject);
            for (int i = 0; i < count; i++)
                Instantiate(pipPrefab, pipContainer);
        }

        private void ActivatePip(int index)
        {
            if (pipContainer == null) return;
            if (index >= 0 && index < pipContainer.childCount)
            {
                var img = pipContainer.GetChild(index).GetComponent<Image>();
                if (img != null) img.color = phaseColor;
            }
        }
    }
}
