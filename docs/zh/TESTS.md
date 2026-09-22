# TESTS(测试文档)

> **English (source of truth)**: [../../TESTS.md](../../TESTS.md)

本文说明如何**在游戏内**通过自带的开发者控制台(默认按键 **`~`** 或 **F1**)测试 TwilightTimer 的每一个功能。

## 这是什么

TwilightTimer 在插件加载时向游戏的 `Shell` 控制台注册了一套 `twi ...` 命令。你可以查看实时状态、切换设置、修改配置、开关标签(tags)、编辑 HUD 布局、管理预设(presets)、操作分段(subsegment)与标记(markers)、以及模拟有效性标记(validity flags)——全部无需重启游戏或手工编辑文件。

所有 `twi` 命令都可以安全地在控制台中执行。大多数修改会走与设置面板相同的 `ConfigService.SaveSettings()` 流程,因此会正常持久化到 `settings.ini` / `tags.ini` / `layout.ini`。

## 打开控制台

1. 安装 TwilightTimer 后启动《人类一败涂地》。
2. 按 **`~`**(波浪线 / 反引号)或 **F1** 打开游戏自带的开发者控制台。
3. 输入 `twi` 并回车查看命令列表。
4. 输入 `twi help <topic>` 查看某个命令的详细帮助。

> 如果控制台没有出现,可能是该游戏版本的按键有变动。插件启动时仍会在 BepInEx 日志中输出 `TwilightTimer: dev-console commands registered`。

> **关于大小写:** 游戏控制台会把整行输入转为小写后再执行命令。因此 TwilightTimer 会把标识符(标签 id、语言代码、预设名、排行榜模式)按大小写不敏感方式解析并规范化为标准写法。自由文本(例如自定义文本或标记名称)会按控制台传入的内容保存,即通常是小写。

## 命令参考

| 命令 | 说明 |
|---|---|
| `twi` | 打印完整命令摘要 |
| `twi help [topic]` | 打印某个主题的帮助 |
| `twi status` | 输出计时器 / 配置 / HUD / 分段 / 标记 / LC 的实时状态 |
| `twi keys` | 列出所有可设置的 settings/layout 键 |
| `twi get <key>` / `twi get all` | 读取单个配置值,或全部配置值 |
| `twi set <key> <value>` | 设置并保存一个配置值 |
| `twi reload` | 从磁盘重新读取所有配置文件与语言文件 |
| `twi save` | 保存当前内存中的配置 |
| `twi reset` | 整局重置(等同于重置键) |
| `twi retry` | 一键重试(等同于重试键,R6) |
| `twi hud [on\|off\|toggle\|status]` | 控制计时 HUD 的显示 |
| `twi panel [open\|close\|toggle\|status]` | 控制设置面板 |
| `twi leaderboard [cycle\|show\|hide\|mode <Subsegment\|Markers>\|status]` | 控制排行榜 HUD |
| `twi layout [status\|row ...\|text ...\|get <key>\|set <key> <value>]` | 查看 / 编辑 HUD 布局 |
| `twi tag [list\|enable <id>\|disable <id>\|set <id> <on\|off>]` | 开关启用的标签规则 |
| `twi lang [list\|set <code>\|reload\|current]` | 管理本地化 |
| `twi preset [list\|current\|create <name>\|apply [name]\|save\|delete <name>]` | 管理预设(R11) |
| `twi sub [status\|entries\|clear]` | 查看 / 清空分段模块 |
| `twi marker [list\|feed\|add ...\|remove <id>\|toggle <id>\|pb <ms>\|pbclear\|clear\|save\|reload]` | 查看 / 编辑标记(R10) |
| `twi flags [list\|raise <Reason>\|clear [forgivable\|soft\|all]]` | 查看 / 修改有效性标记(R5) |
| `twi lc [status\|restart]` | 查看 LevelCollections 集成或触发 `lc restart` |
| `twi config [path\|files]` | 打印 TwilightTimer 配置路径 |

## `twi get` / `twi set` 可接受的键

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
twi status                 # 查看游戏/应用状态、游戏时间、分段、现实时间
twi reset                  # 验证计时器归零、标记被清除
twi retry                  # 验证一键重试会重载关卡
```

在关卡内,`twi status` 应显示 `segment=True`、`timing=True` 且 `gameTime` 持续增长。
执行 `twi reset` 后,`gameTime` 应为 `0`,`inSegment` 应为 `False`(或过渡缓存被保留,关卡不会因此重新计时)。

### 2. 有效性标记(R5)

```text
twi flags list
twi flags raise CheatCode            # 不可原谅
twi flags raise CheckpointSkip       # 可原谅
twi flags raise Ec                   # 软标记,计数递增
twi flags clear forgivable
twi flags clear soft
twi flags clear all
```

`twi status` 的 `flags:` 行应显示硬性原因,软标记显示为 `Ec xN`。

### 3. 标签 / 类别(R3)

```text
twi tag list
twi tag enable Checkpoint
twi tag enable Jumpless
twi tag set NoEC on
twi tag disable Jumpless
twi tag set NoCheckpoint off
twi status          # 会列出已启用的标签
```

标签改动会立即持久化到 `tags.ini`。

### 4. HUD / 布局(R2)

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

HUD 应在下一帧生效,且改动持久化到 `layout.ini`。

### 5. 设置面板 / 常规设置

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

`twi panel open` 应弹出与 Home 键相同的 IMGUI 设置面板。

### 6. 预设(R11)

```text
twi preset list
twi preset create test-preset
twi layout set font_size 30
twi preset save
twi preset apply default
twi preset apply test-preset
twi preset delete test-preset
```

执行 `apply` 后,HUD 应反映该预设保存的布局 / 标记。

### 7. 分段模块(R8)

```text
twi sub status
twi sub entries
twi sub clear
```

`twi sub status` 打印启用标志、路径、多局状态与当前排行榜条目数。
`twi sub entries` 列出每个已加载参考及其最新结算的差值。

### 8. 标记(R10)

进入关卡后:

```text
twi marker list
twi marker add range "Test Box"          # 使用玩家位置,2 米盒子
twi marker add checkpoint "CP1" 1
twi marker add grab "My Box"             # 需要当前恰好抓取一个物体
twi marker toggle m1
twi marker pb 12345
twi marker list
twi marker save
twi marker reload
twi marker clear
```

当编辑模式开启(`twi set markers_edit_mode true`)时,标记覆盖层 / feed 应响应这些改动。

### 9. 本地化(R7)

```text
twi lang list
twi lang set zh-Hans
twi lang set en
twi lang reload
```

设置面板与 HUD 文案应即时切换语言。

### 10. 排行榜 HUD

```text
twi leaderboard status
twi leaderboard show
twi leaderboard mode Markers
twi leaderboard mode Subsegment
twi leaderboard hide
twi leaderboard cycle
```

### 11. LevelCollections 集成(可选)

```text
twi lc status
twi lc restart
```

`twi lc status` 报告 LC 集成是否启用、是否处于集合运行中,以及当前集合名称。
`twi lc restart` 触发与重试委托相同的 `lc restart` 命令。

### 12. 配置文件位置

```text
twi config path
twi config files
```

打印插件使用的确切路径,方便你在磁盘上核对或编辑文件。

## 测试清单

- [ ] `twi` 打印命令摘要。
- [ ] 关卡内 `twi status` 显示合理的实时值。
- [ ] `twi reset` 将计时器归零并清除标记。
- [ ] `twi retry` 重载当前关卡(或配置的重定向目标)。
- [ ] `twi hud off/on` 隐藏 / 显示计时 HUD。
- [ ] `twi panel open/close` 打开 / 关闭设置面板。
- [ ] `twi tag enable/disable` 改变启用的标签并持久化。
- [ ] `twi set language zh-Hans` 切换界面语言。
- [ ] `twi layout row add/remove` 改变 HUD 行。
- [ ] `twi preset create/save/apply` 完整往返布局 + 标记。
- [ ] 有分段数据时 `twi sub status/entries` 正常。
- [ ] 关卡内 `twi marker add/list/toggle/pb` 正常。
- [ ] `twi flags raise/clear` 显示预期的 HUD 横幅 / 软标记行。
- [ ] 安装或不安装 LevelCollections 时 `twi lc status` 均正确报告。
