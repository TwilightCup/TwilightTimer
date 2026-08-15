using UnityEngine;

namespace HSRTimer
{
    /// <summary>
    /// Match-only checkpoint-skip penalty (Twilight Cup rule): when a skip
    /// violation fires mid-round (e.g. CP 9 → CP 11), the player's checkpoint
    /// progress is rolled back to the checkpoint BEFORE the skipped span
    /// (CP 8; a skip from CP 0 rolls back to CP 0). The rollback only
    /// rewrites the progress number — the player is NOT teleported: they must
    /// open the pause menu and load the save point themselves, which respawns
    /// them at the rolled-back checkpoint (the game's own RestartCheckpoint
    /// performs the full level-state/trigger reset). That load is what clears
    /// the CheckpointSkip mark.
    ///
    /// Active only while a match round is in flight; practice and local runs
    /// keep the v1 behavior (flag raised, no rollback).
    /// </summary>
    public static class MatchCheckpointPenalty
    {
        /// <summary>
        /// True while the CheckpointSkip mark from this round's rollback is
        /// still uncleared (i.e. the player has not yet loaded a save point).
        /// </summary>
        public static bool Pending { get; private set; }

        /// <summary>
        /// Called by <see cref="CheckpointTagRule"/> when a skip violation is
        /// detected (only for genuine violations — R4.1.2 exception-table
        /// spans never reach here). Rewrites the progress number to
        /// max(fromCp - 1, 0) so the pause-menu save-load respawns the player
        /// there. Writing the number (instead of auto-respawning) is the
        /// point: the player must perform the load themselves.
        /// </summary>
        public static void OnSkipViolation(int fromCp)
        {
            if (!RoundTracker.RoundActive || !MatchMode.Active)
                return; // match-only rule

            int target = Mathf.Max(fromCp - 1, 0); // a skip from CP 0 stays at CP 0
            Pending = true;

            var game = Game.instance;
            if (game == null)
                return;
            try
            {
                game.currentCheckpointNumber = target;
                game.currentCheckpointSubObjectives = 0;
                Plugin.Logger.LogInfo(
                    $"HSRTimer: match checkpoint-skip penalty — progress rolled back to CP {target}; load the save point from the pause menu to clear the mark.");
            }
            catch (System.Exception ex)
            {
                Plugin.Logger.LogWarning($"HSRTimer: checkpoint rollback failed: {ex.Message}");
            }
        }

        /// <summary>
        /// The pause-menu "load save point" (RestartCheckpoint) completed and
        /// the player is back at their (rolled-back) checkpoint: clear the
        /// pending state. The CheckpointSkip flag itself is cleared by the
        /// patch caller (PauseMenuLoadPatch).
        /// </summary>
        public static void OnSaveLoaded()
        {
            if (!Pending) return;
            Pending = false;
            Plugin.Logger.LogInfo("HSRTimer: match checkpoint-skip penalty cleared (save point loaded).");
        }

        /// <summary>Round bookkeeping: reset the pending latch on a new round.</summary>
        public static void OnRoundStarted() => Pending = false;
    }
}
