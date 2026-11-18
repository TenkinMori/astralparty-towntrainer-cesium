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

## 使用

1. 进入单人模式「吉星小镇」；
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
- 仅本地修改器，不修改服务端数据。

## 构建

需要 .NET SDK 8+：

```powershell
pwsh -File build.ps1
```

产物：

```text
dist/TownTrainer/
dist/TownTrainer-1.2.0.zip
```

## 更新日志

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

