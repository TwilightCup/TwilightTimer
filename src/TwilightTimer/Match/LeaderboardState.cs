using System.Collections.Generic;
using System.Text;

namespace TwilightTimer
{
    /// <summary>Match seat, as seen by the leaderboard. None = unknown (transition mode).</summary>
    public enum LeaderboardSeat { None = 0, PlayerA = 1, PlayerB = 2 }

    /// <summary>SINGLE-project final-score rule (server match config).</summary>
    public enum SingleScoringMode { Fastest = 0, Average = 1 }

    /// <summary>
    /// One leaderboard row. MULTI rounds fill the level/total fields; SINGLE
    /// rounds fill the score field. Built by <see cref="LeaderboardState"/>.
    /// </summary>
    public sealed class LeaderboardRow
    {
        /// <summary>Display name; transition mode falls back to LEADERBOARD_YOU.</summary>
        public string DisplayName = "";
        public LeaderboardSeat Seat;
        /// <summary>True for the local player's own row.</summary>
        public bool IsLocal;
        /// <summary>True when the values came from the TwilightCore feed (authoritative).</summary>
        public bool FeedSourced;
        // ── MULTI ──
        /// <summary>0-based current level index; -1 unknown.</summary>
        public int CurrentLevelIndex = -1;
        /// <summary>Resolved display name of the current level, or null.</summary>
        public string LevelName;
        /// <summary>Cumulative run total frozen at arrival at the current level (ms).</summary>
        public long TotalAtArrivalMs;
        // ── SINGLE ──
        /// <summary>Final score (ms); null = no valid attempt yet → "--:--".</summary>
        public long? SingleScoreMs;
    }

    /// <summary>
    /// Pure state + row building + sorting for the in-match leaderboard HUD
    /// (no drawing — that lives in <see cref="LeaderboardHud"/>). Two data
    /// sources with a strict precedence:
    ///
    /// 1. <see cref="LeaderboardFeed"/> — TwilightCore's server-fed snapshot
    ///    covering BOTH seats (display names, seats, opponent progress,
    ///    server-authoritative SINGLE scores). When available it wins for
    ///    every row, including the local one (single authoritative source).
    /// 2. Local-only (transition mode, feed absent): the local row is built
    ///    entirely from <see cref="RoundTracker"/> /
    ///    <see cref="TimerCore.State"/> / TwilightCore's public
    ///    CollectionManager — all fully available without the feed. The
    ///    opponent row simply does not exist yet.
    ///
    /// Row formats (per the feature spec):
    ///   MULTI:  {name} {currentLevelName} {totalAtArrival, seconds precision}
    ///   SINGLE: {name} {score}
    /// </summary>
    public static class LeaderboardState
    {
        /// <summary>
        /// True while the leaderboard should render: inside a match session
        /// (MatchMode) AND an active round. Local practice never shows it.
        /// </summary>
        public static bool Visible => MatchMode.Active && RoundTracker.RoundActive;

        /// <summary>
        /// The local player's seat. Only the feed knows it (TwilightCore's
        /// session state is internal to it); None during transition mode.
        /// </summary>
        public static LeaderboardSeat LocalSeat
        {
            get
            {
                if (!LeaderboardFeed.Available) return LeaderboardSeat.None;
                return LeaderboardFeed.LocalSeat;
            }
        }

        /// <summary>
        /// SINGLE scoring rule. Feed value when available; provisional
        /// Fastest fallback in transition mode (a best-attempt time is always
        /// meaningful, an average needs the server's mode to be correct).
        /// </summary>
        public static SingleScoringMode ScoringMode
        {
            get
            {
                var snap = LeaderboardFeed.Snapshot;
                return snap != null && snap.IsSingle
                    ? snap.Scoring
                    : SingleScoringMode.Fastest;
            }
        }

        /// <summary>
        /// Build the sorted rows (progress-first: deeper level first, then
        /// lower total; SINGLE: better score first, no-score last; finally a
        /// deterministic A-above-B tiebreak). 1 row in transition mode, 2
        /// when the feed covers both seats.
        /// </summary>
        public static List<LeaderboardRow> BuildRows()
        {
            var rows = new List<LeaderboardRow>(2);

            var snap = LeaderboardFeed.Snapshot;
            if (snap != null && snap.A != null && snap.B != null)
            {
                rows.Add(FeedRow(snap, snap.A));
                rows.Add(FeedRow(snap, snap.B));
            }
            else
            {
                rows.Add(LocalRow());
            }

            rows.Sort(CompareRows);
            return rows;
        }

        /// <summary>Human-readable debug summary (console/log verification).</summary>
        public static string StatusString()
        {
            if (!Visible) return "leaderboard hidden (not in match round)";
            var sb = new StringBuilder();
            var snap = LeaderboardFeed.Snapshot;
            sb.Append(LeaderboardFeed.Available
                ? $"feed v{LeaderboardFeed.DetectedApiVersion}"
                : "local-only (feed absent)");
            sb.Append(RoundTracker.IsSingleProject ? " SINGLE" : " MULTI");
            if (RoundTracker.IsSingleProject)
                sb.Append($" scoring={ScoringMode}");
            foreach (var r in BuildRows())
            {
                sb.Append("\n  ").Append(r.DisplayName);
                if (RoundTracker.IsSingleProject)
                    sb.Append(' ').Append(r.SingleScoreMs.HasValue
                        ? TimeFormatter.Format(r.SingleScoreMs.Value / 1000d)
                        : "--:--");
                else
                    sb.Append(' ').Append(r.LevelName ?? "?")
                      .Append(' ').Append(TimeFormatter.FormatSeconds(r.TotalAtArrivalMs / 1000d));
            }
            return sb.ToString();
        }

        // ── Row builders ────────────────────────────────────────────────

        /// <summary>
        /// Build a row from a feed player (authoritative). The local row is
        /// identified by matching live local progress against the feed —
        /// see <see cref="IsLocalPlayer"/>.
        /// </summary>
        private static LeaderboardRow FeedRow(LeaderboardFeed.FeedSnapshot snap, LeaderboardFeed.FeedPlayer p)
        {
            var row = new LeaderboardRow
            {
                DisplayName = p.DisplayName,
                Seat = p.Seat,
                IsLocal = IsLocalPlayer(p),
                FeedSourced = true,
            };
            if (snap.IsSingle)
            {
                // Server-authoritative score; compute from attempt times when
                // the server has not produced one yet (0 valid attempts → null).
                row.SingleScoreMs = p.ScoreMs ?? ComputeSingleScore(p.LevelTimesMs);
            }
            else
            {
                row.CurrentLevelIndex = p.CurrentLevelIndex;
                row.LevelName = ResolveLevelName(snap, p.CurrentLevelIndex);
                row.TotalAtArrivalMs = p.TotalMs;
            }
            return row;
        }

        /// <summary>
        /// Build the local player's row from this plugin's own data
        /// (transition mode; also the fallback when the feed is missing a
        /// seat). MULTI total is FROZEN at level arrival — the last completed
        /// segment's TotalMs (0 on the first level) — matching exactly what
        /// the opponent's machine can know (level_time_update.total_ms fires
        /// at level completion), so both rows carry identical semantics.
        /// </summary>
        private static LeaderboardRow LocalRow()
        {
            var cfg = ConfigService.Instance;
            string fallbackName = cfg != null ? cfg.Localization.Get("LEADERBOARD_YOU") : "You";

            var row = new LeaderboardRow
            {
                DisplayName = fallbackName,
                Seat = LeaderboardSeat.None,
                IsLocal = true,
            };

            var segments = RoundTracker.GetCompletedSegments();
            if (RoundTracker.IsSingleProject)
            {
                // Only valid attempts count toward the score — an attempt
                // completed under invalid marks is N/A for scoring (the
                // invalid verdict is frozen on its segment record).
                var durations = new List<long>(segments.Count);
                foreach (var s in segments)
                    if (!s.IsInvalid)
                        durations.Add(s.DurationMs);
                row.SingleScoreMs = ComputeSingleScore(durations);
            }
            else
            {
                row.CurrentLevelIndex = LocalLevelIndex();
                row.LevelName = ResolveLevelName(null, row.CurrentLevelIndex);
                // Frozen arrival total = last completed segment's cumulative.
                row.TotalAtArrivalMs = segments.Count > 0 ? segments[segments.Count - 1].TotalMs : 0L;
            }
            return row;
        }

        // ── Helpers ─────────────────────────────────────────────────────

        /// <summary>
        /// Current level index for the local player: the round segment index
        /// assigned by the engine, falling back to TwilightCore's collection
        /// manager before the first segment edge fires.
        /// </summary>
        private static int LocalLevelIndex()
        {
            var state = TimerCore.State;
            if (state != null && state.RoundSegmentIndex >= 0)
                return state.RoundSegmentIndex;
            var mgr = TwilightCore.CollectionManager.Instance;
            if (mgr != null && mgr.IsInCollectionRun)
                return mgr.CurrentLevelIndex;
            return -1;
        }

        /// <summary>
        /// Resolve a level's display name. Feed snapshots carry the round's
        /// level ids (self-contained); locally the running collection is the
        /// source. English names, matching TwilightCore's CollectionInfoHud
        /// convention.
        /// </summary>
        private static string ResolveLevelName(LeaderboardFeed.FeedSnapshot snap, int index)
        {
            string levelId = null;
            if (snap != null && index >= 0 && index < snap.LevelIds.Length)
                levelId = snap.LevelIds[index];
            if (levelId == null)
            {
                var mgr = TwilightCore.CollectionManager.Instance;
                var col = mgr != null ? mgr.CurrentCollection : null;
                if (col != null && col.Levels != null && index >= 0 && index < col.Levels.Count)
                    levelId = col.Levels[index];
            }
            if (string.IsNullOrEmpty(levelId)) return null;
            try
            {
                return TwilightCore.CollectionManager.GetEnglishLevelName(levelId);
            }
            catch (System.Exception ex)
            {
                Plugin.Logger.LogWarning($"TwilightTimer: level name lookup failed for '{levelId}': {ex.Message}");
                return levelId;
            }
        }

        /// <summary>
        /// SINGLE score from valid-attempt durations: minimum (Fastest) or
        /// arithmetic mean (Average). Skipped attempts never reach the
        /// completed-segments list (N/A excluded). Null when there are no
        /// valid attempts.
        /// </summary>
        private static long? ComputeSingleScore(IList<long> durationsMs)
        {
            if (durationsMs == null || durationsMs.Count == 0) return null;
            if (ScoringMode == SingleScoringMode.Average)
            {
                long sum = 0;
                foreach (var d in durationsMs) sum += d;
                // Round half-up to whole milliseconds.
                return (sum + durationsMs.Count / 2) / durationsMs.Count;
            }
            long min = long.MaxValue;
            foreach (var d in durationsMs)
                if (d < min) min = d;
            return min;
        }

        /// <summary>
        /// Is a feed player the local player? Determined by the feed's own
        /// local-seat report — reliable at any progress state (the obvious
        /// alternative, matching by live progress, breaks whenever both
        /// players sit at the same level index, which is common).
        /// </summary>
        private static bool IsLocalPlayer(LeaderboardFeed.FeedPlayer p)
        {
            return p.Seat != LeaderboardSeat.None && p.Seat == LocalSeat;
        }

        // ── Sorting (confirmed rule: progress-first) ────────────────────

        private static int CompareRows(LeaderboardRow x, LeaderboardRow y)
        {
            if (ReferenceEquals(x, y)) return 0;
            if (x == null) return 1;
            if (y == null) return -1;

            if (RoundTracker.IsSingleProject)
            {
                // Better (lower) score first; no valid score last.
                bool xn = !x.SingleScoreMs.HasValue, yn = !y.SingleScoreMs.HasValue;
                if (xn != yn) return xn ? 1 : -1;
                if (!xn && !yn && x.SingleScoreMs.Value != y.SingleScoreMs.Value)
                    return x.SingleScoreMs.Value.CompareTo(y.SingleScoreMs.Value);
            }
            else
            {
                // Deeper progress first; equal progress → lower arrival total first.
                if (x.CurrentLevelIndex != y.CurrentLevelIndex)
                    return y.CurrentLevelIndex.CompareTo(x.CurrentLevelIndex);
                if (x.TotalAtArrivalMs != y.TotalAtArrivalMs)
                    return x.TotalAtArrivalMs.CompareTo(y.TotalAtArrivalMs);
            }

            // Deterministic tiebreak: seat A above seat B, local above unknown.
            return SeatRank(x).CompareTo(SeatRank(y));
        }

        private static int SeatRank(LeaderboardRow r)
            => r.IsLocal ? 0 : (int)r.Seat;
    }
}
