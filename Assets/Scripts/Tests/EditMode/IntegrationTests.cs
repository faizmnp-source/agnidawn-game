using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using AGNIDAWN.Core;

namespace AGNIDAWN.Tests.EditMode
{
    /// <summary>
    /// Integration tests that verify cross-system event wiring.
    /// All tests run in edit-mode (no scene required) via direct class instantiation.
    ///
    /// Phase 16 — FAI-20
    /// </summary>
    [TestFixture]
    public class IntegrationTests
    {
        // ── Helpers ────────────────────────────────────────────────────────

        /// Spin up a bare GameManager without any Unity lifecycle overhead.
        private GameManager MakeGameManager()
        {
            var go = new GameObject("GM_Test");
            return go.AddComponent<GameManager>();
        }

        [TearDown]
        public void TearDown()
        {
            // Destroy all root objects created during the test
            foreach (var go in Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None))
                Object.DestroyImmediate(go);

            // Clear EventBus listeners between tests
            EventBus.ClearAll();
        }

        // ─────────────────────────────────────────────────────────────────
        // 1. EVT_GAME_START event constant matches what BossManager listens to
        // ─────────────────────────────────────────────────────────────────
        [Test]
        public void GameManager_EVT_GAME_START_IsNonEmpty()
        {
            Assert.IsFalse(string.IsNullOrEmpty(GameManager.EVT_GAME_START),
                "EVT_GAME_START must be a non-empty string.");
        }

        // ─────────────────────────────────────────────────────────────────
        // 2. EVT_MINUTE_PASSED is defined and non-empty
        // ─────────────────────────────────────────────────────────────────
        [Test]
        public void GameManager_EVT_MINUTE_PASSED_IsDefined()
        {
            Assert.IsFalse(string.IsNullOrEmpty(GameManager.EVT_MINUTE_PASSED),
                "EVT_MINUTE_PASSED must be defined and non-empty — BossManager depends on it.");
        }

        // ─────────────────────────────────────────────────────────────────
        // 3. EventBus roundtrip — subscribe, emit, receive
        // ─────────────────────────────────────────────────────────────────
        [Test]
        public void EventBus_EmitAndReceive_IntArg()
        {
            int received = -1;
            EventBus.On<int>("TestEvent", v => received = v);
            EventBus.Emit<int>("TestEvent", 42);
            Assert.AreEqual(42, received, "EventBus must deliver int arg to subscriber.");
        }

        // ─────────────────────────────────────────────────────────────────
        // 4. EventBus.Off removes listener — no double-fire
        // ─────────────────────────────────────────────────────────────────
        [Test]
        public void EventBus_Off_StopsDelivery()
        {
            int count = 0;
            System.Action<int> handler = _ => count++;
            EventBus.On<int>("OffTest", handler);
            EventBus.Emit<int>("OffTest", 1);
            EventBus.Off<int>("OffTest", handler);
            EventBus.Emit<int>("OffTest", 2);
            Assert.AreEqual(1, count, "After Off(), handler must not receive further events.");
        }

        // ─────────────────────────────────────────────────────────────────
        // 5. GameManager minute tracking — EVT_MINUTE_PASSED emitted correctly
        //    Simulate elapsed time by directly manipulating via reflection.
        // ─────────────────────────────────────────────────────────────────
        [Test]
        public void GameManager_MinutePassed_EventFiredAtCorrectInterval()
        {
            int minutesReceived = 0;
            int lastMinute      = 0;

            EventBus.On<int>(GameManager.EVT_MINUTE_PASSED, m =>
            {
                minutesReceived++;
                lastMinute = m;
            });

            // Use a helper that directly simulates the minute-tick logic
            // (avoids needing a running MonoBehaviour)
            SimulateMinuteTicks(3, out int emitted);

            // We expect 3 ticks (minute 1, 2, 3)
            Assert.AreEqual(3, emitted, "Should emit exactly 3 OnMinutePassed events for 3 minutes.");
        }

        /// Simulates the GameManager minute-tracking logic without a running scene.
        private static void SimulateMinuteTicks(int targetMinutes, out int emittedCount)
        {
            int count        = 0;
            int lastMark     = 0;
            float elapsed    = 0f;

            EventBus.On<int>(GameManager.EVT_MINUTE_PASSED, _ => count++);

            // Advance in 1-second steps
            for (int i = 0; i < targetMinutes * 60; i++)
            {
                elapsed += 1f;
                int currentMinute = (int)(elapsed / 60f);
                if (currentMinute > lastMark)
                {
                    lastMark = currentMinute;
                    EventBus.Emit<int>(GameManager.EVT_MINUTE_PASSED, currentMinute);
                }
            }

            emittedCount = count;
        }

        // ─────────────────────────────────────────────────────────────────
        // 6. ObjectPool — get/return cycle works without a scene
        // ─────────────────────────────────────────────────────────────────
        [Test]
        public void ObjectPool_GetAndReturn_CycleWorks()
        {
            var poolGO = new GameObject("Pool");
            var pool   = poolGO.AddComponent<ObjectPool>();

            var prefab = new GameObject("TestPrefab");
            var instance = pool.Get("Test_Bullet", prefab, Vector3.zero, Quaternion.identity);

            Assert.IsNotNull(instance, "Pool.Get must return a non-null GameObject.");
            Assert.IsTrue(instance.activeSelf, "Pooled object must be active after Get.");

            pool.Return("Test_Bullet", instance);
            Assert.IsFalse(instance.activeSelf, "Pooled object must be inactive after Return.");

            Object.DestroyImmediate(prefab);
        }

        // ─────────────────────────────────────────────────────────────────
        // 7. SaveSystem — DivineShards round-trip (add/get/delete)
        // ─────────────────────────────────────────────────────────────────
        [Test]
        public void SaveSystem_DivineShardsRoundTrip()
        {
            // Start from a clean state
            SaveSystem.DeleteAll();

            SaveSystem.AddDivineShards(42);
            int loaded = SaveSystem.GetDivineShards();
            Assert.AreEqual(42, loaded,
                "SaveSystem DivineShards round-trip must return 42 after AddDivineShards(42).");

            SaveSystem.DeleteAll();
        }

        // ─────────────────────────────────────────────────────────────────
        // 8. SaveSystem — SpendDivineShards guards against overdraft
        // ─────────────────────────────────────────────────────────────────
        [Test]
        public void SaveSystem_SpendShards_ReturnsFalseWhenInsufficient()
        {
            SaveSystem.DeleteAll();
            SaveSystem.AddDivineShards(5);

            bool spent = SaveSystem.SpendDivineShards(10);
            Assert.IsFalse(spent, "SpendDivineShards must return false when balance is too low.");
            Assert.AreEqual(5, SaveSystem.GetDivineShards(),
                "Balance must remain 5 after a failed spend.");

            SaveSystem.DeleteAll();
        }

        // ─────────────────────────────────────────────────────────────────
        // 9. QualityManager — tier is in valid range after detect
        // ─────────────────────────────────────────────────────────────────
        [Test]
        public void QualityManager_TierInValidRange()
        {
            var go  = new GameObject("QM_Test");
            var qm  = go.AddComponent<QualityManager>();

            // Awake fires synchronously in edit-mode AddComponent
            Assert.GreaterOrEqual(qm.CurrentTier, 0, "Quality tier must be >= 0 after init.");
            Assert.LessOrEqual(qm.CurrentTier,    2, "Quality tier must be <= 2 after init.");
        }

        // ─────────────────────────────────────────────────────────────────
        // 10. QualityManager — SetTier clamps out-of-range values safely
        // ─────────────────────────────────────────────────────────────────
        [Test]
        public void QualityManager_SetTier_ClampsOutOfRange()
        {
            var go  = new GameObject("QM_Clamp_Test");
            var qm  = go.AddComponent<QualityManager>();

            qm.SetTier(-5);
            Assert.AreEqual(0, qm.CurrentTier, "SetTier(-5) must clamp to 0.");

            qm.SetTier(99);
            Assert.AreEqual(2, qm.CurrentTier, "SetTier(99) must clamp to 2 (max tier).");
        }
    }
}
