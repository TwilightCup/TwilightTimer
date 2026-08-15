using System.Collections.Generic;

namespace TwilightTimer
{
    /// <summary>
    /// Match-mode controller (T2/T7 of the Twilight Cup requirements). Match
    /// mode corresponds to one Twilight Cup match session: entered/exited only
    /// through <see cref="TwilightTimerApi"/> (TwilightCore in production, console
    /// or debug tooling otherwise) — the settings panel offers no switch.
    ///
    /// On enter, the user's enabled tag set is snapshotted and the pushed
    /// round tags become the sole authority (T5.3). The T7.3 pause rule is
    /// enforced at read time (TimingOptions.FromSettings forces
    /// CountInPause) rather than by overwriting the user's settings field,
    /// so settings.ini can never be persisted with a match-forced value.
    /// On exit, the snapshot is restored (T2.4/T2.6). Round data produced
    /// during the match is deliberately left alone here (T3.5: it stays
    /// queryable until the next StartRound).
    /// </summary>
    public static class MatchMode
    {
        /// <summary>True between Enter() and Exit().</summary>
        public static bool Active { get; private set; }

        private static readonly List<string> _savedTags = new List<string>();

        /// <summary>
        /// Enter match mode (T2.2): snapshot the user's tag set. Must be
        /// idempotent — a second Enter without an Exit is a no-op that keeps
        /// the original snapshot.
        /// </summary>
        public static void Enter()
        {
            if (Active) return;
            var cfg = ConfigService.Instance;
            if (cfg == null)
            {
                Plugin.Logger.LogWarning("TwilightTimer: match mode enter ignored — config not ready.");
                return;
            }
            _savedTags.Clear();
            _savedTags.AddRange(cfg.EnabledTags.Tags);
            Active = true;
            TimerCore.Instance?.RefreshOptions();
            Plugin.Logger.LogInfo("TwilightTimer: match mode entered (tags snapshotted).");
        }

        /// <summary>
        /// Exit match mode (T2.4): restore the user's tag set. Round data and
        /// invalid marks survive until the next StartRound (T3.5).
        /// </summary>
        public static void Exit()
        {
            if (!Active) return;
            var cfg = ConfigService.Instance;
            if (cfg != null)
            {
                cfg.EnabledTags.Tags.Clear();
                cfg.EnabledTags.Tags.AddRange(_savedTags);
            }
            _savedTags.Clear();
            Active = false;
            TimerCore.Instance?.RefreshOptions();
            Plugin.Logger.LogInfo("TwilightTimer: match mode exited (user tags restored).");
        }

        /// <summary>
        /// Swap the snapshot (user's real) tags back in so a save writes the
        /// user's configuration, not the match-forced set (A1: tags.ini must
        /// stay untouched by match play). Paired with
        /// <see cref="RestoreMatchOverrides"/>; only meaningful while active.
        /// </summary>
        internal static void PushUserValuesForSave()
        {
            if (!Active) return;
            var cfg = ConfigService.Instance;
            if (cfg == null) return;
            _matchTags.Clear();
            _matchTags.AddRange(cfg.EnabledTags.Tags);
            cfg.EnabledTags.Tags.Clear();
            cfg.EnabledTags.Tags.AddRange(_savedTags);
        }

        internal static void RestoreMatchOverrides()
        {
            if (!Active) return;
            var cfg = ConfigService.Instance;
            if (cfg == null) return;
            cfg.EnabledTags.Tags.Clear();
            cfg.EnabledTags.Tags.AddRange(_matchTags);
            _matchTags.Clear();
        }

        private static readonly List<string> _matchTags = new List<string>();
    }
}
