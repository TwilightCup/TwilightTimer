using System;
using System.Collections.Generic;
using UnityEngine;

namespace TwilightTimer
{
    /// <summary>
    /// The shared IMGUI leaderboard HUD (left side of the screen). It shows
    /// either the subsegment reference leaderboard (R8.5) or the markers
    /// trigger feed (R10.7), chosen by <c>LayoutModel.LeaderboardMode</c>.
    /// The top edge is fixed at the screen center (plus the configured Y
    /// offset) and rows extend downward as content grows. Appearance (font
    /// size, offsets, entry colors) comes from the layout model and applies
    /// to both modes; the mode-cycle key (<c>SubsegmentToggleKey</c>) lives
    /// here and cycles: hidden → Subsegment → Markers → hidden.
    ///
    /// During a match session (T7.6) the standalone cycle state is ignored
    /// entirely: the HUD is shown exactly while the match leaderboard
    /// (<see cref="MatchLeaderboardHud"/>) is shown, it hangs directly below
    /// that block instead of at the screen center, and its content is forced
    /// to Markers (subsegment stays force-disabled per T7.5, so the saved
    /// Subsegment mode would otherwise leave the block empty). The mode-cycle
    /// key is disabled for the whole match session so the competition display
    /// cannot be flipped by a stray keypress.
    /// </summary>
    public sealed class LeaderboardHud : MonoBehaviour
    {
        public static LeaderboardHud Instance { get; private set; }

        private bool _visible = true;
        private GUIStyle _rowStyle;
        private Font _font;
        private int _appliedFontSize = -1;

        /// <summary>Whether the leaderboard is currently shown (R8.5.1.2).</summary>
        public bool Visible => _visible;

        /// <summary>Top Y used by the last draw (console/test diagnostics).</summary>
        public float LastTopY { get; private set; }

        /// <summary>True when the last draw hung below the match leaderboard (T7.6).</summary>
        public bool AnchoredBelowMatch { get; private set; }

        private void Awake()
        {
            Instance = this;
            _rowStyle = new GUIStyle
            {
                fontSize = 16,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.UpperLeft,
                normal = { textColor = Color.white },
            };
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        /// <summary>
        /// Cycle the leaderboard through hidden → Subsegment → Markers → hidden.
        /// When turning it back on from hidden, it always starts in Subsegment
        /// mode; while visible it switches between the two content modes.
        /// </summary>
        public void CycleMode()
        {
            var cfg = ConfigService.Instance;
            if (cfg == null) return;

            if (!_visible)
            {
                _visible = true;
                cfg.Layout.LeaderboardMode = "Subsegment";
                Plugin.Logger.LogInfo("TwilightTimer: leaderboard shown in Subsegment mode.");
                return;
            }

            bool markersMode = string.Equals(cfg.Layout.LeaderboardMode, "Markers", System.StringComparison.OrdinalIgnoreCase);
            if (markersMode)
            {
                _visible = false;
                Plugin.Logger.LogInfo("TwilightTimer: leaderboard hidden.");
            }
            else
            {
                cfg.Layout.LeaderboardMode = "Markers";
                Plugin.Logger.LogInfo("TwilightTimer: leaderboard switched to Markers mode.");
            }
        }

        /// <summary>
        /// Show or hide the leaderboard without toggling if it is already in the
        /// requested state. Used by the in-game dev console.
        /// </summary>
        public void SetVisible(bool visible)
        {
            if (_visible == visible)
                return;
            _visible = visible;
            if (_visible && ConfigService.Instance != null
                && string.IsNullOrEmpty(ConfigService.Instance.Layout.LeaderboardMode))
            {
                ConfigService.Instance.Layout.LeaderboardMode = "Subsegment";
            }
        }

        /// <summary>
        /// Switch the leaderboard to a content mode ("Subsegment" or "Markers")
        /// and make sure it is visible. Used by the in-game dev console.
        /// </summary>
        public void SetMode(string mode)
        {
            if (string.IsNullOrEmpty(mode))
                return;
            var cfg = ConfigService.Instance;
            if (cfg != null)
            {
                if (string.Equals(mode, "Markers", StringComparison.OrdinalIgnoreCase))
                    cfg.Layout.LeaderboardMode = "Markers";
                else if (string.Equals(mode, "Subsegment", StringComparison.OrdinalIgnoreCase))
                    cfg.Layout.LeaderboardMode = "Subsegment";
            }
            _visible = true;
        }

        private void EnsureFont(int size)
        {
            if (size <= 0) size = 16;
            if (_font != null && _appliedFontSize == size) return;
            try
            {
                _font = Font.CreateDynamicFontFromOSFont(new[]
                {
                    "PingFang SC", "Microsoft YaHei", "Noto Sans CJK SC",
                    "Noto Sans CJK", "Heiti SC", "Arial Unicode MS", "Arial",
                }, size);
                _appliedFontSize = size;
            }
            catch (System.Exception ex)
            {
                Plugin.Logger.LogWarning($"TwilightTimer: leaderboard dynamic font creation failed: {ex.Message}");
                _font = null;
            }
        }

        private void OnGUI()
        {
            var cfg = ConfigService.Instance;
            if (cfg == null) return;

            // T7.6: during a match session the shared leaderboard follows the
            // match leaderboard — shown exactly while it is shown (the local
            // cycle state is ignored) and anchored directly below it. Outside
            // a match the standalone behavior is kept.
            bool followMatch = MatchMode.Active;
            bool shown = followMatch ? MatchLeaderboardHud.Visible : _visible;
            AnchoredBelowMatch = shown && followMatch && !float.IsNaN(MatchLeaderboardHud.BottomY);

            // Fixed top anchor: the title/rows always start here and extend
            // downward, so the top edge does not move as content changes.
            // Inside a match it hangs below the match leaderboard (T7.6),
            // otherwise it keeps the standalone center anchor (R8.5.1.1).
            LastTopY = AnchoredBelowMatch
                ? MatchLeaderboardHud.BottomY + MatchLeaderboardHud.SharedLeaderboardGap
                : Screen.height * 0.5f + cfg.Layout.LeaderboardOffsetY;

            if (!shown) return;

            // T7.6: the whole cycle state (hidden/Subsegment/Markers) is
            // ignored while a match is active. Visibility already follows the
            // match leaderboard above; content is forced to Markers because
            // T7.5 keeps the subsegment module force-disabled for the whole
            // session, so a user whose saved mode is "Subsegment" (the
            // default) would otherwise get an empty block — and the cycle key
            // is disabled in a match, so they could not switch out of it.
            bool markersMode = MatchMode.Active
                || string.Equals(cfg.Layout.LeaderboardMode, "Markers", System.StringComparison.OrdinalIgnoreCase);
            if (markersMode)
                DrawMarkers(cfg, LastTopY);
            else
                DrawSubsegment(cfg, LastTopY);
        }

        // ── Subsegment mode (R8.5) ─────────────────────────────────────────

        private void DrawSubsegment(ConfigService cfg, float topY)
        {
            var mgr = SubsegmentManager.Instance;
            // T7.5: the local subsegment leaderboard is suppressed for the whole
            // match session (Enabled folds in MatchMode.Active); the shared HUD
            // stays usable in Markers mode.
            if (mgr == null || !mgr.Enabled) return;
            var state = TimerCore.State;
            if (state == null) return;
            // During a level transition (LoadingLevel between levels) keep the
            // previous leaderboard on screen until the next level's first
            // settled diff refreshes it (R8.5.6.4).
            bool inPlayableSegment = state.InSegment;
            bool inMultiTransition = mgr.InMultiRunActive && state.GameTime > 0d;
            bool inPreservedTransition = mgr.InPreservedTransition && state.GameTime > 0d;
            if (!inPlayableSegment && !inMultiTransition && !inPreservedTransition) return;

            var entries = mgr.Entries;
            if (entries.Count == 0) return;

            var layout = cfg.Layout;
            int hudSize = layout.LeaderboardFontSize;
            EnsureFont(hudSize);
            ApplyFont();
            _rowStyle.fontSize = hudSize;

            float lineHeight = _rowStyle.CalcSize(new GUIContent("Wg")).y + 2f;
            float x = layout.LeaderboardOffsetX;
            float y = topY;

            string title = mgr.LeaderboardTitle;
            if (!string.IsNullOrEmpty(title))
            {
                DrawLine(title, Color.white, x, y);
                y += lineHeight;
            }

            foreach (var entry in entries)
            {
                string line = entry.DisplayId + "  " + TimeFormatter.FormatSignedDiff(entry.DiffMs);
                Color color;
                if (!entry.DiffMs.HasValue || entry.DiffMs.Value == 0)
                    color = layout.LeaderboardColorTie;
                else if (entry.DiffMs.Value < 0)
                    color = layout.LeaderboardColorFaster;
                else
                    color = layout.LeaderboardColorSlower;
                DrawLine(line, color, x, y);
                y += lineHeight;
            }
        }

        // ── Markers mode (R10.7) ───────────────────────────────────────────

        private void DrawMarkers(ConfigService cfg, float topY)
        {
            var mgr = MarkersManager.Instance;
            if (mgr == null || !mgr.HasFeedData) return;
            var settings = cfg.Settings;
            if (settings == null || !settings.MarkersEnable) return;
            var layout = cfg.Layout;
            var state = TimerCore.State;
            if (state == null) return;

            // R10.7.6: keep the previous feed on screen through a level
            // transition; hide it once a run has reset (GameTime back to 0).
            if (!state.InSegment && state.GameTime <= 0d) return;

            var feed = mgr.Feed;
            if (feed == null || feed.Count == 0) return;

            bool relative = string.Equals(layout.LeaderboardMarkersTimeMode, "Relative", System.StringComparison.OrdinalIgnoreCase);

            int hudSize = layout.LeaderboardFontSize;
            EnsureFont(hudSize);
            ApplyFont();
            _rowStyle.fontSize = hudSize;

            string title = mgr.LeaderboardTitle;
            bool hasTitle = !string.IsNullOrEmpty(title);
            float lineHeight = _rowStyle.CalcSize(new GUIContent("Wg")).y + 2f;
            float x = layout.LeaderboardOffsetX;
            float y = topY;

            if (hasTitle)
            {
                DrawLine(title, Color.white, x, y);
                y += lineHeight;
            }

            // Newest trigger on top (R10.7.4): the feed is stored oldest-first.
            for (int i = feed.Count - 1; i >= 0; i--)
            {
                var row = feed[i];
                if (row == null) continue;
                long? pb = mgr.PbTimeOf(row.MarkerId);
                long diff = pb.HasValue ? row.TMs - pb.Value : 0L;

                string value;
                Color color;
                if (relative)
                {
                    if (pb.HasValue)
                    {
                        value = TimeFormatter.FormatSignedDiff(diff);
                        color = ToneColor(layout, diff);
                    }
                    else
                    {
                        value = "--";
                        color = layout.LeaderboardColorTie;
                    }
                }
                else
                {
                    // Absolute segment time (R10.7.3); the color still reflects
                    // ahead/behind vs PB (R10.7.5).
                    value = TimeFormatter.Format(row.TMs / 1000.0);
                    color = pb.HasValue ? ToneColor(layout, diff) : layout.LeaderboardColorTie;
                }

                DrawLine(row.Name + ": " + value, color, x, y);
                y += lineHeight;
            }
        }

        private static Color ToneColor(LayoutModel layout, long diff)
        {
            if (diff < 0) return layout.LeaderboardColorFaster;
            if (diff > 0) return layout.LeaderboardColorSlower;
            return layout.LeaderboardColorTie;
        }

        private void ApplyFont()
        {
            if (_font == null) return;
            _rowStyle.font = _font;
            _rowStyle.fontSize = _appliedFontSize > 0 ? _appliedFontSize : 16;
        }

        private void DrawLine(string text, Color color, float x, float y)
        {
            _rowStyle.normal.textColor = color;
            GUI.Label(new Rect(x, y, 600f, 28f), text, _rowStyle);
        }
    }
}
