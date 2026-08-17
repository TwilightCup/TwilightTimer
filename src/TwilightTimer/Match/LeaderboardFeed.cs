using System;
using System.Collections;
using System.Reflection;

namespace TwilightTimer
{
    /// <summary>
    /// Reflection gateway to TwilightCore's (future) public leaderboard API —
    /// the consumer-side seam, direction-reversed from
    /// <see cref="TwilightTimerProvider"/>: there TwilightCore owns the
    /// interface and this plugin implements it; here TwilightCore owns the
    /// data (server-fed opponent progress/scores) and publishes a snapshot
    /// API that this plugin polls.
    ///
    /// The API does not exist in the currently-built TwilightCore.dll (it is
    /// specified in docs/LEADERBOARD_REQ.md, to be implemented on the
    /// TwilightCore side), so it CANNOT be compiled against. Instead
    /// <see cref="Init"/> probes for the well-known static class
    /// <c>TwilightCore.Leaderboard.LeaderboardApi</c> at load time; when
    /// absent (transition period) the probe logs once and every poll is a
    /// no-op — <see cref="LeaderboardState"/> then renders the local row only
    /// from its own fully-available data. The type/member names below and in
    /// the requirements doc are a HARD contract: a rename on the TwilightCore
    /// side silently reverts this plugin to transition mode (by design, not
    /// by crash).
    ///
    /// <see cref="Poll"/> runs on the Unity main thread (called from
    /// LeaderboardHud.Update) and normalizes the reflection snapshot into the
    /// internal DTO immediately, so no reflection objects escape this class.
    /// Any reflection failure logs one warning and permanently degrades to
    /// transition mode (never spam).
    /// </summary>
    internal static class LeaderboardFeed
    {
        // Hard contract with docs/LEADERBOARD_REQ.md §2/§6 — do not rename.
        private const string ApiTypeName = "TwilightCore.Leaderboard.LeaderboardApi, TwilightCore";

        /// <summary>True once the probe found a compatible API.</summary>
        internal static bool Available { get; private set; }

        /// <summary>ApiVersion reported by the detected API (0 = unknown).</summary>
        internal static int DetectedApiVersion { get; private set; }

        /// <summary>
        /// The last normalized snapshot, or null when unavailable / outside a
        /// round. Never a partially-filled object — null or complete.
        /// </summary>
        internal static FeedSnapshot Snapshot { get; private set; }

        /// <summary>
        /// The local player's seat ("PLAYER_A"/"PLAYER_B"), or null when the
        /// feed is absent or the seat unknown. Refetched on every poll —
        /// TwilightCore may authenticate after our probe.
        /// </summary>
        internal static LeaderboardSeat LocalSeat { get; private set; }

        // Cached reflection handles (null until Init succeeds).
        private static MethodInfo _getSnapshot;
        private static MethodInfo _getLocalSeat;
        private static FieldInfo _snapRoundId, _snapIsSingle, _snapScoring, _snapRetry, _snapLevelIds, _snapA, _snapB;
        private static FieldInfo[] _playerFields;

        /// <summary>
        /// Probe for the TwilightCore leaderboard API. Called once from
        /// Plugin.Awake. Logs exactly one line either way — the log line is
        /// also the primary manual verification signal for the seam.
        /// </summary>
        internal static void Init()
        {
            try
            {
                var apiType = Type.GetType(ApiTypeName);
                if (apiType == null)
                {
                    Plugin.Logger.LogInfo(
                        "TwilightTimer: TwilightCore leaderboard API not present — leaderboard runs in local-only transition mode.");
                    return;
                }

                _getSnapshot = apiType.GetMethod("GetSnapshot",
                    BindingFlags.Public | BindingFlags.Static);
                _getLocalSeat = apiType.GetMethod("GetLocalSeat",
                    BindingFlags.Public | BindingFlags.Static);
                var versionField = apiType.GetField("ApiVersion",
                    BindingFlags.Public | BindingFlags.Static);
                var versionProp = apiType.GetProperty("ApiVersion",
                    BindingFlags.Public | BindingFlags.Static);
                if (versionField != null)
                    DetectedApiVersion = Convert.ToInt32(versionField.GetValue(null));
                else if (versionProp != null)
                    DetectedApiVersion = Convert.ToInt32(versionProp.GetValue(null));

                if (_getSnapshot == null || _getLocalSeat == null || DetectedApiVersion < 1)
                {
                    Plugin.Logger.LogWarning(
                        $"TwilightTimer: TwilightCore leaderboard API found but unusable (GetSnapshot/GetLocalSeat missing or ApiVersion={DetectedApiVersion}) — transition mode.");
                    Reset();
                    return;
                }

                // Locate the snapshot DTO layout once. Field order here is
                // fixed by the requirements doc; a missing field means an
                // incompatible build → degrade instead of throwing per frame.
                var snapType = _getSnapshot.ReturnType;
                _snapRoundId = snapType.GetField("RoundId");
                _snapIsSingle = snapType.GetField("IsSingleProject");
                _snapScoring = snapType.GetField("Scoring");
                _snapRetry = snapType.GetField("RetryCount");
                _snapLevelIds = snapType.GetField("LevelIds");
                _snapA = snapType.GetField("PlayerA");
                _snapB = snapType.GetField("PlayerB");

                var playerType = _snapA != null ? _snapA.FieldType : null;
                if (playerType != null)
                {
                    _playerFields = new[]
                    {
                        playerType.GetField("Seat"),
                        playerType.GetField("DisplayName"),
                        playerType.GetField("Status"),
                        playerType.GetField("CurrentLevelIndex"),
                        playerType.GetField("LevelTimesMs"),
                        playerType.GetField("SkippedIndexes"),
                        playerType.GetField("TotalMs"),
                        playerType.GetField("ScoreMs"),
                    };
                }

                bool layoutOk = _snapRoundId != null && _snapIsSingle != null
                    && _snapScoring != null && _snapRetry != null
                    && _snapLevelIds != null && _snapA != null && _snapB != null
                    && _playerFields != null;
                foreach (var f in _playerFields ?? new FieldInfo[0])
                    layoutOk &= f != null;

                if (!layoutOk)
                {
                    Plugin.Logger.LogWarning(
                        "TwilightTimer: TwilightCore leaderboard snapshot shape mismatch — transition mode.");
                    Reset();
                    return;
                }

                Available = true;
                Plugin.Logger.LogInfo(
                    $"TwilightTimer: TwilightCore leaderboard API v{DetectedApiVersion} detected.");
            }
            catch (System.Exception ex)
            {
                Plugin.Logger.LogWarning(
                    $"TwilightTimer: leaderboard API probe failed: {ex.Message} — transition mode.");
                Reset();
            }
        }

        /// <summary>
        /// Re-read the snapshot from TwilightCore and normalize it. Main
        /// thread only (called from LeaderboardHud.Update). Cheap: the
        /// reflection call only fetches a pre-built immutable object; the
        /// server updates at most once per level/attempt completion.
        /// </summary>
        internal static void Poll()
        {
            if (!Available) return;
            try
            {
                LocalSeat = ParseSeat(ReadString(_getLocalSeat.Invoke(null, null)),
                    LeaderboardSeat.None);

                var raw = _getSnapshot.Invoke(null, null);
                if (raw == null)
                {
                    Snapshot = null;
                    return;
                }

                var a = ReadPlayer(_snapA.GetValue(raw), 0);
                var b = ReadPlayer(_snapB.GetValue(raw), 1);
                if (a == null && b == null)
                {
                    Snapshot = null;
                    return;
                }

                Snapshot = new FeedSnapshot
                {
                    RoundId = ReadString(_snapRoundId.GetValue(raw)),
                    IsSingle = Convert.ToBoolean(_snapIsSingle.GetValue(raw)),
                    // Enums cross the seam as their underlying int (req doc §6).
                    Scoring = (SingleScoringMode)Convert.ToInt32(_snapScoring.GetValue(raw)),
                    RetryCount = Convert.ToInt32(_snapRetry.GetValue(raw)),
                    LevelIds = ReadStringArray(_snapLevelIds.GetValue(raw)),
                    A = a,
                    B = b,
                };
            }
            catch (System.Exception ex)
            {
                // One warning, then permanent degrade — a per-frame exception
                // log would flood the console for the rest of the session.
                Plugin.Logger.LogWarning(
                    $"TwilightTimer: leaderboard poll failed: {ex.Message} — falling back to local-only mode.");
                Reset();
                Snapshot = null;
            }
        }

        /// <summary>Clear all probe state (transition mode).</summary>
        private static void Reset()
        {
            Available = false;
            DetectedApiVersion = 0;
            Snapshot = null;
            LocalSeat = LeaderboardSeat.None;
            _getSnapshot = null;
            _getLocalSeat = null;
            _snapRoundId = _snapIsSingle = _snapScoring = _snapRetry = _snapLevelIds = _snapA = _snapB = null;
            _playerFields = null;
        }

        // ── Normalized internal DTO (plain TwilightTimer types) ─────────

        internal sealed class FeedSnapshot
        {
            public string RoundId;
            public bool IsSingle;
            public SingleScoringMode Scoring;
            public int RetryCount;
            /// <summary>Collection level ids by index; empty when not supplied.</summary>
            public string[] LevelIds = EmptyStrings;
            public FeedPlayer A;
            public FeedPlayer B;
        }

        internal sealed class FeedPlayer
        {
            public LeaderboardSeat Seat;
            public string DisplayName = "";
            /// <summary>0 = playing, 1 = finished, 2 = forfeited (req doc enum, as int).</summary>
            public int Status;
            /// <summary>0-based; MULTI = collection index, SINGLE = attempt index; -1 unknown.</summary>
            public int CurrentLevelIndex = -1;
            /// <summary>Completed segment durations in order (SINGLE: valid attempts only).</summary>
            public long[] LevelTimesMs = EmptyLongs;
            /// <summary>SINGLE attempt indexes recorded N/A.</summary>
            public int[] SkippedIndexes = EmptyInts;
            /// <summary>MULTI: cumulative total at the latest completed level; SINGLE: 0.</summary>
            public long TotalMs;
            /// <summary>SINGLE: server-authoritative score; null when not computable yet.</summary>
            public long? ScoreMs;
        }

        private static readonly string[] EmptyStrings = new string[0];
        private static readonly long[] EmptyLongs = new long[0];
        private static readonly int[] EmptyInts = new int[0];

        // ── Defensive readers ───────────────────────────────────────────

        private static FeedPlayer ReadPlayer(object raw, int seatFallback)
        {
            if (raw == null) return null;
            var p = new FeedPlayer
            {
                Seat = ParseSeat(ReadString(_playerFields[0].GetValue(raw)),
                    (LeaderboardSeat)(seatFallback + 1)),
                DisplayName = ReadString(_playerFields[1].GetValue(raw)),
                Status = Convert.ToInt32(_playerFields[2].GetValue(raw)),
                CurrentLevelIndex = Convert.ToInt32(_playerFields[3].GetValue(raw)),
                LevelTimesMs = ReadLongArray(_playerFields[4].GetValue(raw)),
                SkippedIndexes = ReadIntArray(_playerFields[5].GetValue(raw)),
                TotalMs = Convert.ToInt64(_playerFields[6].GetValue(raw)),
            };
            // long? boxes to either null or a boxed long.
            var scoreBox = _playerFields[7].GetValue(raw);
            p.ScoreMs = scoreBox == null ? (long?)null : Convert.ToInt64(scoreBox);
            return p;
        }

        private static LeaderboardSeat ParseSeat(string seat, LeaderboardSeat fallback)
        {
            switch ((seat ?? "").Trim().ToUpperInvariant())
            {
                case "PLAYER_A": return LeaderboardSeat.PlayerA;
                case "PLAYER_B": return LeaderboardSeat.PlayerB;
                case "": return fallback;
                default: return fallback;
            }
        }

        private static string ReadString(object v)
            => v == null ? "" : v.ToString();

        private static string[] ReadStringArray(object v)
        {
            var arr = v as IEnumerable;
            if (arr == null) return EmptyStrings;
            var list = new System.Collections.Generic.List<string>();
            foreach (var item in arr)
                list.Add(item == null ? "" : item.ToString());
            return list.ToArray();
        }

        private static long[] ReadLongArray(object v)
        {
            var arr = v as IEnumerable;
            if (arr == null) return EmptyLongs;
            var list = new System.Collections.Generic.List<long>();
            foreach (var item in arr)
                list.Add(Convert.ToInt64(item));
            return list.ToArray();
        }

        private static int[] ReadIntArray(object v)
        {
            var arr = v as IEnumerable;
            if (arr == null) return EmptyInts;
            var list = new System.Collections.Generic.List<int>();
            foreach (var item in arr)
                list.Add(Convert.ToInt32(item));
            return list.ToArray();
        }
    }
}
