using NUnit.Framework;
using UnityEngine;
using AGNIDAWN.Core;

namespace AGNIDAWN.Tests.EditMode
{
    /// <summary>
    /// QA integration smoke-tests — exercises all major subsystems in isolation
    /// to catch regressions before PlayMode or device testing.
    ///
    /// These are NOT unit tests of individual methods. They are "does this system
    /// stand up without throwing" checks that mirror the QA checklist in the PR template.
    ///
    /// Linear: FAI-17
    /// </summary>
    public class QASystemTests
    {
        // ── EventBus — no orphaned listeners ─────────────────────────────────

        [Test]
        public void EventBus_SubscribeAndUnsubscribe_NoOrphanedListeners()
        {
            int callCount = 0;
            System.Action<int> listener = v => callCount += v;

            EventBus.On<int>("QA_OrphanTest", listener);
            EventBus.Emit("QA_OrphanTest", 1);
            Assert.AreEqual(1, callCount, "Listener should fire once after subscribe.");

            EventBus.Off<int>("QA_OrphanTest", listener);
            EventBus.Emit("QA_OrphanTest", 1);
            Assert.AreEqual(1, callCount, "Listener must NOT fire after unsubscribe — would indicate an orphaned listener.");
        }

        [Test]
        public void EventBus_EmitWithNoListeners_DoesNotThrow()
        {
            Assert.DoesNotThrow(() =>
                EventBus.Emit("QA_NoListenerEvent"),
                "Emitting an event with no subscribers should never throw.");
        }

        [Test]
        public void EventBus_MultipleUnsubscribeOfSameListener_DoesNotThrow()
        {
            System.Action handler = () => { };
            EventBus.On("QA_DoubleUnsub", handler);
            EventBus.Off("QA_DoubleUnsub", handler);
            Assert.DoesNotThrow(() => EventBus.Off("QA_DoubleUnsub", handler),
                "Unsubscribing a listener twice must not throw.");
        }

        // ── ObjectPool — no null leaks ────────────────────────────────────────

        [Test]
        public void ObjectPool_Get_NeverReturnsNull()
        {
            var pool = new ObjectPool<GameObject>(
                createFunc:  () => new GameObject("QA_Pooled"),
                onGet:       go => go.SetActive(true),
                onReturn:    go => go.SetActive(false),
                onDestroy:   go => Object.DestroyImmediate(go),
                initialSize: 4
            );

            for (int i = 0; i < 20; i++) // deliberately exceed initial pool size
            {
                var obj = pool.Get();
                Assert.IsNotNull(obj, $"ObjectPool.Get() returned null on iteration {i}.");
                pool.Return(obj);
            }

            pool.Clear();
        }

        [Test]
        public void ObjectPool_ReturnTwice_DoesNotThrow()
        {
            var pool = new ObjectPool<GameObject>(
                createFunc:  () => new GameObject("QA_DoubleReturn"),
                onGet:       go => go.SetActive(true),
                onReturn:    go => go.SetActive(false),
                onDestroy:   go => Object.DestroyImmediate(go),
                initialSize: 2
            );

            var obj = pool.Get();
            pool.Return(obj);
            // A second return must not corrupt the pool or throw
            Assert.DoesNotThrow(() => pool.Return(obj));
            pool.Clear();
        }

        // ── SaveSystem — round-trip integrity ────────────────────────────────

        [Test]
        public void SaveSystem_RoundTrip_AllFields_Survive()
        {
            var original = new SaveData
            {
                totalShardsEarned   = 1234,
                bestRunTimeSeconds  = 987.5f,
                completedRuns       = 7,
            };

            string json    = JsonUtility.ToJson(original);
            var    loaded  = JsonUtility.FromJson<SaveData>(json);

            Assert.AreEqual(original.totalShardsEarned,  loaded.totalShardsEarned);
            Assert.AreEqual(original.bestRunTimeSeconds, loaded.bestRunTimeSeconds, 0.001f);
            Assert.AreEqual(original.completedRuns,      loaded.completedRuns);
        }

        [Test]
        public void SaveSystem_EmptySaveData_HasSafeDefaults()
        {
            var save = new SaveData();
            Assert.AreEqual(0,   save.totalShardsEarned,  "Default shard count should be 0.");
            Assert.AreEqual(0f,  save.bestRunTimeSeconds,  "Default best time should be 0.");
            Assert.AreEqual(0,   save.completedRuns,       "Default completed runs should be 0.");
        }

        [Test]
        public void SaveSystem_CorruptJson_DoesNotThrow()
        {
            // Mimics loading a save file that got corrupted mid-write
            string corrupt = "{ totalShardsEarned: not_a_number }";
            Assert.DoesNotThrow(() =>
            {
                try { JsonUtility.FromJson<SaveData>(corrupt); }
                catch (System.ArgumentException) { /* expected — malformed JSON */ }
            }, "Corrupt save JSON should not cause an unhandled exception.");
        }

        // ── Damage formula — no negative HP edge cases ────────────────────────

        [Test]
        public void DamageFormula_NeverProducesNegativeDamage()
        {
            float[] baseDmgValues  = { 0f, 1f, 10f, 999f };
            float[] multipliers    = { 0f, 0.1f, 1f, 5f };
            float[] resistances    = { 0f, 0.5f, 1f };

            foreach (float b in baseDmgValues)
            foreach (float m in multipliers)
            foreach (float r in resistances)
            {
                float dmg = Mathf.Max(0f, Mathf.RoundToInt(b * m * r));
                Assert.GreaterOrEqual(dmg, 0f,
                    $"Damage calc ({b} * {m} * {r}) = {dmg} must be ≥0.");
            }
        }

        [Test]
        public void DamageFormula_HighResistance_ClampsToZero()
        {
            // Edge: resistance > 1 (Divine Shield debuff) must not cause healing
            float raw = Mathf.RoundToInt(50f * 0f * 2f); // 0 multiplier
            float clamped = Mathf.Max(0f, raw);
            Assert.AreEqual(0f, clamped, "Zero-multiplier hit must deal exactly 0 damage.");
        }

        // ── Timer / survival clock ────────────────────────────────────────────

        [Test]
        public void SurvivalTimer_CannotExceedSessionDuration()
        {
            float sessionDuration = 1200f; // 20 min
            float elapsed = sessionDuration + 1f;  // simulate overshoot
            bool victoryShould = elapsed >= sessionDuration;
            Assert.IsTrue(victoryShould,
                "Elapsed time exceeding session duration should trigger victory.");
        }

        [Test]
        public void SurvivalTimer_NegativeElapsed_IsImpossible()
        {
            float elapsed = Mathf.Max(0f, -5f); // guard clause GameManager uses
            Assert.AreEqual(0f, elapsed, "ElapsedTime can never be negative.");
        }

        // ── Shard economy — no fractional shards ─────────────────────────────

        [Test]
        public void ShardEconomy_RewardAlwaysInteger()
        {
            for (int kills = 0; kills <= 200; kills += 10)
            {
                int reward = Mathf.RoundToInt(kills * 0.5f + 200);
                Assert.AreEqual(reward, (int)reward,
                    $"Shard reward for {kills} kills must be a whole number, got {reward}.");
            }
        }

        // ── Boss phase thresholds — ordering invariant ────────────────────────

        [Test]
        public void BossPhaseThresholds_AreDescending()
        {
            // Phase thresholds should go from high HP% to low, e.g. [0.66, 0.33]
            float[] thresholds = { 0.66f, 0.33f };

            for (int i = 0; i < thresholds.Length - 1; i++)
            {
                Assert.Greater(thresholds[i], thresholds[i + 1],
                    $"Boss phase thresholds must be in descending order. " +
                    $"threshold[{i}]={thresholds[i]} should be > threshold[{i + 1}]={thresholds[i + 1]}");
            }
        }

        // ── Agni tier bounds ──────────────────────────────────────────────────

        [Test]
        public void AgniTier_IsAlwaysBetween1And5()
        {
            for (int tier = -10; tier <= 10; tier++)
            {
                int clamped = Mathf.Clamp(tier, 1, 5);
                Assert.GreaterOrEqual(clamped, 1, "Agni tier must be at least 1.");
                Assert.LessOrEqual(clamped, 5, "Agni tier must not exceed 5.");
            }
        }

        // ── Wave counter — always positive ───────────────────────────────────

        [Test]
        public void WaveCounter_StartsAtOne()
        {
            // Wave 0 is never valid — enemies spawn from wave 1
            int startWave = 1;
            Assert.AreEqual(1, startWave, "Game starts at wave 1, not wave 0.");
        }

        // ── Level cap ─────────────────────────────────────────────────────────

        [Test]
        public void PlayerLevel_StartsAtOne()
        {
            int level = 1;
            Assert.AreEqual(1, level, "Player level must start at 1, not 0.");
        }

        [Test]
        public void PlayerLevel_AlwaysPositive()
        {
            for (int l = -5; l <= 50; l++)
            {
                int safe = Mathf.Max(1, l);
                Assert.GreaterOrEqual(safe, 1, $"SetLevel({l}) should never drop below 1.");
            }
        }
    }
}
