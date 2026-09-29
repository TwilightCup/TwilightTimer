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
| `twitimer tag [list\|label <status\|on\|off\|auto>\|enable <id>\|disable <id>\|set <id> <on\|off>]` | 开关启用的标签规则;查看/强制自动 Co-op 标签 |
| `twitimer lang [list\|set <code>\|reload\|current]` | 管理本地化 |
| `twitimer preset [list\|current\|create <name>\|apply [name]\|save\|delete <name>]` | 管理预设(R11) |
| `twitimer sub [status\|entries\|clear\|clientmode <status\|on\|off\|auto>]` | 查看 / 清空分段模块;查看 / 强制合作客机门控 |
| `twitimer marker [list\|feed\|add ...\|remove <id>\|toggle <id>\|clientmode <status\|on\|off\|auto>\|pb <ms>\|pbclear\|clear\|save\|reload]` | 查看 / 编辑标记(R10);查看 / 强制合作 PB 角色 |
| `twitimer flags [list\|raise <Reason>\|clear [forgivable\|soft\|all]]` | 查看 / 修改有效性标记(R5) |
| `twitimer lc [status\|restart]` | 查看 LevelCollections 集成或触发 `lc restart` |
| `twitimer config [path\|files\|source [status\|hsrtimer\|twilighttimer\|toggle]]` | 打印 TwilightTimer 配置路径,或切换配置来源目录 |
| `twitimer match [status\|enter\|exit\|start ...\|resume ...\|stop\|tags ...\|segments\|leaderboard\|penalty]` | 通过 `TwilightTimerApi`(同步调试面)驱动黄昏杯比赛模式 / 回合生命周期 |
| `twitimer sim [status\|drain\|enter\|exit\|start ...\|resume ...\|stop\|tags ...\|resolvetag ...\|events ...]` | 驱动已注册的 `ITimerProvider` 适配器,解析服务端标签字符串,并把其对外事件镜像到日志 |
| `twitimer about` | 打印插件名称、版本、许可证声明与仓库 URL(R12) |
| `twitimer update [status\|check\|apply\|cancel\|base [url]]` | 从 GitHub releases 检测 / 安装插件更新(R13) |

## `twitimer get` / `twitimer set` 可接受的键

命令使用 `SettingsModel` 与 `LayoutModel` 公共字段的 snake_case 形式。常见示例:

- `auto_reset`、`use_plcc_timing_standard`、`restart_clears_forgivable`、`retry_min_dwell`
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

**边界确定性(R1.11)。** `twitimer clock` 打印原始整数 tick 时钟(`playableTicks`、`segmentStartTicks`、`pendingEndTicks` 等)。`twitimer clock history` 列出每次分段起止 tick。两个边界都由权威 hook 锁存,不依赖轮询节奏或 Unity 脚本执行顺序:`load` 是 `Game.AfterLoad` 起点 tick。**终点** tick 的取点取决于 `use_plcc_timing_standard` 开关(默认关闭):
- **关闭(默认,既有机制)** —— `pass` 行(`Game.Fall` 通关检测)携带锁存的终点 tick,含通过帧本身。
- **开启(plcc 计时标准)** —— `leave` 行(`Game.BeginLoadLevel` / `Game.AfterUnload`)携带终点 tick —— 即游戏离开 `PlayingLevel` 的时刻,与 plcc Timer 相同口径。

连续载入 + 通关同一关卡 50 次,相同操作下的 `dur=`(终点 tick − 起点 tick)必须**完全一致**(零 ±1 抖动)。测量前用 `twitimer clock clear` 清空历史。

测量前用 `twitimer clock clear` 清空历史。每条 `end` 记录在使用锁存 tick 时带 `src=hook`,`step=` 是轮询消费该边界的全局物理帧;因此分段时间不依赖轮询时机。

`pass` 行在**两种模式下都会记录**:它是 `Game.Fall` 通关检测的 tick(`processed=` 表示本插件的 `FixedUpdate` 是否已处理该物理帧)。plcc 模式下它在"完成标志已锁存"的提前返回之前输出,因此不决定终点 tick —— 但正因为它是 `Game.Fall` 帧的纯观测,plcc 标准下 `twitimer clock history` 会依次出现 `pass`(通关检测)→ `leave`(游戏离开 `PlayingLevel`)→ `end`,而 `leave tick − pass tick` 正是 plcc 口径多出的那一个渲染帧延迟。`zone` 行标记 `Game.EnterPassZone`(它通常在 `Game.Fall` 之前就锁存 `LevelPassed`,所以 plcc 模式下真正输出的是这行观测性的 `pass`,而不是锁存完成标志的那行)。既有模式下 `pass` **就是**终点 tick;plcc 标准下终点是 `leave`。

起点侧同理:`load` 是 `Game.AfterLoad` hook 帧(权威分段起点),紧随其后的 `start` 行是轮询消费该闩锁的结果。`start` 的 tick 等于 `load` 的 tick 而 `step=` 更大,即证明起点边界不再取决于轮询何时发现它。

**计时标准开关。** 该设置位于设置面板 **常规 → 计时** 部分最上方("使用 plcc 计时标准"),也存在于 `settings.ini` 的 `use_plcc_timing_standard`。用控制台切换并确认 `twitimer status` 报告当前模式:`twitimer set use_plcc_timing_standard true` / `false`,然后 `twitimer status` → `plccTiming=True|False`。改动即时生效(引擎每 tick 重新读取),无需重置或重试。关闭时重复通关在 `pass` tick 结束;开启时在 `leave` tick 结束 —— 两种模式下 `twitimer clock history` 对相同的重复操作都必须记录到一致的 `dur=`(R1.11.8)。开启期间,计时器 HUD 在时间行正下方显示一行 `plcc计时模式`(英文:`plcc timing mode`);关闭后该行消失(R2.6.1)。

**整局运行中锁定(R1.4.2a)。** 一局运行进行中会钉住当前口径,整局内都不能切换 —— 包括两关之间的加载与暂停:`twitimer status` 显示 `realTimeActive=True` 时,设置面板的该开关变灰、提示文字变为"整局运行中不可切换";`twitimer set use_plcc_timing_standard` 会打印 `Cannot change use_plcc_timing_standard while a run is in progress. Reset the run or leave the level first.` 且不改变取值(用 `twitimer get use_plcc_timing_standard` 复核)。锁定窗口从本局第一个可游玩分段开始,跨越全部关卡加载与暂停,直到本局完成、玩家返回菜单/大厅(`realTimeActive=False`)或执行 `twitimer reset` 才解除。该锁定只是防护 —— 引擎仍每 tick 读取该设置。

### 2. 有效性标记(R5)

```text
twitimer flags list
twitimer flags raise CheatCode            # 不可原谅
twitimer flags raise CheckpointSkip       # 可原谅
twitimer flags raise Ec                   # 软标记,计数递增
twitimer flags raise Ssg                  # 软标记(Glitchless),计数递增
twitimer flags raise PropFly              # 软标记(Glitchless),计数递增
twitimer flags raise Footsie              # 软标记(Glitchless),计数递增
twitimer flags clear forgivable
twitimer flags clear soft
twitimer flags clear all
```

`twitimer status` 的 `flags:` 行应显示硬性原因,软标记显示为 `Ec xN` / `Ssg xN` 等。

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

**自动标签 `Co-op`(R3.10)。** `twitimer tag list` 会在独立的 `labels (auto):` 行中列出标签型标签及其实时状态 —— 仅在多人会话期间(`twitimer status` 中的 `server=` / `client=`)显示 `Co-op [on]`,单人模式下为 `[off]`。该标签无法像规则标签一样手动开关:

```text
twitimer tag set Co-op on      # 会被拒绝:"is an auto label ... cannot be toggled manually"
```

无需真实多人会话即可测试标签显示,可用 `twitimer tag label` 强制开关(仅本次会话有效;`auto` 恢复按多人模式自动):

```text
twitimer tag label status      # effective / net / override
twitimer tag label on          # 强制开启(HUD"规则标签"行与 {category} 显示 "Co-op")
twitimer tag label off
twitimer tag label auto        # 恢复自动(仅在多人会话期间开启)
```

验证真实周期:主持或加入合作游戏 → `twitimer tag list` 显示 `Co-op [on]`,且 HUD 的"规则标签"行 / `{category}` 模板变量包含 "多人"(中文界面;英文界面为 "Co-op");离开会话 → 变回 `[off]`。该标签从不持久化:`twitimer get all`(tags 段)与 `tags.ini` 中永远不会有 `Co-op`。

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

`twitimer sub status` 打印启用标志、合作门控状态、路径、多局状态与当前排行榜条目数。
`twitimer sub entries` 列出每个已加载参考及其最新结算的差值。

`twitimer sub status` 同时报告比赛门控:
`enable=<用户设置> matchSuppressed=<bool> effective=<bool>
samplingAllowedForLevel=<bool>`。模块实际遵循的是 `effective`。与自动禁用接口一致
(R8.5.1.5),比赛激活会登记为 `match` 来源,因此同时显示
`autoDisabled=on autoReasons=match`。

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

**合作门控(R8.10)。** 作为多人客机时模块整体禁用(`twitimer sub status` 显示 `active=off`、`coopClientGate=on`);作为主机时正常运行,且只使用自己(主机)的角色判定。无需真实客机会话,可用 `twitimer sub clientmode` 强制门控(仅本次会话有效;`auto` 恢复):

```text
twitimer sub clientmode status    # effective / net / override
twitimer sub clientmode on        # 模拟为合作客机 -> 禁用 subsegment
twitimer sub clientmode off       # 模拟为主机 / 单人 -> 正常
twitimer sub clientmode auto      # 恢复自动(仅 NetGame.isClient 时禁用)
```

验证:`twitimer sub clientmode on` 后 `twitimer sub status` 显示 `active=off, coopClientGate=on`(以及新的自动禁用字段 `userEnabled=on autoDisabled=on autoReasons=coop-client`),关卡内排行榜不再显示任何 subsegment 内容;`twitimer sub clientmode off` 恢复采样 / 检测。真实合作会话中,客机端会自动呈现相同行为。

**用户禁用 vs 自动禁用(R8.5.1.5)。** `twitimer sub status` 把用户的 `Subsegment.Enable` 设置(`enable` / `userEnabled`)与自动禁用来源(`autoDisabled` / `autoReasons`)分开报告。例如 `twitimer set subsegment_enable false` 得到 `userEnabled=off autoDisabled=off`,而 `twitimer sub clientmode on` 得到 `userEnabled=on autoDisabled=on autoReasons=coop-client`——两个维度相互独立,任一者都会使模块处于 `active=off`。

### 8. 标记(R10)

进入关卡后:

```text
twitimer marker status
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

`twitimer marker status` 报告模块的启用 / 可用状态:用户的 `Markers.Enable` 设置(`enable` / `userEnabled`)、当前生效的自动禁用来源(`autoDisabled` / `autoReasons`,目前没有)、合并后的 `active`、合作 PB 角色(`pbWrite`)与当前 feed 大小。`twitimer set markers_enable false` 使 `userEnabled=off` 而 `autoDisabled` 仍为 off——两个维度相互独立(R8.5.1.5)。

当编辑模式开启(`twitimer set markers_edit_mode true`)时,标记覆盖层 / feed 应响应这些改动。

排行榜 feed 必须**按尝试**重置:`twitimer pass` 过关后进入下一次尝试——即使下一关仍是同一关(剧情重复关卡)——`twitimer marker feed` 应只列出新尝试的行。新尝试的首次触发会**替换**掉上一次尝试的残留行,而不是追加在其后。

**合作行为(R10.10)。** 多人会话中**任意玩家**都可以触发标记(主机与客机端都会遍历全部玩家判定);标记 PB 写入仅主机进行——客机从不持久化 PB(`twitimer marker list` 显示 `pbWrite=disabled (co-op client, host-only)`,`twitimer marker pb` 会被拒绝)。无需真实客机会话,可用 `twitimer marker clientmode` 强制角色(仅本次会话有效;`auto` 恢复):

```text
twitimer marker clientmode status   # role / pbWrite / net / override
twitimer marker clientmode on       # 模拟为合作客机 -> 禁止写 PB
twitimer marker clientmode off      # 模拟为主机 / 单人 -> 允许写 PB
twitimer marker clientmode auto     # 恢复自动(仅 NetGame.isClient 时禁止)
```

验证:`twitimer marker clientmode on` 后 `twitimer marker list` 显示 `pbWrite=disabled...`,`twitimer marker pb 12345` 被拒绝;`twitimer marker clientmode off` 恢复。**"任意玩家触发"**部分需要真实合作会话:两名玩家在同一关时,任一人进入范围盒 / 抓住目标物体都会触发标记,`twitimer marker feed` 在两台机器上都会列出。

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

**循环会跳过被禁用的模式(R8.5.1.2)。** `twitimer leaderboard status` 显示当前 `mode` 与 `available` 模式(用户开启且未被自动禁用)。两个模块都开启时,`cycle` 依次走关闭 → 分段对比 → 标记 → 关闭。禁用 subsegment(`twitimer sub clientmode on`,或 `twitimer set subsegment_enable false`)后,`cycle` 只在关闭 ↔ 标记之间轮换:从关闭直接进入标记(绝不进入分段对比),从标记进入关闭。两者都禁用时 `cycle` 保持关闭。禁用 markers(`twitimer set markers_enable false`)同理,循环变为关闭 ↔ 分段对比。当前显示的模式变得不可用时(例如排行榜处于分段对比模式时客机门控生效),该模式不再绘制任何内容,下一次 `cycle` 按键进入关闭。

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
- [ ] 计时标准开关:关闭 `use_plcc_timing_standard`(默认)时,`twitimer clock history` 由 `pass` 行携带终点 tick;`twitimer set use_plcc_timing_standard true` 后(`twitimer status` 显示 `plccTiming=True`)会同时出现 `pass` 行(`Game.Fall` 帧,仅观测)与携带终点 tick 的 `leave` 行,且 `leave tick − pass tick` ≈ 1 个物理帧(渲染帧延迟),两种模式下相同操作的 `dur=` 均一致。开启期间计时器 HUD 在时间行下方显示 `plcc计时模式` 一行;关闭后该行消失(R2.6.1)。
- [ ] 计时标准开关整局锁定(R1.4.2a):本局运行中(`realTimeActive=True`,含两关之间与暂停)`twitimer set use_plcc_timing_standard true|false` 会被拒绝并打印锁定说明,`twitimer get` 取值不变;本局结束后(`realTimeActive=False`,例如回到主菜单)或执行 `twitimer reset` 后同一条 `twitimer set` 成功。设置面板中该开关在本局期间变灰、提示文字为"整局运行中不可切换",离开本局后恢复可交互。
- [ ] `twitimer retry` 重载当前关卡(或配置的重定向目标)。
- [ ] `twitimer pass` 完成当前关卡;`twitimer status` 显示记录的分段以及(最后一关)`lastRun`,且没有分段 / 标记 PB 文件被改动。
- [ ] `twitimer pass real` 把玩家传送到判定箱内,游戏自身触发流程完成关卡(`LevelPassed` 被闩锁);PB 依旧不写入。
- [ ] `twitimer hud off/on` 隐藏 / 显示计时 HUD。
- [ ] `twitimer panel open/close` 打开 / 关闭设置面板。
- [ ] `twitimer tag enable/disable` 改变启用的标签并持久化。
- [ ] `twitimer sub clientmode on` 后显示 `active=off coopClientGate=on` 且排行榜移除分段模式;`twitimer sub clientmode auto` 恢复(R8.10)。
- [ ] `twitimer set language zh-Hans` 切换界面语言。
- [ ] `twitimer layout row add/remove` 改变 HUD 行。
- [ ] `twitimer preset create/save/apply` 完整往返布局 + 标记。
- [ ] 有分段数据时 `twitimer sub status/entries` 正常。
- [ ] `twitimer match enter` 会禁用分段对比(`matchSuppressed=true`、`effective=false`、`autoReasons=match`)并隐藏其排行榜;`twitimer match exit` 恢复设置。
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
