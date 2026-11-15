# 吉星小镇修改器（CesiumLoader 版）

<p align="left">
  <img src="icon.png" width="96" alt="icon">
</p>

> 《吉星派对》（Steam：AstralParty）单人模式「吉星小镇」的 CesiumLoader 修改器。
> 按 **F8** 打开/关闭面板。

## 功能

- 加/减金币
- 按 ID 加卡
- 按 ID 加遗物
- 设置关卡进度
- 设置骰子点数 / 恢复自动骰子
- 锁定关卡进度
- 直接结算胜利

## 安装

前提：游戏已安装 CesiumLoader。

```text
AstralParty_ModLoader/mods/TownTrainer/
├─ TownTrainer.dll
├─ TownTrainer.json
└─ icon.png
```

重启游戏，进入单人模式，按 `F8`。

## 构建

```powershell
pwsh -File build.ps1
```

产物：

```text
dist/TownTrainer/
```

## 说明

- 只在单人模式使用；
- 直接结算会走游戏原本的 `UploadDataToServer(GameStatus.Victory)`；
- 卡牌/遗物 ID 来自游戏配置表；
- 仅本地修改器，不修改服务端数据。

## 许可

MIT
