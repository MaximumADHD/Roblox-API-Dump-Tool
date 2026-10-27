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

            #if CLI

                if (format.Equals("PNG", StringComparison.OrdinalIgnoreCase))
                    throw new NotSupportedException("PNG rendering is only available in the Windows GUI build. Use TXT, JSON, or HTML.");

                if (argMap.ContainsKey("-start"))
                    throw new NotSupportedException("The CLI build does not open desktop applications. Omit -start.");

            #endif

            if (argMap.ContainsKey("-full"))
                full = true;

            string bin = Directory.GetCurrentDirectory();
            bool isDiffLog = argMap.ContainsKey("-difflog");
            var schema = full ? ApiDumpSchema.V1_Full : ApiDumpSchema.V1_Partial;

            if (argMap.ContainsKey("-export"))
            {
                string channel = argMap["-export"];
                string apiFilePath, apiFilePath2;

                if (int.TryParse(channel, out int exportVersion))
                {
                    apiFilePath = await ApiDumpTool.GetApiDumpFilePath(LIVE, exportVersion, schema, useLegacyChannelUrl: true);
                    apiFilePath2 = await ApiDumpTool.GetApiDumpFilePath(LIVE, exportVersion, ApiDumpSchema.V2, useLegacyChannelUrl: true);
                }
                else if (!File.Exists(channel))
                {
                    apiFilePath = await ApiDumpTool.GetApiDumpFilePath(channel, schema, useLegacyChannelUrl: true);
                    apiFilePath2 = await ApiDumpTool.GetApiDumpFilePath(channel, ApiDumpSchema.V2, useLegacyChannelUrl: true);
                }
                else
                {
                    apiFilePath = channel;
                    apiFilePath2 = "";
                }

                string exportBin = Path.Combine(bin, "ExportAPI");

                if (argMap.ContainsKey("-outdir"))
                    exportBin = argMap["-outdir"];

                Directory.CreateDirectory(exportBin);
                string exportName = File.Exists(channel) ? Path.GetFileNameWithoutExtension(channel) : channel;

                if (format.ToUpperInvariant() == "JSON")
                {
                    string jsonPath = Path.Combine(exportBin, exportName + ".json");
                    File.Copy(apiFilePath, jsonPath);
                    return true;
                }

                var api = new ReflectionDatabase(apiFilePath, schema);
                var dumper = new ReflectionDumper(api);

                if (!string.IsNullOrEmpty(apiFilePath2))
                    api.MungeV2(apiFilePath2);

                string result = "";
#if !CLI
                bool isPng = false;
#endif
                if (format.ToUpperInvariant() == "PNG")
                {
#if !CLI
                    isPng = true;
                    format = "html";
                    result = dumper.DumpApi(ReflectionDumper.DumpUsingHtml);
#endif
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

                string exportPath = Path.Combine(exportBin, exportName + '.' + format);

                if (format == "html" || format == "png")
                {
                    FileInfo info = new FileInfo(exportPath);
                    string dir = info.DirectoryName;
                    result = ApiDumpTool.PostProcessHtml(result, dir);
                }

                File.WriteAllText(exportPath, result);

#if !CLI
                if (isPng && OperatingSystem.IsWindowsVersionAtLeast(6, 1))
                {
                    using var bitmap = await ApiDumpTool.RenderApiDump(exportPath);
                    exportPath = Path.Combine(exportBin, channel + ".png");
                    bitmap.Save(exportPath);
                }

                if (argMap.ContainsKey("-start"))
                {
                    var procInfo = new ProcessStartInfo()
                    {
                        FileName = exportPath,
                        UseShellExecute = true,
                    };

                    Process.Start(procInfo);
                }
#endif

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

                string oldFile, oldFile2 = "";
                string oldArg = argMap["-old"];

                if (int.TryParse(oldArg, out int oldVersion))
                {
                    oldFile = await ApiDumpTool.GetApiDumpFilePath(LIVE, oldVersion, schema, useLegacyChannelUrl: true);
                    oldFile2 = await ApiDumpTool.GetApiDumpFilePath(LIVE, oldVersion, ApiDumpSchema.V2, useLegacyChannelUrl: true);
                }
                else if (!File.Exists(oldArg))
                {
                    oldFile = await ApiDumpTool.GetApiDumpFilePath(oldArg, schema, useLegacyChannelUrl: true);
                    oldFile2 = await ApiDumpTool.GetApiDumpFilePath(oldArg, ApiDumpSchema.V2, useLegacyChannelUrl: true);
                }
                else
                {
                    oldFile = oldArg;
                    oldFile2 = "";
                }

                string newFile, newFile2 = "";
                string newArg = argMap["-new"];

                if (int.TryParse(newArg, out int newVersion))
                {
                    newFile = await ApiDumpTool.GetApiDumpFilePath(LIVE, newVersion, schema, useLegacyChannelUrl: true);
                    newFile2 = await ApiDumpTool.GetApiDumpFilePath(LIVE, newVersion, ApiDumpSchema.V2, useLegacyChannelUrl: true);
                }
                else if (!File.Exists(newArg))
                {
                    newFile = await ApiDumpTool.GetApiDumpFilePath(newArg, schema, useLegacyChannelUrl: true);
                    newFile2 = await ApiDumpTool.GetApiDumpFilePath(newArg, ApiDumpSchema.V2, useLegacyChannelUrl: true);
                }
                else
                {
                    newFile = newArg;
                    newFile2 = "";
                }

                var oldApi = new ReflectionDatabase(oldFile, schema);
                var newApi = new ReflectionDatabase(newFile, schema);

                if (!string.IsNullOrEmpty(oldFile2))
                    oldApi.MungeV2(oldFile2);

                if (!string.IsNullOrEmpty(newFile2))
                    newApi.MungeV2(newFile2);

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

                File.WriteAllText(exportPath, result);

#if !CLI
                if (isPng && OperatingSystem.IsWindowsVersionAtLeast(6, 1))
                {
                    using (var bitmap = await ApiDumpTool.RenderApiDump(exportPath))
                    {
                        exportPath = exportPath.Replace(".html", ".png");
                        bitmap.Save(exportPath);
                    }
                }

                if (argMap.ContainsKey("-start") || isDiffLog)
                {
                    var procInfo = new ProcessStartInfo()
                    {
                        FileName = exportPath,
                        UseShellExecute = true,
                    };

                    Process.Start(procInfo);
                }
#endif

                return true;
            }
            else if (argMap.TryGetValue("-updatePages", out string dir))
            {
                if (!Directory.Exists(dir))
                    return false;

                string currentPath = null;
                string prevPath = null;

                StudioDeployLogs logs = await StudioDeployLogs.Get();
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
                        currentPath = await ApiDumpTool.GetApiDumpFilePath(LIVE, version, ApiDumpSchema.V1_Partial, useLegacyChannelUrl: true);
                        prevPath = await ApiDumpTool.GetApiDumpFilePath(LIVE, version - 1, ApiDumpSchema.V1_Partial, useLegacyChannelUrl: true);

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
                        var logQuery = logs.CurrentLogs
                            .Where(log => log.Version == version)
                            .OrderBy(log => log.CommitId);

                        currentLog = logQuery.Last();
                    }

                    currentVersion = version;
                    prevVersion = version - 1;
                }
                else
                {
                    var versionGuid = await ApiDumpTool.GetVersion(LIVE);
                    var logQuery = logs.CurrentLogs.Where(log => log.VersionGuid == versionGuid);
                    currentLog = logQuery.FirstOrDefault();
                }

                if (currentPath == null && currentLog != null)
                {
                    prevLog = logs.CurrentLogs
                        .Where(log => log.Version < currentLog.Version)
                        .OrderBy(log => log.CommitId)
                        .LastOrDefault();

                    currentPath = await ApiDumpTool.GetApiDumpFilePath(LIVE, currentLog.VersionGuid, schema, useLegacyChannelUrl: true);
                    currentVersionId = currentLog.VersionId;

                    prevPath = await ApiDumpTool.GetApiDumpFilePath(LIVE, prevLog.VersionGuid, schema, useLegacyChannelUrl: true);
                    prevVersionId = currentLog.VersionId;

                    currentVersion = currentLog.Version;
                    prevVersion = prevLog.Version;
                }

                var currentData = new ReflectionDatabase(currentPath, ApiDumpSchema.V1_Full)
                {
                    Channel = LIVE,
                    Version = currentVersionId,
                };

                var prevData = new ReflectionDatabase(prevPath, ApiDumpSchema.V1_Full)
                {
                    Channel = LIVE,
                    Version = prevVersionId,
                };

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
                
                // Write version.txt
                string versionPath = Path.Combine(dir, "version.txt");
                File.WriteAllText(versionPath, currentData.Version);

                if (argMap.ContainsKey("-commit"))
                {
                    // Commit changes to git
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
                }

                return true;
            }

            return false;
        }
    }
}
