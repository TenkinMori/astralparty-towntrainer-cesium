using System;
using System.Collections.Generic;
using System.Text;
using Cysharp.Threading.Tasks;
using Core.Net;
using Core.Scene;
using FairyGUI;
using GameLogic;
using SinglePlayer;
using SinglePlayer.GamePlay;
using Tools;
using UI;
using UnityEngine;
using PartyModel = party.model;
using PartyProtocol = party.protocol;

namespace TownTrainer;

/// <summary>
/// 在通行证页面注入一个按钮：点一下静默进入吉星小镇单人局，
/// 用真实的 GameData 逐回合上报（5349），把「单人模式完成回合」任务刷满。
/// </summary>
internal sealed class BattlePassTaskButton
{
    private const float ButtonWidth = 176f;
    private const float ButtonHeight = 34f;
    private const int DefaultTargetRound = 100;
    private const float CooldownSeconds = 3f;
    private const float SceneWaitSeconds = 30f;
    private const float SettleSeconds = 2f;

    private enum Stage
    {
        Idle,
        EnteringScene,
        PumpingRounds,
        Finishing,
        Returning
    }

    private readonly TownTrainerController _owner;
    private readonly CesiumLoader.SDK.Logging.ModLogger _log;

    private GComponent _button;
    private GTextField _label;

    private Stage _stage = Stage.Idle;
    private float _stageAt;
    private float _lastClickAt = -100f;

    private int _taskId;
    private int _targetRound = DefaultTargetRound;
    private int _roundIndex;
    private int _baseRound;
    private int _roundsToSend = DefaultTargetRound;
    private float _lastPumpAt;
    private bool _enteredScene;
    private bool _taskHooked;
    private bool _reopened;

    /// <summary>
    /// 逐次 +1 上报：服务器无论按「包数」还是按「Round 值」记账，都能凑满。
    /// 间隔每次现读配置（毫秒），改配置文件后下一次点击就生效。
    /// </summary>
    private float IntervalSeconds
    {
        get
        {
            int ms = _owner != null ? _owner.RoundIntervalMs : TownTrainerController.DefaultRoundIntervalMs;
            return ms / 1000f;
        }
    }

    internal BattlePassTaskButton(TownTrainerController owner)
    {
        _owner = owner;
        _log = owner != null ? owner.Log : null;
    }

    internal void Tick()
    {
        try
        {
            if (_owner == null || !_owner.Enabled)
            {
                Detach();
                _stage = Stage.Idle;
                return;
            }

            DriveFlow();
            UpdateButton();
        }
        catch (Exception e)
        {
            Warn("通行证按钮 Tick 异常: " + e);
        }
    }

    internal void Dispose()
    {
        Detach();
        UnhookTaskSignal();
        _stage = Stage.Idle;
    }

    // ---------------- 按钮挂载 ----------------

    private void UpdateButton()
    {
        BattlePassPanel panel = CurrentPanel();
        if (panel == null)
        {
            Detach();
            return;
        }

        GComponent root = panel.UIObject;
        if (root == null || root.isDisposed)
        {
            Detach();
            return;
        }

        if (_button == null || _button.isDisposed || _button.parent != root)
        {
            Attach(root, panel);
        }

        if (_button != null && root.numChildren > 0 && root.GetChildAt(root.numChildren - 1) != _button)
        {
            root.SetChildIndex(_button, root.numChildren - 1);
        }
    }

    private static BattlePassPanel CurrentPanel()
    {
        if (!SimpleSingletonProvider<UIManager>.hasInstance)
        {
            return null;
        }

        UIManager manager = SimpleSingletonProvider<UIManager>.inst;
        if (manager == null)
        {
            return null;
        }

        BattlePassPanel panel = manager.currentPanel as BattlePassPanel;
        if (panel == null || !panel.IsOpen())
        {
            return null;
        }

        return panel;
    }

    private void Attach(GComponent root, BattlePassPanel panel)
    {
        Detach();

        GComponent button = new GComponent();
        button.SetSize(ButtonWidth, ButtonHeight);
        button.opaque = true;
        button.touchable = true;
        button.draggable = false;

        GGraph background = new GGraph();
        background.SetSize(ButtonWidth, ButtonHeight);
        background.DrawRoundRect(ButtonWidth, ButtonHeight, new Color(0.93f, 0.55f, 0.16f, 1f), new float[] { 8f, 8f, 8f, 8f });
        background.touchable = false;
        button.AddChild(background);

        GTextField label = new GTextField();
        label.text = "完成回合任务";
        label.textFormat.size = 15;
        label.textFormat.color = Color.white;
        label.autoSize = AutoSizeType.None;
        label.SetSize(ButtonWidth, ButtonHeight);
        label.align = AlignType.Center;
        label.verticalAlign = VertAlignType.Middle;
        label.touchable = false;
        button.AddChild(label);

        button.onClick.Add(OnClick);

        Vector2 position = TopLeftPosition(root, panel);
        button.SetPosition(position.x, position.y, 0f);

        root.AddChild(button);
        if (root.numChildren > 0)
        {
            root.SetChildIndex(button, root.numChildren - 1);
        }

        _button = button;
        _label = label;

        Info("通行证按钮已注入，位置 " + button.position + "（面板 " + root.width + "x" + root.height + "）");
    }

    private Vector2 TopLeftPosition(GComponent root, BattlePassPanel panel)
    {
        try
        {
            GButton back = panel.UIObject.btn_Return;
            if (back != null && back.parent != null)
            {
                Vector2 local = root.GlobalToLocal(back.LocalToGlobal(Vector2.zero));
                bool topLeft = local.x < root.width * 0.5f && local.y < root.height * 0.4f;
                if (topLeft)
                {
                    return new Vector2(local.x + back.width + 10f, local.y + (back.height - ButtonHeight) * 0.5f);
                }
                return new Vector2(8f, Mathf.Max(8f, local.y));
            }
        }
        catch (Exception)
        {
        }

        return new Vector2(8f, 8f);
    }

    private void Detach()
    {
        if (_button == null)
        {
            _label = null;
            return;
        }

        try
        {
            _button.onClick.Clear();
            if (_button.parent != null)
            {
                _button.parent.RemoveChild(_button, true);
            }
        }
        catch (Exception)
        {
        }

        _button = null;
        _label = null;
    }

    private void SetCaption(string text)
    {
        if (_label != null && !_label.isDisposed)
        {
            _label.text = text;
        }
    }

    // ---------------- 点击 ----------------

    private void OnClick()
    {
        if (_stage != Stage.Idle)
        {
            return;
        }

        float now = Time.realtimeSinceStartup;
        if (now - _lastClickAt < CooldownSeconds)
        {
            SetCaption("请稍候再点");
            return;
        }
        _lastClickAt = now;

        string error;
        if (!Begin(out error))
        {
            SetCaption(error);
            Info("无法开始：" + error);
            return;
        }
    }

    private bool Begin(out string error)
    {
        error = null;

        if (_owner == null)
        {
            error = "内部错误";
            return false;
        }

        if (!SimpleSingletonProvider<GameLogicManager>.hasInstance)
        {
            error = "游戏未就绪";
            return false;
        }

        GameLogicManager logic = SimpleSingletonProvider<GameLogicManager>.inst;
        if (logic == null || logic.battlePass == null)
        {
            error = "通行证未初始化";
            return false;
        }

        BattlePassData data = logic.battlePass.BattlePassData;
        if (data == null || data.taskDataDict == null)
        {
            error = "通行证数据为空";
            return false;
        }

        _taskId = 0;
        _targetRound = 0;
        bool finished = false;
        foreach (KeyValuePair<int, BattlePassTaskData> pair in data.taskDataDict)
        {
            BattlePassTaskData task = pair.Value;
            if (task == null || task.taskConfig == null)
            {
                continue;
            }
            if (task.taskConfig.ConditionType != ConditionType.TotalSingleModeRoundCount)
            {
                continue;
            }

            _taskId = task.taskConfig.Id;
            _targetRound = task.taskConfig.Param;
            finished = data.GetStatus(_taskId);
            break;
        }

        if (_taskId == 0 || _targetRound <= 0)
        {
            error = "未找到回合任务";
            return false;
        }

        if (finished)
        {
            error = "任务已完成";
            return false;
        }

        if (!MonoSingletonProvider<NetManager>.hasInstance)
        {
            error = "网络未就绪";
            return false;
        }
        NetManager net = MonoSingletonProvider<NetManager>.inst;
        if (net == null || !net.IsConnected)
        {
            error = "未连接服务器";
            return false;
        }

        int localProgress = data.GetProgress(_taskId, (int)ConditionType.TotalSingleModeRoundCount);
        _roundsToSend = Mathf.Max(1, _targetRound - localProgress);
        _roundIndex = 0;
        _baseRound = 0;
        _enteredScene = false;
        _reopened = false;

        Info("开始执行：任务 id=" + _taskId + " 目标=" + _targetRound + " 本地进度=" + localProgress +
             " 需补回合=" + _roundsToSend + " 间隔=" + (_owner != null ? _owner.RoundIntervalMs : TownTrainerController.DefaultRoundIntervalMs) + "ms");

        HookTaskSignal(logic);

        if (IsInSinglePlayer())
        {
            Info("检测到已在单人局内，直接使用现场 GameData");
            _stage = Stage.PumpingRounds;
            _stageAt = Time.realtimeSinceStartup;
            _lastPumpAt = Time.realtimeSinceStartup;
            SetCaption("上报中 0/" + _roundsToSend);
            return true;
        }

        if (!SimpleSingletonProvider<Core.Scene.SceneManager>.hasInstance)
        {
            error = "场景系统未就绪";
            return false;
        }

        // 必须走 EnterSinglePlayer：它先发 5351 让服务器建立本局单机存档，
        // 再切场景。直接从主页 LoadSceneAsync 的话服务端没有这一局的上下文，
        // 后续 5349 的结算会被静默丢弃（1.3.1 已实测过）。
        if (logic.singlePlayer == null)
        {
            error = "单机模块未就绪";
            return false;
        }

        Info("静默进入吉星小镇单人局");
        _stage = Stage.EnteringScene;
        _stageAt = Time.realtimeSinceStartup;
        _enteredScene = true;
        SetCaption("进入单人局…");
        logic.singlePlayer.EnterSinglePlayer().Forget();
        return true;
    }

    private static bool IsInSinglePlayer()
    {
        try
        {
            if (!SimpleSingletonProvider<Core.Scene.SceneManager>.hasInstance)
            {
                return false;
            }
            Core.Scene.SceneManager scene = SimpleSingletonProvider<Core.Scene.SceneManager>.inst;
            if (scene == null || scene.currentType == null)
            {
                return false;
            }
            if (scene.currentType.Value != Core.Scene.SceneType.SinglePlayer)
            {
                return false;
            }
            // GameData 先注册、后 Initialize；BoardManager 在其后注册。
            // 三者都在才算场景初始化完成，否则 ReInitMapMission 会拿到未初始化的地块表。
            return Game.GetModel<GameData>() != null
                && Game.GetSystem<BoardManager>() != null
                && Game.GetSystem<SinglePlayer.GamePlay.GamePlayManager>() != null;
        }
        catch (Exception)
        {
            return false;
        }
    }

    // ---------------- 状态机 ----------------

    private void DriveFlow()
    {
        if (_stage == Stage.Idle)
        {
            return;
        }

        float now = Time.realtimeSinceStartup;

        if (_stage == Stage.EnteringScene)
        {
            if (IsInSinglePlayer())
            {
                PrepareScene(now);
                return;
            }

            if (now - _stageAt > SceneWaitSeconds)
            {
                Fail("进入单人局超时");
            }
            return;
        }

        if (_stage == Stage.PumpingRounds)
        {
            PumpRounds(now);
            return;
        }

        if (_stage == Stage.Finishing)
        {
            if (now - _stageAt >= SettleSeconds)
            {
                Finish();
            }
            return;
        }

        if (_stage == Stage.Returning)
        {
            TryReopenBattlePass(now);
        }
    }

    /// <summary>
    /// 结算完回到主页面后，自动把通行证页重新打开，让用户直接看到任务变成 100/100。
    /// 只开一次：开了就回到 Idle，避免和用户的手动切页打架。
    /// </summary>
    private void TryReopenBattlePass(float now)
    {
        if (_reopened)
        {
            _stage = Stage.Idle;
            return;
        }

        try
        {
            if (!SimpleSingletonProvider<SceneManager>.hasInstance ||
                SimpleSingletonProvider<SceneManager>.inst.currentType.Value != SceneType.Home)
            {
                if (now - _stageAt > SceneWaitSeconds)
                {
                    Warn("回主页超时，放弃自动打开通行证");
                    _stage = Stage.Idle;
                }
                return;
            }

            if (!SimpleSingletonProvider<UIManager>.hasInstance)
            {
                return;
            }

            UIManager manager = SimpleSingletonProvider<UIManager>.inst;
            if (!(manager.currentPanel is HomePanel) || !manager.currentPanel.IsOpen())
            {
                if (now - _stageAt > SceneWaitSeconds)
                {
                    Warn("等主页面超时，放弃自动打开通行证");
                    _stage = Stage.Idle;
                }
                return;
            }

            _reopened = true;
            _stage = Stage.Idle;
            Info("已回到主页面，自动打开通行证页");
            manager.OpenPanel(UIPanelType.BattlePass).Forget();
        }
        catch (Exception e)
        {
            Warn("自动打开通行证异常: " + e.Message);
            _stage = Stage.Idle;
        }
    }

    private void PrepareScene(float now)
    {
        GameData data = Game.GetModel<GameData>();
        if (data == null)
        {
            if (now - _stageAt > SceneWaitSeconds)
            {
                Fail("GameData 未就绪");
            }
            return;
        }

        try
        {
            int levelId = data.LevelId > 0 ? data.LevelId : 10000;
            data.SetLeveId(levelId);
            data.UpdateLevelConfigBeforeGameStart();
            try
            {
                // RequestSyncSingleGameDataC2S 在 IsReStartGame 为 true 时会静默早退，这里先清掉。
                SimpleSingletonProvider<GameLogicManager>.inst?.singlePlayer?.FinishRestartGame();
            }
            catch (Exception)
            {
            }

            _baseRound = Mathf.Max(0, data.Round.Value);
            Info("场景已就绪：LevelId=" + levelId + " ChapterId=" + data.ChapterId +
                 " 基线回合=" + _baseRound + " Progress=" + data.GameProgress.Value +
                 " HeroId=" + data.heroProperty?.GetUploadData()?.HeroId);
        }
        catch (Exception e)
        {
            Warn("准备场景数据异常: " + e.Message);
        }

        CloseStartPanel();

        _stage = Stage.PumpingRounds;
        _stageAt = now;
        _lastPumpAt = now - IntervalSeconds;
        SetCaption("上报中 0/" + _roundsToSend);
    }

    private static void CloseStartPanel()
    {
        try
        {
            if (!SimpleSingletonProvider<UIManager>.hasInstance)
            {
                return;
            }
            UIManager manager = SimpleSingletonProvider<UIManager>.inst;
            if (manager == null)
            {
                return;
            }
            SinglePlayerStartPanel start = manager.currentPanel as SinglePlayerStartPanel;
            if (start != null && start.IsOpen())
            {
                start.Close();
            }
        }
        catch (Exception)
        {
        }
    }

    private void PumpRounds(float now)
    {
        if (now - _lastPumpAt < IntervalSeconds)
        {
            return;
        }
        _lastPumpAt = now;

        GameData data = Game.GetModel<GameData>();
        if (data == null)
        {
            Fail("GameData 丢失");
            return;
        }

        if (_roundIndex >= _roundsToSend)
        {
            SendVictory(data);
            return;
        }

        _roundIndex++;
        int round = _baseRound + _roundIndex;
        try
        {
            data.Round.Value = round;
            data.GameProgress.Value = round;
            data.SetRoundTiming(RoundTiming.UserOperationEnd);
            data.UploadDataToServer(GameStatus.Running);
        }
        catch (Exception e)
        {
            Warn("上报第 " + _roundIndex + " 回合异常: " + e.Message);
        }

        CloseStartPanel();
        SetCaption("上报中 " + _roundIndex + "/" + _roundsToSend);
    }

    private void SendVictory(GameData data)
    {
        int finalRound = _baseRound + _roundsToSend;
        try
        {
            data.Round.Value = finalRound;
            data.GameProgress.Value = finalRound;
            data.SetRoundTiming(RoundTiming.GameEnd);
            data.UploadDataToServer(GameStatus.Victory);
            Info("已发送终局结算：Round=" + data.Round.Value + " Progress=" + data.GameProgress.Value +
                 " LevelId=" + data.LevelId + " ChapterId=" + data.ChapterId);
        }
        catch (Exception e)
        {
            Warn("发送终局结算异常: " + e.Message);
        }

        SetCaption("已上报 " + _roundsToSend + " 回合");
        _stage = Stage.Finishing;
        _stageAt = Time.realtimeSinceStartup;
    }

    private void Finish()
    {
        _stage = Stage.Idle;
        UnhookTaskSignal();

        SetCaption("已上报 " + _roundsToSend + " 回合，正在返回通行证");

        if (_enteredScene)
        {
            _enteredScene = false;
            try
            {
                Info("返回主页面");
                SimpleSingletonProvider<GameLogicManager>.inst?.singlePlayer?.ExitSinglePlayer();
                _reopened = false;
                _stage = Stage.Returning;
                _stageAt = Time.realtimeSinceStartup;
                return;
            }
            catch (Exception e)
            {
                Warn("返回主页异常: " + e.Message);
            }
        }

        SetCaption("已完成，请打开通行证查看");
    }

    private void Fail(string reason)
    {
        Warn("执行失败：" + reason);
        _stage = Stage.Idle;
        UnhookTaskSignal();
        SetCaption(reason);

        if (_enteredScene)
        {
            _enteredScene = false;
            try
            {
                SimpleSingletonProvider<GameLogicManager>.inst?.singlePlayer?.ExitSinglePlayer();
            }
            catch (Exception)
            {
            }
        }
    }

    // ---------------- 服务器推送监听 ----------------

    private void HookTaskSignal(GameLogicManager logic)
    {
        if (_taskHooked)
        {
            return;
        }

        try
        {
            if (logic == null || logic.battlePass == null || logic.battlePass.signal == null)
            {
                return;
            }
            logic.battlePass.signal.updateTask.AddListener(OnTaskUpdated);
            _taskHooked = true;
            Info("已挂上任务推送监听");
        }
        catch (Exception e)
        {
            Warn("挂任务监听失败: " + e.Message);
        }
    }

    private void OnTaskUpdated()
    {
        try
        {
            GameLogicManager logic = SimpleSingletonProvider<GameLogicManager>.inst;
            BattlePassData data = logic?.battlePass?.BattlePassData;
            if (data == null)
            {
                return;
            }
            int progress = data.GetProgress(_taskId, (int)ConditionType.TotalSingleModeRoundCount);
            Info("服务器推送任务更新：进度=" + progress + "/" + _targetRound + " 已完成=" + data.GetStatus(_taskId));
        }
        catch (Exception)
        {
        }
    }

    private void UnhookTaskSignal()
    {
        if (!_taskHooked)
        {
            return;
        }
        _taskHooked = false;
        try
        {
            if (SimpleSingletonProvider<GameLogicManager>.hasInstance)
            {
                SimpleSingletonProvider<GameLogicManager>.inst?.battlePass?.signal?.updateTask?.RemoveListener(OnTaskUpdated);
            }
        }
        catch (Exception)
        {
        }
    }

    // ---------------- 日志 ----------------

    private void Info(string message)
    {
        try
        {
            _log?.Info(message);
        }
        catch (Exception)
        {
        }
    }

    private void Warn(string message)
    {
        try
        {
            _log?.Warn(message);
        }
        catch (Exception)
        {
        }
    }
}
