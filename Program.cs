using System;
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

            string arg = args[0];
            string branch = args[1];

            if (arg == "-export")
            {
                string bin = Directory.GetCurrentDirectory();

                string exportBin = Path.Combine(bin, "ExportAPI");
                Directory.CreateDirectory(exportBin);

                string apiFilePath = await Roblox.ApiDumpTool.GetApiDumpFilePath(branch);
                ReflectionDatabase api = new ReflectionDatabase(apiFilePath);

                ReflectionDumper dumper = new ReflectionDumper(api);
                string result = dumper.DumpApi(ReflectionDumper.DumpUsingTxt);

                string exportPath = Path.Combine(exportBin, branch + ".txt");
                File.WriteAllText(exportPath, result);

                Environment.Exit(0);
            }
            else if (arg == "-history")
            {
                string baseApiFilePath = await ApiDumpTool.GetApiDumpFilePath(branch);
                
                ReflectionDiffer differ = new ReflectionDiffer();
                differ.PostProcessHtml = false;

                ReflectionDatabase currentDatabase = new ReflectionDatabase(baseApiFilePath);
                currentDatabase.Branch = "roblox";

                string currentGuid = GetRegistryString(ApiDumpTool.VersionRegistry, branch);
                string currentPath = baseApiFilePath;

                ReflectionDumper history = new ReflectionDumper();

                while (true)
                {
                    string previousGuid = await ReflectionHistory.GetPreviousVersionGuid(branch, currentGuid);
                    string previousPath = await ApiDumpTool.GetApiDumpFilePath(branch, previousGuid);

                    ReflectionDatabase previousDatabase = new ReflectionDatabase(previousPath);
                    previousDatabase.Branch = branch;
                    previousDatabase.VersionGuid = previousGuid;

                    DeployLog deployLog = await ReflectionHistory.FindDeployLog(branch, previousGuid);
                    Console.WriteLine("Working on {0}", deployLog.ToString());

                    string differences = await differ.CompareDatabases(previousDatabase, currentDatabase, "HTML");
                    history.Write(differences);

                    if (currentPath != baseApiFilePath)
                        File.Delete(currentPath);

                    if (deployLog.Version == StudioDeployLogs.EarliestVersion)
                        break;

                    currentGuid = previousGuid;
                    currentPath = previousPath;

                    currentDatabase = previousDatabase;
                }

                string workDir = ApiDumpTool.GetWorkDirectory();
                string exportPath = Path.Combine(workDir, branch + "-history.html");

                string results = history.ExportResults(ApiDumpTool.PostProcessHtml);
                File.WriteAllText(exportPath, results);
                
                Process.Start(exportPath);
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