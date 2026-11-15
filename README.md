# 吉星小镇修改器（CesiumLoader 版）

<p align="left">
  <img src="icon.png" width="96" alt="icon">
</p>

> 《吉星派对》（Steam：AstralParty）单人模式「吉星小镇」的 CesiumLoader 修改器。
> 在游戏内按 **F8**，或点右上角「修改器」按钮打开 FairyGUI 面板。

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
2. 按 `F8` 或点右上角「修改器」；
3. 按钮和面板都可以拖动，拖到顺手的位置。

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
- 仅本地修改器，不修改服务端数据。

## 构建

需要 .NET SDK 8+：

```powershell
pwsh -File build.ps1
```

产物：

```text
dist/TownTrainer/
```

## 许可

MIT
