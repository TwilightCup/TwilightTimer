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
| `twitimer clock [status\|history [n]\|clear]` | Inspect the pure-tick game clock and segment boundaries (R1.11) |
| `twitimer keys` | List every settable settings/layout key |
| `twitimer get <key>` / `twitimer get all` | Read one config value, or all values |
| `twitimer set <key> <value>` | Set and save a config value |
| `twitimer reload` | Re-read all config + language files from disk |
| `twitimer save` | Save current in-memory config |
| `twitimer reset` | Full-run reset (same as the reset key) |
| `twitimer retry` | One-key retry (same as the retry key, R6) |
| `twitimer pass [real]` | Simulate a level-completion flow: default sets `Game.passedLevel` then dispatches `Game.Fall`; `real` clears momentum, releases grabs and teleports the player into the pass zone so the game's own triggers complete the level. No subsegment/marker PB is written |
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
| `twitimer config [path\|files\|source [status\|hsrtimer\|twilighttimer\|toggle]]` | Print TwilightTimer config paths or switch the config source directory |
| `twitimer match [status\|enter\|exit\|start ...\|resume ...\|stop\|tags ...\|segments\|leaderboard\|penalty]` | Drive Twilight Cup match mode / round lifecycle through `TwilightTimerApi` (synchronous debug surface) |
| `twitimer sim [status\|drain\|enter\|exit\|start ...\|resume ...\|stop\|tags ...\|resolvetag ...\|events ...]` | Drive the registered `ITimerProvider` adapter, resolve server tag strings, and mirror its outbound events to the log |

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
twitimer clock                  # pure-tick clock: PlayableTicks, segment start/end ticks
twitimer clock history          # last segment start/end ticks (jitter check, R1.11)
twitimer clock clear            # clear the boundary history
twitimer reset                  # verify timers zero and flags clear
twitimer retry                  # verify one-key retry reloads the level
twitimer pass                   # simulate passing the current level (过关)
twitimer pass real              # teleport into the pass zone and let the game complete it
```

While playing a level, `twitimer status` should show `segment=True`, `timing=True`
and an increasing `gameTime`. After `twitimer reset`, `gameTime` should be `0` and
`inSegment` should be `False` (or the transition cache is preserved so the
level does not restart).

`twitimer pass` drives the real completion flow without touching the exit zone: it
sets `Game.passedLevel` (like `Game.EnterPassZone`), waits one frame for the
engine to latch `LevelPassed`, then calls `Game.Fall` — so a BuiltIn campaign
level advances via `PassLevel` → `StartNextLevel`, and a Workshop/EditorPick
level leaves via `PauseLeave`. Afterwards `twitimer status` should show the final
segment time and (for the last level of a run) a `lastRun` value. It requires
an active segment with a local player and is a no-op for clients / during
replays. **Subsegment and marker PBs are deliberately not written** (it is a
test pass), so real PB files stay untouched.

`twitimer pass real` exercises the actual trigger chain instead of forcing the
flag: it zeroes the player's momentum (linear + angular on every body part),
releases both hand grabs, and teleports the local player to the center of the
current level's `LevelPassTrigger` (the 通关判定箱). The game's own flow then
takes over — the pass zone latches `passedLevel`, the player drops into the
`FallTrigger` below and `Game.Fall` completes the level. PBs are suppressed
exactly as in default mode. If the command reports that `LevelPassed` was not
latched, the level's pass trigger could not be entered (collider/tag/layout).

**Boundary determinism (R1.11).** `twitimer clock` prints the raw integer tick clock
(`playableTicks`, `segmentStartTicks`, `pendingEndTicks`, …). `twitimer clock
history` lists each segment start/end tick: load + finish a level 50 times and
the `dur=` (end tick − start tick) for the same level and same inputs must be
**identical** every time (zero ±1 jitter). `twitimer clock clear` resets the history
before a measurement run.

Each history `end` line carries two diagnostics: `src=hook|poll` says whether
the precise `Game.Fall` boundary hook fixed the end tick (a genuine completion)
or the polling loop recorded it (a mid-level quit), and `step=` is the global
physics step it was observed on. A `pass` line marks the authoritative pass
tick. Together they show that physics steps between the pass and the observed
state flip are **not** counted into the segment — the source of the old ±1.

The same pair exists on the start side: `load` is the `Game.AfterLoad` hook frame
(the authoritative segment start) and the following `start` line is the polling
loop consuming that latch. A `start tick` equal to `load tick` with a larger
`step=` proves the start boundary no longer depends on when the poll noticed it.

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
The **About** tab is the first entry in the panel navigation; it shows the
plugin name, version, the first two lines of the MIT license, a **GitHub
Repository** button, and a **Check Update** button (R13); when a newer release
is found it shows the release title, its date + Highlights summary, and
**Open Release Page** / **Update** buttons. `twitimer about` prints
the same identity/license/repository text so it can be checked without a
screenshot:

```text
twitimer panel open
twitimer about
```

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

The config source can be switched between the fork's own directory and the
upstream HSRTimer directory (only when `config/HSRTimer/` exists; refused during
a match round):

```text
twitimer config source status
twitimer config source hsrtimer
twitimer config path          # now reports config/HSRTimer/
twitimer set show_hud false   # writes settings.ini in config/HSRTimer/
twitimer config source twilighttimer
```

`twitimer status` also reports `source=TwilightTimer|HSRTimer` at the end of its
`configDir=` line. The switch itself is stored in
`config/TwilightTimer/config_dir.ini` (`[config] use_hsrtimer`).

### 13. Update checker (R13)

The About tab's **Check Update** flow can be exercised end to end from the
console (results are logged to the BepInEx log, which `twitimer-cmd.sh` reads):

```text
twitimer update status                        # phase / repo base / feed / last checked tag
twitimer update check                         # reads github.com/{owner}/{repo}/releases.atom
twitimer update status                        # phase should become HasUpdate (or up to date / error)
twitimer update apply                         # download + install the release DLL
twitimer update status                        # phase should become RestartRequired
```

Without network access you can still exercise both the success and the failure
paths by pointing the checker at a local HTTP server:

```text
twitimer update base http://127.0.0.1:PORT/repo   # session-only repo-base override
twitimer update check                              # feed URL = <base>/releases.atom
twitimer update apply                              # download URL = <base>/releases/download/<tag>/TwilightTimer-v<ver>.dll
twitimer update base clear                         # restore the real repo base
```

Notes:

- The feed must be GitHub Atom XML (`<feed>` with `<entry>` items; each entry's
  alternate link ends in `/releases/tag/{tag}`, with `<title>` and optional
  `<content type="html">`). The newest **non-prerelease** entry is used.
- For a real apply test, serve a valid `.NET` assembly (e.g. a copy of
  `TwilightTimer-v0.0.0.0.dll`) at the derived download path — the installer
  rejects zero-byte / invalid downloads, so a non-assembly file exercises the
  "invalid plugin DLL" error path; serving no file at all exercises the
  "release asset not found" (404) path.
- After a successful `apply`, verify on disk that `BepInEx/plugins/` contains
  `TwilightTimer-v{newVersion}.dll` and no older `TwilightTimer-v*.dll` (an
  undeletable one becomes `TwilightTimer-v*.dll.dis`, cleaned up at the next
  launch).
- `twitimer update cancel` aborts an in-flight check/download.

### 14. Twilight Cup match mode / round lifecycle (T2–T5)

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

### 15. `ITimerProvider` adapter / event bus (T1, T4)

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
- `twitimer sim resolvetag <serverTag...>` runs the same mapping TwilightCore uses
  when it ingests a `round_start` pick (`ITimerTagProvider.ResolveServerTag`), so
  it proves which server CT tag strings the provider accepts. Registered tags
  match loosely: `Glitchless`, `glitchless`, `No EC`, `no-checkpoint`,
  `NoCheckpoint` all resolve; unregistered strings print `<unsupported>`. Use it
  to confirm an extension tag added via `TagRuleRegistry` is receivable from the
  server too.

```text
twitimer sim resolvetag Glitchless "No Checkpoint" "No EC" Voiceline Pinch
# Glitchless -> Glitchless   (No Checkpoint -> NoCheckpoint, No EC -> NoEC,
# Voiceline -> Voiceline,     Pinch -> <unsupported>)
```

### 16. Shared leaderboard under the match leaderboard (T7.6)

During a match session the shared leaderboard (subsegment/markers HUD) must be
shown only while the match leaderboard is shown, hang directly below it, and
ignore its own cycle key. `twitimer leaderboard status` reports the resolved
state (`matchMode`, `matchLeaderboard`, `follow`, `anchoredBelowMatch`,
`matchBottomY`, `topY`).

```text
twitimer match enter
twitimer match start t76 multi 3 Checkpoint
twitimer leaderboard mode Markers
level 7 0
twitimer marker add range t76
twitimer marker feed
twitimer leaderboard status
twitimer hud off
twitimer leaderboard status
twitimer hud on
twitimer set show_leaderboard false
twitimer leaderboard status
twitimer set show_leaderboard true
twitimer match stop
twitimer match exit
```

- With the round active and the HUD on, `twitimer leaderboard status` must show
  `matchLeaderboard = shown, follow = shown, anchoredBelowMatch = true`, and
  `topY = matchBottomY + 6`.
- `twitimer hud off` or `twitimer set show_leaderboard false` must report
  `matchLeaderboard = hidden, follow = hidden` — the shared leaderboard hides
  with the match leaderboard (the `level`/`marker` rows are only needed to give
  the markers HUD something to draw).
- The mode-cycle key (`Subsegment.ToggleKey`, default `Tab`) must not cycle the
  shared leaderboard while match mode is active, including between rounds
  (`MATCH_STOPPED` with the match still active).
- Outside a match, `twitimer leaderboard cycle/show/hide/mode` and the cycle key
  behave exactly as before (standalone center anchor).

## Test checklist

- [ ] `twitimer` prints the command summary.
- [ ] `twitimer status` shows plausible live values while in a level.
- [ ] `twitimer reset` zeroes timers and clears flags.
- [ ] `twitimer clock` shows integer ticks and `twitimer clock history` records identical `dur=` for repeated identical runs (R1.11).
- [ ] `twitimer retry` reloads the current level (or the configured override).
- [ ] `twitimer pass` completes the current level; `twitimer status` shows the recorded segment and (on the final level) `lastRun`, and no subsegment/marker PB file changed.
- [ ] `twitimer pass real` teleports the player into the pass zone and the game's own trigger flow completes the level (with `LevelPassed` latched); PBs still not written.
- [ ] `twitimer hud off/on` hides/shows the timer HUD.
- [ ] `twitimer panel open/close` opens/closes the settings panel.
- [ ] The About tab (first in the navigation) shows name/version/license and the repository button; `twitimer about` matches it.
- [ ] `twitimer update check` reports up to date / shows a newer release / shows a one-line error (offline), and `twitimer update apply` installs the DLL (R13).
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
- [ ] While match mode is active, the shared leaderboard hangs below the match leaderboard and follows its `show_hud`/`show_leaderboard` visibility; its cycle key does nothing (T7.6).
- [ ] `twitimer match segments` retains completed round data after `twitimer match stop`.
- [ ] `twitimer sim status` reports the registered provider and its live queries.
- [ ] `twitimer sim events on` logs the expected T4 outbound event sequence.
