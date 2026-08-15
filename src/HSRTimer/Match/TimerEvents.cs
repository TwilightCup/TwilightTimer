using System;

namespace HSRTimer
{
    /// <summary>
    /// Outbound timer events for external consumers (T4.1–T4.7). Raised from
    /// the engine tick (main thread); every subscriber delegate is invoked
    /// individually inside try/catch so a throwing subscriber (TwilightCore
    /// or any debug tool) can never interrupt the timing engine (T4.7).
    /// These are the plugin-internal events; the ITimerProvider adapter
    /// (Phase B) re-exposes them over TwilightCore's interface.
    /// </summary>
    public static class TimerEvents
    {
        /// <summary>T4.1 — a level was PASSED. (index, segment ms, round total ms).</summary>
        public static event Action<int, long, long> SegmentCompleted;

        /// <summary>T4.2 — SINGLE attempt abandoned for the next one. (attempt index).</summary>
        public static event Action<int> AttemptSkipped;

        /// <summary>T4.3 — whole collection finished. (game-time total ms).</summary>
        public static event Action<long> RunCompleted;

        /// <summary>T4.4 — unpassed exit with no further segment this round. (segment index).</summary>
        public static event Action<int> IncompleteExit;

        /// <summary>T4.6 — a NEW invalid reason was raised mid-round. (reason, unforgivable).</summary>
        public static event Action<InvalidReason, bool> InvalidMarked;

        internal static void RaiseSegmentCompleted(int index, long durationMs, long totalMs)
            => InvokeAll(SegmentCompleted, index, durationMs, totalMs);

        internal static void RaiseAttemptSkipped(int index)
            => InvokeAll(AttemptSkipped, index);

        internal static void RaiseRunCompleted(long totalMs)
            => InvokeAll(RunCompleted, totalMs);

        internal static void RaiseIncompleteExit(int index)
            => InvokeAll(IncompleteExit, index);

        internal static void RaiseInvalidMarked(InvalidReason reason, bool unforgivable)
            => InvokeAll(InvalidMarked, reason, unforgivable);

        private static void InvokeAll<T1>(Action<T1> evt, T1 a1)
        {
            if (evt == null) return;
            foreach (Action<T1> h in evt.GetInvocationList())
            {
                try { h(a1); }
                catch (Exception ex)
                {
                    Plugin.Logger.LogWarning($"HSRTimer: event subscriber threw: {ex.Message}");
                }
            }
        }

        private static void InvokeAll<T1, T2>(Action<T1, T2> evt, T1 a1, T2 a2)
        {
            if (evt == null) return;
            foreach (Action<T1, T2> h in evt.GetInvocationList())
            {
                try { h(a1, a2); }
                catch (Exception ex)
                {
                    Plugin.Logger.LogWarning($"HSRTimer: event subscriber threw: {ex.Message}");
                }
            }
        }

        private static void InvokeAll<T1, T2, T3>(Action<T1, T2, T3> evt, T1 a1, T2 a2, T3 a3)
        {
            if (evt == null) return;
            foreach (Action<T1, T2, T3> h in evt.GetInvocationList())
            {
                try { h(a1, a2, a3); }
                catch (Exception ex)
                {
                    Plugin.Logger.LogWarning($"HSRTimer: event subscriber threw: {ex.Message}");
                }
            }
        }
    }
}
