# TESTS

This document explains how to test every TwilightTimer feature from inside the game
using the built-in dev console (default keys **`~`** or **F1**).

## What this gives you

TwilightTimer registers a `twitimer ...` command family with the game's `Shell` console
at plugin load. You can inspect live state, toggle settings, mutate config,
switch tags, edit the HUD layout, manage presets, exercise subsegments and
markers, simulate validity flags, and drive Twilight Cup match rounds / the
registered timer-provider adapter — all without restarting the game or editing
files by hand.

All `twitimer` commands are safe to run from the console. Most mutations call the
same `ConfigService.SaveSettings()` path used by the settings panel, so they
persist to the normal `settings.ini` / `tags.ini` / `layout.ini` files.

## Opening the console

1. Launch Human: Fall Flat with TwilightTimer installed.
2. Press **`~`** (backquote) or **F1** to open the game's dev console.
3. Type `twitimer` and press Enter to see the command list.
4. Type `twitimer help <topic>` for detailed help on one command.

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
| `twitimer` | Print the full command summary |
| `twitimer help [topic]` | Print help for one topic |
| `twitimer status` | Dump live timer/config/HUD/subsegment/marker/LC state |
| `twitimer keys` | List every settable settings/layout key |
| `twitimer get <key>` / `twitimer get all` | Read one config value, or all values |
| `twitimer set <key> <value>` | Set and save a config value |
| `twitimer reload` | Re-read all config + language files from disk |
| `twitimer save` | Save current in-memory config |
| `twitimer reset` | Full-run reset (same as the reset key) |
| `twitimer retry` | One-key retry (same as the retry key, R6) |
| `twitimer hud [on\|off\|toggle\|status]` | Control timer HUD visibility |
| `twitimer panel [open\|close\|toggle\|status]` | Control the settings panel |
| `twitimer leaderboard [cycle\|show\|hide\|mode <Subsegment\|Markers>\|status]` | Control the leaderboard HUD |
| `twitimer layout [status\|row ...\|text ...\|get <key>\|set <key> <value>]` | Inspect/edit the HUD layout |
| `twitimer tag [list\|enable <id>\|disable <id>\|set <id> <on\|off>]` | Toggle enabled tag rules |
| `twitimer lang [list\|set <code>\|reload\|current]` | Manage localization |
| `twitimer preset [list\|current\|create <name>\|apply [name]\|save\|delete <name>]` | Manage presets (R11) |
| `twitimer sub [status\|entries\|clear]` | Inspect/clear the subsegment module |
| `twitimer marker [list\|feed\|add ...\|remove <id>\|toggle <id>\|pb <ms>\|pbclear\|clear\|save\|reload]` | Inspect/edit markers (R10) |
| `twitimer flags [list\|raise <Reason>\|clear [forgivable\|soft\|all]]` | Inspect/mutate validity flags (R5) |
| `twitimer lc [status\|restart]` | Inspect LevelCollections integration or dispatch `lc restart` |
| `twitimer config [path\|files]` | Print TwilightTimer config paths |
| `twitimer match [status\|enter\|exit\|start ...\|resume ...\|stop\|tags ...\|segments\|leaderboard\|penalty]` | Drive Twilight Cup match mode / round lifecycle through `TwilightTimerApi` (synchronous debug surface) |
| `twitimer sim [status\|drain\|enter\|exit\|start ...\|resume ...\|stop\|tags ...\|events ...]` | Drive the registered `ITimerProvider` adapter and mirror its outbound events to the log |

## Keys accepted by `twitimer get` / `twitimer set`

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
twitimer status                 # see game/app state, game time, segment, real time
twitimer reset                  # verify timers zero and flags clear
twitimer retry                  # verify one-key retry reloads the level
```

While playing a level, `twitimer status` should show `segment=True`, `timing=True`
and an increasing `gameTime`. After `twitimer reset`, `gameTime` should be `0` and
`inSegment` should be `False` (or the transition cache is preserved so the
level does not restart).

### 2. Validity flags (R5)

```text
twitimer flags list
twitimer flags raise CheatCode            # unforgivable
twitimer flags raise CheckpointSkip       # forgivable
twitimer flags raise Ec                   # soft, increments count
twitimer flags clear forgivable
twitimer flags clear soft
twitimer flags clear all
```

`twitimer status` should show the hard reasons in the `flags:` line and soft flags
as `Ec xN`.

### 3. Tags / category (R3)

```text
twitimer tag list
twitimer tag enable Checkpoint
twitimer tag enable Jumpless
twitimer tag set NoEC on
twitimer tag disable Jumpless
twitimer tag set NoCheckpoint off
twitimer status          # enabled tags are listed
```

Tag changes persist to `tags.ini` immediately.

### 4. HUD / layout (R2)

```text
twitimer hud off
twitimer hud on
twitimer layout status
twitimer layout row list
twitimer layout row add CurrentState
twitimer layout row remove 5
twitimer layout text add 20 400 "Hello {gametime}"
twitimer layout text list
twitimer layout set font_size 24
twitimer layout set offset_x 30
twitimer layout set color_a FF0000FF
```

The HUD should update on the next frame and the changes should persist to
`layout.ini`.

### 5. Settings panel / general settings

```text
twitimer panel open
twitimer panel close
twitimer set auto_reset false
twitimer set auto_reset true
twitimer set language zh-Hans
twitimer lang list
twitimer lang set en
twitimer reload
```

`twitimer panel open` should show the same IMGUI settings panel as the Home key.

### 6. Presets (R11)

```text
twitimer preset list
twitimer preset create test-preset
twitimer layout set font_size 30
twitimer preset save
twitimer preset apply default
twitimer preset apply test-preset
twitimer preset delete test-preset
```

After `apply`, the HUD should reflect the preset's saved layout/markers.

### 7. Subsegment module (R8)

```text
twitimer sub status
twitimer sub entries
twitimer sub clear
```

`twitimer sub status` prints the enabled flag, paths, multi-run state and current
leaderboard entry count. `twitimer sub entries` lists each loaded reference and its
latest settled diff.

`twitimer sub status` also reports the match gate:
`enable=<user setting> matchSuppressed=<bool> effective=<bool>
samplingAllowedForLevel=<bool>`. The `effective` value is what the module
actually obeys.

**Match suppression (T7.5 / R8.9).** While a match is active the local subsegment
is disabled automatically:

```text
twitimer set subsegment_enable true
twitimer match enter
twitimer sub status              # enable=true matchSuppressed=true effective=false
twitimer sub entries             # no entries; HUD subsegment rows disappear
twitimer match exit
twitimer sub status              # enable=true matchSuppressed=false; recording resumes at the next level
```

Use `twitimer match enter` / `twitimer match exit` to toggle the state without a
real match. Enter a level with reference data present (see section 7) first: the
subsegment leaderboard shows before `match enter`, disappears on enter, and comes
back after exiting the match and starting the next level. No PB file may be
written for a level that was suppressed at any point.

### 8. Markers (R10)

Enter a level, then:

```text
twitimer marker list
twitimer marker add range "Test Box"          # uses player position, 2m box
twitimer marker add checkpoint "CP1" 1
twitimer marker add grab "My Box"             # requires currently grabbing exactly one object
twitimer marker toggle m1
twitimer marker pb 12345
twitimer marker list
twitimer marker save
twitimer marker reload
twitimer marker clear
```

The marker overlay/feed should react to these changes when edit mode is enabled
(`twitimer set markers_edit_mode true`).

### 9. Localization (R7)

```text
twitimer lang list
twitimer lang set zh-Hans
twitimer lang set en
twitimer lang reload
```

The settings panel and HUD labels should switch language immediately.

### 10. Leaderboard HUD

```text
twitimer leaderboard status
twitimer leaderboard show
twitimer leaderboard mode Markers
twitimer leaderboard mode Subsegment
twitimer leaderboard hide
twitimer leaderboard cycle
```

### 11. LevelCollections integration (optional)

```text
twitimer lc status
twitimer lc restart
```

`twitimer lc status` reports whether the LC integration is enabled, whether a
collection run is active, and the current collection name. `twitimer lc restart`
dispatches the same `lc restart` command used by the retry delegation.

### 12. Config file locations

```text
twitimer config path
twitimer config files
```

These print the exact paths used by the plugin so you can verify or edit files
on disk.

### 13. Twilight Cup match mode / round lifecycle (T2–T5)

`twitimer match` drives the direct `TwilightTimerApi` debug surface synchronously —
ideal for checking lifecycle and data retention without TwilightCore in the
loop.

```text
twitimer match status
twitimer match enter
twitimer match start test-round single 3 Checkpoint Jumpless
twitimer match status
twitimer match tags NoCheckpoint,NoEC
twitimer match segments
twitimer match leaderboard
twitimer match penalty
twitimer match stop
twitimer match status
twitimer match exit
```

- `twitimer match status` reports match/round state, the current tag set, checkpoint
  penalty latch, provider registration, and the match leaderboard state.
- `twitimer match start <roundId> <single|multi> [retryCount] [tag...]` performs the
  T3.1 full reset and applies the tag push; the clock still starts at the next
  `PlayingLevel` edge. Tags may be separated by spaces or commas.
- `twitimer match tags [clear|tag...]` changes the pushed tag set mid-round (only
  honored while match mode is active).
- `twitimer match segments` lists completed round segments and their validity
  snapshots; data remains queryable after `twitimer match stop` (T3.5).
- `twitimer match resume ...` re-activates a stopped round with the same round id
  without clearing segments/totals.
- `twitimer match exit` restores the user's pre-match tag set; it does not clear
  round data.

### 14. `ITimerProvider` adapter / event bus (T1, T4)

`twitimer sim` exercises the actual adapter registered with TwilightCore's
`TimerProviderRegistry`. Mutating calls are queued through `MainThreadQueue`, so
use `twitimer sim drain` (or wait one frame) before checking the result.

```text
twitimer sim status
twitimer sim events on
twitimer sim enter
twitimer sim drain
twitimer sim start test-round multi 0 Checkpoint
twitimer sim drain
twitimer sim status
twitimer sim tags NoCheckpoint
twitimer sim drain
twitimer sim stop
twitimer sim drain
twitimer sim exit
twitimer sim drain
twitimer sim events off
```

- `twitimer sim status` prints the provider API version, registration state, match /
  round / segment / real-time queries, completed segments, and active invalid
  marks.
- `twitimer sim events on` mirrors `SegmentCompleted`, `AttemptSkipped`,
  `RunCompleted`, `IncompleteExit`, and `InvalidMarked` to the BepInEx log so
  the outbound event sequence can be verified.
- `twitimer sim start/resume/stop/tags` call the interface methods; `twitimer sim drain`
  runs the queued main-thread actions immediately.

## Test checklist

- [ ] `twitimer` prints the command summary.
- [ ] `twitimer status` shows plausible live values while in a level.
- [ ] `twitimer reset` zeroes timers and clears flags.
- [ ] `twitimer retry` reloads the current level (or the configured override).
- [ ] `twitimer hud off/on` hides/shows the timer HUD.
- [ ] `twitimer panel open/close` opens/closes the settings panel.
- [ ] `twitimer tag enable/disable` changes the enabled tags and persists them.
- [ ] `twitimer set language zh-Hans` switches UI language.
- [ ] `twitimer layout row add/remove` changes the HUD rows.
- [ ] `twitimer preset create/save/apply` round-trips layout + markers.
- [ ] `twitimer sub status/entries` works with subsegment data present.
- [ ] `twitimer match enter` disables subsegment (`matchSuppressed=true`, `effective=false`) and hides its leaderboard; `twitimer match exit` restores the setting.
- [ ] `twitimer marker add/list/toggle/pb` works while in a level.
- [ ] `twitimer flags raise/clear` shows the expected HUD banner / soft-flag line.
- [ ] `twitimer lc status` reports correctly with LevelCollections installed or absent.
- [ ] `twitimer match enter/start/tags/stop/exit` drives a round and restores the user's tags.
- [ ] `twitimer match segments` retains completed round data after `twitimer match stop`.
- [ ] `twitimer sim status` reports the registered provider and its live queries.
- [ ] `twitimer sim events on` logs the expected T4 outbound event sequence.
