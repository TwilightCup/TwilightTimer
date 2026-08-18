using System.Collections.Generic;
using System.Text;

namespace TwilightTimer
{
    /// <summary>One completed segment of a round (T4.5):
    /// a level (MULTI) or an attempt (SINGLE).</summary>
    public sealed class RoundSegment
    {
        internal static readonly string[] NoReasons = new string[0];

        /// <summary>Index within the round (MULTI: collection index; SINGLE: attempt index).</summary>
        public int Index;
        /// <summary>This segment's duration, game-time, milliseconds.</summary>
        public long DurationMs;
        /// <summary>Cumulative round total at this segment's end, milliseconds.</summary>
        public long TotalMs;
        public bool Passed;
        public bool Skipped;

        /// <summary>
        /// Invalid reasons active at the moment this segment completed (empty
        /// = valid). SINGLE: the attempt's validity verdict — a non-empty set
        /// means the attempt's score must be treated as invalid (N/A for
        /// scoring, evidence retained). MULTI: informational snapshot (the
        /// round is one continuous unit; arbitration is the referee's).
        /// </summary>
        public string[] InvalidReasons = NoReasons;

        /// <summary>True when invalid marks were active at completion.</summary>
        public bool IsInvalid => InvalidReasons != null && InvalidReasons.Length > 0;
    }

    /// <summary>
    /// Round lifecycle tracking for Twilight Cup matches (T3/T4/T5). Driven
    /// through TwilightTimerApi (and later the ITimerProvider adapter); the engine
    /// tick (TimerCore) reports segment starts/ends/run completions into it.
    ///
    /// Skip vs incomplete-exit is decided deferred (T4.2/T4.4): an unpassed
    /// segment end latches a "pending incomplete exit"; if another segment
    /// starts while the round is still active it becomes AttemptSkipped, and
    /// if the round ends first (StopRound, or the LC RunAborted event) it
    /// becomes IncompleteExit. "No further segment follows" is genuinely
    /// unknowable at exit time, so the decision must wait.
    /// </summary>
    public static class RoundTracker
    {
        /// <summary>True between StartRound and StopRound (T3.1/T3.2).</summary>
        public static bool RoundActive { get; private set; }

        /// <summary>The active round's id (opaque, from the server pick).</summary>
        public static string RoundId { get; private set; }

        /// <summary>True for SINGLE projects, false for MULTI.</summary>
        public static bool IsSingleProject { get; private set; }

        /// <summary>SINGLE: total attempts allowed (from the pick).</summary>
        public static int RetryCount { get; private set; }

        /// <summary>Index the next started segment will receive.</summary>
        public static int NextSegmentIndex { get; private set; }

        /// <summary>
        /// SINGLE: attempts with a recorded completion that was valid at the
        /// moment of passing (T4.5). An attempt completed under invalid
        /// marks still reports a SegmentCompleted (the server gets the time
        /// + invalid evidence) but does not count as valid here.
        /// </summary>
        public static int ValidAttemptCount { get; private set; }

        private static bool _pendingIncompleteExit;
        private static int _pendingExitIndex = -1;
        private static readonly List<RoundSegment> _segments = new List<RoundSegment>();

        // ── T3: lifecycle ────────────────────────────────────────────────

        /// <summary>
        /// Start a round (T3.1): full reset of the engine (live timing,
        /// last-segment/last-run snapshots, ALL invalid marks) and apply the
        /// pushed tag set. The clock itself starts at the next PlayingLevel
        /// edge (R1.2) — never at call time. A round already in flight is
        /// replaced wholesale (previous data was queryable until now, T3.5).
        /// </summary>
        public static bool StartRound(string roundId, bool isSingleProject, int retryCount, IEnumerable<string> tags)
        {
            var engine = TimerCore.Instance;
            if (engine == null)
            {
                Plugin.Logger.LogWarning("TwilightTimer: StartRound ignored — engine not ready.");
                return false;
            }
            RoundActive = true;
            RoundId = roundId;
            IsSingleProject = isSingleProject;
            RetryCount = retryCount;
            NextSegmentIndex = 0;
            ValidAttemptCount = 0;
            _pendingIncompleteExit = false;
            _pendingExitIndex = -1;
            _segments.Clear();
            // T3.1: even if a segment is somehow already in flight (upstream
            // ordering raced the first level load), the full reset wins — the
            // first segment edge after this call is the round's true start.
            engine.FullResetForRound();
            MatchCheckpointPenalty.OnRoundStarted();
            SetRoundTags(tags);
            Plugin.Logger.LogInfo($"TwilightTimer: round '{roundId}' started ({(isSingleProject ? "SINGLE" : "MULTI")}, retry={retryCount}).");
            return true;
        }

        /// <summary>
        /// Stop the round (T3.2): stop timing and resolve any pending
        /// unpassed exit as IncompleteExit (T4.4). Segment data stays
        /// queryable until the next StartRound (T3.5).
        /// </summary>
        public static void StopRound()
        {
            if (!RoundActive) return;
            RoundActive = false;
            if (TimerCore.State != null)
                TimerCore.State.TimingActive = false;
            ResolvePendingExit();
            Plugin.Logger.LogInfo("TwilightTimer: round stopped.");
        }

        /// <summary>
        /// LC RunAborted arrived mid-round: resolve a pending unpassed exit
        /// immediately (T4.4) rather than waiting for the StopRound that
        /// follows. Idempotent — a later StopRound will not double-fire.
        /// </summary>
        public static void OnRunAborted()
        {
            if (RoundActive)
                ResolvePendingExit();
        }

        // ── engine hooks (called from TimerCore) ─────────────────────────

        /// <summary>
        /// A segment started. Resolves a pending unpassed exit as a skip
        /// (T4.2: an unpassed leave followed by another segment in-round is a
        /// skipped attempt) and assigns the segment its round index.
        /// </summary>
        public static void OnSegmentStart()
        {
            if (!RoundActive) return;
            // SINGLE invariant: every attempt starts with a clean slate. The
            // previous attempt's verdict was frozen at its OnSegmentEnd
            // (including exit-handler marks); any forgivable residue dying to
            // reach a clearing path is wiped here so a new attempt never
            // inherits it. MULTI keeps marks across levels — the round is one
            // continuous unit and arbitration is the referee's.
            if (IsSingleProject)
            {
                if (TimerCore.State != null)
                    TimerCore.State.Flags.ClearForgivable();
                MatchCheckpointPenalty.OnAttemptAbandoned();
            }
            if (_pendingIncompleteExit)
            {
                TimerEvents.RaiseAttemptSkipped(_pendingExitIndex);
                _pendingIncompleteExit = false;
                _pendingExitIndex = -1;
            }
            if (TimerCore.State != null)
                TimerCore.State.RoundSegmentIndex = NextSegmentIndex++;
        }

        /// <summary>
        /// A segment ended. A passed segment is recorded and reported
        /// (T4.1); an unpassed one only latches the pending exit — it gets no
        /// segment record (A3).
        /// </summary>
        public static void OnSegmentEnd(int index, long durationMs, long totalMs, bool passed)
        {
            if (!RoundActive) return;
            if (passed)
            {
                // Snapshot the invalid reasons active at completion: SINGLE
                // attempts are scored independently, so an attempt completed
                // under marks is invalid as a whole (the mark evidence was
                // already reported live via InvalidMarked; this records the
                // verdict on the score itself). MULTI keeps them as an
                // informational snapshot.
                string[] reasons = SnapshotInvalidReasons();
                _segments.Add(new RoundSegment
                {
                    Index = index,
                    DurationMs = durationMs,
                    TotalMs = totalMs,
                    Passed = true,
                    InvalidReasons = reasons,
                });
                if (reasons.Length == 0)
                    ValidAttemptCount++;
                TimerEvents.RaiseSegmentCompleted(index, durationMs, totalMs);
                // SINGLE: the attempt's verdict is final — a completed
                // attempt never re-runs, so its marks die with it and the
                // next attempt starts clean (a valid pass "forgives" the
                // marks exactly like abandoning the attempt does).
                if (IsSingleProject && reasons.Length > 0)
                {
                    if (TimerCore.State != null)
                        TimerCore.State.Flags.ClearForgivable();
                    MatchCheckpointPenalty.OnAttemptAbandoned();
                }
            }
            else
            {
                // SINGLE：本次尝试被放弃（lc skip / 未通关退出）——无论最终
                // 判成跳过还是中途退出，该尝试都记 N/A、不产生成绩，其中
                // 产生的可原谅标记（如 CheckpointSkip：回滚存档点后玩家没有
                // 读档而是直接放弃了尝试）随尝试作废。横幅与标记在下一次
                // 尝试开始前清干净；作废证据已在上报时刻（InvalidMarked）发给
                // 服务端，清的是本地状态不是记录。MULTI 不清：整局是一个连续
                // 单元，放弃关卡的标记须保留给裁判仲裁。
                if (IsSingleProject)
                {
                    if (TimerCore.State != null)
                        TimerCore.State.Flags.ClearForgivable();
                    MatchCheckpointPenalty.OnAttemptAbandoned();
                }
                // SINGLE 最后一次尝试被跳过（如 lc skip 收尾）：尝试预算已用尽，
                // 不可能有下一段开始来把 pending exit 转成 AttemptSkipped——
                // 延迟判定在这里会永久悬死（既无 attempt_skip 也无完成信号）。
                // 此刻「跳过 vs 中途退出」已无歧义，立即按跳过上报。
                if (IsSingleProject && RetryCount > 0 && index + 1 >= RetryCount)
                {
                    TimerEvents.RaiseAttemptSkipped(index);
                    return;
                }
                _pendingIncompleteExit = true;
                _pendingExitIndex = index;
            }
        }

        /// <summary>
        /// The round's whole collection completed (engine R1.6, T4.3). The
        /// round stays active until StopRound — round end is TwilightCore's
        /// judgment (T3.2).
        /// </summary>
        public static void OnRunCompleted(long totalMs)
        {
            if (!RoundActive) return;
            TimerEvents.RaiseRunCompleted(totalMs);
        }

        /// <summary>
        /// A NEW invalid reason was raised (T4.6). Reported live; also always
        /// available via <see cref="GetActiveInvalidMarks"/>.
        /// </summary>
        public static void OnInvalidRaised(InvalidReason reason, bool unforgivable)
        {
            if (!RoundActive) return;
            TimerEvents.RaiseInvalidMarked(reason, unforgivable);
        }

        // ── T5: tag push ─────────────────────────────────────────────────

        /// <summary>
        /// Set the round tag set (T5.2). Only honored in match mode (T5.1);
        /// unknown ids are logged and ignored (T5.5). Rebuilds the in-memory
        /// enabled set — disk (tags.ini) is never touched (the save guard
        /// writes the user's snapshot instead). Effective from the next
        /// segment.
        /// </summary>
        public static bool SetRoundTags(IEnumerable<string> tagIds)
        {
            var cfg = ConfigService.Instance;
            if (cfg == null || TagRuleRegistry.Instance == null)
                return false;
            if (!MatchMode.Active)
            {
                Plugin.Logger.LogWarning("TwilightTimer: SetRoundTags ignored — not in match mode (T5.1).");
                return false;
            }
            cfg.EnabledTags.Tags.Clear();
            if (tagIds != null)
            {
                foreach (var id in tagIds)
                {
                    if (string.IsNullOrEmpty(id)) continue;
                    if (TagRuleRegistry.Instance.Find(id) == null)
                    {
                        Plugin.Logger.LogWarning($"TwilightTimer: ignoring unsupported round tag '{id}' (T5.5).");
                        continue;
                    }
                    cfg.EnabledTags.Enable(id);
                }
            }
            return true;
        }

        // ── T4.5: queries ────────────────────────────────────────────────

        /// <summary>True while a segment is in flight.</summary>
        public static bool IsInSegment => TimerCore.State != null && TimerCore.State.InSegment;

        /// <summary>Game-time ms accumulated in the current segment.</summary>
        public static long CurrentSegmentMs
        {
            get
            {
                var s = TimerCore.State;
                return s == null ? 0 : Ms(s.GameTime - s.SegmentStart);
            }
        }

        /// <summary>Game-time ms accumulated this round.</summary>
        public static long RoundTotalMs
            => TimerCore.State != null ? Ms(TimerCore.State.GameTime) : 0;

        /// <summary>Snapshot copy of the completed segments so far.</summary>
        public static List<RoundSegment> GetCompletedSegments()
        {
            lock (_segments)
            {
                return new List<RoundSegment>(_segments);
            }
        }

        /// <summary>
        /// Snapshot of the currently-active invalid marks with severity
        /// classification (T4.5/T4.6). Uses TwilightCore's public
        /// InvalidMarkInfo (the adapter passes it through unchanged).
        /// </summary>
        public static List<TwilightCore.Timer.InvalidMarkInfo> GetActiveInvalidMarks()
        {
            var result = new List<TwilightCore.Timer.InvalidMarkInfo>();
            var s = TimerCore.State;
            if (s == null) return result;
            foreach (var r in s.Flags.All)
            {
                result.Add(new TwilightCore.Timer.InvalidMarkInfo
                {
                    Reason = r.ToString(),
                    Unforgivable = InvalidReasons.SeverityOf(r) == Severity.Unforgivable,
                });
            }
            return result;
        }

        /// <summary>Human-readable round status for console/debug output.</summary>
        public static string StatusString()
        {
            if (!RoundActive)
                return _segments.Count > 0
                    ? $"round '{RoundId}' stopped — {_segments.Count} segment(s), {ValidAttemptCount} valid attempt(s)"
                    : "no active round";
            var sb = new StringBuilder();
            sb.Append("round '").Append(RoundId).Append("' ")
              .Append(IsSingleProject ? "SINGLE" : "MULTI")
              .Append(IsSingleProject ? $" retry={RetryCount}" : "")
              .Append($": {_segments.Count} segment(s) done");
            if (IsInSegment)
                sb.Append($", in segment #{NextSegmentIndex - 1} ({CurrentSegmentMs} ms)");
            return sb.ToString();
        }

        private static void ResolvePendingExit()
        {
            if (!_pendingIncompleteExit) return;
            TimerEvents.RaiseIncompleteExit(_pendingExitIndex);
            _pendingIncompleteExit = false;
            _pendingExitIndex = -1;
        }

        /// <summary>
        /// Copy of the currently-active invalid reasons as stable strings
        /// (empty array when the run is clean). Used to freeze a segment's
        /// validity verdict at completion time.
        /// </summary>
        private static string[] SnapshotInvalidReasons()
        {
            var s = TimerCore.State;
            if (s == null) return RoundSegment.NoReasons;
            var all = s.Flags.All;
            var list = new List<string>();
            foreach (var r in all)
                list.Add(r.ToString());
            return list.Count == 0 ? RoundSegment.NoReasons : list.ToArray();
        }

        internal static long Ms(double seconds) => (long)System.Math.Round(seconds * 1000d);
    }
}
