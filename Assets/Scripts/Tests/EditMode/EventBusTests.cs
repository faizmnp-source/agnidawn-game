using NUnit.Framework;
using AGNIDAWN.Core;

namespace AGNIDAWN.Tests.EditMode
{
    /// <summary>
    /// EditMode unit tests for EventBus.
    /// Verifies subscribe, emit, unsubscribe, and multi-arg overloads.
    /// Linear: FAI-6
    /// </summary>
    public class EventBusTests
    {
        [SetUp]
        public void SetUp() => EventBus.ClearAll();

        // ── Subscribe / Emit ──────────────────────────────────────────────

        [Test]
        public void Emit_NoArgs_CallsHandler()
        {
            bool called = false;
            EventBus.On("TestEvent", () => called = true);
            EventBus.Emit("TestEvent");
            Assert.IsTrue(called, "Handler should be invoked on Emit.");
        }

        [Test]
        public void Emit_OneArg_PassesValueCorrectly()
        {
            int received = 0;
            EventBus.On<int>("TestInt", v => received = v);
            EventBus.Emit("TestInt", 42);
            Assert.AreEqual(42, received);
        }

        [Test]
        public void Emit_TwoArgs_PassesBothValues()
        {
            string r1 = null; int r2 = 0;
            EventBus.On<string, int>("TwoArg", (a, b) => { r1 = a; r2 = b; });
            EventBus.Emit("TwoArg", "hello", 7);
            Assert.AreEqual("hello", r1);
            Assert.AreEqual(7, r2);
        }

        [Test]
        public void Emit_NoListeners_DoesNotThrow()
        {
            Assert.DoesNotThrow(() => EventBus.Emit("NoListenerEvent"));
        }

        [Test]
        public void Emit_MultipleListeners_AllCalled()
        {
            int count = 0;
            EventBus.On("Multi", () => count++);
            EventBus.On("Multi", () => count++);
            EventBus.On("Multi", () => count++);
            EventBus.Emit("Multi");
            Assert.AreEqual(3, count);
        }

        // ── Unsubscribe ───────────────────────────────────────────────────

        [Test]
        public void Off_RemovesHandler_NotCalledAfterOff()
        {
            int count = 0;
            System.Action handler = () => count++;
            EventBus.On("OffTest", handler);
            EventBus.Emit("OffTest");
            EventBus.Off("OffTest", handler);
            EventBus.Emit("OffTest");
            Assert.AreEqual(1, count, "Handler should only fire once, before Off.");
        }

        [Test]
        public void DuplicateSubscribe_OnlyRegistersOnce()
        {
            int count = 0;
            System.Action handler = () => count++;
            EventBus.On("DupTest", handler);
            EventBus.On("DupTest", handler); // duplicate
            EventBus.Emit("DupTest");
            Assert.AreEqual(1, count, "Duplicate subscription should be ignored.");
        }

        // ── Clear ─────────────────────────────────────────────────────────

        [Test]
        public void Clear_RemovesAllHandlersForEvent()
        {
            int count = 0;
            EventBus.On("ClearTest", () => count++);
            EventBus.On("ClearTest", () => count++);
            EventBus.Clear("ClearTest");
            EventBus.Emit("ClearTest");
            Assert.AreEqual(0, count);
        }

        [Test]
        public void ClearAll_RemovesEverything()
        {
            int count = 0;
            EventBus.On("A", () => count++);
            EventBus.On("B", () => count++);
            EventBus.ClearAll();
            EventBus.Emit("A");
            EventBus.Emit("B");
            Assert.AreEqual(0, count);
        }

        // ── Safety ────────────────────────────────────────────────────────

        [Test]
        public void Emit_HandlerUnsubscribesDuringSelf_DoesNotThrow()
        {
            // Handler that removes itself during emit — safe-copy should handle this
            System.Action handler = null;
            handler = () => EventBus.Off("SelfUnsub", handler);
            EventBus.On("SelfUnsub", handler);
            Assert.DoesNotThrow(() => EventBus.Emit("SelfUnsub"));
        }

        [Test]
        public void Emit_ThrowingHandler_DoesNotPreventOtherHandlers()
        {
            int count = 0;
            EventBus.On("ThrowTest", () => throw new System.Exception("test error"));
            EventBus.On("ThrowTest", () => count++);
            // Should not throw — EventBus catches per-handler exceptions
            Assert.DoesNotThrow(() => EventBus.Emit("ThrowTest"));
            Assert.AreEqual(1, count, "Second handler should still fire despite first throwing.");
        }
    }
}
