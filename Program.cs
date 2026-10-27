using System;
using System.Collections.Generic;
using System.Globalization;

#if !CLI
using System.IO;
using System.Diagnostics;
using System.Windows.Forms;
using System.Runtime.Versioning;
using Microsoft.Win32;
#endif

namespace RobloxApiDumpTool
{
    #if !CLI
        // Windows only when building as GUI.
        [SupportedOSPlatform("windows")]
    #endif

    static class Program
    {
        public const string LIVE = "LIVE";
        public static readonly NumberFormatInfo NumberFormat = NumberFormatInfo.InvariantInfo;
        public const StringComparison StringFormat = StringComparison.InvariantCulture;

    #if !CLI
        public static RegistryKey MainRegistry => GetRegistryKey(Registry.CurrentUser, "SOFTWARE", "Roblox API Dump Tool");

        public static RegistryKey GetRegistryKey(RegistryKey root, params string[] subKeys)
        {
            string path = string.Join("\\", subKeys);
            return root.CreateSubKey(path, RegistryKeyPermissionCheck.ReadWriteSubTree, RegistryOptions.None);
        }

        public static RegistryKey GetMainRegistryKey(params string[] subKeys)
        {
            return GetRegistryKey(MainRegistry, subKeys);
        }

        public static string GetRegistryString(RegistryKey key, string name)
        {
            return key.GetValue(name, "") as string;
        }

        public static RegistryKey GetRegistryKey(params string[] subKeys)
        {
            return GetRegistryKey(MainRegistry, subKeys);
        }

        public static string GetRegistryString(string name)
        {
            return GetRegistryString(MainRegistry, name);
        }

        public static bool GetRegistryBool(RegistryKey key, string name)
        {
            string value = GetRegistryString(key, name);

            if (bool.TryParse(value, out bool result))
                return result;

            return false;
        }

        public static bool GetRegistryBool(string name)
        {
            return GetRegistryBool(MainRegistry, name);
        }
    #endif

        static Dictionary<string, string> ReadArgs(params string[] args)
        {
            if (args.Length < 1)
                return null;

            var argMap = new Dictionary<string, string>();
            string currentArg = "";

            foreach (string arg in args)
            {
                if (arg.StartsWith("-"))
                {
                    if (!string.IsNullOrEmpty(currentArg))
                        argMap.Add(currentArg, "");

                    currentArg = arg;
                }
                else if (!string.IsNullOrEmpty(currentArg))
                {
                    argMap.Add(currentArg, arg);
                    currentArg = "";
                }
            }

            if (!string.IsNullOrEmpty(currentArg))
                argMap.Add(currentArg, "");

            return argMap;
        }

    #if CLI
        static int Main(string[] args)
        {
            if (args.Length == 0 || Array.Exists(args, arg => arg == "--help" || arg == "-help" || arg == "-h"))
            {
                Console.WriteLine("Roblox API Dump Tool (CLI)");
                Console.WriteLine("    -export <channel|version|file> [-format TXT|JSON|HTML] [-full] [-outdir <directory>]");
                Console.WriteLine("    -compare -old <channel|version|file> -new <channel|version|file> [-format TXT|HTML] [-full] [-out <file>]");
                Console.WriteLine("    -difflog <version> [-out <file>]");
                Console.WriteLine("    -updatePages <directory> [-version <version>] [-full] (commits and pushes with git)");
                return 0;
            }

            try
            {
                var argDict = ReadArgs(args);
                var task = ArgProcessor.Run(argDict);

                if (task.GetAwaiter().GetResult())
                    return 0;

                Console.Error.WriteLine("Invalid arguments. Use --help for usage.");
            }
            catch (Exception e)
            {
                Console.Error.WriteLine(e.Message);
            }

            return 1;
        }
    #else
        [STAThread]
        static void Main(string[] args)
        {
            // Set the browser emulation mode for
            // rendering generated html as an image.
            var browserEmulation = GetRegistryKey
            (
                Registry.CurrentUser,
                "Software", "Microsoft",
                "Internet Explorer",
                "Main", "FeatureControl",
                "FEATURE_BROWSER_EMULATION"
            );

            Process exe = Process.GetCurrentProcess();
            ProcessModule main = exe.MainModule;

            string exeName = Path.GetFileName(main.FileName);
            browserEmulation.SetValue(exeName, 11000);

            // Check the launch arguments.
            if (args.Length > 0)
            {
                var argMap = ReadArgs(args);

                if (argMap != null)
                {
                    var processArgsTask = ArgProcessor.Run(argMap);
                    processArgsTask.Wait();

                    if (processArgsTask.Result)
                        Environment.Exit(0);

                    Environment.Exit(1);
                }
            }

            // Start the application window.
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new ApiDumpTool());
        }
    #endif
    }
}