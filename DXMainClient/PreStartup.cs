﻿using System;
#if WINFORMS
using System.Windows.Forms;
#endif
using System.Diagnostics;
using System.IO;
using DTAClient.Domain;
using Rampastring.Tools;
using ClientCore;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Collections.Generic;
using ClientCore.Extensions;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using ClientCore.I18N;
using System.Globalization;
using System.Transactions;

namespace DTAClient
{
    /// <summary>
    /// Contains client startup parameters.
    /// </summary>
    struct StartupParams
    {
        public StartupParams(bool noAudio, bool multipleInstanceMode,
            List<string> unknownParams)
        {
            NoAudio = noAudio;
            MultipleInstanceMode = multipleInstanceMode;
            UnknownStartupParams = unknownParams;
        }

        public bool NoAudio { get; }
        public bool MultipleInstanceMode { get; }
        public List<string> UnknownStartupParams { get; }
    }

    static class PreStartup
    {
        /// <summary>
        /// Initializes various basic systems like the client's logger, 
        /// constants, and the general exception handler.
        /// Reads the user's settings from an INI file, 
        /// checks for necessary permissions and starts the client if
        /// everything goes as it should.
        /// </summary>
        /// <param name="parameters">The client's startup parameters.</param>
        public static void Initialize(StartupParams parameters)
        {
            Translation.InitialUICulture = CultureInfo.CurrentUICulture;
            CultureInfo.CurrentUICulture = new CultureInfo(ProgramConstants.HARDCODED_LOCALE_CODE);

#if WINFORMS
            Application.SetUnhandledExceptionMode(UnhandledExceptionMode.ThrowException);
            Application.ThreadException += (sender, args) => HandleException(sender, args.Exception);
#endif
            AppDomain.CurrentDomain.UnhandledException += (sender, args) => HandleException(sender, (Exception)args.ExceptionObject);

            DirectoryInfo gameDirectory = SafePath.GetDirectory(ProgramConstants.GamePath);

            Environment.CurrentDirectory = gameDirectory.FullName;

            DirectoryInfo clientUserFilesDirectory = SafePath.GetDirectory(ProgramConstants.ClientUserFilesPath);
            FileInfo clientLogFile = SafePath.GetFile(clientUserFilesDirectory.FullName, "client.log");
            ProgramConstants.LogFileName = clientLogFile.FullName;

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                CheckPermissions();

            if (clientLogFile.Exists)
                File.Move(clientLogFile.FullName, SafePath.GetFile(clientUserFilesDirectory.FullName, "client_previous.log").FullName, true);

            Logger.Initialize(clientUserFilesDirectory.FullName, clientLogFile.Name);
            Logger.WriteLogFile = true;

            if (!clientUserFilesDirectory.Exists)
                clientUserFilesDirectory.Create();

            MainClientConstants.Initialize();

            Logger.Log("***" + MainClientConstants.GAME_NAME_LONG + " 客户端日志文件***");
            Logger.Log("客户端版本: " + Assembly.GetAssembly(typeof(PreStartup)).GetName().Version);

            // Log information about given startup params
            if (parameters.NoAudio)
            {
                Logger.Log("启动参数: 无音频");

                // TODO fix
                throw new NotImplementedException("-NOAUDIO 目前尚未实现，请取消后再运行客户端.".L10N("Client:Main:NoAudio"));
            }

            if (parameters.MultipleInstanceMode)
                Logger.Log("启动参数: 允许多个客户端实例");

            parameters.UnknownStartupParams.ForEach(p => Logger.Log("未知的启动参数: " + p));

            Logger.Log("正在加载设置.");

            UserINISettings.Initialize(ClientConfiguration.Instance.SettingsIniName);

            // Try to load translation
            try
            {
                Translation translation;
                FileInfo translationThemeFile = SafePath.GetFile(UserINISettings.Instance.TranslationThemeFolderPath, ClientConfiguration.Instance.TranslationIniName);
                FileInfo translationFile = SafePath.GetFile(UserINISettings.Instance.TranslationFolderPath, ClientConfiguration.Instance.TranslationIniName);

                if (translationFile.Exists)
                {
                    Logger.Log($"正在加载通用翻译文件，路径为 {translationFile.FullName}");
                    translation = new Translation(translationFile.FullName, UserINISettings.Instance.Translation);
                    if (translationThemeFile.Exists)
                    {
                        Logger.Log($"正在加载主题特定的翻译文件，路径为 {translationThemeFile.FullName}");
                        translation.AppendValuesFromIniFile(translationThemeFile.FullName);
                    }

                    Translation.Instance = translation;
                }
                else
                {
                    Logger.Log($"加载翻译文件失败. " +
                        $" {translationThemeFile.FullName} 与 {translationFile.FullName} 均不存在.");
                }

                Logger.Log("已加载翻译: " + Translation.Instance.Name);
            }
            catch (Exception ex)
            {
                Logger.Log("加载翻译文件失败. " + ex.Message);
                Translation.Instance = new Translation(UserINISettings.Instance.Translation);
            }

            CultureInfo.CurrentUICulture = Translation.Instance.Culture;

            try
            {
                if (UserINISettings.Instance.GenerateTranslationStub)
                {
                    string stubPath = SafePath.CombineFilePath(
                        ProgramConstants.ClientUserFilesPath, ClientConfiguration.Instance.TranslationIniName);

                    AppDomain.CurrentDomain.ProcessExit += (sender, e) =>
                    {
                        Logger.Log("正在写入翻译存根文件.");
                        var ini = Translation.Instance.DumpIni(UserINISettings.Instance.GenerateOnlyNewValuesInTranslationStub);
                        ini.WriteIniFile(stubPath);
                    };

                    Logger.Log("翻译存根生成功能已启用.退出客户端时将写入存根文件.");

                    // Lookup all compile-time available strings
                    ClientCore.Generated.TranslationNotifier.Register();
                    ClientGUI.Generated.TranslationNotifier.Register();
                    DTAConfig.Generated.TranslationNotifier.Register();
                    DTAClient.Generated.TranslationNotifier.Register();
                }
            }
            catch (Exception ex)
            {
                Logger.Log("生成翻译存根失败: " + ex.Message);
            }

            // Delete obsolete files from old target project versions

            gameDirectory.EnumerateFiles("mainclient.log").SingleOrDefault()?.Delete();
            gameDirectory.EnumerateFiles("aunchupdt.dat").SingleOrDefault()?.Delete();

            try
            {
                gameDirectory.EnumerateFiles("wsock32.dll").SingleOrDefault()?.Delete();
            }
            catch (Exception ex)
            {
                LogException(ex);

                string error = "Deleting wsock32.dll failed! Please close any " +
                    "applications that could be using the file, and then start the client again."
                    + Environment.NewLine + Environment.NewLine +
                    "Message: " + ex.Message;

                ProgramConstants.DisplayErrorAction(null, error, true);
            }

#if WINFORMS
            ApplicationConfiguration.Initialize();
#endif

            new Startup().Execute();
        }

        public static void LogException(Exception ex, bool innerException = false)
        {
            if (!innerException)
                Logger.Log("KABOOOOOOM!!! 信息:");
            else
                Logger.Log("内部异常信息:");

            Logger.Log("类型: " + ex.GetType());
            Logger.Log("消息: " + ex.Message);
            Logger.Log("来源: " + ex.Source);
            Logger.Log("目标方法名称: " + ex.TargetSite.Name);
            Logger.Log("堆栈跟踪: " + ex.StackTrace);

            if (ex.InnerException is not null)
                LogException(ex.InnerException, true);
        }

        static void HandleException(object sender, Exception ex)
        {
            LogException(ex);

            string errorLogPath = SafePath.CombineFilePath(ProgramConstants.ClientUserFilesPath, "ClientCrashLogs", FormattableString.Invariant($"ClientCrashLog{DateTime.Now.ToString("_yyyy_MM_dd_HH_mm")}.txt"));
            bool crashLogCopied = false;

            try
            {
                DirectoryInfo crashLogsDirectoryInfo = SafePath.GetDirectory(ProgramConstants.ClientUserFilesPath, "ClientCrashLogs");

                if (!crashLogsDirectoryInfo.Exists)
                    crashLogsDirectoryInfo.Create();

                File.Copy(SafePath.CombineFilePath(ProgramConstants.ClientUserFilesPath, "client.log"), errorLogPath, true);
                crashLogCopied = true;
            }
            catch { }

            string error = string.Format("{0} has crashed. Error message:".L10N("Client:Main:FatalErrorText1") + Environment.NewLine + Environment.NewLine +
                ex.Message + Environment.NewLine + Environment.NewLine + (crashLogCopied ?
                "A crash log has been saved to the following file:".L10N("Client:Main:FatalErrorText2") + " " + Environment.NewLine + Environment.NewLine +
                errorLogPath + Environment.NewLine + Environment.NewLine : "") +
                (crashLogCopied ? "If the issue is repeatable, contact the {1} staff at {2} and provide the crash log file.".L10N("Client:Main:FatalErrorText3") :
                "If the issue is repeatable, contact the {1} staff at {2}.".L10N("Client:Main:FatalErrorText4")),
                MainClientConstants.GAME_NAME_LONG,
                MainClientConstants.GAME_NAME_SHORT,
                MainClientConstants.SUPPORT_URL_SHORT);

            ProgramConstants.DisplayErrorAction("KABOOOOOOOM".L10N("Client:Main:FatalErrorTitle"), error, true);
        }

        [SupportedOSPlatform("windows")]
        private static void CheckPermissions()
        {
            if (UserHasDirectoryAccessRights(ProgramConstants.GamePath, FileSystemRights.Modify))
                return;

            string error = string.Format(("You seem to be running {0} from a write-protected directory.\n\n" +
                "For {1} to function properly when run from a write-protected directory, it needs administrative priveleges.\n\n" +
                "Would you like to restart the client with administrative rights?\n\n" +
                "Please also make sure that your security software isn't blocking {1}.").L10N("Client:Main:AdminRequiredText"), MainClientConstants.GAME_NAME_LONG, MainClientConstants.GAME_NAME_SHORT);

            ProgramConstants.DisplayErrorAction("Administrative privileges required".L10N("Client:Main:AdminRequiredTitle"), error, false);

            using var _ = Process.Start(new ProcessStartInfo
            {
                FileName = "dotnet",
                Arguments = SafePath.CombineFilePath(ProgramConstants.StartupExecutable),
                Verb = "runas",
                CreateNoWindow = true
            });
            Environment.Exit(1);
        }

        /// <summary>
        /// Checks whether the client has specific file system rights to a directory.
        /// See ssds's answer at https://stackoverflow.com/questions/1410127/c-sharp-test-if-user-has-write-access-to-a-folder
        /// </summary>
        /// <param name="path">The path to the directory.</param>
        /// <param name="accessRights">The file system rights.</param>
        [SupportedOSPlatform("windows")]
        private static bool UserHasDirectoryAccessRights(string path, FileSystemRights accessRights)
        {
            var currentUser = WindowsIdentity.GetCurrent();
            var principal = new WindowsPrincipal(currentUser);

            // If the user is not running the client with administrator privileges in Program Files, they need to be prompted to do so.
            if (!principal.IsInRole(WindowsBuiltInRole.Administrator))
            {
                string progfiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
                string progfilesx86 = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);
                if (ProgramConstants.GamePath.Contains(progfiles) || ProgramConstants.GamePath.Contains(progfilesx86))
                    return false;
            }

            var isInRoleWithAccess = false;

            try
            {
                var di = new DirectoryInfo(path);
                var acl = di.GetAccessControl();
                var rules = acl.GetAccessRules(true, true, typeof(NTAccount));

                foreach (AuthorizationRule rule in rules)
                {
                    var fsAccessRule = rule as FileSystemAccessRule;
                    if (fsAccessRule == null)
                        continue;

                    if ((fsAccessRule.FileSystemRights & accessRights) > 0)
                    {
                        var ntAccount = rule.IdentityReference as NTAccount;
                        if (ntAccount == null)
                            continue;

                        if (principal.IsInRole(ntAccount.Value))
                        {
                            if (fsAccessRule.AccessControlType == AccessControlType.Deny)
                                return false;
                            isInRoleWithAccess = true;
                        }
                    }
                }
            }
            catch (UnauthorizedAccessException)
            {
                return false;
            }
            return isInRoleWithAccess;
        }
    }
}