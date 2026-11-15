using System;
using CesiumLoader.SDK.Mods;
using Core.Scene;
using FairyGUI;
using SinglePlayer;
using SinglePlayer.GamePlay;
using SinglePlayer.GamePlay.Relic;
using Tools;
using UnityEngine;

namespace TownTrainer
{
    public static class ModEntry
    {
        public static void Main()
        {
            ModBase.Run(new TownTrainerController());
        }
    }

    public sealed class TownTrainerController : ModBase
    {
        private TownTrainerPanel _panel;

        public override string Name { get { return "吉星小镇修改器"; } }
        public override string Version { get { return "1.1.0"; } }
        public override string Author { get { return "TenkinMori"; } }
        public override string Description
        {
            get { return "单人模式（吉星小镇）修改器：金币、卡牌、遗物、进度、骰子、结算。"; }
        }

        public override void OnInitialize()
        {
            Log.Info("修改器已加载，F8 打开/关闭");
        }

        public override void OnUpdate()
        {
            try
            {
                if (_panel == null) _panel = new TownTrainerPanel(this);
                _panel.Tick();
            }
            catch (Exception e) { Log.Info("[WARN] UI: " + e.Message); }
        }

        public override void OnUnload()
        {
            _panel?.Dispose();
            _panel = null;
        }

        internal void ChangeGold(int amount)
        {
            BoardManager board = Game.GetSystem<BoardManager>();
            board.characterManager.Hero.Property.ChangeGold(amount);
            board.characterManager.Hero.UpdateViewProperty();
        }

        internal void AddCard(int id)
        {
            Game.GetSystem<BoardManager>().cardManager.AddCardToBag(id);
        }

        internal void AddRelic(int id)
        {
            Game.GetSystem<BoardManager>().relicManager.AddRelic(new RelicInfo(id));
        }

        internal void SetProgress(int value)
        {
            GameData data = Game.GetModel<GameData>();
            data.GameProgress.Value = value;
            foreach (var mission in data.MapData.MapMissionData)
            {
                mission.GMSkipProgress(value);
            }
        }

        internal void SetDice(int value)
        {
            Game.GetModel<GMData>().SetDicePoint(value);
        }

        internal void SetLockProgress(bool value)
        {
            Game.GetModel<GMData>().SetLockGameProgress(value);
        }

        internal void InstantVictory()
        {
            Game.GetModel<GameData>().UploadDataToServer(GameStatus.Victory);
        }
    }

    internal sealed class TownTrainerPanel
    {
        private readonly TownTrainerController _controller;
        private GComponent _root;
        private GComponent _toggle;
        private GTextInput _gold;
        private GTextInput _card;
        private GTextInput _relic;
        private GTextInput _progress;
        private GTextInput _dice;
        private GTextField _status;
        private bool _visible;

        public TownTrainerPanel(TownTrainerController controller)
        {
            _controller = controller;
        }

        public void Tick()
        {
            if (GRoot.inst == null) return;

            if (_toggle == null) BuildToggle();
            if (_root == null) BuildPanel();

            bool singlePlayer = false;
            try
            {
                singlePlayer = SimpleSingletonProvider<Core.Scene.SceneManager>.inst.currentType.Value == SceneType.SinglePlayer;
            }
            catch { }

            if (_toggle != null)
            {
                _toggle.visible = singlePlayer;
                _toggle.SetPosition(GRoot.inst.width - 124f, 18f, 0f);
                GRoot.inst.SetChildIndex(_toggle, GRoot.inst.numChildren - 1);
            }
            if (_root != null)
            {
                _root.visible = singlePlayer && _visible;
                _root.SetPosition(GRoot.inst.width - 380f, 62f, 0f);
                GRoot.inst.SetChildIndex(_root, GRoot.inst.numChildren - 1);
            }

            if (singlePlayer && Input.GetKeyDown(KeyCode.F8))
            {
                _visible = !_visible;
            }
        }

        public void Dispose()
        {
            if (_root != null) { _root.Dispose(); _root = null; }
            if (_toggle != null) { _toggle.Dispose(); _toggle = null; }
        }

        private void BuildToggle()
        {
            _toggle = Button("修改器", 0f, 0f, 96f, 34f, new Color(0.92f, 0.23f, 0.45f, 1f), () => _visible = !_visible);
            _toggle.SetPosition(GRoot.inst.width - 124f, 18f, 0f);
            
            GRoot.inst.AddChild(_toggle);
        }

        private void BuildPanel()
        {
            const float w = 360f;
            const float h = 500f;
            _root = new GComponent();
            _root.SetSize(w, h);
            _root.SetPosition(GRoot.inst.width - 380f, 62f, 0f);

            GGraph bg = new GGraph();
            bg.DrawRoundRect(w, h, new Color(0.06f, 0.07f, 0.10f, 0.94f), new float[] { 12, 12, 12, 12 });
            _root.AddChild(bg);

            _root.AddChild(Label("吉星小镇修改器  v1.1.0", 18, 16, 24, Color.white));
            _status = Label("F8 打开/关闭", 18, 48, 16, new Color(0.75f, 0.78f, 0.85f, 1f));
            _root.AddChild(_status);

            float y = 84f;
            _gold = AddRow("金币", y, "1000", "+", new Color(0.93f, 0.55f, 0.15f, 1f), () =>
            {
                _controller.ChangeGold(Parse(_gold, 1000));
                _status.text = "金币已增加";
            }, "-", () =>
            {
                _controller.ChangeGold(-Parse(_gold, 1000));
                _status.text = "金币已减少";
            });

            y += 46f;
            _card = AddRow("卡牌 ID", y, "1001", "添加", new Color(0.25f, 0.65f, 0.95f, 1f), () =>
            {
                _controller.AddCard(Parse(_card, 0));
                _status.text = "已添加卡牌 " + _card.text;
            }, null, null);

            y += 46f;
            _relic = AddRow("遗物 ID", y, "1001", "添加", new Color(0.60f, 0.42f, 0.90f, 1f), () =>
            {
                _controller.AddRelic(Parse(_relic, 0));
                _status.text = "已添加遗物 " + _relic.text;
            }, null, null);

            y += 46f;
            _progress = AddRow("进度", y, "100", "设置", new Color(0.25f, 0.75f, 0.55f, 1f), () =>
            {
                _controller.SetProgress(Parse(_progress, 0));
                _status.text = "进度已设置";
            }, null, null);

            y += 46f;
            _dice = AddRow("骰子", y, "6", "设置", new Color(0.92f, 0.55f, 0.20f, 1f), () =>
            {
                _controller.SetDice(Parse(_dice, 1));
                _status.text = "骰子已设置";
            }, "自动", () =>
            {
                _controller.SetDice(-1);
                _status.text = "骰子已恢复自动";
            });

            y += 56f;
            _root.AddChild(Button("锁定进度 ON", 18, y, 150, 32, new Color(0.30f, 0.65f, 0.45f, 1f), () =>
            {
                _controller.SetLockProgress(true);
                _status.text = "进度已锁定";
            }));
            _root.AddChild(Button("锁定进度 OFF", 180, y, 160, 32, new Color(0.30f, 0.40f, 0.55f, 1f), () =>
            {
                _controller.SetLockProgress(false);
                _status.text = "进度已解锁";
            }));

            y += 42f;
            _root.AddChild(Button("直接结算胜利", 18, y, 322, 38, new Color(0.88f, 0.22f, 0.42f, 1f), () =>
            {
                _controller.InstantVictory();
                _status.text = "已发送结算";
            }));

            y += 50f;
            _root.AddChild(Button("关闭 (F8)", 18, y, 322, 32, new Color(0.22f, 0.24f, 0.30f, 1f), () =>
            {
                _visible = false;
            }));

            _root.visible = false;
            GRoot.inst.AddChild(_root);
        }

        private GTextInput AddRow(string label, float y, string value, string primary, Color color, Action primaryAction, string secondary, Action secondaryAction)
        {
            _root.AddChild(Label(label, 18, y + 7, 16, new Color(0.85f, 0.86f, 0.90f, 1f)));

            GTextInput input = new GTextInput();
            input.SetSize(140f, 30f);
            input.SetPosition(110f, y, 0f);
            input.text = value;
            input.textFormat.size = 16;
            input.color = Color.white;
            _root.AddChild(input);

            GGraph inputBg = new GGraph();
            inputBg.DrawRoundRect(140f, 30f, new Color(0.16f, 0.17f, 0.22f, 1f), new float[] { 6, 6, 6, 6 });
            inputBg.SetPosition(110f, y, 0f);
            _root.AddChild(inputBg);
            _root.SetChildIndex(inputBg, _root.GetChildIndex(input));

            GComponent primaryBtn = Button(primary, 258f, y, secondary != null ? 62f : 84f, 30f, color, primaryAction);
            _root.AddChild(primaryBtn);

            if (secondary != null)
            {
                _root.AddChild(Button(secondary, 326f, y, 50f, 30f, new Color(0.26f, 0.28f, 0.34f, 1f), secondaryAction));
            }
            return input;
        }

        private static GTextField Label(string text, float x, float y, int size, Color color)
        {
            GTextField label = new GTextField();
            label.text = text;
            label.textFormat.size = size;
            label.color = color;
            label.SetPosition(x, y, 0f);
            return label;
        }

        private GComponent Button(string text, float x, float y, float w, float h, Color color, Action onClick)
        {
            GComponent button = new GComponent();
            button.SetSize(w, h);
            button.SetPosition(x, y, 0f);

            GGraph bg = new GGraph();
            bg.DrawRoundRect(w, h, color, new float[] { 8, 8, 8, 8 });
            button.AddChild(bg);

            GTextField label = new GTextField();
            label.text = text;
            label.textFormat.size = 16;
            label.color = Color.white;
            label.SetSize(w, h);
            label.SetPosition(0f, (h - 20f) / 2f, 0f);
            label.align = AlignType.Center;
            button.AddChild(label);

            button.touchable = true;
            button.onClick.Add((EventContext context) => onClick());
            return button;
        }

        private static int Parse(GTextInput input, int fallback)
        {
            int value;
            return int.TryParse(input.text, out value) ? value : fallback;
        }
    }
}
