using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Roblox.Reflection
{
    public struct StudioDeployLog
    {
        public string VersionGuid;

        public int MajorRev;
        public int Version;
        public int Patch;
        public int Changelist;

        public override string ToString() => string.Join(".", MajorRev, Version, Patch, Changelist);
    }

    public class StudioDeployLogs
    {
        public Dictionary<string, StudioDeployLog> LookupFromGuid;
        public Dictionary<int, StudioDeployLog> LookupFromVersion;

        private static string MatchLog = "New Studio (version-[a-f\\d]+) at \\d+/\\d+/\\d+ \\d+:\\d+:\\d+ [A,P]M, file version: (\\d+), (\\d+), (\\d+), (\\d+)";

        private void Add(StudioDeployLog deployLog)
        {
            // Add by version guid
            if (!LookupFromGuid.ContainsKey(deployLog.VersionGuid))
                LookupFromGuid.Add(deployLog.VersionGuid, deployLog);

            // Add by version info
            if (!LookupFromVersion.ContainsKey(deployLog.Version) && deployLog.Patch == 0)
                LookupFromVersion.Add(deployLog.Version, deployLog);
        }

        private void InitializeLogs(string deployHistory)
        {
            MatchCollection matches = Regex.Matches(deployHistory, MatchLog);

            foreach (Match match in matches)
            {
                string[] data = match.Groups.Cast<Group>()
                    .Select(group => group.Value)
                    .Where(value => value.Length != 0)
                    .ToArray();

                StudioDeployLog deployLog = new StudioDeployLog();
                deployLog.VersionGuid = data[1];

                int.TryParse(data[2], out deployLog.MajorRev);
                int.TryParse(data[3], out deployLog.Version);
                int.TryParse(data[4], out deployLog.Patch);
                int.TryParse(data[5], out deployLog.Changelist);

                Add(deployLog);
            }
        }

        public StudioDeployLogs(string deployHistory)
        {
            LookupFromGuid = new Dictionary<string, StudioDeployLog>();
            LookupFromVersion = new Dictionary<int, StudioDeployLog>();

            InitializeLogs(deployHistory);
        }
    }

    static class ReflectionHistory
    {
        private static Dictionary<string, StudioDeployLogs> Logs = new Dictionary<string, StudioDeployLogs>();
        
        public static async Task<StudioDeployLogs> GetDeployLogs(string branch)
        {
            if (!Logs.ContainsKey(branch))
            {
                string deployHistoryUrl = "https://s3.amazonaws.com/setup." + branch + ".com/DeployHistory.txt";
                string deployHistory;

                using (WebClient http = new WebClient())
                    deployHistory = await http.DownloadStringTaskAsync(deployHistoryUrl);

                StudioDeployLogs deployLogs = new StudioDeployLogs(deployHistory);
                Logs.Add(branch, deployLogs);
            }

            return Logs[branch];
        }

        public static async Task<string> GetPreviousVersionGuid(string branch, string versionGuid)
        {
            StudioDeployLogs deployLogs = await GetDeployLogs(branch);

            if (deployLogs.LookupFromGuid.ContainsKey(versionGuid))
            {
                StudioDeployLog currentLog = deployLogs.LookupFromGuid[versionGuid];
                int previousVersion = currentLog.Version - 1;

                if (deployLogs.LookupFromVersion.ContainsKey(previousVersion))
                {
                    StudioDeployLog previousLog = deployLogs.LookupFromVersion[previousVersion];
                    return previousLog.VersionGuid;
                }
                else
                {
                    throw new Exception("Could not resolve previous version.");
                }
            }
            else
            {
                throw new Exception("Unknown version guid: " + versionGuid);
            }
        }
    }
}
