using System;
using System.Collections.Generic;

namespace HSRTimer
{
    /// <summary>
    /// Main-thread marshaling for external callers (T1.4). TwilightCore may
    /// invoke timer mutations from its WebSocket thread; the ITimerProvider
    /// adapter (Phase B) enqueues them here and TimerCore.Update drains the
    /// queue on the Unity main thread. Empty-queue cost is one lock-free
    /// count check.
    /// </summary>
    public static class MainThreadQueue
    {
        private static readonly object _lock = new object();
        private static readonly Queue<Action> _queue = new Queue<Action>();

        /// <summary>Enqueue an action for main-thread execution (thread-safe).</summary>
        public static void Enqueue(Action action)
        {
            if (action == null) return;
            lock (_lock) _queue.Enqueue(action);
        }

        /// <summary>
        /// Run every queued action. Called from TimerCore.Update; executes
        /// outside the lock, one try/catch per item so a bad action cannot
        /// stall the drain.
        /// </summary>
        public static void Drain()
        {
            while (true)
            {
                Action action;
                lock (_lock)
                {
                    if (_queue.Count == 0) return;
                    action = _queue.Dequeue();
                }
                try { action(); }
                catch (Exception ex)
                {
                    Plugin.Logger.LogWarning($"HSRTimer: main-thread queue action threw: {ex.Message}");
                }
            }
        }
    }
}
