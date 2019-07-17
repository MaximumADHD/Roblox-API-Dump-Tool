using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using System.Windows.Forms;

using Microsoft.Win32;
using Roblox.Reflection;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Roblox
{
    static class Program
    {
        public static RegistryKey MainRegistry => GetRegistryKey(Registry.CurrentUser, "SOFTWARE", "Roblox API Dump Tool"); 
        
        private const string clientTracker = "CloneTrooper1019/Roblox-Client-Tracker";
        private const string apiHistoryUrl = "https://clonetrooper1019.github.io/Roblox-API-History.html";

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
            bool isDiffLog = argMap.ContainsKey("-difflog");

            if (argMap.ContainsKey("-export"))
            {
                string branch = argMap["-export"];
                int exportVersion = -1;
                string apiFilePath;
                
                if (int.TryParse(branch, out exportVersion))
                    apiFilePath = await ApiDumpTool.GetApiDumpFilePath("roblox", exportVersion);
                else if (branch == "roblox" || branch.StartsWith("gametest") && branch.EndsWith(".robloxlabs"))
                    apiFilePath = await ApiDumpTool.GetApiDumpFilePath(branch);
                else
                    apiFilePath = branch;

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
            else if (argMap.ContainsKey("-compare") || isDiffLog)
            {
                int version = -1;

                if (isDiffLog)
                {
                    string diffLog = argMap["-difflog"];
                    
                    if (int.TryParse(diffLog, out version))
                    {
                        argMap["-new"] = version.ToString();
                        argMap["-old"] = (version - 1).ToString();
                    }
                    else
                    {
                        Environment.Exit(1);
                    }
                }
                else if (!argMap.ContainsKey("-old") || !argMap.ContainsKey("-new"))
                {
                    Environment.Exit(1);
                }

                int oldVersion = -1;
                string oldFile = "";
                string oldArg = argMap["-old"];

                if (int.TryParse(oldArg, out oldVersion))
                    oldFile = await ApiDumpTool.GetApiDumpFilePath("roblox", oldVersion);
                else if (oldArg == "roblox" || oldArg.StartsWith("gametest") && oldArg.EndsWith(".robloxlabs"))
                    oldFile = await ApiDumpTool.GetApiDumpFilePath(oldArg);
                else
                    oldFile = oldArg;

                int newVersion = -1;
                string newFile = "";
                string newArg = argMap["-new"];

                if (int.TryParse(newArg, out newVersion))
                    newFile = await ApiDumpTool.GetApiDumpFilePath("roblox", newVersion);
                else if (newArg == "roblox" || newArg.StartsWith("gametest") && newArg.EndsWith(".robloxlabs"))
                    newFile = await ApiDumpTool.GetApiDumpFilePath(newArg);
                else
                    newFile = newArg;

                var oldApi = new ReflectionDatabase(oldFile);
                var newApi = new ReflectionDatabase(newFile);

                if (format.ToLower() == "html" && !isDiffLog)
                    format = "HTML";
                else
                    format = "TXT";

                string result = await ReflectionDiffer.CompareDatabases(oldApi, newApi, format, false);
                string exportPath = "";

                if (isDiffLog)
                    exportPath = Path.Combine(bin, version + ".md");
                else if (argMap.ContainsKey("-out"))
                    exportPath = argMap["-out"];
                else
                    exportPath = Path.Combine(bin, "custom-comp." + format.ToLower());

                if (format == "HTML")
                {
                    FileInfo info = new FileInfo(exportPath);
                    string dir = info.DirectoryName;
                    result = ApiDumpTool.PostProcessHtml(result, dir);
                }

                if (isDiffLog)
                {
                    string commitUrl = "";
                    
                    var userAgent = new WebHeaderCollection();
                    userAgent.Add("User-Agent", "Roblox API Dump Tool");

                    using (WebClient http = new WebClient() { Headers = userAgent })
                    {
                        string commitsUrl = $"https://api.github.com/repos/{clientTracker}/commits?sha=roblox";
                        string commitsJson = await http.DownloadStringTaskAsync(commitsUrl);

                        using (StringReader reader = new StringReader(commitsJson))
                        {
                            JsonTextReader jsonReader = new JsonTextReader(reader);
                            JArray data = JArray.Load(jsonReader);
                            string prefix = "0." + version;
                            
                            foreach (JObject info in data)
                            {
                                var commit = info.Value<JObject>("commit");
                                string message = commit.Value<string>("message");

                                if (message.StartsWith(prefix))
                                {
                                    string sha = info.Value<string>("sha");
                                    commitUrl = $"https://github.com/{clientTracker}/commit/{sha}";

                                    break;
                                }
                            }
                        }
                    }

                    result = "## Client Difference Log\n\n"
                           + $"{commitUrl}\n\n"

                           + "## API Changes\n\n"

                           + "```plain\n"
                           + $"{result}\n"
                           + "```\n\n"

                           + $"(Click [here]({apiHistoryUrl}#{version}) for a syntax highlighted version!)";
                }

                File.WriteAllText(exportPath, result);

                if (argMap.ContainsKey("-start") || isDiffLog)
                    Process.Start(exportPath);

                Environment.Exit(0);
            }
            else if (argMap.ContainsKey("-updatePages"))
            {
                string dir = argMap["-updatePages"];

                if (!Directory.Exists(dir))
                    Environment.Exit(1);

                var versionInfo = await ClientVersionInfo.Get("WindowsStudio", "roblox");
                StudioDeployLogs logs = await StudioDeployLogs.GetDeployLogs("roblox");

                DeployLog currentLog = logs.LookupFromGuid[versionInfo.Guid];
                DeployLog prevLog = logs.LookupFromVersion[currentLog.Version - 1];

                string currentPath = await ApiDumpTool.GetApiDumpFilePath("roblox", currentLog.VersionGuid);
                string prevPath = await ApiDumpTool.GetApiDumpFilePath("roblox", prevLog.VersionGuid);

                ReflectionDatabase currentData = new ReflectionDatabase(currentPath, "roblox", currentLog.ToString());
                ReflectionDatabase prevData = new ReflectionDatabase(prevPath, "roblox", prevLog.ToString());

                var postProcess = new ReflectionDumper.DumpPostProcesser((dump, workDir) =>
                {
                    var head = "<head>\n"
                        + $"\t<link rel=\"stylesheet\" href=\"api-dump.css\">\n"
                        + "</head>\n\n";

                    return head + dump.Trim();
                });

                // Write Roblox-API-Dump.html
                ReflectionDumper dumper = new ReflectionDumper(currentData);
                string currentApi = dumper.DumpApi(ReflectionDumper.DumpUsingHtml, postProcess);

                string dumpPath = Path.Combine(dir, "Roblox-API-Dump.html");
                File.WriteAllText(dumpPath, currentApi);

                // Append to Roblox-API-History.html
                string comparison = await ReflectionDiffer.CompareDatabases(prevData, currentData, "HTML", false);
                string historyPath = Path.Combine(dir, "Roblox-API-History.html");

                if (!File.Exists(historyPath))
                    Environment.Exit(1);

                string history = File.ReadAllText(historyPath);
                string appendMarker = $"<hr id=\"{currentLog.Version}\"/>";

                if (!history.Contains(appendMarker))
                {
                    string prevMarker = $"<hr id=\"{prevLog.Version}\"/>";
                    int index = history.IndexOf(prevMarker);

                    string insert = $"{appendMarker}\n{comparison}";
                    history = history.Insert(index, insert);

                    File.WriteAllText(historyPath, history);
                }

                Environment.Exit(0);
            }
        }

        [STAThread]
        static void Main(string[] args)
        {
            // Set the security protocol to be used for HTTPS.
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

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
                Task processArgsTask = Task.Run(() => processArgs(args));
                processArgsTask.Wait();
            }

            // Start the application window.
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new ApiDumpTool());
        }
    }
}