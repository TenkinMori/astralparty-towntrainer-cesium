# 吉星小镇修改器（CesiumLoader 版）

<p align="left">
  <img src="icon-town.png" width="96" alt="icon">
</p>

> 《吉星派对》（Steam：AstralParty）单人模式「吉星小镇」的 CesiumLoader 修改器。
> 在游戏内按 **F8**（可改），或点右上角「修改器」按钮打开 FairyGUI 面板。

## 功能介绍

一个给单人模式用的轻量修改器，面板直接用 FairyGUI 做在游戏里，不弹控制台、不切窗口。

| 功能 | 说明 |
|---|---|
| 金币 | 增加/减少指定数量 |
| 卡牌 | 按 ID 直接加入背包 |
| 遗物 | 按 ID 直接获得 |
| 关卡进度 | 设置当前进度，并同步跳过对应任务 |
| 骰子 | 固定下一次骰子点数，或恢复自动 |
| 锁定进度 | 一个按钮切换 ON/OFF |
| 直接结算 | 走游戏原本的结算流程，打开结算面板 |
| ID 预览 | 独立面板，卡牌/遗物分页查看 ID + 中文名字 |
| 通行证回合任务 | 在「通行证」页面左上角多一个按钮，点一下把「吉星小镇完成回合数」那条任务直接推满 |

## 使用

1. 进入单人模式「吉星小镇」；（通行证按钮在主页面的通行证页面里，不需要进单人局）
2. 按 `F8`（默认，可在配置里改）或点右上角「修改器」；
3. 按钮和面板都可以拖动，拖到顺手的位置。

## 配置

配置文件是 `AstralParty_ModLoader/configs/TownTrainer.json`（老版本加载器会放在 `mods/TownTrainer/config.json`），首次加载时自动生成带默认值和说明，改完保存即可热重载，不用重启游戏。

```json
{
  "enabled": true,
  "toggleKey": "F8",
  "showByDefault": false,
  "_comments": { "...": "自动生成的多语言说明，可忽略" }
}
```

| 键 | 类型 | 默认 | 说明 |
|---|---|---|---|
| `enabled` | bool | `true` | 是否启用。`false` 时按钮和面板都不出现 |
| `toggleKey` | string | `"F8"` | 开关面板的按键，填 Unity `KeyCode` 名（`F8`、`F9`、`Insert`、`Home`、`BackQuote`、`KeyPad1`…）。填 `None` 停用热键，只能用按钮开关。写错会退回 `F8` |
| `showByDefault` | bool | `false` | 进入单人模式时是否直接展开面板。`false` 只显示右上角按钮 |

> 键名用的是 Unity 的 `KeyCode` 枚举名，不分大小写。改 `toggleKey` 后，面板标题旁和「关闭」按钮上的提示会立刻跟着变。

## 安装

```text
AstralParty_ModLoader/mods/TownTrainer/
├─ TownTrainer.dll
├─ TownTrainer.json
└─ icon.png
```

重启游戏。

## 说明

- 只建议在单人模式使用；
- 直接结算会调用游戏原本的 `UploadDataToServer(GameStatus.Victory)`；
- 卡牌/遗物 ID 只读取 `StaticConfigure.SinglePlayer`，即吉星小镇自己的配置；
- ID 预览每页 20 条，可以翻页看完全部；
- 热键走 SDK 的 `InputService`（反射 + Win32 兜底），比 `UnityEngine.Input` 在 IL2CPP 下更稳；输入框正在接收文字时不会触发；
- 面板上的功能都只改本地数据；**只有通行证那个「完成回合任务」按钮会向服务端发请求**（伪造一局单机结算），用它之前请自己清楚风险。

## 构建

需要 .NET SDK 8+：

```powershell
pwsh -File build.ps1
```

产物：

```text
dist/TownTrainer/
dist/TownTrainer-1.3.6.zip
```

## 更新日志

### 1.3.6

- 新增配置项 `roundIntervalMs`：通行证按钮逐回合上报的间隔，单位**毫秒**，默认 `50`（100 回合约 5 秒），可填 `10–5000`，超出范围会被夹回边界；
- 间隔每次点击时现读配置，改完配置文件不用重启游戏，下一次点按钮就按新间隔跑；
- 日志里的 `开始执行：…` 会带上本次实际使用的 `间隔=Nms`，方便核对配置有没有生效。

### 1.3.5

- 结算完成后**自动返回通行证页**：不再把你丢在主页面。流程是先 `ExitSinglePlayer()` 回主页，等 `HomePanel` 起来后自动 `OpenPanel(UIPanelType.BattlePass)`，你回来时看到的就是任务页；
- 自动打开只做一次，之后就把控制权交回给你，不会和你手动切页打架；
- 回主页或等主页面超过 30 秒会放弃自动打开，只写一条 Warn 日志，不影响已经上报的战绩。

### 1.3.4

- 回合上报间隔从 0.2 秒压到 0.05 秒，100 回合从 20 秒缩到约 5 秒，赶在开始面板弹出来之前跑完；
- 每推一回合都重新关掉 `SinglePlayerStartPanel`，防止面板后弹出来挡住流程；
- 进度按「任务目标 − 服务器已记进度」补齐，支持断点续跑：中途被打断，下次点按钮只补剩下的部分（实测 25 → 78 → 100 能接上）；
- 新增任务推送监听，服务器每次下发 `BattlePassTaskUpdateS2C` 都会写一行 `服务器推送任务更新：进度=X/100 已完成=Y`，用来确认服务器到底认没认这局。

### 1.3.3

- 按钮改成**静默进入吉星小镇单人局**再上报，不再从主页面拼包：
  1. 点击后调用 `SinglePlayerLogic.EnterSinglePlayer()`，先发 `SingleGameDataC2S`(5351) 让服务器建立这一局的存档上下文，再切到单人场景；
  2. 等场景内 `GameData` / `BoardManager` / `GamePlayManager` 三件套都注册完成；
  3. 按 0.2 秒一回合的节奏，把 `Round` / `GameProgress` 从**当前值**往上推，每回合调 `UploadDataToServer(GameStatus.Running)`（和游戏自动循环完全同一条路）；
  4. 推到任务要求的回合数后，以 `GameStatus.Victory` + `RoundTiming.GameEnd` 发终局结算，然后 `ExitSinglePlayer()` 回主页面。
- 上一版（1.3.1）日志实测证实：从主页伪造的 5349 包服务器回 `err=0` 却不落库（前后 5351 快照全 0），所以必须让服务器先有这一局的上下文。
- 回合数按「目标 − 当前本地进度」补齐，不再写死 100；已有进度不会被覆盖。
- 进场前会调 `FinishRestartGame()`，否则 `RequestSyncSingleGameDataC2S` 会因 `IsReStartGame == true` 静默早退、一个包都不发。
- 新增日志：`开始执行：…` / `场景已就绪：…` / `已发送终局结算：…`，都在 `AstralParty_ModLoader/logs/activity-mod-*.log` 里带 `[TownTrainer]`。

### 1.3.0

- **通行证页面新增「完成回合任务」按钮**：打开通行证页时，返回键右侧会出现一个橙色按钮，点一下就把「吉星小镇完成回合数」（`ConditionType.TotalSingleModeRoundCount` = 319）这条任务推满；
- 按钮只在该任务还没完成时可用，已完成会直接提示；连点有 3 秒冷却；
- 做法：客户端没有「上报任务进度」的协议，通行证的进度完全由服务端按单机对局结算累加。所以按钮会拼一份单机结算包（`Round` 取该任务要求的回合数），走 `SyncSingleGameDataC2S`(5349) 直接发给服务器，由服务器自己把这局算进任务；
- 上方发的是**真实请求**，不是本地改显示：点完会提示「已提交」，服务器接受后通行证卡片才会变满，重开通行证或重登都还在。失败时按钮上会显示原因（未连接、未找到任务、err 码等）；
- 这个按钮用自绘 `GComponent` 而不是 `GButton`，因为 `BasePanel.Refresh()` 会把面板里所有 `GButton` 的点击事件释放掉。

### 1.2.0

- 新增配置项 `toggleKey`：面板开关按键可改为任意 Unity `KeyCode`（`F8`、`F9`、`Insert`、`Home`、`BackQuote`、`KeyPad1`…），填 `None` 停用热键；
- 新增配置项 `showByDefault`：进入单人模式是否直接展开面板，默认 `false`（只留右上角按钮）；
- 新增配置项 `enabled`：整个模组的开关；
- 热键改走 SDK `InputService`，不再依赖 `UnityEngine.Input`，并自算下降沿避免掉帧丢键；
- 改配置即时热重载，标题、提示文字和「关闭」按钮上的按键名会自动跟着变；
- 面板标题的版本号改为读取模组版本，不再硬编码。

### 1.1.0

- 加入独立 ID 预览面板，卡牌/遗物分页。

### 1.0.0

- 首个版本。

## 许可

MIT

