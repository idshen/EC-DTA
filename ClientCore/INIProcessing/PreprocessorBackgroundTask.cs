using Rampastring.Tools;
using System.IO;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace ClientCore.INIProcessing
{
    /// <summary>
    /// Background task for pre-processing INI files.
    /// Singleton.
    /// </summary>
    public class PreprocessorBackgroundTask
    {
        private PreprocessorBackgroundTask()
        {
        }

        private static PreprocessorBackgroundTask _instance;
        public static PreprocessorBackgroundTask Instance
        {
            get
            {
                if (_instance == null)
                    _instance = new PreprocessorBackgroundTask();

                return _instance;
            }
        }

        private Task task;

        public bool IsRunning => !task.IsCompleted;

        public void Run()
        {
            task = Task.Factory.StartNew(CheckFiles);
        }

        private static void CheckFiles()
        {
            Logger.Log("开始后台处理 INI 文件。");

            DirectoryInfo iniFolder = SafePath.GetDirectory(ProgramConstants.GamePath, "INI", "Base");

            if (!iniFolder.Exists)
            {
                Logger.Log("/INI/Base 不存在，跳过后台处理 INI 文件。");
                return;
            }

            IniPreprocessInfoStore infoStore = new IniPreprocessInfoStore();
            infoStore.Load();

            IniPreprocessor processor = new IniPreprocessor();

            IEnumerable<FileInfo> iniFiles = iniFolder.EnumerateFiles("*.ini", SearchOption.TopDirectoryOnly);

            int processedCount = 0;

            foreach (FileInfo iniFile in iniFiles)
            {
                if (!infoStore.IsIniUpToDate(iniFile.Name))
                {
                    Logger.Log("INI 文件 " + iniFile.Name + " 未处理或过时，重新处理它。");

                    string sourcePath = iniFile.FullName;
                    string destinationPath = SafePath.CombineFilePath(ProgramConstants.GamePath, "INI", iniFile.Name);

                    processor.ProcessIni(sourcePath, destinationPath);

                    string sourceHash = Utilities.CalculateSHA1ForFile(sourcePath);
                    string destinationHash = Utilities.CalculateSHA1ForFile(destinationPath);
                    infoStore.UpsertRecord(iniFile.Name, sourceHash, destinationHash);
                    processedCount++;
                }
                else
                {
                    Logger.Log("INI 文件 " + iniFile.Name + " 已是最新。");
                }
            }

            if (processedCount > 0)
            {
                Logger.Log("正在写入预处理的 INI 信息存储。");
                infoStore.Write();
            }

            Logger.Log("结束后台处理 INI 文件。");
        }
    }
}