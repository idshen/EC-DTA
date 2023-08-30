using System;
using System.IO;
using System.Text.RegularExpressions;
using ClientCore.Extensions;
using ClientGUI;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Rampastring.Tools;
using Rampastring.XNAUI;
using Rampastring.XNAUI.XNAControls;

namespace DTAClient.DXGUI.Generic
{
    public class CreditsWindow : XNAWindow
    {
        private const string CREDITS_FILE_PATH = "Resources/DIY/致谢.ini";

        private XNAMultiColumnListBox lbCreditsList;
        private XNAClientButton btnClose;

        public CreditsWindow(WindowManager windowManager) : base(windowManager)
        {
        }

        public override void Initialize()
        {
            Name = "CreditsWindow";
            BackgroundTexture = AssetLoader.LoadTexture("loadmissionbg.png");
            ClientRectangle = new Rectangle(0, 0, 600, 380);
            CenterOnParent();

            lbCreditsList = new XNAMultiColumnListBox(WindowManager);
            lbCreditsList.Name = nameof(lbCreditsList);
            lbCreditsList.ClientRectangle = new Rectangle(13, 13, 574, 317);
            lbCreditsList.AddColumn("List of makers".L10N("Client:Main:Listofmakers"), 574); // 列宽度
            lbCreditsList.BackgroundTexture = AssetLoader.CreateTexture(new Color(0, 0, 0, 128), 1, 1);
            lbCreditsList.PanelBackgroundDrawMode = PanelBackgroundImageDrawMode.STRETCHED;
            lbCreditsList.AllowKeyboardInput = true;

            btnClose = new XNAClientButton(WindowManager);
            btnClose.Name = nameof(btnClose);
            btnClose.ClientRectangle = new Rectangle(250, 345, 110, 23);
            btnClose.Text = "OK".L10N("Client:ClientGUI:ButtonOK");
            btnClose.LeftClick += BtnClose_LeftClick;
            btnClose.HotKey = Keys.Enter;

            AddChild(lbCreditsList);
            AddChild(btnClose);

            base.Initialize();

            LoadCredits();
        }

        private void LoadCredits()
        {
            if (!File.Exists(CREDITS_FILE_PATH))
            {
                Logger.Log("致谢文件未找到！");
                return;
            }

            // 读取文件内容，并按行分割，同时保留换行符
            string[] lines = File.ReadAllText(CREDITS_FILE_PATH)
                                 .Replace("\r\n", "\n") // 统一换行符
                                 .Split('\n');

            foreach (string line in lines)
            {
                // 如果是空行，也添加到列表中，保持格式
                if (string.IsNullOrWhiteSpace(line))
                {
                    lbCreditsList.AddItem(new[] { "" }, true);
                }
                else
                {
                    // 对每一行内容应用换行逻辑
                    AddWrappedText(line, 360); // 最大宽度
                }
            }
        }

        /// <summary>
        /// 根据给定的宽度将文本手动换行，并逐行添加到列表框中
        /// 英文行每个字符宽度较窄，中文行较宽
        /// </summary>
        private void AddWrappedText(string text, int maxWidth)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return;
            }

            // 判断是否为中文行
            bool isChinese = Regex.IsMatch(text, @"[\u4e00-\u9fa5]");

            // 假设中文字符宽度为20像素，英文字符宽度为10像素
            int approxCharWidth = isChinese ? 10 : 4;
            int maxCharsPerLine = maxWidth / approxCharWidth;

            if (text.Length <= maxCharsPerLine)
            {
                lbCreditsList.AddItem(new[] { text }, true);
                return;
            }

            for (int i = 0; i < text.Length; i += maxCharsPerLine)
            {
                if (i + maxCharsPerLine > text.Length)
                    lbCreditsList.AddItem(new[] { text.Substring(i) }, true);
                else
                    lbCreditsList.AddItem(new[] { text.Substring(i, maxCharsPerLine) }, true);
            }
        }

        private void BtnClose_LeftClick(object sender, EventArgs e)
        {
            Enabled = false;
        }
    }
}
