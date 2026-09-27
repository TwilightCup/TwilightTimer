# TESTS(测试文档)

> **English (source of truth)**: [TESTS.md](TESTS.md)

本文说明如何**在游戏内**通过自带的开发者控制台(默认按键 **`~`** 或 **F1**)测试 TwilightTimer 的每一个功能。

## 这是什么

TwilightTimer 在插件加载时向游戏的 `Shell` 控制台注册了一套 `twitimer ...` 命令。你可以查看实时状态、切换设置、修改配置、开关标签(tags)、编辑 HUD 布局、管理预设(presets)、操作分段(subsegment)与标记(markers)、模拟有效性标记(validity flags)、以及驱动黄昏杯回合 / 已注册的计时器 provider 适配器——全部无需重启游戏或手工编辑文件。

所有 `twitimer` 命令都可以安全地在控制台中执行。大多数修改会走与设置面板相同的 `ConfigService.SaveSettings()` 流程,因此会正常持久化到 `settings.ini` / `tags.ini` / `layout.ini`。

## 打开控制台

1. 安装 TwilightTimer 后启动《人类一败涂地》。
2. 按 **`~`**(波浪线 / 反引号)或 **F1** 打开游戏自带的开发者控制台。
3. 输入 `twitimer` 并回车查看命令列表。
4. 输入 `twitimer help <topic>` 查看某个命令的详细帮助。

> 如果控制台没有出现,可能是该游戏版本的按键有变动。插件启动时仍会在 BepInEx 日志中输出 `TwilightTimer: dev-console commands registered`。

> **关于大小写:** 游戏控制台会把整行输入转为小写后再执行命令。因此 TwilightTimer 会把标识符(标签 id、语言代码、预设名、排行榜模式)按大小写不敏感方式解析并规范化为标准写法。自由文本(例如自定义文本或标记名称)会按控制台传入的内容保存,即通常是小写。

## 命令参考

| 命令 | 说明 |
|---|---|
| `twitimer` | 打印完整命令摘要 |
| `twitimer help [topic]` | 打印某个主题的帮助 |
| `twitimer status` | 输出计时器 / 配置 / HUD / 分段 / 标记 / LC 的实时状态 |
| `twitimer clock [status\|history [n]\|clear]` | 查看纯整数 tick 游戏时钟与分段边界(R1.11) |
| `twitimer keys` | 列出所有可设置的 settings/layout 键 |
| `twitimer get <key>` / `twitimer get all` | 读取单个配置值,或全部配置值 |
| `twitimer set <key> <value>` | 设置并保存一个配置值 |
| `twitimer reload` | 从磁盘重新读取所有配置文件与语言文件 |
| `twitimer save` | 保存当前内存中的配置 |
| `twitimer reset` | 整局重置(等同于重置键) |
| `twitimer retry` | 一键重试(等同于重试键,R6) |
| `twitimer pass [real]` | 模拟通关流程:默认设置 `Game.passedLevel` 后派发 `Game.Fall`;`real` 清零动量、松手并把玩家传送到判定箱内,由游戏自身触发完成通关。两种模式都不写入分段 / 标记 PB |
| `twitimer hud [on\|off\|toggle\|status]` | 控制计时 HUD 的显示 |
| `twitimer panel [open\|close\|toggle\|status]` | 控制设置面板 |
| `twitimer leaderboard [cycle\|show\|hide\|mode <Subsegment\|Markers>\|status]` | 控制排行榜 HUD |
| `twitimer layout [status\|row ...\|text ...\|get <key>\|set <key> <value>]` | 查看 / 编辑 HUD 布局 |
| `twitimer tag [list\|enable <id>\|disable <id>\|set <id> <on\|off>]` | 开关启用的标签规则 |
| `twitimer lang [list\|set <code>\|reload\|current]` | 管理本地化 |
| `twitimer preset [list\|current\|create <name>\|apply [name]\|save\|delete <name>]` | 管理预设(R11) |
| `twitimer sub [status\|entries\|clear]` | 查看 / 清空分段模块 |
| `twitimer marker [list\|feed\|add ...\|remove <id>\|toggle <id>\|pb <ms>\|pbclear\|clear\|save\|reload]` | 查看 / 编辑标记(R10) |
| `twitimer flags [list\|raise <Reason>\|clear [forgivable\|soft\|all]]` | 查看 / 修改有效性标记(R5) |
| `twitimer lc [status\|restart]` | 查看 LevelCollections 集成或触发 `lc restart` |
| `twitimer config [path\|files\|source [status\|hsrtimer\|twilighttimer\|toggle]]` | 打印 TwilightTimer 配置路径,或切换配置来源目录 |
| `twitimer match [status\|enter\|exit\|start ...\|resume ...\|stop\|tags ...\|segments\|leaderboard\|penalty]` | 通过 `TwilightTimerApi`(同步调试面)驱动黄昏杯比赛模式 / 回合生命周期 |
| `twitimer sim [status\|drain\|enter\|exit\|start ...\|resume ...\|stop\|tags ...\|resolvetag ...\|events ...]` | 驱动已注册的 `ITimerProvider` 适配器,解析服务端标签字符串,并把其对外事件镜像到日志 |
| `twitimer about` | 打印插件名称、版本、许可证声明与仓库 URL(R12) |
| `twitimer update [status\|check\|apply\|cancel\|base [url]]` | 从 GitHub releases 检测 / 安装插件更新(R13) |

## `twitimer get` / `twitimer set` 可接受的键

命令使用 `SettingsModel` 与 `LayoutModel` 公共字段的 snake_case 形式。常见示例:

- `auto_reset`、`restart_clears_forgivable`、`retry_min_dwell`
- `retry_level_override_enabled`、`retry_level_override`
- `show_hud`、`show_real_time`、`show_wake_up_time`
- `only_record_first_wake_up_time`、`center_loading_saving`、`language`
- `reset_key`、`retry_key`、`menu_key`
- `subsegment_enable`、`subsegment_pb_path`、`subsegment_load_path`、
  `subsegment_toggle_key`、`subsegment_multi_project`、`subsegment_debug_logging`
- `markers_enable`、`markers_edit_mode`、`markers_path`、`markers_debug_logging`、
  `markers_overlay_fill_color`、`markers_overlay_label_color`
- 布局: `offset_x`、`offset_y`、`font_size`、`color_a`、`color_b`、
  `leaderboard_font_size`、`leaderboard_offset_x`、`leaderboard_offset_y`、
  `leaderboard_mode`、`leaderboard_markers_time_mode`

布尔值接受 `true/false`、`1/0`、`on/off`、`yes/no`。键位接受 Unity `KeyCode`
名称(例如 `Backspace`、`R`、`Home`、`Tab`)。颜色接受 `RRGGBB` 或 `RRGGBBAA` 十六进制。

## 分功能测试指南

### 1. 计时核心 / 成绩控制

```text
twitimer status                 # 查看游戏/应用状态、游戏时间、分段、现实时间
twitimer clock                  # 纯整数 tick 时钟:PlayableTicks、分段起止 tick
twitimer clock history          # 最近的分段起止 tick(抖动检查,R1.11)
twitimer clock clear            # 清空边界历史
twitimer reset                  # 验证计时器归零、标记被清除
twitimer retry                  # 验证一键重试会重载关卡
twitimer pass                   # 模拟通关当前关卡(过关)
twitimer pass real              # 传送到判定箱内,让游戏自身完成通关
```

在关卡内,`twitimer status` 应显示 `segment=True`、`timing=True` 且 `gameTime` 持续增长。
执行 `twitimer reset` 后,`gameTime` 应为 `0`,`inSegment` 应为 `False`(或过渡缓存被保留,关卡不会因此重新计时)。

`twitimer pass` 在不触碰出口判定箱的情况下驱动真实通关流程:它像 `Game.EnterPassZone` 一样设置
`Game.passedLevel`,等待一帧让引擎闩锁 `LevelPassed`,然后调用 `Game.Fall` —— 因此 BuiltIn 战役关卡经由
`PassLevel` → `StartNextLevel` 前进,Workshop / EditorPick 关卡经由 `PauseLeave` 离开。之后
`twitimer status` 应显示最终分段时间,且(整局最后一关)`lastRun` 有值。它需要一个带本地玩家的活跃分段,
对客户端 / 重放播放无效。**分段与标记 PB 故意不写入**(测试性通关),真实 PB 文件不受影响。

`twitimer pass real` 走真实的触发链而不是强行设置标志:它清零玩家动量(所有身体部位的线速度与角速度)、
松开双手抓取,并把本地玩家传送到当前关卡 `LevelPassTrigger`(通关判定箱)中心。之后游戏自身流程接管 ——
判定箱闩锁 `passedLevel`,玩家落入下方的 `FallTrigger`,`Game.Fall` 完成通关。PB 抑制方式与默认模式完全一致。
若命令报告 `LevelPassed` 未被闩锁,说明关卡的通关触发箱无法进入(碰撞体 / 标签 / 布局问题)。

**边界确定性(R1.11)。** `twitimer clock` 打印原始整数 tick 时钟(`playableTicks`、`segmentStartTicks`、`pendingEndTicks` 等)。`twitimer clock history` 列出每次分段起止 tick:连续载入 + 通关同一关卡 50 次,相同操作下的 `dur=`(终点 tick − 起点 tick)必须**完全一致**(零 ±1 抖动)。测量前用 `twitimer clock clear` 清空历史。

每条 `end` 记录带两个诊断字段:`src=hook|poll` 表示终点 tick 是由精确的 `Game.Fall` 边界 hook 锁定(真实通关)还是由轮询记录(中途退出);`step=` 是观察到该边界的全局物理帧。另有一条 `pass` 行标记权威过关 tick。二者合起来说明:**过关到状态翻转之间的物理帧没有被计入分段** —— 这正是旧版 ±1 抖动的来源。

起点侧同样成对出现:`load` 是 `Game.AfterLoad` hook 帧(权威分段起点),紧随其后的 `start` 行是轮询消费该闩锁的结果。`start` 的 tick 等于 `load` 的 tick 而 `step=` 更大,即证明起点边界不再取决于轮询何时发现它。

### 2. 有效性标记(R5)

```text
twitimer flags list
twitimer flags raise CheatCode            # 不可原谅
twitimer flags raise CheckpointSkip       # 可原谅
twitimer flags raise Ec                   # 软标记,计数递增
twitimer flags clear forgivable
twitimer flags clear soft
twitimer flags clear all
```

`twitimer status` 的 `flags:` 行应显示硬性原因,软标记显示为 `Ec xN`。

### 3. 标签 / 类别(R3)

```text
twitimer tag list
twitimer tag enable Checkpoint
twitimer tag enable Jumpless
twitimer tag set NoEC on
twitimer tag disable Jumpless
twitimer tag set NoCheckpoint off
twitimer status          # 会列出已启用的标签
```

标签改动会立即持久化到 `tags.ini`。

### 4. HUD / 布局(R2)

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

HUD 应在下一帧生效,且改动持久化到 `layout.ini`。

### 5. 设置面板 / 常规设置

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

`twitimer panel open` 应弹出与 Home 键相同的 IMGUI 设置面板。
**关于** 标签页是面板导航的第一项;它显示插件名称、版本、MIT 许可证的前两行、一个 **GitHub 仓库** 按钮,以及一个 **检查更新** 按钮(R13);发现新版本时会显示发布标题、其日期 + Highlights 摘要,以及 **打开 Release 页面** / **更新** 按钮。`twitimer about` 打印相同的身份 / 许可证 / 仓库文本,因此无需截图即可核对:

```text
twitimer panel open
twitimer about
```

### 6. 预设(R11)

```text
twitimer preset list
twitimer preset create test-preset
twitimer layout set font_size 30
twitimer preset save
twitimer preset apply default
twitimer preset apply test-preset
twitimer preset delete test-preset
```

执行 `apply` 后,HUD 应反映该预设保存的布局 / 标记。

### 7. 分段模块(R8)

```text
twitimer sub status
twitimer sub entries
twitimer sub clear
```

`twitimer sub status` 打印启用标志、路径、多局状态与当前排行榜条目数。
`twitimer sub entries` 列出每个已加载参考及其最新结算的差值。

`twitimer sub status` 同时报告比赛门控:
`enable=<用户设置> matchSuppressed=<bool> effective=<bool>
samplingAllowedForLevel=<bool>`。模块实际遵循的是 `effective`。

**比赛抑制(T7.5 / R8.9)。** 比赛激活期间本地分段对比会自动禁用:

```text
twitimer set subsegment_enable true
twitimer match enter
twitimer sub status              # enable=true matchSuppressed=true effective=false
twitimer sub entries             # 无条目; HUD 分段行消失
twitimer match exit
twitimer sub status              # enable=true matchSuppressed=false; 从下一关恢复记录
```

用 `twitimer match enter` / `twitimer match exit` 即可在不进行真实比赛的情况下切换
该状态。先进入一个带有参考数据的关卡(见本节):分段排行榜在 `match enter` 前显示、
进入后消失,退出比赛并开始下一关后恢复。任何时刻被抑制过的关卡都不得写入 PB。

### 8. 标记(R10)

进入关卡后:

```text
twitimer marker list
twitimer marker add range "Test Box"          # 使用玩家位置,2 米盒子
twitimer marker add checkpoint "CP1" 1
twitimer marker add grab "My Box"             # 需要当前恰好抓取一个物体
twitimer marker toggle m1
twitimer marker pb 12345
twitimer marker list
twitimer marker save
twitimer marker reload
twitimer marker clear
```

当编辑模式开启(`twitimer set markers_edit_mode true`)时,标记覆盖层 / feed 应响应这些改动。

排行榜 feed 必须**按尝试**重置:`twitimer pass` 过关后进入下一次尝试——即使下一关仍是同一关(剧情重复关卡)——`twitimer marker feed` 应只列出新尝试的行。新尝试的首次触发会**替换**掉上一次尝试的残留行,而不是追加在其后。

### 9. 本地化(R7)

```text
twitimer lang list
twitimer lang set zh-Hans
twitimer lang set en
twitimer lang reload
```

设置面板与 HUD 文案应即时切换语言。

### 10. 排行榜 HUD

```text
twitimer leaderboard status
twitimer leaderboard show
twitimer leaderboard mode Markers
twitimer leaderboard mode Subsegment
twitimer leaderboard hide
twitimer leaderboard cycle
```

### 11. LevelCollections 集成(可选)

```text
twitimer lc status
twitimer lc restart
```

`twitimer lc status` 报告 LC 集成是否启用、是否处于集合运行中,以及当前集合名称。
`twitimer lc restart` 触发与重试委托相同的 `lc restart` 命令。

### 12. 配置文件位置

```text
twitimer config path
twitimer config files
```

打印插件使用的确切路径,方便你在磁盘上核对或编辑文件。

配置来源可在 fork 自己的目录与上游 HSRTimer 目录之间切换(仅当 `config/HSRTimer/` 存在时可用;比赛回合中会拒绝):

```text
twitimer config source status
twitimer config source hsrtimer
twitimer config path          # 此时显示 config/HSRTimer/
twitimer set show_hud false   # settings.ini 写入 config/HSRTimer/
twitimer config source twilighttimer
```

`twitimer status` 也会在末尾的 `configDir=` 行报告 `source=TwilightTimer|HSRTimer`。开关本身存放在 `config/TwilightTimer/config_dir.ini`(`[config] use_hsrtimer`)。

### 13. 更新检查(R13)

关于标签页的 **检查更新** 流程可以从控制台端到端测试(结果写入 BepInEx 日志,`twitimer-cmd.sh` 可直接读取):

```text
twitimer update status                        # 阶段 / 仓库基地址 / feed / 上次检测的版本 tag
twitimer update check                         # 读取 github.com/{owner}/{repo}/releases.atom
twitimer update status                        # 阶段应变 HasUpdate(或已最新 / 出错)
twitimer update apply                         # 下载并安装发布 DLL
twitimer update status                        # 阶段应变 RestartRequired
```

无网络时,可把检测指向本地 HTTP 服务器来分别走通成功与失败分支:

```text
twitimer update base http://127.0.0.1:PORT/repo   # 仅本次会话的仓库基地址覆盖
twitimer update check                              # feed 地址 = <base>/releases.atom
twitimer update apply                              # 下载地址 = <base>/releases/download/<tag>/TwilightTimer-v<ver>.dll
twitimer update base clear                         # 恢复真实仓库基地址
```

说明:

- feed 必须是 GitHub Atom XML(`<feed>` 内含 `<entry>`;每个 entry 的 alternate 链接以 `/releases/tag/{tag}` 结尾,含 `<title>` 与可选的 `<content type="html">`)。取最新**非预发布** entry。
- 真实 apply 测试时,在推导出的下载路径下提供一个有效的 .NET 程序集(例如 `TwilightTimer-v0.0.0.0.dll` 的副本)——安装器会拒绝零字节 / 无效下载,因此非程序集文件可用来走「无效插件 DLL」错误分支;完全不提供文件则可走「发布资产未找到」(404)分支。
- apply 成功后,在磁盘上核对 `BepInEx/plugins/` 中出现 `TwilightTimer-v{新版本}.dll` 且不再有旧 `TwilightTimer-v*.dll`(无法删除者变为 `TwilightTimer-v*.dll.dis`,下次启动清理)。
- `twitimer update cancel` 中止进行中的检测 / 下载。

### 14. 黄昏杯比赛模式 / 回合生命周期(T2–T5)

`twitimer match` 直接、同步地驱动 `TwilightTimerApi` 调试面,适合在不经过
TwilightCore 的情况下检查生命周期与数据保留。

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

- `twitimer match status` 报告比赛 / 回合状态、当前标签集、检查点惩罚锁存、
  provider 注册状态以及比赛排行榜状态。
- `twitimer match start <roundId> <single|multi> [retryCount] [tag...]` 执行 T3.1
  完整重置并应用标签推送;计时仍在下一个 `PlayingLevel` 边沿开始。标签可用
  空格或逗号分隔。
- `twitimer match tags [clear|tag...]` 在回合中修改推送标签集(仅在比赛模式激活时生效)。
- `twitimer match segments` 列出已完成的回合分段及其有效性快照;`twitimer match stop`
  之后数据仍可查询(T3.5)。
- `twitimer match resume ...` 在回合 id 相同的情况下重新激活已停止的回合,不会清空
  分段 / 累计时间。
- `twitimer match exit` 恢复赛前的用户标签集,但不会清空回合数据。

### 15. `ITimerProvider` 适配器 / 事件总线(T1、T4)

`twitimer sim` 测试实际注册到 TwilightCore `TimerProviderRegistry` 的适配器。变更
调用会经 `MainThreadQueue` 排队,因此请先用 `twitimer sim drain`(或等待一帧)再检查结果。

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

- `twitimer sim status` 打印 provider API 版本、注册状态、比赛 / 回合 / 分段 / 现实时间
  查询结果、已完成分段与当前有效无效标记。
- `twitimer sim events on` 会把 `SegmentCompleted`、`AttemptSkipped`、`RunCompleted`、
  `IncompleteExit`、`InvalidMarked` 镜像到 BepInEx 日志,便于核对对外事件序列。
- `twitimer sim start/resume/stop/tags` 调用接口方法;`twitimer sim drain` 立即执行排队中的
  主线程动作。
- `twitimer sim resolvetag <serverTag...>` 走 TwilightCore 处理 `round_start` pick 时
  所用的同一映射(`ITimerTagProvider.ResolveServerTag`),因此可证明提供方接受哪些服务端
  CT 词条。已注册标签做宽松匹配:`Glitchless`、`glitchless`、`No EC`、`no-checkpoint`、
  `NoCheckpoint` 均可解析;未注册字符串打印 `<unsupported>`。可用它确认经
  `TagRuleRegistry` 注册的扩展标签同样能从服务端接收。

```text
twitimer sim resolvetag Glitchless "No Checkpoint" "No EC" Voiceline Pinch
# Glitchless -> Glitchless   (No Checkpoint -> NoCheckpoint, No EC -> NoEC,
# Voiceline -> Voiceline,     Pinch -> <unsupported>)
```

### 16. 共享排行榜挂在比赛排行榜下方（T7.6）

比赛对局期间,共享排行榜(subsegment/markers HUD)必须仅在对局排行榜显示时显示、直接挂在
其下方,并忽略自身的切换键。`twitimer leaderboard status` 会报告解析后的状态
(`matchMode`、`matchLeaderboard`、`follow`、`anchoredBelowMatch`、`matchBottomY`、`topY`)。

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

- 回合进行中且 HUD 开启时,`twitimer leaderboard status` 必须显示
  `matchLeaderboard = shown, follow = shown, anchoredBelowMatch = true`,且
  `topY = matchBottomY + 6`。
- `twitimer hud off` 或 `twitimer set show_leaderboard false` 必须报告
  `matchLeaderboard = hidden, follow = hidden` —— 共享排行榜随对局排行榜一起隐藏
  (`level`/`marker` 步骤只是为了让 markers HUD 有内容可画)。
- 比赛激活期间(包括回合之间,即 `MATCH_STOPPED` 但 match 仍激活)切换键
  (`Subsegment.ToggleKey`,默认 `Tab`)不得循环共享排行榜。
- 比赛之外,`twitimer leaderboard cycle/show/hide/mode` 与切换键行为完全不变
  (独立的屏幕中心锚点)。

## 测试清单

- [ ] `twitimer` 打印命令摘要。
- [ ] 关卡内 `twitimer status` 显示合理的实时值。
- [ ] `twitimer reset` 将计时器归零并清除标记。
- [ ] `twitimer clock` 显示整数 tick,且 `twitimer clock history` 对重复的相同操作记录一致的 `dur=`(R1.11)。
- [ ] `twitimer retry` 重载当前关卡(或配置的重定向目标)。
- [ ] `twitimer pass` 完成当前关卡;`twitimer status` 显示记录的分段以及(最后一关)`lastRun`,且没有分段 / 标记 PB 文件被改动。
- [ ] `twitimer pass real` 把玩家传送到判定箱内,游戏自身触发流程完成关卡(`LevelPassed` 被闩锁);PB 依旧不写入。
- [ ] `twitimer hud off/on` 隐藏 / 显示计时 HUD。
- [ ] `twitimer panel open/close` 打开 / 关闭设置面板。
- [ ] `twitimer tag enable/disable` 改变启用的标签并持久化。
- [ ] `twitimer set language zh-Hans` 切换界面语言。
- [ ] `twitimer layout row add/remove` 改变 HUD 行。
- [ ] `twitimer preset create/save/apply` 完整往返布局 + 标记。
- [ ] 有分段数据时 `twitimer sub status/entries` 正常。
- [ ] `twitimer match enter` 会禁用分段对比(`matchSuppressed=true`、`effective=false`)并隐藏其排行榜;`twitimer match exit` 恢复设置。
- [ ] 关卡内 `twitimer marker add/list/toggle/pb` 正常。
- [ ] `twitimer flags raise/clear` 显示预期的 HUD 横幅 / 软标记行。
- [ ] 安装或不安装 LevelCollections 时 `twitimer lc status` 均正确报告。
- [ ] `twitimer match enter/start/tags/stop/exit` 能驱动回合并恢复用户标签。
- [ ] match 激活期间,共享排行榜挂在比赛排行榜下方并跟随其 `show_hud`/`show_leaderboard` 显隐;其切换键无效(T7.6)。
- [ ] `twitimer match segments` 在 `twitimer match stop` 后仍保留已完成回合数据。
- [ ] `twitimer sim status` 报告已注册 provider 及其实时查询结果。
- [ ] `twitimer sim events on` 输出预期的 T4 对外事件序列。
- [ ] 关于标签页(导航第一项)显示名称 / 版本 / 许可证与仓库按钮;`twitimer about` 与之匹配。
- [ ] `twitimer update check` 报告已最新 / 显示更新版本 / 离线时显示一行错误,`twitimer update apply` 安装 DLL(R13)。
