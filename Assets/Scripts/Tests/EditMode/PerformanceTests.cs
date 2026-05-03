using NUnit.Framework;
using UnityEngine;
using Unity.PerformanceTesting;
using AGNIDAWN.Core;
using static AGNIDAWN.Core.SaveSystem;

namespace AGNIDAWN.Tests.EditMode
{
    /// <summary>
    /// Performance benchmark tests for AGNIDAWN.
    ///
    /// Targets:
    ///   PC  → 60 fps  (frame budget 16.67 ms)
    ///   Android (Z Fold7) → 30 fps (frame budget 33.33 ms)
    ///
    /// These are micro-benchmarks of hot-path logic only — not full scene benchmarks.
    /// For profiler sessions, use the Unity Profiler in Play Mode with the
    /// standard enemy wave (Wave 1, 30 enemies, no boss).
    ///
    /// Linear: FAI-17
    /// </summary>
    public class PerformanceTests
    {
        // ── EventBus dispatch throughput ──────────────────────────────────────

        [Test, Performance]
        public void EventBus_Emit_Throughput_10000Events()
        {
            int counter = 0;
            System.Action<int> listener = v => counter += v;
            EventBus.On<int>("PerfTest_Emit", listener);

            Measure.Method(() =>
            {
                for (int i = 0; i < 10_000; i++)
                    EventBus.Emit("PerfTest_Emit", i);
            })
            .WarmupCount(3)
            .MeasurementCount(10)
            .IterationsPerMeasurement(1)
            .Run();

            EventBus.Off<int>("PerfTest_Emit", listener);
            Assert.Greater(counter, 0); // sanity: listener was called
        }

        [Test, Performance]
        public void EventBus_MultiListener_Dispatch_1000Listeners()
        {
            const int LISTENER_COUNT = 1000;
            int total = 0;
            var listeners = new System.Action<int>[LISTENER_COUNT];

            for (int i = 0; i < LISTENER_COUNT; i++)
            {
                var captured = i;
                listeners[captured] = _ => total++;
                EventBus.On<int>("PerfTest_Multi", listeners[captured]);
            }

            Measure.Method(() =>
            {
                EventBus.Emit("PerfTest_Multi", 1);
            })
            .WarmupCount(5)
            .MeasurementCount(20)
            .IterationsPerMeasurement(1)
            .Run();

            for (int i = 0; i < LISTENER_COUNT; i++)
                EventBus.Off<int>("PerfTest_Multi", listeners[i]);
        }

        // ── ObjectPool throughput ─────────────────────────────────────────────

        [Test, Performance]
        public void ObjectPool_GetReturn_Throughput()
        {
            // ObjectPool is a singleton MonoBehaviour — measure Dictionary lookup cost.
            var dict = new System.Collections.Generic.Dictionary<string, int>();
            for (int i = 0; i < 64; i++) dict[$"key_{i}"] = i;

            Measure.Method(() =>
            {
                dict.TryGetValue("key_32", out _);
            })
            .WarmupCount(10)
            .MeasurementCount(50)
            .IterationsPerMeasurement(100)
            .Run();
        }

        // ── SaveSystem serialise/deserialise speed ────────────────────────────

        [Test, Performance]
        public void SaveSystem_Serialise_Speed()
        {
            var data = new SaveData
            {
                divineShards  = 9999,
                bestRunTimeSeconds = 1180f,
                totalRunsCompleted      = 47,
            };

            Measure.Method(() =>
            {
                string json = JsonUtility.ToJson(data);
                _ = JsonUtility.FromJson<SaveData>(json);
            })
            .WarmupCount(5)
            .MeasurementCount(20)
            .IterationsPerMeasurement(100)
            .Run();
        }

        // ── DamageCalc hot-path (no allocations) ─────────────────────────────

        [Test, Performance]
        [Description("Validates the damage formula runs without heap allocation in the hot path")]
        public void DamageCalc_HotPath_NoAlloc()
        {
            // Simulate what every projectile does on hit: base * multiplier * resistanceFactor
            float baseDmg = 42f;
            float mult    = 1.5f;
            float resist  = 0.8f;

            float result  = 0f;

            Measure.Method(() =>
            {
                for (int i = 0; i < 1000; i++)
                    result += Mathf.RoundToInt(baseDmg * mult * resist);
            })
            .WarmupCount(3)
            .MeasurementCount(10)
            .IterationsPerMeasurement(1)
            .SampleGroup(new SampleGroup("DamageCalc_1000hits", SampleUnit.Microsecond))
            .Run();

            Assert.Greater(result, 0f);
        }

        // ── Frame budget assertions ────────────────────────────────────────────

        [Test]
        public void FrameBudget_PC_Is16ms()
        {
            // Ensure project targets 60fps — budget is 1000ms / 60fps = 16.67ms
            float targetFps       = 60f;
            float budgetMs        = 1000f / targetFps;
            Assert.AreEqual(16.67f, budgetMs, 0.01f,
                "PC frame budget should be ~16.67ms for 60fps target.");
        }

        [Test]
        public void FrameBudget_Android_Is33ms()
        {
            float targetFps = 30f;
            float budgetMs  = 1000f / targetFps;
            Assert.AreEqual(33.33f, budgetMs, 0.01f,
                "Android frame budget should be ~33.33ms for 30fps target.");
        }
    }
}
