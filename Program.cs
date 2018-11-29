using System;
using System.Windows.Forms;

using Microsoft.Win32;

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

        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new Main());
        }
    }
}
