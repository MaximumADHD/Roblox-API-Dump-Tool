using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Forms;

using Microsoft.Win32;
using Roblox.Reflection;

namespace Roblox
{
    static class Program
    {
        public static RegistryKey MainRegistry => GetRegistryKey(Registry.CurrentUser, "SOFTWARE", "Roblox API Dump Tool"); 
        
        public static RegistryKey GetRegistryKey(RegistryKey root, params string[] subKeys)
        {
            string path = string.Join("\\", subKeys);
            return root.CreateSubKey(path, RegistryKeyPermissionCheck.ReadWriteSubTree, RegistryOptions.None);
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

            bool result = false;
            bool.TryParse(value, out result);

            return result;
        }

        public static bool GetRegistryBool(string name)
        {
            return GetRegistryBool(MainRegistry, name);
        }

        public static string GetEnumName<T>(T item)
        {
            return Enum.GetName(typeof(T), item);
        }

        private static async Task processArgs(string[] args)
        {
            if (args.Length < 2)
                return;

            Dictionary<string, string> argMap = new Dictionary<string, string>();
            string currentArg = "";

            foreach (string arg in args)
            {
                if (arg.StartsWith("-"))
                {
                    if (currentArg != "")
                        argMap.Add(currentArg, "");

                    currentArg = arg;
                }
                else if (currentArg != "")
                {
                    argMap.Add(currentArg, arg);
                    currentArg = "";
                }
            }

            if (currentArg != "")
                argMap.Add(currentArg, "");

            string format = "TXT";
            if (argMap.ContainsKey("-format"))
                format = argMap["-format"];

            string bin = Directory.GetCurrentDirectory();

            if (argMap.ContainsKey("-export"))
            {
                string branch = argMap["-export"];
                string apiFilePath = await ApiDumpTool.GetApiDumpFilePath(branch);

                string exportBin = Path.Combine(bin, "ExportAPI");
                if (argMap.ContainsKey("-outdir"))
                    exportBin = argMap["-outdir"];
                else
                    Directory.CreateDirectory(exportBin);

                if (format.ToLower() == "json")
                {
                    string jsonPath = Path.Combine(exportBin, branch + ".json");
                    File.Copy(apiFilePath, jsonPath);

                    Environment.Exit(0);
                    return;
                }

                ReflectionDatabase api = new ReflectionDatabase(apiFilePath);

                ReflectionDumper dumper = new ReflectionDumper(api);
                string result = "";

                if (format.ToLower() != "html")
                {
                    format = "txt";
                    result = dumper.DumpApi(ReflectionDumper.DumpUsingTxt);
                }
                else
                {
                    format = "html";
                    result = dumper.DumpApi(ReflectionDumper.DumpUsingHtml);
                }

                string exportPath = Path.Combine(exportBin, branch + '.' + format);

                if (format == "html")
                {
                    FileInfo info = new FileInfo(exportPath);
                    string dir = info.DirectoryName;
                    result = ApiDumpTool.PostProcessHtml(result, dir);
                }

                File.WriteAllText(exportPath, result);

                if (argMap.ContainsKey("-start"))
                    Process.Start(exportPath);

                Environment.Exit(0);
            }
            else if (argMap.ContainsKey("-compare"))
            {
                if (!argMap.ContainsKey("-old") || !argMap.ContainsKey("-new"))
                    Environment.Exit(1);

                string oldFile = "";
                string oldArg = argMap["-old"];

                if (oldArg == "roblox" || oldArg.StartsWith("gametest") && oldArg.EndsWith(".robloxlabs"))
                    oldFile = await ApiDumpTool.GetApiDumpFilePath(oldArg);
                else
                    oldFile = oldArg;

                string newFile = "";
                string newArg = argMap["-new"];

                if (newArg == "roblox" || newArg.StartsWith("gametest") && newArg.EndsWith(".robloxlabs"))
                    newFile = await ApiDumpTool.GetApiDumpFilePath(newArg);
                else
                    newFile = newArg;

                ReflectionDatabase oldApi = new ReflectionDatabase(oldFile);
                ReflectionDatabase newApi = new ReflectionDatabase(newFile);

                ReflectionDiffer differ = new ReflectionDiffer();
                differ.PostProcessHtml = false;

                if (format.ToLower() == "html")
                    format = "HTML";
                else
                    format = "TXT";

                string result = await differ.CompareDatabases(oldApi, newApi, format);
                string exportPath = "";

                if (argMap.ContainsKey("-out"))
                    exportPath = argMap["-out"];
                else
                    exportPath = Path.Combine(bin, "custom-comp." + format.ToLower());

                if (format == "HTML")
                {
                    FileInfo info = new FileInfo(exportPath);
                    string dir = info.DirectoryName;
                    result = ApiDumpTool.PostProcessHtml(result, dir);
                }

                File.WriteAllText(exportPath, result);

                if (argMap.ContainsKey("-start"))
                    Process.Start(exportPath);

                Environment.Exit(0);
            }
        }

        [STAThread]
        static void Main(string[] args)
        {
            if (args.Length > 0)
            {
                Task processArgsTask = Task.Run(() => processArgs(args));
                processArgsTask.Wait();
            }

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new ApiDumpTool());
        }
    }
}