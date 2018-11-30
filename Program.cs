using System;
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

        public static async Task processArgs(string[] args)
        {
            if (args.Length > 1 && args[0] == "-export")
            {
                string branch = args[1];
                string bin = Directory.GetCurrentDirectory();

                string exportBin = Path.Combine(bin, "ExportAPI");
                Directory.CreateDirectory(exportBin);

                string apiFilePath = await Roblox.Main.GetApiDumpFilePath(branch);
                string apiJson = File.ReadAllText(apiFilePath);

                ReflectionDatabase api = ReflectionDatabase.Load(apiJson);
                ReflectionDumper dumper = new ReflectionDumper(api);

                string result = dumper.Run();
                string exportPath = Path.Combine(exportBin, branch + ".txt");

                File.WriteAllText(exportPath, result);
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
            Application.Run(new Main());
        }
    }
}
