using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

using RobloxDeployHistory;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System.Runtime.InteropServices;

namespace RobloxApiDumpTool
{
    public static class ArgProcessor
    {
        private const string LIVE = Program.LIVE;

        public static async Task<bool> Run(Dictionary<string, string> argMap)
        {
            string format = "TXT";
            bool full = false;

            if (argMap.ContainsKey("-format"))
                format = argMap["-format"];

            if (argMap.ContainsKey("-full"))
                full = true;

            string bin = Directory.GetCurrentDirectory();
            bool isDiffLog = argMap.ContainsKey("-difflog");

            if (argMap.ContainsKey("-export"))
            {
                string channel = argMap["-export"];
                string apiFilePath;

                if (int.TryParse(channel, out int exportVersion))
                    apiFilePath = await ApiDumpTool.GetApiDumpFilePath(LIVE, exportVersion, full).ConfigureAwait(false);
                else if (!File.Exists(channel))
                    apiFilePath = await ApiDumpTool.GetApiDumpFilePath(channel, full).ConfigureAwait(false);
                else
                    apiFilePath = channel;

                string exportBin = Path.Combine(bin, "ExportAPI");

                if (argMap.ContainsKey("-outdir"))
                    exportBin = argMap["-outdir"];
                else
                    Directory.CreateDirectory(exportBin);

                if (format.ToUpperInvariant() == "JSON")
                {
                    string jsonPath = Path.Combine(exportBin, channel + ".json");
                    File.Copy(apiFilePath, jsonPath);
                    return true;
                }

                var lastLog = await ApiDumpTool.GetLastDeployLog(channel);
                var version = lastLog.VersionId;

                var api = new ReflectionDatabase(apiFilePath, channel, version);
                var dumper = new ReflectionDumper(api);

                string result = "";
                bool isPng = false;

                if (format.ToUpperInvariant() == "PNG")
                {
                    isPng = true;
                    format = "html";
                    result = dumper.DumpApi(ReflectionDumper.DumpUsingHtml);
                }
                else if (format.ToUpperInvariant() != "HTML")
                {
                    format = "txt";
                    result = dumper.DumpApi(ReflectionDumper.DumpUsingTxt);
                }
                else
                {
                    format = "html";
                    result = dumper.DumpApi(ReflectionDumper.DumpUsingHtml);
                }

                string exportPath = Path.Combine(exportBin, channel + '.' + format);

                if (format == "html" || format == "png")
                {
                    FileInfo info = new FileInfo(exportPath);
                    string dir = info.DirectoryName;
                    result = ApiDumpTool.PostProcessHtml(result, dir);
                }

                File.WriteAllText(exportPath, result);

                if (isPng)
                {
                    using (var bitmap = await ApiDumpTool.RenderApiDump(exportPath))
                    {
                        exportPath = Path.Combine(exportBin, channel + ".png");
                        bitmap.Save(exportPath);
                    }
                }

                if (argMap.ContainsKey("-start"))
                    Process.Start(exportPath);

                return true;
            }
            else if (argMap.ContainsKey("-compare") || isDiffLog)
            {
                int version = -1;

                if (isDiffLog)
                {
                    string diffLog = argMap["-difflog"];

                    if (!int.TryParse(diffLog, out version))
                    {
                        var lastLog = await ApiDumpTool.GetLastDeployLog(LIVE);
                        version = lastLog.Version;
                    }

                    argMap["-new"] = version.ToString();
                    argMap["-old"] = (version - 1).ToString();
                }
                else if (!argMap.ContainsKey("-old") || !argMap.ContainsKey("-new"))
                {
                    return false;
                }

                string oldFile = "";
                string oldArg = argMap["-old"];

                if (int.TryParse(oldArg, out int oldVersion))
                    oldFile = await ApiDumpTool.GetApiDumpFilePath(LIVE, oldVersion, full);
                else if (!File.Exists(oldArg))
                    oldFile = await ApiDumpTool.GetApiDumpFilePath(oldArg, full);
                else
                    oldFile = oldArg;

                string newFile = "";
                string newArg = argMap["-new"];

                if (int.TryParse(newArg, out int newVersion))
                    newFile = await ApiDumpTool.GetApiDumpFilePath(LIVE, newVersion, full);
                else if (!File.Exists(newArg))
                    newFile = await ApiDumpTool.GetApiDumpFilePath(newArg, full);
                else
                    newFile = newArg;

                var oldApi = new ReflectionDatabase(oldFile);
                var newApi = new ReflectionDatabase(newFile);

                var invFormat = format.ToUpperInvariant();
                bool isPng = (invFormat == "PNG");

                if (invFormat == "PNG" || (invFormat == "HTML" && !isDiffLog))
                    format = "HTML";
                else
                    format = "TXT";

                string result = ReflectionDiffer.CompareDatabases(oldApi, newApi, format, false);
                string exportPath = "";

                if (argMap.ContainsKey("-out"))
                    exportPath = argMap["-out"];
                else if (isDiffLog)
                    exportPath = Path.Combine(bin, version + ".md");
                else
                    exportPath = Path.Combine(bin, "custom-comp." + format.ToLowerInvariant());

                if (format == "HTML")
                {
                    FileInfo info = new FileInfo(exportPath);
                    string dir = info.DirectoryName;
                    result = ApiDumpTool.PostProcessHtml(result, dir);
                }

                if (isPng)
                {
                    using (var bitmap = await ApiDumpTool.RenderApiDump(exportPath))
                    {
                        exportPath = exportPath.Replace(".html", ".png");
                        bitmap.Save(exportPath);
                    }
                }

                if (argMap.ContainsKey("-start") || isDiffLog)
                    Process.Start(exportPath);

                return true;
            }
            else if (argMap.ContainsKey("-updatePages"))
            {
                string dir = argMap["-updatePages"];

                if (!Directory.Exists(dir))
                    return false;

                string currentPath = null;
                string prevPath = null;

                StudioDeployLogs logs = await StudioDeployLogs.Get(LIVE);
                DeployLog currentLog = null;
                DeployLog prevLog = null;

                string currentVersionId = null;
                string prevVersionId = null;

                int currentVersion = 0;
                int prevVersion = 0;

                if (argMap.ContainsKey("-version"))
                {
                    string versionStr = argMap["-version"];
                    int version = int.Parse(versionStr);

                    if (version < 350)
                    {
                        var buildMeta = await ApiDumpTool.GetBuildMetadata();
                        currentPath = await ApiDumpTool.GetApiDumpFilePath(LIVE, version, false);
                        prevPath = await ApiDumpTool.GetApiDumpFilePath(LIVE, version - 1, false);

                        var currentInfo = new FileInfo(currentPath);
                        var currentGuid = currentInfo.Name.Replace(".json", "");

                        var prevInfo = new FileInfo(prevPath);
                        var prevGuid = prevInfo.Name.Replace(".json", "");

                        var currentBuild = buildMeta.Builds
                            .Where(build => build.Guid == currentGuid)
                            .First();

                        var prevBuild = buildMeta.Builds
                            .Where(build => build.Guid == prevGuid)
                            .First();

                        currentVersionId = currentBuild.Version;
                        prevVersionId = prevBuild.Version;
                    }
                    else
                    {
                        var logQuery = logs.CurrentLogs_x86
                            .Union(logs.CurrentLogs_x64)
                            .Where(log => log.Version == version)
                            .OrderBy(log => log.Changelist);

                        currentLog = logQuery.Last();
                    }

                    currentVersion = version;
                    prevVersion = version - 1;
                }
                else
                {
                    var versionGuid = await ApiDumpTool.GetVersion(LIVE);
                    var logQuery = logs.CurrentLogs_x64.Where(log => log.VersionGuid == versionGuid);
                    currentLog = logQuery.FirstOrDefault();
                }

                if (currentPath == null && currentLog != null)
                {
                    prevLog = logs.CurrentLogs_x86
                        .Union(logs.CurrentLogs_x64)
                        .Where(log => log.Version < currentLog.Version)
                        .OrderBy(log => log.Changelist)
                        .LastOrDefault();

                    currentPath = await ApiDumpTool.GetApiDumpFilePath(LIVE, currentLog.VersionGuid, full);
                    currentVersionId = currentLog.VersionId;

                    prevPath = await ApiDumpTool.GetApiDumpFilePath(LIVE, prevLog.VersionGuid, full);
                    prevVersionId = currentLog.VersionId;

                    currentVersion = currentLog.Version;
                    prevVersion = prevLog.Version;
                }
                
                var currentData = new ReflectionDatabase(currentPath, LIVE, currentVersionId);
                var prevData = new ReflectionDatabase(prevPath, LIVE, prevVersionId);

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
                string comparison = ReflectionDiffer.CompareDatabases(prevData, currentData, "HTML", false);
                string historyPath = Path.Combine(dir, "Roblox-API-History.html");

                if (!File.Exists(historyPath))
                    return false;

                string history = File.ReadAllText(historyPath).Replace("\r\n", "\n");
                string appendMarker = $"\n\n<hr id=\"{currentVersion}\"/>\n";

                if (!history.Contains(appendMarker))
                {
                    string prevMarker = $"\n\n<hr id=\"{prevVersion}\"/>\n";
                    int index = history.IndexOf(prevMarker);

                    if (index < 0)
                        return false;

                    if (comparison.Length == 0)
                        comparison = "<h2>Version " + currentVersionId + "</h2>\n\nNo changes!";

                    string insert = $"{appendMarker}\n{comparison}";
                    history = history.Insert(index, insert);

                    File.WriteAllText(historyPath, history);
                }

                Directory.SetCurrentDirectory(dir);

                var git = new Action<string>((input) =>
                {
                    var gitExecute = Process.Start(new ProcessStartInfo
                    {
                        FileName = "git",
                        Arguments = input,
                        CreateNoWindow = true,
                        UseShellExecute = false
                    });

                    gitExecute.WaitForExit();
                });

                git("add .");
                git($"commit -m \"{currentLog}\"");
                git("push");

                return true;
            }

            return false;
        }
    }
}
