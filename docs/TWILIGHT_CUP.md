# Twilight Cup Match Integration (TwilightTimer branch)

> [中文版](zh/TWILIGHT_CUP.md)

This branch (`TwilightTimer`) is the special edition of TwilightTimer for the
**Twilight Cup** (黄昏杯) 1v1 speedrun competition. It hard-depends on
**TwilightCore** (the player-side plugin providing WebSocket, chat, ready
lock, and the built-in Level Collections engine) and serves as its timing
engine. Everything match-related activates **only inside a match session**;
local practice and solo speedrunning behave exactly like `main` (except for
the dependency switch itself).

## Dependency

- `TwilightCore` is a compile-time + BepInEx hard dependency
  (`[BepInDependency("TwilightCore", HardDependency)]`). The plugin does not
  load without it.
- The csproj references TwilightCore's **built DLL** (`bin/Release/...`).
  Build TwilightCore first, or override the path with
  `-p:TWILIGHTCORE=/path/to/TwilightCore.dll`.
- The old LevelCollections soft dependency and its reflection bridge are
  gone; collection features (last-level completion, `lc restart` retry,
  `{collection}` template var) consume `TwilightCore.CollectionManager`
  directly.

## Match mode (T2)

Entered/exited only via `TwilightTimerApi` (in production: TwilightCore's
`ITimerProvider` adapter). No settings-panel switch exists.

- On enter: the user's enabled tag set is snapshotted; the panel title is
  badged ("Twilight Cup match #roundId"); conflicting settings become
  view-only (tag checkboxes, auto-reset, reset/retry keybinds).
- Pause time always counts (T7.3) — since the upstream timer now always
  counts paused time globally, no match-only enforcement is needed.
- On exit: the user's tag set is restored. Round data survives until the
  next `StartRound` (reconnection re-reporting).
- `tags.ini` is never written with match tags: the save path swaps the user
  snapshot back in while a match is active.

## Round lifecycle (T3) and events (T4)

`TwilightTimerApi.StartRound(roundId, isSingle, retryCount, tags)` performs a full
reset (manual-reset scope + all invalid marks) and applies the pushed tags;
timing still starts at the first `PlayingLevel` edge. `StopRound()` stops
timing; segment data stays queryable until the next `StartRound`.
`RoundTracker.ResumeRound()` re-activates the same round after a reconnect
without clearing segments, totals or the next-segment index; TwilightCore
reaches it through the optional `IResumableTimerProvider` extension.

Outbound events (`TimerEvents`, re-exposed by the ITimerProvider adapter): `SegmentCompleted(index, durationMs, totalMs)`,
`AttemptSkipped(index)`, `RunCompleted(totalMs)`, `IncompleteExit(index)`,
`InvalidMarked(reason, unforgivable)`. All durations are game-time
milliseconds (the R1.1 engine is the sole authority). Skip vs incomplete
exit is decided deferred: an unpassed level exit followed by another
in-round segment is a skip; if the round ends first (StopRound or LC
`RunAborted`), it is an incomplete exit.

## Tag push (T5)

Pushed tag ids validated against `TagRuleRegistry`; unsupported ids
(Glitchless / Pinch / No EC / Achievement, or anything unknown) are logged
and ignored. The supported mapping: `Checkpoint`, `NoCheckpoint`,
`Jumpless` (plus the non-CT built-in `Voiceline`). The pushed set shows on
the HUD tags line and the panel Category page.

## In-round constraints (T7)

While a round is active: reset key and one-key retry (both the level reload
and the collection restart) are log-only no-ops; auto-reset never clears
round data; validity detection runs as usual and new marks are reported
live. A pushed `Jumpless` tag also physically disables the jump key for the
round — the input-layer enforcement (R3.5.3) follows the live tag set that
`SetRoundTags` swaps in, and lifts automatically when the match ends.

## ITimerProvider adapter (T1)

`Match/TwilightTimerProvider.cs` implements TwilightCore's published
`TwilightCore.Timer.ITimerProvider` and self-registers with
`TimerProviderRegistry` at plugin load (unregisters on destroy). Mutating
calls are marshaled through `MainThreadQueue` (WebSocket-thread safe);
queries are point-in-time snapshots; the internal `TimerEvents` are
re-exposed as the interface's events (per-subscriber try/catch). It also
implements the optional `IRealtimeTimerProvider`, so TwilightCore's
`live_time` stream carries the Real Time wall-clock value as
`real_time_ms`. In-game, `twitimer sim status` prints the registered provider's
live state.

## In-match leaderboard

`Hud/MatchLeaderboardHud.cs` renders a real-time leaderboard during match rounds,
anchored at the middle of the left screen edge in the timer's own style
(see [HUD.md](HUD.md) for formats/sorting/config). The data direction is the
reverse of the provider: TwilightCore owns the server-fed state and this
plugin consumes it. The seam is `Match/LeaderboardFeed.cs` — a reflection
gateway probing for `TwilightCore.Leaderboard.LeaderboardApi` (specified in
[LEADERBOARD_REQ.md](LEADERBOARD_REQ.md)); until TwilightCore implements
that API, the gateway probe fails cleanly and the leaderboard runs in
**transition mode**: the local row only, built entirely from
`RoundTracker` / `RunState` / `CollectionManager`, name falling back to the
localized "You" and no seat colour.

## Driving without TwilightCore (debug)

`TwilightTimerApi` is the direct-consumption surface (T1.6): EnterMatchMode /
ExitMatchMode / StartRound / StopRound / SetRoundTags / RoundStatusString.
In-game, `twitimer match ...` drives this surface synchronously; `twitimer sim ...` drives
the registered `ITimerProvider` adapter through `MainThreadQueue`.

## Acceptance mapping

The acceptance scenarios A1–A13 of the requirements (T*-numbered clauses)
map to: tag push behavior (A1/A8), event sequences for MULTI/SINGLE rounds
(A2–A4), in-round key/constraint behavior (A5/A6), data retention (A7),
exit restoration (A9), local collection runs on the built-in LC (A10/A11),
and degraded/no-timer modes (A12/A13).
