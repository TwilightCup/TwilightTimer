using System.Collections.Generic;
using UnityEngine;

namespace TwilightTimer
{
    /// <summary>
    /// The in-match real-time leaderboard, rendered in the timer's own style
    /// (plain bold text, per-character gradient, dynamic OS font, no window
    /// chrome) and anchored at the middle of the screen's left edge —
    /// vertically centred, so it never competes with the top-left timer
    /// stack. One row per player; row content follows the round's project
    /// type (see <see cref="LeaderboardState"/>):
    ///
    ///   MULTI:  {name} {currentLevelName} {totalAtArrival, seconds only}
    ///   SINGLE: {name} {score}
    ///
    /// Rows are sorted progress-first. The name is drawn in the seat colour
    /// (PLAYER_A blue / PLAYER_B red, configurable); the rest of the row
    /// keeps the default gradient. Transition mode (TwilightCore's
    /// leaderboard feed absent) shows the local row only, seat-unknown →
    /// default gradient. Hidden entirely outside match rounds and behind the
    /// ShowHud/ShowLeaderboard toggles; the keybind (default Tab, rebindable
    /// like the other keys) flips ShowLeaderboard live.
    /// </summary>
    public class LeaderboardHud : MonoBehaviour
    {
        private GUIStyle _rowStyle;
        private Font _font;
        private int _appliedFontSize = -1;

        private void Awake()
        {
            _rowStyle = new GUIStyle
            {
                fontSize = 18,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.UpperLeft,
                richText = false,
                // White base so GUI.color carries the per-character gradient
                // unchanged (final text color = GUI.color * textColor).
                normal = { textColor = Color.white },
            };
        }

        private void Update()
        {
            // Main-thread poll of TwilightCore's snapshot feed (no-op in
            // transition mode). OnGUI may fire multiple times per frame
            // (layout+repaint passes) — poll once here instead.
            LeaderboardFeed.Poll();

            // Show/hide keybind (default Tab), mirroring the engine's keybind
            // handling: always active except while the settings panel is open
            // (so rebinding/typing can't flip it). Toggling persists to disk
            // like the other keybinds' settings do.
            var cfg = ConfigService.Instance;
            if (cfg == null) return;
            if (SettingsPanel.Instance != null && SettingsPanel.Instance.IsVisible) return;
            if (Input.GetKeyDown(cfg.Settings.LeaderboardKey))
            {
                cfg.Settings.ShowLeaderboard = !cfg.Settings.ShowLeaderboard;
                cfg.SaveSettings();
            }
        }

        private void OnGUI()
        {
            var cfg = ConfigService.Instance;
            if (cfg == null) return;

            // The ShowHud master toggle gates the whole timer HUD family;
            // ShowLeaderboard is the per-component switch.
            if (!cfg.Settings.ShowHud || !cfg.Settings.ShowLeaderboard) return;
            if (!LeaderboardState.Visible) return;

            var layout = cfg.Layout;
            int size = layout.LeaderboardFontSize > 0 ? layout.LeaderboardFontSize : layout.FontSize;
            HudFont.EnsureDynamic(ref _font, ref _appliedFontSize, size);
            if (_font != null && _rowStyle.font != _font) _rowStyle.font = _font;
            if (_rowStyle.fontSize != size) _rowStyle.fontSize = size;

            var rows = LeaderboardState.BuildRows();
            if (rows.Count == 0) return;

            // Build the line segments first, measure the block, then draw
            // vertically centred on the left edge.
            var lines = new List<RowLines>(rows.Count);
            float widest = 0f, blockHeight = 0f;
            foreach (var row in rows)
            {
                var segs = BuildSegments(row);
                float w = 0f;
                foreach (var s in segs)
                {
                    var sz = _rowStyle.CalcSize(new GUIContent(s.Text));
                    w += sz.x + s.GapAfter;
                }
                var lineH = _rowStyle.CalcSize(new GUIContent("Ag")).y;
                lines.Add(new RowLines { Segments = segs, Height = lineH });
                if (w > widest) widest = w;
                blockHeight += lineH + 2f;
            }

            float x = layout.LeaderboardMarginX;
            float y = Screen.height * 0.5f - blockHeight * 0.5f + layout.LeaderboardOffsetY;
            foreach (var line in lines)
            {
                float cx = x;
                foreach (var seg in line.Segments)
                {
                    TimerHud.DrawGradientLine(seg.Text, seg.ColorA, seg.ColorB, cx, y, _rowStyle);
                    cx += _rowStyle.CalcSize(new GUIContent(seg.Text)).x + seg.GapAfter;
                }
                y += line.Height + 2f;
            }
        }

        /// <summary>
        /// One row as draw segments: the name (seat-coloured) followed by the
        /// value part (default gradient). The seat pair is flat (a == b) — a
        /// single colour reads cleaner on a short name than a gradient.
        /// </summary>
        private List<Segment> BuildSegments(LeaderboardRow row)
        {
            var cfg = ConfigService.Instance;
            var layout = cfg.Layout;

            Color nameA, nameB;
            switch (row.Seat)
            {
                case LeaderboardSeat.PlayerA:
                    nameA = nameB = layout.LeaderboardSeatAColor;
                    break;
                case LeaderboardSeat.PlayerB:
                    nameA = nameB = layout.LeaderboardSeatBColor;
                    break;
                default:
                    // Seat unknown (transition mode) — default gradient.
                    nameA = layout.ColorA;
                    nameB = layout.ColorB;
                    break;
            }

            string value;
            if (RoundTracker.IsSingleProject)
            {
                // Full precision — attempt times are the score itself.
                value = row.SingleScoreMs.HasValue
                    ? TimeFormatter.Format(row.SingleScoreMs.Value / 1000d)
                    : "--:--";
            }
            else
            {
                // MULTI total: seconds precision only (spec).
                value = (string.IsNullOrEmpty(row.LevelName) ? "?" : row.LevelName)
                        + " " + TimeFormatter.FormatSeconds(row.TotalAtArrivalMs / 1000d);
            }

            return new List<Segment>
            {
                new Segment { Text = row.DisplayName, ColorA = nameA, ColorB = nameB, GapAfter = 8f },
                new Segment { Text = value, ColorA = layout.ColorA, ColorB = layout.ColorB, GapAfter = 0f },
            };
        }

        private struct Segment
        {
            public string Text;
            public Color ColorA;
            public Color ColorB;
            public float GapAfter; // extra space after this segment (px)
        }

        private struct RowLines
        {
            public List<Segment> Segments;
            public float Height;
        }
    }
}
