# 黄昏杯比赛集成（TwilightTimer 分支）

> [English](../TWILIGHT_CUP.md)

本分支（`TwilightTimer`）是 TwilightTimer 面向**黄昏杯** 1v1 速通比赛的特供版。
硬依赖 **TwilightCore**（选手端插件，提供 WebSocket、聊天、准备锁与内置
Level Collections 引擎），并作为其计时引擎。所有比赛相关能力**只在比赛
对局内生效**；本地练习与单机速通行为与 `main` 完全一致（依赖切换本身除外）。

## 依赖

- `TwilightCore` 为编译期 + BepInEx 硬依赖
  （`[BepInDependency("TwilightCore", HardDependency)]`），缺失时插件不加载。
- csproj 引用 TwilightCore 的**构建产物 DLL**（`bin/Release/...`）。请先构建
  TwilightCore，或用 `-p:TWILIGHTCORE=/path/to/TwilightCore.dll` 覆盖路径。
- 原 LevelCollections 软依赖与反射桥已移除；合集功能（末关完成判定、
  `lc restart` 重试、`{collection}` 模板变量）直连
  `TwilightCore.CollectionManager`。

## 比赛模式（T2）

仅可经 `TwilightTimerApi`（生产环境为 TwilightCore 的 `ITimerProvider` 适配器）
进入/退出，设置面板无此开关。

- 进入：快照用户标签集；面板标题加比赛徽标（"黄昏杯比赛 #回合号"）；
  与比赛规则冲突的设置项变为只读（标签勾选、暂停计时、自动重置、
  重置/重试键位）。
- 比赛期间暂停时间强制计时（T7.3）——在读取时强制，`settings.ini` 永远
  不会存入比赛强制值。
- 退出：恢复用户标签集。回合数据保留至下一次 `StartRound`（断线补报）。
- `tags.ini` 绝不会写入比赛标签：保存路径在比赛期间会把用户快照换回。

## 回合生命周期（T3）与事件（T4）

`TwilightTimerApi.StartRound(roundId, isSingle, retryCount, tags)` 执行完整重置
（手动重置范围 + 全部无效标记）并应用推送标签；计时仍在首个
`PlayingLevel` 边沿开始。`StopRound()` 停止计时；分段数据保留可查至下一次
`StartRound`。`RoundTracker.ResumeRound()` 在断线重连后重新激活同一回合，
不清空已完成分段、累计时长与下一段序号；TwilightCore 经可选扩展接口
`IResumableTimerProvider` 调用它。

对外事件（`TimerEvents`，由 ITimerProvider 适配器转暴露）：
`SegmentCompleted(index, durationMs, totalMs)`、`AttemptSkipped(index)`、
`RunCompleted(totalMs)`、`IncompleteExit(index)`、
`InvalidMarked(reason, unforgivable)`。所有时长均为游戏时间毫秒（R1.1 引擎
为唯一口径）。跳过/未通关退出采用延迟判定：未通关离开后回合内又有下一段即
跳过；回合先结束（StopRound 或 LC `RunAborted`）则为未通关退出。

## 标签推送（T5）

推送的标签 id 经 `TagRuleRegistry` 校验；不支持的 id（Glitchless / Pinch /
No EC / Achievement 及任何未知 id）记日志并忽略。支持映射：`Checkpoint`、
`NoCheckpoint`、`Jumpless`（另有非 CT 内置 `Voiceline`）。推送集显示在
HUD 标签行与面板类别页。

## 回合内约束（T7）

回合进行中：重置键与一键重试（单关重载与合集重启）均为仅日志的无效操作；
自动重置绝不清零回合数据；有效性检测照常运行并实时上报新标记。推送的
`Jumpless` 标签同样会在输入层强制禁用跳跃键——强制（R3.5.3）跟随
`SetRoundTags` 换入的实时标签集，比赛结束自动解除。

## ITimerProvider 适配器（T1）

`Match/TwilightTimerProvider.cs` 实现 TwilightCore 已发布的
`TwilightCore.Timer.ITimerProvider`，并在插件加载时向
`TimerProviderRegistry` 自注册（卸载时注销）。变更调用经
`MainThreadQueue` 编组（WebSocket 线程安全）；查询为时点快照；内部
`TimerEvents` 转暴露为接口事件（逐订阅者 try/catch）。游戏内
`twi sim status` 会打印已注册提供方的实时状态。

## 对局排行榜

`Hud/LeaderboardHud.cs` 在比赛回合内渲染实时排行榜，锚点在屏幕左缘
垂直居中处，风格与计时器一致（格式/排序/配置见
[HUD.md](HUD.md)）。其数据方向与提供方相反：TwilightCore 持有服务器
汇总的状态，本插件消费。接缝为 `Match/LeaderboardFeed.cs` —— 反射
探测 `TwilightCore.Leaderboard.LeaderboardApi`（契约见
[LEADERBOARD_REQ.md](../LEADERBOARD_REQ.md)）；在 TwilightCore 实现该
接口之前，探测干净地失败，排行榜以**过渡模式**运行：仅本地一行，
完全由 `RoundTracker` / `RunState` / `CollectionManager` 构建，名字
回退为本地化的「你」，无座席配色。

## 无 TwilightCore 驱动的调试（T1.6）

`TwilightTimerApi` 为直连面：EnterMatchMode / ExitMatchMode / StartRound /
StopRound / SetRoundTags / RoundStatusString。

## 验收映射

需求的验收场景 A1–A13 对应：标签推送行为（A1/A8）、MULTI/SINGLE 回合事件
序列（A2–A4）、回合内按键与约束行为（A5/A6）、数据保留（A7）、退出恢复
（A9）、内置 LC 的本地合集运行（A10/A11）、降级/无计时器模式（A12/A13）。
