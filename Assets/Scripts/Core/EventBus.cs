using System;
using System.Collections.Generic;
using UnityEngine;

namespace AGNIDAWN.Core
{
    /// <summary>
    /// Global decoupled event system. Any system can emit or listen without
    /// holding direct references to each other.
    ///
    /// Usage:
    ///   EventBus.On("OnPlayerDied", MyCallback);
    ///   EventBus.Emit("OnPlayerDied");
    ///   EventBus.Off("OnPlayerDied", MyCallback);
    ///
    /// Linear: FAI-6
    /// </summary>
    public static class EventBus
    {
        // ── Storage ───────────────────────────────────────────────────────
        private static readonly Dictionary<string, List<Delegate>> _handlers
            = new Dictionary<string, List<Delegate>>();

        // ──────────────────────────────────────────────────────────────────
        #region Subscribe

        /// Subscribe with no arguments
        public static void On(string eventName, Action handler)
            => AddHandler(eventName, handler);

        /// Subscribe with one typed argument
        public static void On<T>(string eventName, Action<T> handler)
            => AddHandler(eventName, handler);

        /// Subscribe with two typed arguments
        public static void On<T1, T2>(string eventName, Action<T1, T2> handler)
            => AddHandler(eventName, handler);

        /// Subscribe with three typed arguments
        public static void On<T1, T2, T3>(string eventName, Action<T1, T2, T3> handler)
            => AddHandler(eventName, handler);

        #endregion

        // ──────────────────────────────────────────────────────────────────
        #region Unsubscribe

        public static void Off(string eventName, Action handler)
            => RemoveHandler(eventName, handler);

        public static void Off<T>(string eventName, Action<T> handler)
            => RemoveHandler(eventName, handler);

        public static void Off<T1, T2>(string eventName, Action<T1, T2> handler)
            => RemoveHandler(eventName, handler);

        public static void Off<T1, T2, T3>(string eventName, Action<T1, T2, T3> handler)
            => RemoveHandler(eventName, handler);

        #endregion

        // ──────────────────────────────────────────────────────────────────
        #region Emit

        /// Emit with no arguments
        public static void Emit(string eventName)
        {
            if (!_handlers.TryGetValue(eventName, out var list)) return;
            foreach (var d in GetSafeCopy(list))
            {
                try { (d as Action)?.Invoke(); }
                catch (Exception e) { Debug.LogError($"[EventBus] {eventName}: {e}"); }
            }
        }

        /// Emit with one argument
        public static void Emit<T>(string eventName, T arg)
        {
            if (!_handlers.TryGetValue(eventName, out var list)) return;
            foreach (var d in GetSafeCopy(list))
            {
                try { (d as Action<T>)?.Invoke(arg); }
                catch (Exception e) { Debug.LogError($"[EventBus] {eventName}: {e}"); }
            }
        }

        /// Emit with two arguments
        public static void Emit<T1, T2>(string eventName, T1 a1, T2 a2)
        {
            if (!_handlers.TryGetValue(eventName, out var list)) return;
            foreach (var d in GetSafeCopy(list))
            {
                try { (d as Action<T1, T2>)?.Invoke(a1, a2); }
                catch (Exception e) { Debug.LogError($"[EventBus] {eventName}: {e}"); }
            }
        }

        /// Emit with three arguments
        public static void Emit<T1, T2, T3>(string eventName, T1 a1, T2 a2, T3 a3)
        {
            if (!_handlers.TryGetValue(eventName, out var list)) return;
            foreach (var d in GetSafeCopy(list))
            {
                try { (d as Action<T1, T2, T3>)?.Invoke(a1, a2, a3); }
                catch (Exception e) { Debug.LogError($"[EventBus] {eventName}: {e}"); }
            }
        }

        #endregion

        // ──────────────────────────────────────────────────────────────────
        #region Utilities

        /// Remove all listeners for an event (useful on scene unload)
        public static void Clear(string eventName)
        {
            if (_handlers.ContainsKey(eventName))
                _handlers[eventName].Clear();
        }

        /// Wipe all events — call on full scene reload
        public static void ClearAll()
            => _handlers.Clear();

        #endregion

        // ──────────────────────────────────────────────────────────────────
        #region Private Helpers

        private static void AddHandler(string eventName, Delegate handler)
        {
            if (!_handlers.ContainsKey(eventName))
                _handlers[eventName] = new List<Delegate>();

            if (!_handlers[eventName].Contains(handler))
                _handlers[eventName].Add(handler);
        }

        private static void RemoveHandler(string eventName, Delegate handler)
        {
            if (_handlers.TryGetValue(eventName, out var list))
                list.Remove(handler);
        }

        private static List<Delegate> GetSafeCopy(List<Delegate> list)
            => new List<Delegate>(list); // copy so handlers can safely unsub during emit

        #endregion
    }
}
