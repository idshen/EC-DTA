using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using ClientCore;
using ClientCore.CnCNet5;
using ClientGUI;
using ClientUpdater;
using DTAClient.Domain.Multiplayer;
using DTAClient.DXGUI.Multiplayer.CnCNet;
using DTAClient.Online;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Xna.Framework;
using Rampastring.Tools;
using Rampastring.XNAUI;
using Microsoft.Xna.Framework.Graphics;

namespace DTAClient.DXGUI.Generic
{
    public class LoadingScreen : XNAWindow
    {
        private List<Texture2D> backgroundFrames; // 用于存储动态背景帧
        private int currentFrame;
        private float frameTime;
        private bool animationComplete; // 标记动画是否完成

        public LoadingScreen(
            CnCNetManager cncnetManager,
            WindowManager windowManager,
            IServiceProvider serviceProvider,
            MapLoader mapLoader
        ) : base(windowManager)
        {
            this.cncnetManager = cncnetManager;
            this.serviceProvider = serviceProvider;
            this.mapLoader = mapLoader;
        }

        private MapLoader mapLoader;
        private bool visibleSpriteCursor;
        private Task updaterInitTask;
        private Task mapLoadTask;
        private readonly CnCNetManager cncnetManager;
        private readonly IServiceProvider serviceProvider;

        public override void Initialize()
        {
            ClientRectangle = new Rectangle(0, 0, 800, 600);
            Name = "LoadingScreen";

            LoadDynamicBackgroundFrames(); // 加载动态背景帧

            base.Initialize();
            CenterOnParent();

            updaterInitTask = new Task(InitUpdater);
            updaterInitTask.Start();

            mapLoadTask = mapLoader.LoadMapsAsync();

            if (Cursor.Visible)
            {
                Cursor.Visible = false;
                visibleSpriteCursor = true;
            }
        }

        private void LoadDynamicBackgroundFrames()
        {
            backgroundFrames = new List<Texture2D>();
            int frameIndex = 0;

            string customThemePath = Path.Combine(ProgramConstants.GamePath, "Resources", UserINISettings.Instance.ThemeFolderPath, "载入动画");

            while (File.Exists(Path.Combine(customThemePath, $"背景{frameIndex}.png")))
            {
                string filePath = Path.Combine(customThemePath, $"背景{frameIndex}.png");
                var texture = AssetLoader.LoadTexture(filePath);

                if (texture != null)
                {
                    backgroundFrames.Add(texture);
                }

                frameIndex++;
            }

            if (backgroundFrames.Count > 0)
            {
                currentFrame = 0;
                frameTime = 0;
                BackgroundTexture = backgroundFrames[currentFrame]; // 设置初始背景
            }
            else
            {
                animationComplete = true; // 没有背景帧时标记动画完成
            }
        }

        private void InitUpdater()
        {
            Updater.OnLocalFileVersionsChecked += LogGameClientVersion;
            Updater.CheckLocalFileVersions();
        }

        private void LogGameClientVersion()
        {
            // Logger.Log($"游戏客户端版本: {ClientConfiguration.Instance.LocalGame} {Updater.GameVersion}");
            Updater.OnLocalFileVersionsChecked -= LogGameClientVersion;
        }

        private void Finish()
        {
            if (animationComplete) // 动画完成时再移除加载屏幕
            {
                ProgramConstants.GAME_VERSION = ClientConfiguration.Instance.ModMode ? "N/A" : Updater.GameVersion;

                MainMenu mainMenu = serviceProvider.GetService<MainMenu>();
                WindowManager.AddAndInitializeControl(mainMenu);
                mainMenu.PostInit();

                if (UserINISettings.Instance.AutomaticCnCNetLogin &&
                    NameValidator.IsNameValid(ProgramConstants.PLAYERNAME) == null)
                {
                    cncnetManager.Connect();
                }

                if (!UserINISettings.Instance.PrivacyPolicyAccepted)
                {
                    WindowManager.AddAndInitializeControl(new PrivacyNotification(WindowManager));
                }

                WindowManager.RemoveControl(this);
                Cursor.Visible = visibleSpriteCursor;
            }
        }

        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime);

            // 更新动态背景帧
            if (backgroundFrames != null && backgroundFrames.Count > 0)
            {
                frameTime += (float)gameTime.ElapsedGameTime.TotalSeconds;

                if (frameTime >= 0.1f) // 每0.1秒切换帧
                {
                    currentFrame = (currentFrame + 1) % backgroundFrames.Count;
                    BackgroundTexture = backgroundFrames[currentFrame];

                    if (currentFrame == backgroundFrames.Count - 1) // 检查是否是最后一帧
                    {
                        animationComplete = true; // 设置动画完成标记
                    }
                    frameTime = 0;
                }
            }

            if (updaterInitTask == null || updaterInitTask.Status == TaskStatus.RanToCompletion)
            {
                if (mapLoadTask.Status == TaskStatus.RanToCompletion)
                    Finish();
            }
            else if (backgroundFrames.Count == 0) // 如果没有背景帧，直接进入主菜单
            {
                Finish();
            }
        }

        public override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.Black); // 清空背景

            if (backgroundFrames != null && backgroundFrames.Count > 0) // 仅在存在背景帧时绘制
            {
                using (var spriteBatch = new SpriteBatch(GraphicsDevice))
                {
                    spriteBatch.Begin();

                    // 绘制背景图
                    if (BackgroundTexture != null)
                    {
                        spriteBatch.Draw(BackgroundTexture, ClientRectangle, Color.White);
                    }

                    spriteBatch.End();
                }
            }

            base.Draw(gameTime);
        }
    }
}