using System;

namespace TwilightTimer
{
    /// <summary>
    /// Integration with the Level Collections module built into TwilightCore
    /// (TwilightCore.CollectionManager). On this branch TwilightCore is a hard
    /// compile-time + BepInEx dependency, so this bridge consumes its public
    /// API directly — no reflection and no soft-detection fallback. It exposes
    /// the current collection name (for the {collection} template var), detects
    /// when a collection's final level completes (so the HUD can surface the
    /// run total), and lets the one-key retry delegate to the <c>lc restart</c>
    /// console command while a collection run is active. Instance may still be
    /// null during a brief init window even though the dependency guarantees
    /// load order, so every accessor tolerates that.
    /// </summary>
    public sealed class LcIntegration
    {
        public static LcIntegration Instance { get; private set; }

        private bool _subscribed;

        private LcIntegration()
        {
            // The manager singleton may not exist yet at plugin Awake; the
            // accessors below tolerate that window (T8.5).
            Plugin.Logger.LogInfo("TwilightTimer: TwilightCore LC integration enabled.");
        }

        public static void Init() => Instance = new LcIntegration();

        /// <summary>TwilightCore is loaded (hard dependency); the manager
        /// singleton may still be initializing.</summary>
        public bool Enabled => Manager != null;

        /// <summary>Whether the LC lifecycle events are currently subscribed
        /// (the retry coroutine in Plugin polls this).</summary>
        public bool SubscribedForEvents => _subscribed;

        private static TwilightCore.CollectionManager Manager
            => TwilightCore.CollectionManager.Instance;

        /// <summary>
        /// True when the player is in the middle of a collection run
        /// (a config collection or a transient <c>lc random</c> run). When true,
        /// the one-key retry should delegate to <c>lc restart</c> so the whole
        /// collection restarts from level 1 instead of just reloading the
        /// current level.
        /// </summary>
        public bool IsInCollectionRun
        {
            get
            {
                var mgr = Manager;
                return mgr != null && mgr.IsInCollectionRun;
            }
        }

        /// <summary>
        /// True while a delayed console command
        /// (<c>lc restart/skip/random &lt;seconds&gt;</c>) is counting down. The
        /// command handler refuses a new <c>lc restart</c> in that window, so
        /// TwilightTimer should also refuse (rather than zero the timer and then
        /// stall with no reload).
        /// </summary>
        public bool IsDelayedCommandPending
        {
            get
            {
                var mgr = Manager;
                return mgr != null && mgr.IsDelayedCommandPending;
            }
        }

        /// <summary>
        /// Restart the current collection from its first level by dispatching
        /// the <c>lc restart</c> command through the game's dev-console
        /// registry (<c>Shell.RawInvoke</c>) — the same entry TwilightCore's
        /// built-in LC registers on load, so it works for both config
        /// collections and transient (<c>lc random</c>) runs, reusing its
        /// scene-reload forcing, validation, and level launching. Returns true
        /// if dispatched. No-op (returns false) when not in a run or a delayed
        /// command is pending.
        /// </summary>
        public bool RestartCollection()
        {
            if (!IsInCollectionRun || IsDelayedCommandPending)
                return false;

            try
            {
                // Shell.RawInvoke runs the command through the same registry
                // Shell.Update uses, so it behaves exactly like typing
                // "lc restart" into the console.
                Shell.RawInvoke("lc restart");
                return true;
            }
            catch (System.Exception ex)
            {
                Plugin.Logger.LogWarning($"TwilightTimer: LC collection restart failed: {ex.Message}");
                return false;
            }
        }

        /// <summary>True when the player is in a collection run and on its last level.</summary>
        public bool IsLastLevelOfCollection
        {
            get
            {
                var mgr = Manager;
                return mgr != null && mgr.IsInCollectionRun && mgr.IsLastLevel;
            }
        }

        /// <summary>The active collection's display name, or null if not in a run.</summary>
        public string CollectionName
        {
            get
            {
                var mgr = Manager;
                if (mgr == null || !mgr.IsInCollectionRun) return null;
                // CollectionDefinition.Name is a public field (not a property).
                return mgr.CurrentCollection?.Name;
            }
        }

        /// <summary>
        /// Subscribe to the built-in LC's lifecycle events (T8.3). Idempotent;
        /// paired with <see cref="UnsubscribeEvents"/> on plugin destroy.
        /// LevelStarted/LevelCompleted/RunCompleted are used only as
        /// cross-check logs — the engine's own per-tick latching remains the
        /// authoritative source for R1.6 (it latches before Game.Fall tears
        /// the run down). RunAborted resolves a pending unpassed exit
        /// immediately (T4.4) so it is not double-fired by a later StopRound.
        /// </summary>
        public void SubscribeEvents()
        {
            var mgr = Manager;
            if (_subscribed || mgr == null) return;
            _subscribed = true;
            mgr.LevelStarted += OnLevelStarted;
            mgr.LevelCompleted += OnLevelCompleted;
            mgr.RunCompleted += OnRunCompleted;
            mgr.RunAborted += OnRunAborted;
        }

        public void UnsubscribeEvents()
        {
            if (!_subscribed) return;
            _subscribed = false;
            var mgr = Manager;
            if (mgr == null) return;
            mgr.LevelStarted -= OnLevelStarted;
            mgr.LevelCompleted -= OnLevelCompleted;
            mgr.RunCompleted -= OnRunCompleted;
            mgr.RunAborted -= OnRunAborted;
        }

        private void OnLevelStarted(string levelId, int levelIndex)
            => Plugin.Logger.LogInfo($"TwilightTimer: LC level started (#{levelIndex}): {levelId}");

        private void OnLevelCompleted(int levelIndex, bool skipped)
            => Plugin.Logger.LogInfo($"TwilightTimer: LC level completed (#{levelIndex}, skipped={skipped})");

        private void OnRunCompleted()
            => Plugin.Logger.LogInfo("TwilightTimer: LC collection run completed.");

        private void OnRunAborted()
        {
            Plugin.Logger.LogInfo("TwilightTimer: LC collection run aborted.");
            // T4.4: resolve a pending unpassed exit immediately rather than
            // waiting for the StopRound call that follows; idempotent.
            RoundTracker.OnRunAborted();
        }
    }
}
