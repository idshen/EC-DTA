using ClientGUI;
using DTAClient.Domain;
using DTAClient.Domain.Multiplayer;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Rampastring.XNAUI;
using Rampastring.XNAUI.XNAControls;
using System;

namespace DTAClient.DXGUI.Generic
{
    /// <summary>
    /// TODO Replace this class with DarkeningPanels.
    /// Handles transitions between the main menu and its sub-menus.
    /// </summary>
    public class MainMenuDarkeningPanel : XNAPanel
    {
        public MainMenuDarkeningPanel(WindowManager windowManager, DiscordHandler discordHandler, MapLoader mapLoader)
            : base(windowManager)
        {
            this.discordHandler = discordHandler;
            this.mapLoader = mapLoader;
            DrawBorders = false;
            DrawMode = ControlDrawMode.UNIQUE_RENDER_TARGET;
        }

        private DiscordHandler discordHandler;
        private MapLoader mapLoader;

        public CampaignSelector CampaignSelector;
        public GameLoadingWindow GameLoadingWindow;
        public StatisticsWindow StatisticsWindow;
        public UpdateQueryWindow UpdateQueryWindow;
        public ManualUpdateQueryWindow ManualUpdateQueryWindow;
        public UpdateWindow UpdateWindow;
        public ExtrasWindow ExtrasWindow;
        public CreditsWindow CreditsWindow;

        public override void Initialize()
        {
            base.Initialize();

            Name = "DarkeningPanel";
            BorderColor = UISettings.ActiveSettings.PanelBorderColor;
            BackgroundTexture = AssetLoader.CreateTexture(new Color(0, 0, 0, 128), 1, 1);
            PanelBackgroundDrawMode = PanelBackgroundImageDrawMode.STRETCHED;
            Alpha = 1.0f;

            CampaignSelector = new CampaignSelector(WindowManager, discordHandler);
            AddChild(CampaignSelector);

            GameLoadingWindow = new GameLoadingWindow(WindowManager, discordHandler);
            AddChild(GameLoadingWindow);

            StatisticsWindow = new StatisticsWindow(WindowManager, mapLoader);
            AddChild(StatisticsWindow);

            UpdateQueryWindow = new UpdateQueryWindow(WindowManager);
            AddChild(UpdateQueryWindow);

            ManualUpdateQueryWindow = new ManualUpdateQueryWindow(WindowManager);
            AddChild(ManualUpdateQueryWindow);

            UpdateWindow = new UpdateWindow(WindowManager);
            AddChild(UpdateWindow);

            ExtrasWindow = new ExtrasWindow(WindowManager);
            AddChild(ExtrasWindow);

            CreditsWindow = new CreditsWindow(WindowManager);
            AddChild(CreditsWindow); // 将致谢窗口添加为子控件

            foreach (XNAControl child in Children)
            {
                child.Visible = false;
                child.Enabled = false;
                child.EnabledChanged += Child_EnabledChanged;
            }
        }

        private void Child_EnabledChanged(object sender, EventArgs e)
        {
            XNAWindow child = (XNAWindow)sender;
            if (!child.Enabled)
                Hide();
        }

        public void Show(XNAControl control)
        {
            // 主菜单打开内部窗口时禁用主菜单的快捷键
            ((MainMenu)Parent).DisableButtonHotkeys();

            foreach (XNAControl child in Children)
            {
                child.Enabled = false;
                child.Visible = false;
            }

            Enabled = true;
            Visible = true;

            AlphaRate = DarkeningPanel.ALPHA_RATE;

            if (control != null)
            {
                control.Enabled = true;
                control.Visible = true;
                control.IgnoreInputOnFrame = true;

                // 获取窗口的中心点
                var centerX = WindowManager.GraphicsDevice.Viewport.Width / 2;
                var centerY = WindowManager.GraphicsDevice.Viewport.Height / 2;

                // 设置鼠标位置为窗口中心
                Mouse.SetPosition(centerX, centerY);
            }
        }

        public void Hide()
        {
            // 主菜单关闭内部窗口时恢复主菜单的快捷键
            ((MainMenu)Parent).SetButtonHotkeys();

            AlphaRate = -DarkeningPanel.ALPHA_RATE;

            foreach (XNAControl child in Children)
            {
                child.Enabled = false;
                child.Visible = false;
            }
        }

        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime);

            if (Alpha <= 0f)
            {
                Enabled = false;
                Visible = false;

                foreach (XNAControl child in Children)
                {
                    child.Visible = false;
                }
            }
        }

        public override void Draw(GameTime gameTime)
        {
            DrawTexture(BackgroundTexture, Point.Zero, Color.White);
            base.Draw(gameTime);
        }
    }
}