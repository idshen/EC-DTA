using System;
using System.Collections.Generic;
#if !DEBUG
using System.IO;
using System.Reflection;
using System.Runtime.Loader;
#endif
using System.Threading;

/* !! 我们无法在这个类中使用对其他项目或非框架程序集的引用，因为程序集加载事件尚未连接 !! */

namespace DTAClient
{
    static class Program
    {
#if !DEBUG
        static Program()
        {
            /* 我们有不同的二进制文件，具体取决于构建平台，但为了简化起见，
             * 目标项目（DTA，TI，MO，YR）将它们全部提供在单个下载中。
             * 为了避免DLL地狱，我们从不同的目录加载二进制文件，
             * 具体取决于构建平台。 */

            string startupPath = new FileInfo(Assembly.GetEntryAssembly().Location).Directory.Parent.Parent.FullName + Path.DirectorySeparatorChar;

            COMMON_LIBRARY_PATH = Path.Combine(startupPath, "Binaries") + Path.DirectorySeparatorChar;

#if XNA
            SPECIFIC_LIBRARY_PATH = Path.Combine(startupPath, "Binaries", "XNA") + Path.DirectorySeparatorChar;
#elif GL && ISWINDOWS
            SPECIFIC_LIBRARY_PATH = Path.Combine(startupPath, "Binaries", "OpenGL") + Path.DirectorySeparatorChar;
#elif GL && !ISWINDOWS
            SPECIFIC_LIBRARY_PATH = Path.Combine(startupPath, "Binaries", "UniversalGL") + Path.DirectorySeparatorChar;
#elif DX
            SPECIFIC_LIBRARY_PATH = Path.Combine(startupPath, "Binaries", "Windows") + Path.DirectorySeparatorChar;
#else
            // 处理未定义的构建平台
#endif

            // 尽早设置DLL加载路径
            AssemblyLoadContext.Default.Resolving += DefaultAssemblyLoadContextOnResolving;
        }

        private static string COMMON_LIBRARY_PATH;
        private static string SPECIFIC_LIBRARY_PATH;

#endif
        /// <summary>
        /// 应用程序的主入口点。
        /// </summary>
#if WINFORMS
        [STAThread]
#endif
        static void Main(string[] args)
        {
            bool noAudio = false;
            bool multipleInstanceMode = true;
            List<string> unknownStartupParams = new List<string>();

            for (int arg = 0; arg < args.Length; arg++)
            {
                string argument = args[arg].ToUpperInvariant();

                switch (argument)
                {
                    case "-NOAUDIO":
                        noAudio = true;
                        break;
                    case "-MULTIPLEINSTANCE":
                        multipleInstanceMode = true;
                        break;
                    default:
                        unknownStartupParams.Add(argument);
                        break;
                }
            }

            var parameters = new StartupParams(noAudio, multipleInstanceMode, unknownStartupParams);

            if (multipleInstanceMode)
            {
                // 继续客户端启动
                PreStartup.Initialize(parameters);
                return;
            }

            // 我们是单实例应用程序！
            // http://stackoverflow.com/questions/229565/what-is-a-good-pattern-for-using-a-global-mutex-in-c/229567
            // 全局前缀表示该互斥体对整个机器全局可见
            string mutexId = FormattableString.Invariant($"Global{Guid.Parse("1CC9F8E7-9F69-4BBC-B045-E734204027A9")}");

            using var mutex = new Mutex(false, mutexId, out _);
            bool hasHandle = false;

            try
            {
                try
                {
                    hasHandle = mutex.WaitOne(8000, false);
                    if (hasHandle == false)
                        throw new TimeoutException("等待独占访问超时");
                }
                catch (AbandonedMutexException)
                {
                    hasHandle = true;
                }
                catch (TimeoutException)
                {
                    return;
                }

                // 继续客户端启动
                PreStartup.Initialize(parameters);
            }
            finally
            {
                if (hasHandle)
                    mutex.ReleaseMutex();
            }
        }
#if !DEBUG

        private static Assembly DefaultAssemblyLoadContextOnResolving(AssemblyLoadContext assemblyLoadContext, AssemblyName assemblyName)
        {
            if (assemblyName.Name.EndsWith(".resources", StringComparison.OrdinalIgnoreCase))
                return null;

            var commonFileInfo = new FileInfo(Path.Combine(COMMON_LIBRARY_PATH, FormattableString.Invariant($"{assemblyName.Name}.dll")));

            if (commonFileInfo.Exists)
                return assemblyLoadContext.LoadFromAssemblyPath(commonFileInfo.FullName);

            var specificFileInfo = new FileInfo(Path.Combine(SPECIFIC_LIBRARY_PATH, FormattableString.Invariant($"{assemblyName.Name}.dll")));

            if (specificFileInfo.Exists)
                return assemblyLoadContext.LoadFromAssemblyPath(specificFileInfo.FullName);

            return null;
        }
#endif
    }
}