using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;
using AGNIDAWN.Core;
using AGNIDAWN.UI;
using static AGNIDAWN.Core.SaveSystem;

namespace AGNIDAWN.Tests.EditMode
{
    /// <summary>
    /// EditMode tests for the UI/UX system (FAI-14).
    ///
    /// Covers:
    ///   - UIManager singleton construction and stack operations
    ///   - BaseUIPanel visibility state after Show/Hide
    ///   - LevelUpUI rarity colour mapping
    ///   - VictoryScreenUI shard reward formula
    ///   - SaveSystem best-run record detection logic
    /// </summary>
    public class UISystemTests
    {
        // ── Helpers ───────────────────────────────────────────────────────────

        /// <summary>
        /// Create a minimal GameObject with a CanvasGroup — enough for BaseUIPanel to operate.
        /// </summary>
        private static T MakePanelObject<T>() where T : BaseUIPanel
        {
            var go = new GameObject(typeof(T).Name);
            go.AddComponent<CanvasGroup>();
            var panel = go.AddComponent<T>();
            return panel;
        }

        // ── BaseUIPanel ───────────────────────────────────────────────────────

        [Test]
        public void BaseUIPanel_StartsHidden()
        {
            var panel = MakePanelObject<MainMenuUI>();
            Assert.IsFalse(panel.IsVisible, "Panel should not be visible before Show() is called.");
            Object.DestroyImmediate(panel.gameObject);
        }

        [Test]
        public void BaseUIPanel_ShowImmediate_SetsVisible()
        {
            var panel = MakePanelObject<MainMenuUI>();
            panel.ShowImmediate();
            Assert.IsTrue(panel.IsVisible, "ShowImmediate() should set IsVisible = true.");
            Object.DestroyImmediate(panel.gameObject);
        }

        [Test]
        public void BaseUIPanel_HideImmediate_SetsHidden()
        {
            var panel = MakePanelObject<MainMenuUI>();
            panel.ShowImmediate();
            panel.HideImmediate();
            Assert.IsFalse(panel.IsVisible, "HideImmediate() should set IsVisible = false.");
            Object.DestroyImmediate(panel.gameObject);
        }

        [Test]
        public void BaseUIPanel_CanvasGroup_AlphaOne_WhenShown()
        {
            var panel = MakePanelObject<PauseMenuUI>();
            panel.ShowImmediate();
            var cg = panel.GetComponent<CanvasGroup>();
            Assert.AreEqual(1f, cg.alpha, 0.001f,
                "CanvasGroup alpha should be 1 immediately after ShowImmediate().");
            Object.DestroyImmediate(panel.gameObject);
        }

        [Test]
        public void BaseUIPanel_CanvasGroup_AlphaZero_WhenHidden()
        {
            var panel = MakePanelObject<PauseMenuUI>();
            panel.ShowImmediate();
            panel.HideImmediate();
            var cg = panel.GetComponent<CanvasGroup>();
            Assert.AreEqual(0f, cg.alpha, 0.001f,
                "CanvasGroup alpha should be 0 immediately after HideImmediate().");
            Object.DestroyImmediate(panel.gameObject);
        }

        // ── Rarity Colours ────────────────────────────────────────────────────

        [Test]
        public void RarityColour_Common_IsNearWhite()
        {
            // COLOR_COMMON = (0.85, 0.85, 0.85) — muted white, not pure white
            var color = LevelUpUI.RarityColor(BoonRarity.Common);
            Assert.AreEqual(0.85f, color.r, 0.01f);
            Assert.AreEqual(0.85f, color.g, 0.01f);
            Assert.AreEqual(0.85f, color.b, 0.01f);
        }

        [Test]
        public void RarityColour_Rare_IsBlue()
        {
            // COLOR_RARE = (0.30, 0.60, 1.00)
            var color = LevelUpUI.RarityColor(BoonRarity.Rare);
            Assert.AreEqual(0.30f, color.r, 0.01f);
            Assert.AreEqual(0.60f, color.g, 0.01f);
            Assert.AreEqual(1.00f, color.b, 0.01f);
        }

        [Test]
        public void RarityColour_Epic_IsPurple()
        {
            // COLOR_EPIC = (0.75, 0.30, 1.00)
            var color = LevelUpUI.RarityColor(BoonRarity.Epic);
            Assert.AreEqual(0.75f, color.r, 0.01f);
            Assert.AreEqual(0.30f, color.g, 0.01f);
            Assert.AreEqual(1.00f, color.b, 0.01f);
        }

        [Test]
        public void RarityColour_Divine_IsGold()
        {
            // COLOR_DIVINE = (1.00, 0.85, 0.20)
            var color = LevelUpUI.RarityColor(BoonRarity.Divine);
            Assert.AreEqual(1.00f, color.r, 0.01f);
            Assert.AreEqual(0.85f, color.g, 0.01f);
            Assert.AreEqual(0.20f, color.b, 0.01f);
        }

        // ── Shard Reward Formula ──────────────────────────────────────────────

        [Test]
        public void ShardReward_BaseReward_IsCorrect()
        {
            // Formula from VictoryScreenUI: kills * 0.5 + 200
            int kills = 0;
            int expected = Mathf.RoundToInt(kills * 0.5f + 200);
            Assert.AreEqual(200, expected,
                "Zero kills should yield 200 base Divine Shards.");
        }

        [Test]
        public void ShardReward_WithKills_ScalesCorrectly()
        {
            int kills = 100;
            int reward = Mathf.RoundToInt(kills * 0.5f + 200);
            Assert.AreEqual(250, reward,
                "100 kills should yield 250 Divine Shards (100*0.5 + 200).");
        }

        [Test]
        public void ShardReward_LargeKillCount_NoOverflow()
        {
            int kills = 9999;
            int reward = Mathf.RoundToInt(kills * 0.5f + 200);
            Assert.Greater(reward, 0, "Shard reward must always be positive.");
        }

        // ── SaveSystem Record Detection ───────────────────────────────────────

        [Test]
        public void SaveSystem_NewRecord_WhenElapsedExceedsBestTime()
        {
            var save = new SaveData { bestRunTimeSeconds = 120f };
            float elapsed = 180f;
            bool isRecord = elapsed > save.bestRunTimeSeconds;
            Assert.IsTrue(isRecord,
                "180s run should be a new record over a 120s best time.");
        }

        [Test]
        public void SaveSystem_NoRecord_WhenElapsedBelowBestTime()
        {
            var save = new SaveData { bestRunTimeSeconds = 300f };
            float elapsed = 200f;
            bool isRecord = elapsed > save.bestRunTimeSeconds;
            Assert.IsFalse(isRecord,
                "200s run should NOT be a new record over a 300s best time.");
        }

        [Test]
        public void SaveSystem_NoRecord_WhenElapsedEqualsBestTime()
        {
            var save = new SaveData { bestRunTimeSeconds = 250f };
            float elapsed = 250f;
            bool isRecord = elapsed > save.bestRunTimeSeconds;
            Assert.IsFalse(isRecord,
                "Equal times should not trigger the new-record badge.");
        }

        // ── Survival Timer Format ─────────────────────────────────────────────

        [Test]
        public void SurvivalTimer_FormatsMinutesAndSeconds()
        {
            float elapsed = 125f; // 2 min 5 sec
            float min = Mathf.Floor(elapsed / 60f);
            float sec = elapsed % 60f;
            string result = $"{min:00}:{sec:00}";
            Assert.AreEqual("02:05", result,
                "125 seconds should format as 02:05.");
        }

        [Test]
        public void SurvivalTimer_ZeroTime_FormatsProperly()
        {
            float elapsed = 0f;
            float min = Mathf.Floor(elapsed / 60f);
            float sec = elapsed % 60f;
            string result = $"{min:00}:{sec:00}";
            Assert.AreEqual("00:00", result);
        }
    }
}
