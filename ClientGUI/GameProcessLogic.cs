using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using ClientCore;
using Rampastring.Tools;
using ClientCore.INIProcessing;
using System.Threading;
using Rampastring.XNAUI;

namespace ClientGUI
{
    /// <summary>
    /// A static class used for controlling the launching and exiting of the game executable.
    /// </summary>
    public static class GameProcessLogic
    {
        public static event Action GameProcessStarted;

        public static event Action GameProcessStarting;

        public static event Action GameProcessExited;

        public static bool UseQres { get; set; }
        public static bool SingleCoreAffinity { get; set; }

        /// <summary>
        /// Starts the main game process.
        /// </summary>
        public static void StartGameProcess(WindowManager windowManager)
        {
            Logger.Log("即将启动主游戏可执行文件。");

            // In the relatively unlikely event that INI preprocessing is still going on, just wait until it's done.
            // TODO ideally this should be handled in the UI so the client doesn't appear just frozen for the user.
            int waitTimes = 0;
            while (PreprocessorBackgroundTask.Instance.IsRunning)
            {
                Thread.Sleep(1000);
                waitTimes++;
                if (waitTimes > 10)
                {
                    XNAMessageBox.Show(windowManager, "INI preprocessing not complete", "INI preprocessing not complete. Please try " +
                        "launching the game again. If the problem persists, " +
                        "contact the game or mod authors for support.");
                    return;
                }
            }

            OSVersion osVersion = ClientConfiguration.Instance.GetOperatingSystemVersion();

            string gameExecutableName;
            string additionalExecutableName = string.Empty;

            if (osVersion == OSVersion.UNIX)
                gameExecutableName = ClientConfiguration.Instance.UnixGameExecutableName;
            else
            {
                string launcherExecutableName = ClientConfiguration.Instance.GameLauncherExecutableName;
                if (string.IsNullOrEmpty(launcherExecutableName))
                    gameExecutableName = ClientConfiguration.Instance.GetGameExecutableName();
                else
                {
                    gameExecutableName = launcherExecutableName;
                    additionalExecutableName = "\"" + ClientConfiguration.Instance.GetGameExecutableName() + "\" ";
                }
            }

            string extraCommandLine = ClientConfiguration.Instance.ExtraExeCommandLineParameters;

            SafePath.DeleteFileIfExists(ProgramConstants.GamePath, "DTA.LOG");
            SafePath.DeleteFileIfExists(ProgramConstants.GamePath, "TI.LOG");
            SafePath.DeleteFileIfExists(ProgramConstants.GamePath, "TS.LOG");

            GameProcessStarting?.Invoke();

            if (UserINISettings.Instance.WindowedMode && UseQres && RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                Logger.Log("窗口模式已启用 - 正在使用 QRes。");
                Process QResProcess = new Process();
                QResProcess.StartInfo.FileName = ProgramConstants.QRES_EXECUTABLE;

                if (!string.IsNullOrEmpty(extraCommandLine))
                    QResProcess.StartInfo.Arguments = "c=16 /R " + "\"" + SafePath.CombineFilePath(ProgramConstants.GamePath, gameExecutableName) + "\" " + additionalExecutableName + "-SPAWN " + extraCommandLine;
                else
                    QResProcess.StartInfo.Arguments = "c=16 /R " + "\"" + SafePath.CombineFilePath(ProgramConstants.GamePath, gameExecutableName) + "\" " + additionalExecutableName + "-SPAWN";
                QResProcess.EnableRaisingEvents = true;
                QResProcess.Exited += new EventHandler(Process_Exited);
                Logger.Log("启动可执行文件: " + QResProcess.StartInfo.FileName);
                Logger.Log("启动参数: " + QResProcess.StartInfo.Arguments);
                try
                {
                    QResProcess.Start();
                }
                catch (Exception ex)
                {
                    Logger.Log("启动 QRes 时出错: " + ex.Message);
                    XNAMessageBox.Show(windowManager, "Error launching game", "Error launching " + ProgramConstants.QRES_EXECUTABLE + ". Please check that your anti-virus isn't blocking the CnCNet Client. " +
                        "You can also try running the client as an administrator." + Environment.NewLine + Environment.NewLine + "You are unable to participate in this match." +
                        Environment.NewLine + Environment.NewLine + "Returned error: " + ex.Message);
                    Process_Exited(QResProcess, EventArgs.Empty);
                    return;
                }

                if (Environment.ProcessorCount > 1 && SingleCoreAffinity)
                    QResProcess.ProcessorAffinity = (IntPtr)2;
            }
            else
            {
                string arguments;

                if (!string.IsNullOrWhiteSpace(extraCommandLine))
                    arguments = " " + additionalExecutableName + "-SPAWN " + extraCommandLine;
                else
                    arguments = additionalExecutableName + "-SPAWN";

                FileInfo gameFileInfo = SafePath.GetFile(ProgramConstants.GamePath, gameExecutableName);

                var gameProcess = new Process();
                gameProcess.StartInfo.FileName = gameFileInfo.FullName;
                gameProcess.StartInfo.Arguments = arguments;

                gameProcess.EnableRaisingEvents = true;
                gameProcess.Exited += Process_Exited;

                Logger.Log("启动可执行文件: " + gameProcess.StartInfo.FileName);
                Logger.Log("启动参数: " + gameProcess.StartInfo.Arguments);

                try
                {
                    gameProcess.Start();
                    Logger.Log("游戏进程逻辑: 进程已启动。");
                }
                catch (Exception ex)
                {
                    Logger.Log("启动 " + gameFileInfo.Name + " 时出错: " + ex.Message);
                    XNAMessageBox.Show(windowManager, "游戏启动错误", "启动 " + gameFileInfo.Name + " 游戏时出错。请检查您的杀毒软件是否阻止了CnCNet客户端。您也可以尝试以管理员身份运行客户端。" +
                                        Environment.NewLine + Environment.NewLine + "您无法参与本场比赛。" +
                                        Environment.NewLine + Environment.NewLine + "返回的错误信息：" + ex.Message);
                    Process_Exited(gameProcess, EventArgs.Empty);
                    return;
                }

                if ((RuntimeInformation.IsOSPlatform(OSPlatform.Windows) || RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                    && Environment.ProcessorCount > 1 && SingleCoreAffinity)
                {
                    gameProcess.ProcessorAffinity = 2;
                }
            }

            GameProcessStarted?.Invoke();

            Logger.Log("等待 qres.dat 或 " + gameExecutableName + " 退出。");
        }

        static void Process_Exited(object sender, EventArgs e)
        {
            Logger.Log("游戏进程逻辑: 进程已退出。");
            Process proc = (Process)sender;
            proc.Exited -= Process_Exited;
            proc.Dispose();
            GameProcessExited?.Invoke();
        }
    }
}
