using System;
using CesiumLoader.SDK.Mods;
using SinglePlayer;
using SinglePlayer.GamePlay;
using SinglePlayer.GamePlay.Relic;
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
        private GameObject _go;
        private TrainerBehaviour _gui;

        public override string Name { get { return "吉星小镇修改器"; } }
        public override string Version { get { return "1.0.0"; } }
        public override string Author { get { return "TenkinMori"; } }
        public override string Description
        {
            get { return "单人模式（吉星小镇）修改器：金币、卡牌、遗物、进度、骰子、结算。"; }
        }

        public override void OnInitialize()
        {
            _go = new GameObject("TownTrainerGUI");
            UnityEngine.Object.DontDestroyOnLoad(_go);
            _gui = _go.AddComponent<TrainerBehaviour>();
            _gui.Controller = this;
            Log.Info("修改器已加载，F8 打开/关闭");
        }

        public override void OnUnload()
        {
            if (_go != null) UnityEngine.Object.Destroy(_go);
        }
    }

    public sealed class TrainerBehaviour : MonoBehaviour
    {
        public TownTrainerController Controller;

        private bool _show;
        private int _gold = 1000;
        private int _cardId = 1001;
        private int _relicId = 1001;
        private int _progress = 100;
        private int _dice = 6;
        private string _status = "F8 toggle";

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.F8)) _show = !_show;
        }

        private void OnGUI()
        {
            if (!_show) return;

            GUI.skin.label.fontSize = 14;
            GUI.skin.button.fontSize = 14;
            GUILayout.BeginArea(new Rect(20, 20, 360, 520), GUI.skin.box);
            GUILayout.Label("吉星小镇修改器 v1.0.0");
            GUILayout.Label(_status);

            GUILayout.BeginHorizontal();
            GUILayout.Label("Gold", GUILayout.Width(70));
            _gold = ParseInt(GUILayout.TextField(_gold.ToString(), GUILayout.Width(90)), _gold);
            if (GUILayout.Button("+")) ChangeGold(_gold);
            if (GUILayout.Button("-")) ChangeGold(-_gold);
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("Card ID", GUILayout.Width(70));
            _cardId = ParseInt(GUILayout.TextField(_cardId.ToString(), GUILayout.Width(90)), _cardId);
            if (GUILayout.Button("Add")) AddCard(_cardId);
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("Relic ID", GUILayout.Width(70));
            _relicId = ParseInt(GUILayout.TextField(_relicId.ToString(), GUILayout.Width(90)), _relicId);
            if (GUILayout.Button("Add")) AddRelic(_relicId);
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("Progress", GUILayout.Width(70));
            _progress = ParseInt(GUILayout.TextField(_progress.ToString(), GUILayout.Width(90)), _progress);
            if (GUILayout.Button("Set")) SetProgress(_progress);
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("Dice", GUILayout.Width(70));
            _dice = ParseInt(GUILayout.TextField(_dice.ToString(), GUILayout.Width(90)), _dice);
            if (GUILayout.Button("Set")) SetDice(_dice);
            if (GUILayout.Button("Clear")) SetDice(-1);
            GUILayout.EndHorizontal();

            if (GUILayout.Button("Lock progress ON")) SetLockProgress(true);
            if (GUILayout.Button("Lock progress OFF")) SetLockProgress(false);
            if (GUILayout.Button("Instant victory")) InstantVictory();

            if (GUILayout.Button("Close (F8)")) _show = false;
            GUILayout.EndArea();
        }

        private static int ParseInt(string text, int fallback)
        {
            int value;
            return int.TryParse(text, out value) ? value : fallback;
        }

        private void ChangeGold(int amount)
        {
            try
            {
                BoardManager board = Game.GetSystem<BoardManager>();
                board.characterManager.Hero.Property.ChangeGold(amount);
                board.characterManager.Hero.UpdateViewProperty();
                _status = "Gold " + (amount >= 0 ? "+" : "") + amount;
            }
            catch (Exception e) { _status = "Gold failed: " + e.Message; }
        }

        private void AddCard(int id)
        {
            try
            {
                Game.GetSystem<BoardManager>().cardManager.AddCardToBag(id);
                _status = "Card " + id;
            }
            catch (Exception e) { _status = "Card failed: " + e.Message; }
        }

        private void AddRelic(int id)
        {
            try
            {
                Game.GetSystem<BoardManager>().relicManager.AddRelic(new RelicInfo(id));
                _status = "Relic " + id;
            }
            catch (Exception e) { _status = "Relic failed: " + e.Message; }
        }

        private void SetProgress(int value)
        {
            try
            {
                GameData data = Game.GetModel<GameData>();
                data.GameProgress.Value = value;
                foreach (var mission in data.MapData.MapMissionData)
                {
                    mission.GMSkipProgress(value);
                }
                _status = "Progress " + value;
            }
            catch (Exception e) { _status = "Progress failed: " + e.Message; }
        }

        private void SetDice(int value)
        {
            try
            {
                Game.GetModel<GMData>().SetDicePoint(value);
                _status = value < 0 ? "Dice auto" : "Dice " + value;
            }
            catch (Exception e) { _status = "Dice failed: " + e.Message; }
        }

        private void SetLockProgress(bool value)
        {
            try
            {
                Game.GetModel<GMData>().SetLockGameProgress(value);
                _status = "Lock " + value;
            }
            catch (Exception e) { _status = "Lock failed: " + e.Message; }
        }

        private void InstantVictory()
        {
            try
            {
                Game.GetModel<GameData>().UploadDataToServer(GameStatus.Victory);
                _status = "Victory";
            }
            catch (Exception e) { _status = "Victory failed: " + e.Message; }
        }
    }
}
