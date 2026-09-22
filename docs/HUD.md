# HUD

> **中文版**: [zh/HUD.md](zh/HUD.md)

The timer is an IMGUI text overlay drawn **directly on screen** — no window,
no background, not draggable. Its rows can be freely arranged, and text uses a
per-character two-color gradient.

## Row types

Each row renders one value:

| Row type | Shows |
|----------|-------|
| `GameTime` | Total game time accumulated this run |
| `RealTime` | Wall-clock time for the current run (shown by default below Game Time; see below) |
| `CurrentSegment` | Time since entering the current level |
| `TotalAtLastSegment` | Run total frozen at the moment the last segment completed |
| `LastSegment` | Duration of the last completed level |
| `LastRun` | Total time of the last complete run — rendered in its own column immediately to the right of the timer stack (not in it), shown only while idle (hidden once a new run starts timing) |
| `CurrentState` | The engine's detected game state (debug) |

Rows are edited in `layout.ini` under `[rows]` (ordered by index). The panel
height adapts to the number of rows.

## Wake Up Time

The right-hand column (the one that holds **Last Run**) also shows **Wake Up
Time** when enabled. By default it is the time from the most recent
wake-up-relevant moment to the first time the local player leaves the
soft/spawn state (`Spawning` / `Unconscious` / `Dead`). The measurement
restarts whenever the player respawns (e.g. after a fall), loads the current
checkpoint from the pause menu, or restarts the level from the pause menu —
the value then reflects how long it took to get up after that particular
respawn. Once recorded within one measurement, later manual play-dead does not
reset it; a new respawn/restart clears it and starts a fresh measurement.

When the **Only record first wake-up time** option is enabled (visible only
while Wake Up Time display is on), the original behavior is restored: only the
first wake-up after a level starts is measured, and later respawns / checkpoint
loads / level restarts do not reset the value. The value is cleared when the
level is passed or exited.

The value is formatted as `SS:mmm` (seconds and milliseconds, no minute/hour
breakdown). When Last Run is visible, Wake Up Time is drawn as the second row in
the same column; otherwise it appears as the only row in that column so it
remains available during the run. Toggle it from the settings panel's Interface
page, or set `show_wake_up_time = false` in `settings.ini`. The "only first"
behavior is controlled by `only_record_first_wake_up_time` in the same file.

## Real Time clock

Alongside the game-time rows, the HUD has a **Real Time** clock that measures
wall-clock time for the current run. It starts when the game clock starts,
continues through level-loading screens and pauses, and stops when the run is
completed (the final level is passed) or when you leave the run for the menu.

The Real Time row is shown by default directly below **Game Time** in the
default layout, and the clock is always active. You can hide it from the
settings panel's Interface tab (or set `show_real_time = false` in
`settings.ini`). `RealTime` is also a regular row type: if you move it in
`[rows]` it appears in your chosen position; if you remove it from the layout
but leave the setting enabled, it appears below your configured timer rows as a
convenient fallback.

## Colors & gradient

The default text gradient is a two-color left→-right blend. Specify each color
as hex, either `RRGGBB` (opaque) or `RRGGBBAA` (with alpha). Set both in
`layout.ini` `[panel]`:

```ini
color_a = FF5272FF   # start color (pink-red, fully opaque)
color_b = FF9A72FF   # end color
```

If both colors are equal, the text is a flat single color. Per-character alpha
comes from the alpha byte, so you can fade text across the line.

Custom texts (`[custom.<n>]`) each have their own `color_a` / `color_b`.

## Custom texts

Place any number of arbitrary texts at fixed screen coordinates:

```ini
[custom.0]
x = 400
y = 50
text = {date} {time}
color_a = FFFFFFFF
color_b = CCCCCCCF
```

**Template variables** (auto-replaced):

| Variable | Replaced with |
|----------|---------------|
| `{date}` | Current date (`YYYY-MM-DD`) |
| `{time}` | Current time (`HH:MM:SS`) |
| `{version}` | Plugin version |
| `{collection}` | Active Level Collections collection name (or fallback) |
| `{category}` | Active category display name |
| `{gametime}` | Current game time |
| `{realtime}` | Current Real Time clock value |

Unknown `{tokens}` are left intact. Use the literal `\n` in the text for a
newline.

## Position & size

The main text block is drawn **directly on screen** — there is no window or
background, and it cannot be dragged. Adjust its position and font size in
`layout.ini` under `[text]`:

```ini
[text]
offset_x = 16   # pixels from the left edge
offset_y = 16   # pixels from the top edge
font_size = 18  # font size
```

Custom texts (`[custom.<n>]`) each have their own absolute `(x, y)` and so can
appear anywhere on screen regardless of the main block's offset.

## Show / hide

Toggle the whole timer via the settings panel (default key `Home`), or set
`show_hud = false` in `settings.ini`.

## Game Loading/Saving indicator

The game shows its own "Loading"/"Saving" progress indicator in the top-right
corner. If you prefer it at the top-center, enable `center_loading_saving` in
`settings.ini` or toggle **Center Loading/Saving prompts** on the settings
panel's Interface tab.

## Invalid banner

When a run is flagged invalid (R5), a red banner appears inside the panel
listing the reason(s). See [CONFIG.md](CONFIG.md) for the validity options.

The same red style is also used for an invalid **specified retry level**
(R6.5): when the retry-target override is enabled and the configured level name
or Workshop id cannot be resolved, pressing the retry key shows a red
"Specified retry level is invalid" line in the timer HUD. It stays visible
until a resolvable value is pressed with Retry or the override is turned off.

## Fonts

The panel uses a dynamic OS font with a CJK-capable fallback chain (PingFang /
Microsoft YaHei / Noto Sans CJK …), so localized text in Chinese/Japanese
renders correctly. Verify rendering on your platform.

## Match leaderboard

During a Twilight Cup match round a second component renders in the timer's
style, anchored at the **middle of the left screen edge** (vertically
centred). One row per player, sorted progress-first (deeper level first;
equal progress → lower total first; single-level rounds sort by score, best
first, no-score last):

- **Multi-level rounds**: `{name} {currentLevelName} {total at arriving the current level}` — the total is frozen at level arrival and shown with seconds precision only.
- **Single-level rounds**: `{name} {score}` — the final score (fastest attempt or average, per the match config) at full precision.

The name is drawn in the player's seat colour (PLAYER_A blue / PLAYER_B red,
configurable); the rest of the row keeps the default gradient. Rows are fed
by TwilightCore's leaderboard API (see
[LEADERBOARD_REQ.md](LEADERBOARD_REQ.md)); before that API exists the
component runs in **transition mode** — local row only, seat unknown →
default gradient, name falls back to the localized "You".

Hidden entirely outside match rounds. Toggles: the leaderboard keybind
(default `Tab`, rebindable like the other keys in the settings panel), the
`show_leaderboard` setting (which the keybind flips, also editable in the
settings panel), and the `show_hud` master. Layout keys in `layout.ini`:

```ini
[leaderboard]
margin_x = 16          # px from the left screen edge
offset_y = 0           # nudge from vertical centre
font_size = 0          # 0 = follow [text] font_size
seat_a_color = 4F9DFFFF
seat_b_color = FF5A5AFF
```
