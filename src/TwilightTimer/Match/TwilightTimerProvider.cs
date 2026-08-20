using System;
using System.Collections.Generic;
using TwilightCore.Timer;

namespace TwilightTimer
{
    /// <summary>
    /// ITimerProvider adapter (T1, Phase B): implements TwilightCore's public
    /// timer-provider contract on top of the internal match machinery
    /// (MatchMode / RoundTracker / TimerEvents) and is registered with
    /// <see cref="TimerProviderRegistry"/> from Plugin.Awake — the dependency
    /// stays one-way (TwilightTimer → TwilightCore), TwilightCore only ever sees
    /// the interface.
    ///
    /// Mutating calls may arrive from TwilightCore's WebSocket callback
    /// thread (per contract we must not assume main-thread dispatch), so they
    /// are marshaled through <see cref="MainThreadQueue"/> and executed on
    /// the Unity main thread. Queries read point-in-time snapshots and never
    /// throw. Internal TimerEvents are re-exposed as the interface's events;
    /// the per-subscriber try/catch lives in TimerEvents (T4.7).
    /// </summary>
    public sealed class TwilightTimerProvider : ITimerProvider, IResumableTimerProvider
    {
        public static TwilightTimerProvider Instance { get; private set; }

        public int ApiVersion => 1;

        private TwilightTimerProvider()
        {
            // Bridge the internal events (raised on the main thread by the
            // engine tick) to the interface events.
            TimerEvents.SegmentCompleted += (i, dur, total) =>
                SegmentCompleted?.Invoke(new SegmentResult
                {
                    Index = i, DurationMs = dur, TotalMs = total, Passed = true,
                });
            TimerEvents.AttemptSkipped += i =>
                AttemptSkipped?.Invoke(i);
            TimerEvents.RunCompleted += total =>
                RunCompleted?.Invoke(total);
            TimerEvents.IncompleteExit += i =>
                IncompleteExit?.Invoke(i);
            TimerEvents.InvalidMarked += (reason, unforgivable) =>
                InvalidMarked?.Invoke(new TwilightCore.Timer.InvalidMarkInfo
                {
                    Reason = reason.ToString(),
                    Unforgivable = unforgivable,
                });
        }

        /// <summary>Create the singleton and register it with TwilightCore.</summary>
        public static void Register()
        {
            if (Instance != null) return;
            Instance = new TwilightTimerProvider();
            TimerProviderRegistry.Register(Instance);
        }

        /// <summary>Unregister (only if still the current provider).</summary>
        public static void Unregister()
        {
            if (Instance == null) return;
            TimerProviderRegistry.Unregister(Instance);
            Instance = null;
        }

        // ── T2: match mode ───────────────────────────────────────────

        public bool InMatchMode => MatchMode.Active;

        public void EnterMatchMode()
            => MainThreadQueue.Enqueue(MatchMode.Enter);

        public void ExitMatchMode()
            => MainThreadQueue.Enqueue(MatchMode.Exit);

        // ── T3: round lifecycle ──────────────────────────────────────

        public bool InRound => RoundTracker.RoundActive;

        public void StartRound(string roundId, RoundPickInfo pick)
        {
            MainThreadQueue.Enqueue(() =>
            {
                // Full reset first, then the tag push: StartRound itself
                // applies an empty tag set, and the separate SetRoundTags
                // call TwilightCore makes right after re-applies the pick's
                // tags. (RoundTracker.StartRound takes an inline tag list for
                // the TwilightTimerApi debug surface; via the interface the two
                // calls arrive separately, and both orders converge to the
                // same end state — tags applied before the first segment.)
                RoundTracker.StartRound(
                    roundId,
                    pick != null && pick.ProjectType == RoundProjectType.Single,
                    pick != null ? pick.RetryCount : 0,
                    pick != null ? pick.Tags : null);
            });
        }

        public void StopRound()
            => MainThreadQueue.Enqueue(RoundTracker.StopRound);

        public void ResumeRound(string roundId, RoundPickInfo pick)
            => MainThreadQueue.Enqueue(() =>
                RoundTracker.ResumeRound(
                    roundId,
                    pick != null && pick.ProjectType == RoundProjectType.Single,
                    pick != null ? pick.RetryCount : 0,
                    pick != null ? pick.Tags : null));

        // ── T5: tag push ─────────────────────────────────────────────

        public void SetRoundTags(IList<string> tagIds)
            => MainThreadQueue.Enqueue(() => RoundTracker.SetRoundTags(tagIds));

        // ── T4.5: queries (point-in-time snapshots, never throw) ─────

        public bool IsInSegment => RoundTracker.IsInSegment;

        public long CurrentSegmentMs => RoundTracker.CurrentSegmentMs;

        public long RoundTotalMs => RoundTracker.RoundTotalMs;

        public int ValidAttemptCount => RoundTracker.ValidAttemptCount;

        public IList<SegmentResult> GetCompletedSegments()
        {
            var result = new List<SegmentResult>();
            foreach (var seg in RoundTracker.GetCompletedSegments())
            {
                result.Add(new SegmentResult
                {
                    Index = seg.Index,
                    DurationMs = seg.DurationMs,
                    TotalMs = seg.TotalMs,
                    Passed = seg.Passed,
                    Skipped = seg.Skipped,
                });
            }
            return result;
        }

        public IList<InvalidMarkInfo> GetActiveInvalidMarks()
            => RoundTracker.GetActiveInvalidMarks();

        // ── T4.1–T4.6 events (main thread; raised by the engine tick) ──

        public event Action<SegmentResult> SegmentCompleted;
        public event Action<int> AttemptSkipped;
        public event Action<long> RunCompleted;
        public event Action<int> IncompleteExit;
        public event Action<InvalidMarkInfo> InvalidMarked;
    }
}
