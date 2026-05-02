using System.Collections.Generic;
using UnityEngine;
using AGNIDAWN.Core;

namespace AGNIDAWN.Player
{
    /// <summary>
    /// Handles XP accumulation, level-up triggers, and boon selection.
    /// On level-up: pauses the game, presents 3 random boons, resumes after choice.
    /// Linear: FAI-7
    /// </summary>
    public class LevelUpManager : MonoBehaviour
    {
        // ── Inspector ──────────────────────────────────────────────────────
        [Header("XP Curve")]
        [SerializeField] private float baseXpRequired  = 10f;
        [SerializeField] private float xpScaleFactor   = 1.4f; // each level needs 40% more XP
        [SerializeField] private int   maxLevel        = 99;

        [Header("Boon Selection")]
        [SerializeField] private int   boonChoices     = 3;    // cards shown on level-up
        [SerializeField] private BoonDatabase boonDatabase;    // ScriptableObject

        // ── State ──────────────────────────────────────────────────────────
        private int   _currentLevel  = 1;
        private float _currentXp     = 0f;
        private float _xpToNextLevel;

        public int   Level      => _currentLevel;
        public float CurrentXP  => _currentXp;
        public float XPProgress => _currentXp / _xpToNextLevel;

        // ──────────────────────────────────────────────────────────────────
        #region Unity Lifecycle

        private void Awake()
        {
            _xpToNextLevel = CalculateXpRequired(1);
        }

        private void OnEnable()
        {
            EventBus.On<float>("OnXPGained",    OnXPGained);
            EventBus.On<BoonData>("OnBoonChosen", OnBoonChosen);
        }

        private void OnDisable()
        {
            EventBus.Off<float>("OnXPGained",    OnXPGained);
            EventBus.Off<BoonData>("OnBoonChosen", OnBoonChosen);
        }

        #endregion

        // ──────────────────────────────────────────────────────────────────
        #region Public API

        /// Grant XP directly (called by enemies on death, shrines, etc.)
        public void AddXP(float amount)
        {
            if (_currentLevel >= maxLevel) return;

            _currentXp += amount;
            EventBus.Emit<float, float>("OnXPChanged", _currentXp, _xpToNextLevel);

            while (_currentXp >= _xpToNextLevel && _currentLevel < maxLevel)
                ProcessLevelUp();
        }

        #endregion

        // ──────────────────────────────────────────────────────────────────
        #region Private

        private void ProcessLevelUp()
        {
            _currentXp -= _xpToNextLevel;
            _currentLevel++;
            _xpToNextLevel = CalculateXpRequired(_currentLevel);

            EventBus.Emit<int>("OnLevelChanged", _currentLevel);
            GameManager.Instance?.TriggerLevelUp();

            // Pick and present boon choices
            var choices = PickBoons(boonChoices);
            EventBus.Emit<List<BoonData>>("OnBoonChoicesReady", choices);
        }

        private List<BoonData> PickBoons(int count)
        {
            var result   = new List<BoonData>();
            if (boonDatabase == null) return result;

            var available = boonDatabase.GetAvailableBoons(_currentLevel);
            // Fisher-Yates shuffle
            for (int i = available.Count - 1; i > 0; i--)
            {
                int j = Random.Range(0, i + 1);
                (available[i], available[j]) = (available[j], available[i]);
            }

            for (int i = 0; i < Mathf.Min(count, available.Count); i++)
                result.Add(available[i]);

            return result;
        }

        private float CalculateXpRequired(int level)
            => Mathf.Floor(baseXpRequired * Mathf.Pow(xpScaleFactor, level - 1));

        // ── Event Handlers ─────────────────────────────────────────────────

        private void OnXPGained(float amount) => AddXP(amount);

        private void OnBoonChosen(BoonData boon)
        {
            boon?.Apply(gameObject);
            GameManager.Instance?.ResumefromLevelUp();
            EventBus.Emit<BoonData>("OnBoonApplied", boon);
        }

        #endregion
    }
}
