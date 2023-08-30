using ClientCore;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using DTAClient.Domain;
using System.IO;
using ClientGUI;
using Rampastring.XNAUI.XNAControls;
using Rampastring.XNAUI;
using Rampastring.Tools;
using ClientUpdater;
using ClientCore.Extensions;
using Microsoft.Xna.Framework.Input;
using Rampastring.XNAUI.Input;

namespace DTAClient.DXGUI.Generic
{
    public class CampaignSelector : XNAWindow
    {
        private const int DEFAULT_WIDTH = 1100;
        private const int DEFAULT_HEIGHT = 620;

        private static string[] DifficultyNames = new string[] { "Easy", "Medium", "Hard" };

        private static string[] DifficultyIniPaths = new string[]
        {
            "INI/Map Code/Difficulty Easy.ini",
            "INI/Map Code/Difficulty Medium.ini",
            "INI/Map Code/Difficulty Hard.ini"
        };

        public CampaignSelector(WindowManager windowManager, DiscordHandler discordHandler) : base(windowManager)
        {
            this.discordHandler = discordHandler;
        }

        private DiscordHandler discordHandler;

        private List<Mission> Missions = new List<Mission>();
        private XNAListBox lbCampaignList;
        private XNAClientButton btnLaunch;
        private XNATextBlock tbMissionDescription;
        private XNATrackbar trbDifficultySelector;
        private XNATrackbar trbCampaignGameSpeedselector;

        private CheaterWindow cheaterWindow;

        private string[] filesToCheck = new string[]
        {
            "INI/AI.ini",
            "INI/AIE.ini",
            "INI/Art.ini",
            "INI/ArtE.ini",
            "INI/Enhance.ini",
            "INI/Rules.ini",
            "INI/Map Code/Difficulty Hard.ini",
            "INI/Map Code/Difficulty Medium.ini",
            "INI/Map Code/Difficulty Easy.ini"
        };

        private Mission missionToLaunch;

        public override void Initialize()
        {
            BackgroundTexture = AssetLoader.LoadTexture("missionselectorbg.png");
            ClientRectangle = new Rectangle(0, 0, DEFAULT_WIDTH, DEFAULT_HEIGHT);
            BorderColor = UISettings.ActiveSettings.PanelBorderColor;

            Name = "CampaignSelector";

            var lblSelectCampaign = new XNALabel(WindowManager);
            lblSelectCampaign.Name = "lblSelectCampaign";
            lblSelectCampaign.FontIndex = 1;
            lblSelectCampaign.ClientRectangle = new Rectangle(12, 12, 0, 0);
            lblSelectCampaign.Text = "MISSIONS:".L10N("Client:Main:Missions");

            lbCampaignList = new XNAListBox(WindowManager);
            lbCampaignList.Name = "lbCampaignList";
            lbCampaignList.BackgroundTexture = AssetLoader.CreateTexture(new Color(0, 0, 0, 128), 2, 2);
            lbCampaignList.PanelBackgroundDrawMode = PanelBackgroundImageDrawMode.STRETCHED;
            lbCampaignList.ClientRectangle = new Rectangle(12,
                lblSelectCampaign.Bottom + 6, 318, 560);
            lbCampaignList.SelectedIndexChanged += LbCampaignList_SelectedIndexChanged;

            var lblMissionDescriptionHeader = new XNALabel(WindowManager);
            lblMissionDescriptionHeader.Name = "lblMissionDescriptionHeader";
            lblMissionDescriptionHeader.FontIndex = 1;
            lblMissionDescriptionHeader.ClientRectangle = new Rectangle(
                lbCampaignList.Right + 12,
                lblSelectCampaign.Y, 0, 0);
            lblMissionDescriptionHeader.Text = "MISSION DESCRIPTION:".L10N("Client:Main:MissionDescription");

            tbMissionDescription = new XNATextBlock(WindowManager);
            tbMissionDescription.Name = "tbMissionDescription";
            tbMissionDescription.ClientRectangle = new Rectangle(
                lblMissionDescriptionHeader.X,
                lblMissionDescriptionHeader.Bottom + 6,
                Width - 24 - lbCampaignList.Right, 490);
            tbMissionDescription.PanelBackgroundDrawMode = PanelBackgroundImageDrawMode.STRETCHED;
            tbMissionDescription.Alpha = 1.0f;

            tbMissionDescription.BackgroundTexture = AssetLoader.CreateTexture(AssetLoader.GetColorFromString(ClientConfiguration.Instance.AltUIBackgroundColor),
                tbMissionDescription.Width, tbMissionDescription.Height);

            var lblDifficultyLevel = new XNALabel(WindowManager);
            lblDifficultyLevel.Name = "lblDifficultyLevel";
            lblDifficultyLevel.Text = "DIFFICULTY LEVEL".L10N("Client:Main:DifficultyLevel");
            lblDifficultyLevel.FontIndex = 1;
            Vector2 textSize = Renderer.GetTextDimensions(lblDifficultyLevel.Text, lblDifficultyLevel.FontIndex);
            lblDifficultyLevel.ClientRectangle = new Rectangle(
                tbMissionDescription.X + 232,
                 tbMissionDescription.Bottom + 12, (int)textSize.X, (int)textSize.Y);

            trbDifficultySelector = new XNATrackbar(WindowManager);
            trbDifficultySelector.Name = "trbDifficultySelector";
            trbDifficultySelector.ClientRectangle = new Rectangle(
                tbMissionDescription.X + 230, lblDifficultyLevel.Bottom + 6,
                120, 30);
            trbDifficultySelector.MinValue = 0;
            trbDifficultySelector.MaxValue = 2;
            trbDifficultySelector.BackgroundTexture = AssetLoader.CreateTexture(
                new Color(0, 0, 0, 128), 2, 2);
            trbDifficultySelector.ButtonTexture = AssetLoader.LoadTextureUncached(
                "trackbarButton.png");

            var lblCampaignDefaultGameSpeed = new XNALabel(WindowManager);
            lblCampaignDefaultGameSpeed.Name = "lblCampaignDefaultGameSpeed";
            lblCampaignDefaultGameSpeed.Text = "CampaignGameSpeed".L10N("Client:Main:CampaignDefaultGameSpeed");
            lblCampaignDefaultGameSpeed.FontIndex = 1;
            Vector2 textSize1 = Renderer.GetTextDimensions(lblCampaignDefaultGameSpeed.Text, lblCampaignDefaultGameSpeed.FontIndex);
            lblCampaignDefaultGameSpeed.ClientRectangle = new Rectangle(
                tbMissionDescription.X + 2,
                tbMissionDescription.Bottom + 12, (int)textSize.X, (int)textSize.Y);

            trbCampaignGameSpeedselector = new XNATrackbar(WindowManager);
            trbCampaignGameSpeedselector.Name = "trbCampaignGameSpeedselector";
            trbCampaignGameSpeedselector.ClientRectangle = new Rectangle(
                tbMissionDescription.X, lblCampaignDefaultGameSpeed.Bottom + 6,
                220, 30);
            trbCampaignGameSpeedselector.MinValue = 0;
            trbCampaignGameSpeedselector.MaxValue = 6;
            trbCampaignGameSpeedselector.Value = 4;
            trbCampaignGameSpeedselector.BackgroundTexture = AssetLoader.CreateTexture(
                new Color(0, 0, 0, 128), 2, 2);
            trbCampaignGameSpeedselector.ButtonTexture = AssetLoader.LoadTextureUncached(
                "trackbarButton.png");

            var lbl0 = new XNALabel(WindowManager);
            lbl0.Name = "lbl0";
            lbl0.FontIndex = 1;
            lbl0.Text = "0";
            lbl0.ClientRectangle = new Rectangle(trbCampaignGameSpeedselector.X + 2,
                trbCampaignGameSpeedselector.Bottom + 6, 1, 1);

            var lbl1 = new XNALabel(WindowManager);
            lbl1.Name = "lbl1";
            lbl1.FontIndex = 1;
            lbl1.Text = "1";
            lbl1.ClientRectangle = new Rectangle(trbCampaignGameSpeedselector.X + 36,
                trbCampaignGameSpeedselector.Bottom + 6, 1, 1);

            var lbl2 = new XNALabel(WindowManager);
            lbl2.Name = "lbl2";
            lbl2.FontIndex = 1;
            lbl2.Text = "2";
            lbl2.ClientRectangle = new Rectangle(trbCampaignGameSpeedselector.X + 72,
                trbCampaignGameSpeedselector.Bottom + 6, 1, 1);

            var lbl3 = new XNALabel(WindowManager);
            lbl3.Name = "lbl3";
            lbl3.FontIndex = 1;
            lbl3.Text = "3";
            lbl3.ClientRectangle = new Rectangle(trbCampaignGameSpeedselector.X + 108,
                trbCampaignGameSpeedselector.Bottom + 6, 1, 1);

            var lbl4 = new XNALabel(WindowManager);
            lbl4.Name = "lbl4";
            lbl4.FontIndex = 1;
            lbl4.Text = "4";
            lbl4.ClientRectangle = new Rectangle(trbCampaignGameSpeedselector.X + 140,
                trbCampaignGameSpeedselector.Bottom + 6, 1, 1);

            var lbl5 = new XNALabel(WindowManager);
            lbl5.Name = "lbl5";
            lbl5.FontIndex = 1;
            lbl5.Text = "5";
            lbl5.ClientRectangle = new Rectangle(trbCampaignGameSpeedselector.X + 176,
                trbCampaignGameSpeedselector.Bottom + 6, 1, 1);

            var lbl6 = new XNALabel(WindowManager);
            lbl6.Name = "lbl6";
            lbl6.FontIndex = 1;
            lbl6.Text = "6";
            lbl6.ClientRectangle = new Rectangle(trbCampaignGameSpeedselector.Right - 10,
                trbCampaignGameSpeedselector.Bottom + 6, 1, 1);

            var lblEasy = new XNALabel(WindowManager);
            lblEasy.Name = "lblEasy";
            lblEasy.FontIndex = 1;
            lblEasy.Text = "EASY".L10N("Client:Main:DifficultyEasy");
            lblEasy.ClientRectangle = new Rectangle(trbDifficultySelector.X + 2,
                trbDifficultySelector.Bottom + 6, 1, 1);

            var lblNormal = new XNALabel(WindowManager);
            lblNormal.Name = "lblNormal";
            lblNormal.FontIndex = 1;
            lblNormal.Text = "NORMAL".L10N("Client:Main:DifficultyNormal");
            textSize = Renderer.GetTextDimensions(lblNormal.Text, lblNormal.FontIndex);
            lblNormal.ClientRectangle = new Rectangle(
                trbDifficultySelector.X + (trbDifficultySelector.Width - (int)textSize.X) / 2,
                lblEasy.Y, (int)textSize.X, (int)textSize.Y);

            var lblHard = new XNALabel(WindowManager);
            lblHard.Name = "lblHard";
            lblHard.FontIndex = 1;
            lblHard.Text = "HARD".L10N("Client:Main:DifficultyHard");
            lblHard.ClientRectangle = new Rectangle(
                trbDifficultySelector.Right - lblHard.Width,
                lblEasy.Y, 1, 1);

            btnLaunch = new XNAClientButton(WindowManager);
            btnLaunch.Name = "btnLaunch";
            btnLaunch.ClientRectangle = new Rectangle(Width - 290, Height - 50, UIDesignConstants.BUTTON_WIDTH_133, UIDesignConstants.BUTTON_HEIGHT);
            btnLaunch.Text = "Launch".L10N("Client:Main:ButtonLaunch");
            btnLaunch.AllowClick = false;
            btnLaunch.LeftClick += BtnLaunch_LeftClick;
            btnLaunch.HotKey = Keys.None;               // 默认快捷键设置为 None
            LoadHotkeyForButton("11开始", btnLaunch);   // 从 INI 文件加载 btnCancel 的快捷键

            var btnCancel = new XNAClientButton(WindowManager);
            btnCancel.Name = "btnCancel";
            btnCancel.ClientRectangle = new Rectangle(Width - 145,
                btnLaunch.Y, UIDesignConstants.BUTTON_WIDTH_133, UIDesignConstants.BUTTON_HEIGHT);
            btnCancel.Text = "Cancel".L10N("Client:Main:ButtonCancel");
            btnCancel.LeftClick += BtnCancel_LeftClick;
            btnCancel.HotKey = Keys.None;               // 默认快捷键设置为 None
            LoadHotkeyForButton("12返回", btnCancel);   // 从 INI 文件加载 btnCancel 的快捷键

            AddChild(lblCampaignDefaultGameSpeed);
            AddChild(trbCampaignGameSpeedselector);
            AddChild(lbl0);
            AddChild(lbl1);
            AddChild(lbl2);
            AddChild(lbl3);
            AddChild(lbl4);
            AddChild(lbl5);
            AddChild(lbl6);
            AddChild(lblSelectCampaign);
            AddChild(lblMissionDescriptionHeader);
            AddChild(lbCampaignList);
            AddChild(tbMissionDescription);
            AddChild(lblDifficultyLevel);
            AddChild(btnLaunch);
            AddChild(btnCancel);
            AddChild(trbDifficultySelector);
            AddChild(lblEasy);
            AddChild(lblNormal);
            AddChild(lblHard);

            // Set control attributes from INI file
            base.Initialize();

            // Center on screen
            CenterOnParent();

            trbDifficultySelector.Value = UserINISettings.Instance.Difficulty;

            ReadMissionList();

            cheaterWindow = new CheaterWindow(WindowManager);
            var dp = new DarkeningPanel(WindowManager);
            dp.AddChild(cheaterWindow);
            AddChild(dp);
            dp.CenterOnParent();
            cheaterWindow.CenterOnParent();
            cheaterWindow.YesClicked += CheaterWindow_YesClicked;
            cheaterWindow.Disable();
        }

        // 从 INI 文件加载快捷键
        private static void LoadHotkeyForButton(string actionName, XNAClientButton button)
        {
            string iniFilePath = Path.Combine("Resources", "DIY", "快捷键.ini");
            if (File.Exists(iniFilePath))
            {
                var lines = File.ReadAllLines(iniFilePath);
                foreach (var line in lines)
                {
                    var parts = line.Split('=');
                    if (parts.Length == 2)
                    {
                        string action = parts[0].Trim();
                        string key = parts[1].Trim();

                        if (action == actionName)
                        {
                            button.HotKey = ParseKey(key);
                            break;
                        }
                    }
                }
            }
            else
            {
                Logger.Log($"无法找到快捷键文件: {iniFilePath}");
            }
        }

        private static Keys ParseKey(string keyString)
        {
            return keyString switch
            {
                "Enter" => Keys.Enter,
                "Escape" => Keys.Escape,
                "C" => Keys.C,
                "L" => Keys.L,
                "S" => Keys.S,
                "M" => Keys.M,
                "N" => Keys.N,
                "O" => Keys.O,
                "E" => Keys.E,
                "T" => Keys.T,
                "R" => Keys.R,
                "X" => Keys.X,
                "A" => Keys.A,
                "D" => Keys.D,
                "W" => Keys.W,
                "Q" => Keys.Q,
                "F" => Keys.F,
                "Z" => Keys.Z,
                "V" => Keys.V,
                "B" => Keys.B,
                "P" => Keys.P,
                "I" => Keys.I,
                "H" => Keys.H,
                "Space" => Keys.Space,
                "Tab" => Keys.Tab,
                "Left" => Keys.Left,
                "Right" => Keys.Right,
                "Up" => Keys.Up,
                "Down" => Keys.Down,
                "Back" => Keys.Back,
                "Delete" => Keys.Delete,
                "Home" => Keys.Home,
                "End" => Keys.End,
                "PageUp" => Keys.PageUp,
                "PageDown" => Keys.PageDown,
                _ => Keys.None // 默认返回 None
            };
        }

        private void LbCampaignList_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (lbCampaignList.SelectedIndex == -1)
            {
                tbMissionDescription.Text = string.Empty;
                btnLaunch.AllowClick = false;
                return;
            }

            Mission mission = Missions[lbCampaignList.SelectedIndex];

            if (string.IsNullOrEmpty(mission.Scenario))
            {
                tbMissionDescription.Text = string.Empty;
                btnLaunch.AllowClick = false;
                return;
            }

            tbMissionDescription.Text = mission.GUIDescription;

            if (!mission.Enabled)
            {
                btnLaunch.AllowClick = false;
                return;
            }

            btnLaunch.AllowClick = true;
        }

        private void BtnCancel_LeftClick(object sender, EventArgs e)
        {
            Enabled = false;
        }

        private void BtnLaunch_LeftClick(object sender, EventArgs e)
        {
            int selectedMissionId = lbCampaignList.SelectedIndex;

            Mission mission = Missions[selectedMissionId];

            if (!ClientConfiguration.Instance.ModMode &&
                (!Updater.IsFileNonexistantOrOriginal(mission.Scenario) || AreFilesModified()))
            {
                // Confront the user by showing the cheater screen
                missionToLaunch = mission;
                cheaterWindow.Enable();
                return;
            }

            LaunchMission(mission);
        }

        private bool AreFilesModified()
        {
            foreach (string filePath in filesToCheck)
            {
                if (!Updater.IsFileNonexistantOrOriginal(filePath))
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Called when the user wants to proceed to the mission despite having
        /// being called a cheater.
        /// </summary>
        private void CheaterWindow_YesClicked(object sender, EventArgs e)
        {
            LaunchMission(missionToLaunch);
        }

        /// <summary>
        /// Starts a singleplayer mission.
        /// </summary>
        private void LaunchMission(Mission mission)
        {
            bool copyMapsToSpawnmapINI = ClientConfiguration.Instance.CopyMissionsToSpawnmapINI;

            Logger.Log("即将写入 spawn.ini。");
            using var spawnStreamWriter = new StreamWriter(SafePath.CombineFilePath(ProgramConstants.GamePath, "spawn.ini"));
            spawnStreamWriter.WriteLine("; Generated by DTA Client");
            spawnStreamWriter.WriteLine("[Settings]");
            if (copyMapsToSpawnmapINI)
                spawnStreamWriter.WriteLine("Scenario=spawnmap.ini");
            else
                spawnStreamWriter.WriteLine("Scenario=" + mission.Scenario);

            // No one wants to play missions on Fastest, so we'll change it to Faster
            if (UserINISettings.Instance.GameSpeed == 0)
                UserINISettings.Instance.GameSpeed.Value = 1;
            UserINISettings.Instance.CampaignDefaultGameSpeed.Value = trbCampaignGameSpeedselector.Value;
            spawnStreamWriter.WriteLine("CampaignDefaultGameSpeed=" + GetCampaignGameSpeed());
            spawnStreamWriter.WriteLine("CampaignID=" + mission.Index);
            spawnStreamWriter.WriteLine("GameSpeed=" + UserINISettings.Instance.GameSpeed);
#if YR || ARES
            spawnStreamWriter.WriteLine("Ra2Mode=" + !mission.RequiredAddon);
#else
            spawnStreamWriter.WriteLine("Firestorm=" + mission.RequiredAddon);
#endif
            spawnStreamWriter.WriteLine("CustomLoadScreen=" + LoadingScreenController.GetLoadScreenName(mission.Side.ToString()));
            spawnStreamWriter.WriteLine("IsSinglePlayer=Yes");
            spawnStreamWriter.WriteLine("SidebarHack=" + ClientConfiguration.Instance.SidebarHack);
            spawnStreamWriter.WriteLine("Side=" + mission.Side);
            spawnStreamWriter.WriteLine("BuildOffAlly=" + mission.BuildOffAlly);

            UserINISettings.Instance.Difficulty.Value = trbDifficultySelector.Value;

            spawnStreamWriter.WriteLine("DifficultyModeHuman=" + (mission.PlayerAlwaysOnNormalDifficulty ? "1" : trbDifficultySelector.Value.ToString()));
            spawnStreamWriter.WriteLine("DifficultyModeComputer=" + GetComputerDifficulty());

            var difficultyIni = new IniFile(SafePath.CombineFilePath(ProgramConstants.GamePath, DifficultyIniPaths[trbDifficultySelector.Value]));
            string difficultyName = DifficultyNames[trbDifficultySelector.Value];

            spawnStreamWriter.WriteLine();
            spawnStreamWriter.WriteLine();
            spawnStreamWriter.WriteLine();

            if (copyMapsToSpawnmapINI)
            {
                var mapIni = new IniFile(SafePath.CombineFilePath(ProgramConstants.GamePath, mission.Scenario));
                IniFile.ConsolidateIniFiles(mapIni, difficultyIni);
                mapIni.WriteIniFile(SafePath.CombineFilePath(ProgramConstants.GamePath, "spawnmap.ini"));
            }

            UserINISettings.Instance.Difficulty.Value = trbDifficultySelector.Value;
            UserINISettings.Instance.SaveSettings();

            ((MainMenuDarkeningPanel)Parent).Hide();

            discordHandler.UpdatePresence(mission.UntranslatedGUIName, difficultyName, mission.IconPath, true);
            GameProcessLogic.GameProcessExited += GameProcessExited_Callback;

            GameProcessLogic.StartGameProcess(WindowManager);
        }

        private int GetComputerDifficulty() =>
            Math.Abs(trbDifficultySelector.Value - 2);

        private int GetCampaignGameSpeed() =>
            Math.Abs(trbCampaignGameSpeedselector.Value + 0);

        private void GameProcessExited_Callback()
        {
            WindowManager.AddCallback(new Action(GameProcessExited), null);
        }

        protected virtual void GameProcessExited()
        {
            GameProcessLogic.GameProcessExited -= GameProcessExited_Callback;
            // Logger.Log("GameProcessExited: 更新 Discord 存在。");
            discordHandler.UpdatePresence();
        }

        private void ReadMissionList()
        {
            ParseBattleIni("INI/Battle.ini");

            if (Missions.Count == 0)
                ParseBattleIni("INI/" + ClientConfiguration.Instance.BattleFSFileName);
        }

        /// <summary>
        /// Parses a Battle(E).ini file. Returns true if succesful (file found), otherwise false.
        /// </summary>
        /// <param name="path">The path of the file, relative to the game directory.</param>
        /// <returns>True if succesful, otherwise false.</returns>
        private bool ParseBattleIni(string path)
        {
            Logger.Log("尝试解析 " + path + " 以填充任务列表。");

            FileInfo battleIniFileInfo = SafePath.GetFile(ProgramConstants.GamePath, path);
            if (!battleIniFileInfo.Exists)
            {
                Logger.Log("文件 " + path + " 未找到。忽略。");
                return false;
            }

            if (Missions.Count > 0)
            {
                throw new InvalidOperationException("Loading multiple Battle*.ini files is not supported anymore.");
            }

            var battleIni = new IniFile(battleIniFileInfo.FullName);

            List<string> battleKeys = battleIni.GetSectionKeys("Battles");

            if (battleKeys == null)
                return false; // File exists but [Battles] doesn't

            for (int i = 0; i < battleKeys.Count; i++)
            {
                string battleEntry = battleKeys[i];
                string battleSection = battleIni.GetStringValue("Battles", battleEntry, "NOT FOUND");

                if (!battleIni.SectionExists(battleSection))
                    continue;

                var mission = new Mission(battleIni, battleSection, i);

                Missions.Add(mission);

                var item = new XNAListBoxItem();
                item.Text = mission.GUIName;
                if (!mission.Enabled)
                {
                    item.TextColor = UISettings.ActiveSettings.DisabledItemColor;
                }
                else if (string.IsNullOrEmpty(mission.Scenario))
                {
                    item.TextColor = AssetLoader.GetColorFromString(
                        ClientConfiguration.Instance.ListBoxHeaderColor);
                    item.IsHeader = true;
                    item.Selectable = false;
                }
                else
                {
                    item.TextColor = lbCampaignList.DefaultItemColor;
                }

                if (!string.IsNullOrEmpty(mission.IconPath))
                    item.Texture = AssetLoader.LoadTexture(mission.IconPath + "icon.png");

                lbCampaignList.AddItem(item);
            }

            Logger.Log("完成解析 " + path + "。");
            return true;
        }

        public override void Draw(GameTime gameTime)
        {
            base.Draw(gameTime);
        }
    }
}