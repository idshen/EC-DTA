using System;
using System.Collections.Generic;
using System.IO;
using Rampastring.Tools;

namespace ClientCore
{
    /// <summary>
    /// A class for handling saved multiplayer games.
    /// </summary>
    public static class SavedGameManager
    {
        private const string SAVED_GAMES_DIRECTORY = "Saved Games";

        private static bool saveRenameInProgress = false;

        public static int GetSaveGameCount()
        {
            string saveGameDirectory = GetSaveGameDirectoryPath();

            if (!AreSavedGamesAvailable())
                return 0;

            for (int i = 0; i < 1000; i++)
            {
                if (!SafePath.GetFile(saveGameDirectory, string.Format("SVGM_{0}.NET", i.ToString("D3"))).Exists)
                {
                    return i;
                }
            }

            return 1000;
        }

        public static List<string> GetSaveGameTimestamps()
        {
            int saveGameCount = GetSaveGameCount();

            List<string> timestamps = new List<string>();

            string saveGameDirectory = GetSaveGameDirectoryPath();

            for (int i = 0; i < saveGameCount; i++)
            {
                FileInfo sgFile = SafePath.GetFile(saveGameDirectory, string.Format("SVGM_{0}.NET", i.ToString("D3")));

                DateTime dt = sgFile.LastWriteTime;

                timestamps.Add(dt.ToString());
            }

            return timestamps;
        }

        public static bool AreSavedGamesAvailable()
        {
            if (Directory.Exists(GetSaveGameDirectoryPath()))
                return true;

            return false;
        }

        private static string GetSaveGameDirectoryPath()
        {
            return SafePath.CombineDirectoryPath(ProgramConstants.GamePath, SAVED_GAMES_DIRECTORY);
        }

        /// <summary>
        /// Initializes saved MP games for a match.
        /// </summary>
        public static bool InitSavedGames()
        {
            bool success = EraseSavedGames();

            if (!success)
                return false;

            try
            {
                Logger.Log("为保存的游戏写入 spawn.ini。");
                SafePath.DeleteFileIfExists(ProgramConstants.GamePath, SAVED_GAMES_DIRECTORY, "spawnSG.ini");
                File.Copy(SafePath.CombineFilePath(ProgramConstants.GamePath, "spawn.ini"), SafePath.CombineFilePath(ProgramConstants.GamePath, SAVED_GAMES_DIRECTORY, "spawnSG.ini"));
            }
            catch (Exception ex)
            {
                Logger.Log("写入 spawn.ini 失败！异常消息: " + ex.Message);
                return false;
            }

            return true;
        }

        public static void RenameSavedGame()
        {
            Logger.Log("正在重命名保存的游戏。");

            if (saveRenameInProgress)
            {
                Logger.Log("保存重命名进行中！");
                return;
            }

            string saveGameDirectory = GetSaveGameDirectoryPath();

            if (!SafePath.GetFile(saveGameDirectory, "SAVEGAME.NET").Exists)
            {
                Logger.Log("SAVEGAME.NET 不存在！");
                return;
            }

            saveRenameInProgress = true;

            int saveGameId = 0;

            for (int i = 0; i < 1000; i++)
            {
                if (!SafePath.GetFile(saveGameDirectory, string.Format("SVGM_{0}.NET", i.ToString("D3"))).Exists)
                {
                    saveGameId = i;
                    break;
                }
            }

            if (saveGameId == 999)
            {
                if (SafePath.GetFile(saveGameDirectory, "SVGM_999.NET").Exists)
                    Logger.Log("已超出 1000 个保存游戏！正在覆盖之前的 MP 保存。");
            }

            string sgPath = SafePath.CombineFilePath(saveGameDirectory, string.Format("SVGM_{0}.NET", saveGameId.ToString("D3")));

            int tryCount = 0;

            while (true)
            {
                try
                {
                    File.Move(SafePath.CombineFilePath(saveGameDirectory, "SAVEGAME.NET"), sgPath);
                    break;
                }
                catch (Exception ex)
                {
                    Logger.Log("重命名保存的游戏失败！异常消息: " + ex.Message);
                }

                tryCount++;

                if (tryCount > 40)
                {
                    Logger.Log("重命名保存的游戏失败 40 次！正在中止。");
                    return;
                }

                System.Threading.Thread.Sleep(250);
            }

            saveRenameInProgress = false;

            Logger.Log("保存的游戏 SAVEGAME.NET 成功重命名为 " + Path.GetFileName(sgPath));
        }

        public static bool EraseSavedGames()
        {
            Logger.Log("正在删除之前的 MP 保存游戏。");

            try
            {
                for (int i = 0; i < 1000; i++)
                {
                    SafePath.DeleteFileIfExists(GetSaveGameDirectoryPath(), string.Format("SVGM_{0}.NET", i.ToString("D3")));
                }
            }
            catch (Exception ex)
            {
                Logger.Log("删除之前的 MP 保存游戏失败！异常消息: " + ex.Message);
                return false;
            }

            Logger.Log("MP 保存游戏成功删除。");
            return true;
        }
    }
}
