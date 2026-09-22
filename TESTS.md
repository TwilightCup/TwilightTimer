# TESTS

This document explains how to test every TwilightTimer feature from inside the game
using the built-in dev console (default keys **`~`** or **F1**).

## What this gives you

TwilightTimer registers a `twi ...` command family with the game's `Shell` console
at plugin load. You can inspect live state, toggle settings, mutate config,
switch tags, edit the HUD layout, manage presets, exercise subsegments and
markers, simulate validity flags, and drive Twilight Cup match rounds / the
registered timer-provider adapter — all without restarting the game or editing
files by hand.

All `twi` commands are safe to run from the console. Most mutations call the
same `ConfigService.SaveSettings()` path used by the settings panel, so they
persist to the normal `settings.ini` / `tags.ini` / `layout.ini` files.

## Opening the console

1. Launch Human: Fall Flat with TwilightTimer installed.
2. Press **`~`** (backquote) or **F1** to open the game's dev console.
3. Type `twi` and press Enter to see the command list.
4. Type `twi help <topic>` for detailed help on one command.

> If the console does not appear, the game version may have moved the key
> binding. The plugin still logs "TwilightTimer: dev-console commands registered" to
> the BepInEx log at startup.

> **Note on case:** the game's console lowercases the whole line before a
> command runs. TwilightTimer therefore treats identifiers (tag ids, language codes,
> preset names, leaderboard modes) case-insensitively and canonicalizes them.
> Free-text values (e.g. a custom text or a marker name) are stored as typed by
> the console, i.e. lowercased.

## Command reference

| Command | Description |
|---|---|
| `twi` | Print the full command summary |
| `twi help [topic]` | Print help for one topic |
| `twi status` | Dump live timer/config/HUD/subsegment/marker/LC state |
| `twi keys` | List every settable settings/layout key |
| `twi get <key>` / `twi get all` | Read one config value, or all values |
| `twi set <key> <value>` | Set and save a config value |
| `twi reload` | Re-read all config + language files from disk |
| `twi save` | Save current in-memory config |
| `twi reset` | Full-run reset (same as the reset key) |
| `twi retry` | One-key retry (same as the retry key, R6) |
| `twi hud [on\|off\|toggle\|status]` | Control timer HUD visibility |
| `twi panel [open\|close\|toggle\|status]` | Control the settings panel |
| `twi leaderboard [cycle\|show\|hide\|mode <Subsegment\|Markers>\|status]` | Control the leaderboard HUD |
| `twi layout [status\|row ...\|text ...\|get <key>\|set <key> <value>]` | Inspect/edit the HUD layout |
| `twi tag [list\|enable <id>\|disable <id>\|set <id> <on\|off>]` | Toggle enabled tag rules |
| `twi lang [list\|set <code>\|reload\|current]` | Manage localization |
| `twi preset [list\|current\|create <name>\|apply [name]\|save\|delete <name>]` | Manage presets (R11) |
| `twi sub [status\|entries\|clear]` | Inspect/clear the subsegment module |
| `twi marker [list\|feed\|add ...\|remove <id>\|toggle <id>\|pb <ms>\|pbclear\|clear\|save\|reload]` | Inspect/edit markers (R10) |
| `twi flags [list\|raise <Reason>\|clear [forgivable\|soft\|all]]` | Inspect/mutate validity flags (R5) |
| `twi lc [status\|restart]` | Inspect LevelCollections integration or dispatch `lc restart` |
| `twi config [path\|files]` | Print TwilightTimer config paths |
| `twi match [status\|enter\|exit\|start ...\|resume ...\|stop\|tags ...\|segments\|leaderboard\|penalty]` | Drive Twilight Cup match mode / round lifecycle through `TwilightTimerApi` (synchronous debug surface) |
| `twi sim [status\|drain\|enter\|exit\|start ...\|resume ...\|stop\|tags ...\|events ...]` | Drive the registered `ITimerProvider` adapter and mirror its outbound events to the log |

## Keys accepted by `twi get` / `twi set`

The command uses snake_case versions of the public fields in `SettingsModel`
and `LayoutModel`. Common examples:

- `auto_reset`, `restart_clears_forgivable`, `retry_min_dwell`
- `retry_level_override_enabled`, `retry_level_override`
- `show_hud`, `show_real_time`, `show_wake_up_time`
- `only_record_first_wake_up_time`, `center_loading_saving`, `language`
- `reset_key`, `retry_key`, `menu_key`
- `subsegment_enable`, `subsegment_pb_path`, `subsegment_load_path`,
  `subsegment_toggle_key`, `subsegment_multi_project`,
  `subsegment_debug_logging`
- `markers_enable`, `markers_edit_mode`, `markers_path`,
  `markers_debug_logging`, `markers_overlay_fill_color`,
  `markers_overlay_label_color`
- Layout: `offset_x`, `offset_y`, `font_size`, `color_a`, `color_b`,
  `leaderboard_font_size`, `leaderboard_offset_x`, `leaderboard_offset_y`,
  `leaderboard_mode`, `leaderboard_markers_time_mode`

Boolean values accept `true/false`, `1/0`, `on/off`, `yes/no`. KeyCodes accept
Unity `KeyCode` names (e.g. `Backspace`, `R`, `Home`, `Tab`). Colors accept
`RRGGBB` or `RRGGBBAA` hex.

## Feature-by-feature test guide

### 1. Timer core / run controls

```text
twi status                 # see game/app state, game time, segment, real time
twi reset                  # verify timers zero and flags clear
twi retry                  # verify one-key retry reloads the level
```

While playing a level, `twi status` should show `segment=True`, `timing=True`
and an increasing `gameTime`. After `twi reset`, `gameTime` should be `0` and
`inSegment` should be `False` (or the transition cache is preserved so the
level does not restart).

### 2. Validity flags (R5)

```text
twi flags list
twi flags raise CheatCode            # unforgivable
twi flags raise CheckpointSkip       # forgivable
twi flags raise Ec                   # soft, increments count
twi flags clear forgivable
twi flags clear soft
twi flags clear all
```

`twi status` should show the hard reasons in the `flags:` line and soft flags
as `Ec xN`.

### 3. Tags / category (R3)

```text
twi tag list
twi tag enable Checkpoint
twi tag enable Jumpless
twi tag set NoEC on
twi tag disable Jumpless
twi tag set NoCheckpoint off
twi status          # enabled tags are listed
```

Tag changes persist to `tags.ini` immediately.

### 4. HUD / layout (R2)

```text
twi hud off
twi hud on
twi layout status
twi layout row list
twi layout row add CurrentState
twi layout row remove 5
twi layout text add 20 400 "Hello {gametime}"
twi layout text list
twi layout set font_size 24
twi layout set offset_x 30
twi layout set color_a FF0000FF
```

The HUD should update on the next frame and the changes should persist to
`layout.ini`.

### 5. Settings panel / general settings

```text
twi panel open
twi panel close
twi set auto_reset false
twi set auto_reset true
twi set language zh-Hans
twi lang list
twi lang set en
twi reload
```

`twi panel open` should show the same IMGUI settings panel as the Home key.

### 6. Presets (R11)

```text
twi preset list
twi preset create test-preset
twi layout set font_size 30
twi preset save
twi preset apply default
twi preset apply test-preset
twi preset delete test-preset
```

After `apply`, the HUD should reflect the preset's saved layout/markers.

### 7. Subsegment module (R8)

```text
twi sub status
twi sub entries
twi sub clear
```

`twi sub status` prints the enabled flag, paths, multi-run state and current
leaderboard entry count. `twi sub entries` lists each loaded reference and its
latest settled diff.

### 8. Markers (R10)

Enter a level, then:

```text
twi marker list
twi marker add range "Test Box"          # uses player position, 2m box
twi marker add checkpoint "CP1" 1
twi marker add grab "My Box"             # requires currently grabbing exactly one object
twi marker toggle m1
twi marker pb 12345
twi marker list
twi marker save
twi marker reload
twi marker clear
```

The marker overlay/feed should react to these changes when edit mode is enabled
(`twi set markers_edit_mode true`).

### 9. Localization (R7)

```text
twi lang list
twi lang set zh-Hans
twi lang set en
twi lang reload
```

The settings panel and HUD labels should switch language immediately.

### 10. Leaderboard HUD

```text
twi leaderboard status
twi leaderboard show
twi leaderboard mode Markers
twi leaderboard mode Subsegment
twi leaderboard hide
twi leaderboard cycle
```

### 11. LevelCollections integration (optional)

```text
twi lc status
twi lc restart
```

`twi lc status` reports whether the LC integration is enabled, whether a
collection run is active, and the current collection name. `twi lc restart`
dispatches the same `lc restart` command used by the retry delegation.

### 12. Config file locations

```text
twi config path
twi config files
```

These print the exact paths used by the plugin so you can verify or edit files
on disk.

### 13. Twilight Cup match mode / round lifecycle (T2–T5)

`twi match` drives the direct `TwilightTimerApi` debug surface synchronously —
ideal for checking lifecycle and data retention without TwilightCore in the
loop.

```text
twi match status
twi match enter
twi match start test-round single 3 Checkpoint Jumpless
twi match status
twi match tags NoCheckpoint,NoEC
twi match segments
twi match leaderboard
twi match penalty
twi match stop
twi match status
twi match exit
```

- `twi match status` reports match/round state, the current tag set, checkpoint
  penalty latch, provider registration, and the match leaderboard state.
- `twi match start <roundId> <single|multi> [retryCount] [tag...]` performs the
  T3.1 full reset and applies the tag push; the clock still starts at the next
  `PlayingLevel` edge. Tags may be separated by spaces or commas.
- `twi match tags [clear|tag...]` changes the pushed tag set mid-round (only
  honored while match mode is active).
- `twi match segments` lists completed round segments and their validity
  snapshots; data remains queryable after `twi match stop` (T3.5).
- `twi match resume ...` re-activates a stopped round with the same round id
  without clearing segments/totals.
- `twi match exit` restores the user's pre-match tag set; it does not clear
  round data.

### 14. `ITimerProvider` adapter / event bus (T1, T4)

`twi sim` exercises the actual adapter registered with TwilightCore's
`TimerProviderRegistry`. Mutating calls are queued through `MainThreadQueue`, so
use `twi sim drain` (or wait one frame) before checking the result.

```text
twi sim status
twi sim events on
twi sim enter
twi sim drain
twi sim start test-round multi 0 Checkpoint
twi sim drain
twi sim status
twi sim tags NoCheckpoint
twi sim drain
twi sim stop
twi sim drain
twi sim exit
twi sim drain
twi sim events off
```

- `twi sim status` prints the provider API version, registration state, match /
  round / segment / real-time queries, completed segments, and active invalid
  marks.
- `twi sim events on` mirrors `SegmentCompleted`, `AttemptSkipped`,
  `RunCompleted`, `IncompleteExit`, and `InvalidMarked` to the BepInEx log so
  the outbound event sequence can be verified.
- `twi sim start/resume/stop/tags` call the interface methods; `twi sim drain`
  runs the queued main-thread actions immediately.

## Test checklist

- [ ] `twi` prints the command summary.
- [ ] `twi status` shows plausible live values while in a level.
- [ ] `twi reset` zeroes timers and clears flags.
- [ ] `twi retry` reloads the current level (or the configured override).
- [ ] `twi hud off/on` hides/shows the timer HUD.
- [ ] `twi panel open/close` opens/closes the settings panel.
- [ ] `twi tag enable/disable` changes the enabled tags and persists them.
- [ ] `twi set language zh-Hans` switches UI language.
- [ ] `twi layout row add/remove` changes the HUD rows.
- [ ] `twi preset create/save/apply` round-trips layout + markers.
- [ ] `twi sub status/entries` works with subsegment data present.
- [ ] `twi marker add/list/toggle/pb` works while in a level.
- [ ] `twi flags raise/clear` shows the expected HUD banner / soft-flag line.
- [ ] `twi lc status` reports correctly with LevelCollections installed or absent.
- [ ] `twi match enter/start/tags/stop/exit` drives a round and restores the user's tags.
- [ ] `twi match segments` retains completed round data after `twi match stop`.
- [ ] `twi sim status` reports the registered provider and its live queries.
- [ ] `twi sim events on` logs the expected T4 outbound event sequence.
