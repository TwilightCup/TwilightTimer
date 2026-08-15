using System;
using System.Collections.Generic;
using System.Text;

namespace HSRTimer
{
    /// <summary>
    /// Public static entry point for driving HSRTimer externally (T1.6 of the
    /// Twilight Cup requirements) — the debug / direct-consumption surface.
    /// In production TwilightCore drives the timer through its own
    /// ITimerProvider interface (this plugin registers an implementation of
    /// it once TwilightCore ships the contract); this API exists for tooling,
    /// console driving, and acceptance testing, and forwards to the same
    /// internals. Every call is safe before plugin initialization: it returns
    /// a clear not-ready status instead of throwing (T1.4).
    /// </summary>
    public static class HSRTimerApi
    {
        /// <summary>API surface version; increment on breaking changes.</summary>
        public const int ApiVersion = 1;

        /// <summary>True once the plugin finished initializing.</summary>
        public static bool IsReady => TimerCore.Instance != null && ConfigService.Instance != null;

        // ── T2: match mode ──

        public static bool InMatchMode => MatchMode.Active;

        /// <summary>Enter match mode (T2.2). Returns false when not ready.</summary>
        public static bool EnterMatchMode()
        {
            if (!IsReady) return false;
            MatchMode.Enter();
            return true;
        }

        /// <summary>Exit match mode and restore the user snapshot (T2.4).</summary>
        public static bool ExitMatchMode()
        {
            if (!IsReady) return false;
            MatchMode.Exit();
            return true;
        }

        // ── T3: round lifecycle ──

        /// <summary>True between StartRound and StopRound.</summary>
        public static bool InRound => RoundTracker.RoundActive;

        /// <summary>
        /// Start tracking a round (T3.1): full reset of live timing (including
        /// last-segment/last-run snapshots) and all invalid marks, then apply
        /// the pushed tag set. Timing itself still waits for the first
        /// PlayingLevel edge (R1.2) — the call itself never starts the clock.
        /// </summary>
        public static bool StartRound(string roundId, bool isSingleProject, int retryCount, IEnumerable<string> tags)
        {
            if (!IsReady) return false;
            return RoundTracker.StartRound(roundId, isSingleProject, retryCount, tags);
        }

        /// <summary>Stop tracking the round (T3.2); data stays queryable (T3.5).</summary>
        public static bool StopRound()
        {
            if (!IsReady) return false;
            RoundTracker.StopRound();
            return true;
        }

        // ── T5: tag push ──

        /// <summary>
        /// Set the round tag set (T5.2). Only honored in match mode (T5.1);
        /// unknown ids are logged and ignored (T5.5). Effective from the next
        /// segment.
        /// </summary>
        public static bool SetRoundTags(IEnumerable<string> tagIds)
        {
            if (!IsReady) return false;
            return RoundTracker.SetRoundTags(tagIds);
        }

        /// <summary>Human-readable round status for console/debug output.</summary>
        public static string RoundStatusString() => RoundTracker.StatusString();
    }
}
