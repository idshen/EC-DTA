using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ClientCore;
using ClientGUI;
using DTAClient.Domain;
using ClientCore.Extensions;
using Microsoft.Xna.Framework;
using Rampastring.Tools;
using Rampastring.XNAUI;
using Rampastring.XNAUI.XNAControls;
using Microsoft.Xna.Framework.Input;

namespace DTAClient.DXGUI.Generic
{
    /// <summary>
    /// A window for loading saved singleplayer games.
    /// </summary>
    public class GameLoadingWindow : XNAWindow
    {
        private const string SAVED_GAMES_DIRECTORY = "Saved Games";

        public GameLoadingWindow(WindowManager windowManager, DiscordHandler discordHandler) : base(windowManager)
        {
            this.discordHandler = discordHandler;
        }

        private DiscordHandler discordHandler;

        private XNAMultiColumnListBox lbSaveGameList;
        private XNAClientButton btnLaunch;
        private XNAClientButton btnDelete;
        private XNAClientButton btnCancel;

        private List<SavedGame> savedGames = new List<SavedGame>();
        public event EventHandler WindowClosed;
        public override void Initialize()
        {
            Name = "GameLoadingWindow";
            BackgroundTexture = AssetLoader.LoadTexture("loadmissionbg.png");

            ClientRectangle = new Rectangle(0, 0, 600, 380);
            CenterOnParent();

            lbSaveGameList = new XNAMultiColumnListBox(WindowManager);
            lbSaveGameList.Name = nameof(lbSaveGameList);
            lbSaveGameList.ClientRectangle = new Rectangle(13, 13, 574, 317);
            lbSaveGameList.AddColumn("SAVED GAME NAME".L10N("Client:Main:SavedGameNameColumnHeader"), 400);
            lbSaveGameList.AddColumn("DATE / TIME".L10N("Client:Main:SavedGameDateTimeColumnHeader"), 174);
            lbSaveGameList.BackgroundTexture = AssetLoader.CreateTexture(new Color(0, 0, 0, 128), 1, 1);
            lbSaveGameList.PanelBackgroundDrawMode = PanelBackgroundImageDrawMode.STRETCHED;
            lbSaveGameList.SelectedIndexChanged += ListBox_SelectedIndexChanged;
            lbSaveGameList.AllowKeyboardInput = true;

            btnLaunch = new XNAClientButton(WindowManager);
            btnLaunch.Name = nameof(btnLaunch);
            btnLaunch.ClientRectangle = new Rectangle(125, 345, 110, 23);
            btnLaunch.Text = "Load".L10N("Client:Main:ButtonLoad");
            btnLaunch.AllowClick = false;
            btnLaunch.LeftClick += BtnLaunch_LeftClick;
            btnLaunch.HotKey = Keys.None;               // 默认快捷键设置为 None
            LoadHotkeyForButton("13加载", btnLaunch);   // 从 INI 文件加载 btnCancel 的快捷键

            btnDelete = new XNAClientButton(WindowManager);
            btnDelete.Name = nameof(btnDelete);
            btnDelete.ClientRectangle = new Rectangle(btnLaunch.Right + 10, btnLaunch.Y, 110, 23);
            btnDelete.Text = "Delete".L10N("Client:Main:ButtonDelete");
            btnDelete.AllowClick = false;
            btnDelete.LeftClick += BtnDelete_LeftClick;
            btnDelete.HotKey = Keys.None;               // 默认快捷键设置为 None
            LoadHotkeyForButton("14删除", btnDelete);   // 从 INI 文件加载 btnCancel 的快捷键

            btnCancel = new XNAClientButton(WindowManager);
            btnCancel.Name = nameof(btnCancel);
            btnCancel.ClientRectangle = new Rectangle(btnDelete.Right + 10, btnLaunch.Y, 110, 23);
            btnCancel.Text = "Cancel".L10N("Client:Main:ButtonCancel");
            btnCancel.LeftClick += BtnCancel_LeftClick;
            btnCancel.HotKey = Keys.None;               // 默认快捷键设置为 None
            LoadHotkeyForButton("15返回", btnCancel);   // 从 INI 文件加载 btnCancel 的快捷键

            AddChild(lbSaveGameList);
            AddChild(btnLaunch);
            AddChild(btnDelete);
            AddChild(btnCancel);

            base.Initialize();

            ListSaves();
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

        private void ListBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (lbSaveGameList.SelectedIndex == -1)
            {
                btnLaunch.AllowClick = false;
                btnDelete.AllowClick = false;
            }
            else
            {
                btnLaunch.AllowClick = true;
                btnDelete.AllowClick = true;
            }
        }

        private void BtnCancel_LeftClick(object sender, EventArgs e)
        {
            Enabled = false;
        }

        private void BtnLaunch_LeftClick(object sender, EventArgs e)
        {
            SavedGame sg = savedGames[lbSaveGameList.SelectedIndex];
            Logger.Log("正在加载保存游戏 " + sg.FileName);

            FileInfo spawnerSettingsFile = SafePath.GetFile(ProgramConstants.GamePath, ProgramConstants.SPAWNER_SETTINGS);

            if (spawnerSettingsFile.Exists)
                spawnerSettingsFile.Delete();

            using StreamWriter spawnStreamWriter = new StreamWriter(spawnerSettingsFile.FullName);
            spawnStreamWriter.WriteLine("; generated by DTA Client");
            spawnStreamWriter.WriteLine("[Settings]");
            spawnStreamWriter.WriteLine("Scenario=spawnmap.ini");
            spawnStreamWriter.WriteLine("SaveGameName=" + sg.FileName);
            spawnStreamWriter.WriteLine("LoadSaveGame=Yes");
            spawnStreamWriter.WriteLine("SidebarHack=" + ClientConfiguration.Instance.SidebarHack);
            spawnStreamWriter.WriteLine("CustomLoadScreen=" + LoadingScreenController.GetLoadScreenName("g"));
            spawnStreamWriter.WriteLine("Firestorm=No");
            spawnStreamWriter.WriteLine("GameSpeed=" + UserINISettings.Instance.GameSpeed);
            spawnStreamWriter.WriteLine();

            FileInfo spawnMapIniFile = SafePath.GetFile(ProgramConstants.GamePath, "spawnmap.ini");

            if (spawnMapIniFile.Exists)
                spawnMapIniFile.Delete();

            using StreamWriter spawnMapStreamWriter = new StreamWriter(spawnMapIniFile.FullName);
            spawnMapStreamWriter.WriteLine("[Map]");
            spawnMapStreamWriter.WriteLine("Size=0,0,50,50");
            spawnMapStreamWriter.WriteLine("LocalSize=0,0,50,50");
            spawnMapStreamWriter.WriteLine();

            discordHandler.UpdatePresence(sg.GUIName, true);

            Enabled = false;
            GameProcessLogic.GameProcessExited += GameProcessExited_Callback;

            GameProcessLogic.StartGameProcess(WindowManager);
        }

        private void BtnDelete_LeftClick(object sender, EventArgs e)
        {
            SavedGame sg = savedGames[lbSaveGameList.SelectedIndex];
            var msgBox = new XNAMessageBox(WindowManager, "Delete Confirmation".L10N("Client:Main:DeleteConfirmationTitle"),
                string.Format(("The following saved game will be deleted permanently:\n\n" +
                    "Filename: {0}\n" +
                    "Saved game name: {1}\n" +
                    "Date and time: {2}\n\n" +
                    "Are you sure you want to proceed?").L10N("Client:Main:DeleteConfirmationText"),
                    sg.FileName, Renderer.GetSafeString(sg.GUIName, lbSaveGameList.FontIndex), sg.LastModified.ToString()),
                XNAMessageBoxButtons.YesNo);
            msgBox.Show();
            msgBox.YesClickedAction = DeleteMsgBox_YesClicked;
        }

        private void DeleteMsgBox_YesClicked(XNAMessageBox obj)
        {
            SavedGame sg = savedGames[lbSaveGameList.SelectedIndex];
            Logger.Log("正在删除保存游戏 " + sg.FileName);
            SafePath.DeleteFileIfExists(ProgramConstants.GamePath, SAVED_GAMES_DIRECTORY, sg.FileName);
            ListSaves();
        }

        private void GameProcessExited_Callback()
        {
            WindowManager.AddCallback(new Action(GameProcessExited), null);
        }

        protected virtual void GameProcessExited()
        {
            GameProcessLogic.GameProcessExited -= GameProcessExited_Callback;
            discordHandler.UpdatePresence();
        }

        public void ListSaves()
        {
            savedGames.Clear();
            lbSaveGameList.ClearItems();
            lbSaveGameList.SelectedIndex = -1;

            DirectoryInfo savedGamesDirectoryInfo = SafePath.GetDirectory(ProgramConstants.GamePath, SAVED_GAMES_DIRECTORY);

            if (!savedGamesDirectoryInfo.Exists)
            {
                Logger.Log("保存游戏目录未找到！");
                return;
            }

            IEnumerable<FileInfo> files = savedGamesDirectoryInfo.EnumerateFiles("*.SAV", SearchOption.TopDirectoryOnly);

            foreach (FileInfo file in files)
            {
                ParseSaveGame(file.FullName);
            }

            savedGames = savedGames.OrderBy(sg => sg.LastModified.Ticks).ToList();
            savedGames.Reverse();

            foreach (SavedGame sg in savedGames)
            {
                string[] item = new string[] {
                    Renderer.GetSafeString(sg.GUIName, lbSaveGameList.FontIndex),
                    sg.LastModified.ToString() };
                lbSaveGameList.AddItem(item, true);
            }
        }

        private void ParseSaveGame(string fileName)
        {
            string shortName = Path.GetFileName(fileName);

            SavedGame sg = new SavedGame(shortName);
            if (sg.ParseInfo())
                savedGames.Add(sg);
        }
    }
}