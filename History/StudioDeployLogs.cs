using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Roblox.Reflection
{
    public class StudioDeployLogs
    {
        private const string LogPattern = "New Studio (version-[a-f\\d]+) at \\d+/\\d+/\\d+ \\d+:\\d+:\\d+ [A,P]M, file version: (\\d+), (\\d+), (\\d+), (\\d+)";
        public const int EarliestVersion = 349; // The earliest version of studio where the API Dump is available on Roblox's setup servers.

        public string Branch { get; private set; }

        public Dictionary<string, DeployLog> LookupFromGuid;
        public Dictionary<int, DeployLog> LookupFromVersion;

        private static Dictionary<string, StudioDeployLogs> LogCache = new Dictionary<string, StudioDeployLogs>();
        private string LastDeployHistory = "";

        private StudioDeployLogs(string branch)
        {
            LookupFromGuid = new Dictionary<string, DeployLog>();
            LookupFromVersion = new Dictionary<int, DeployLog>();

            Branch = branch;
            LogCache[branch] = this;
        }

        private void Add(DeployLog deployLog)
        {
            if (deployLog.Version >= EarliestVersion)
            {
                // Add by version info
                if (deployLog.Patch == 0)
                    LookupFromVersion[deployLog.Version] = deployLog;

                // Add by version guid
                LookupFromGuid[deployLog.VersionGuid] = deployLog;
            }
        }

        private void UpdateLogs(string deployHistory)
        {
            MatchCollection matches = Regex.Matches(deployHistory, LogPattern);

            foreach (Match match in matches)
            {
                string[] data = match.Groups.Cast<Group>()
                    .Select(group => group.Value)
                    .Where(value => value.Length != 0)
                    .ToArray();

                DeployLog deployLog = new DeployLog();
                deployLog.VersionGuid = data[1];

                int.TryParse(data[2], out deployLog.MajorRev);
                int.TryParse(data[3], out deployLog.Version);
                int.TryParse(data[4], out deployLog.Patch);
                int.TryParse(data[5], out deployLog.Changelist);

                Add(deployLog);
            }
        }

        public static async Task<StudioDeployLogs> GetDeployLogs(string branch)
        {
            StudioDeployLogs logs = null;

            if (LogCache.ContainsKey(branch))
                logs = LogCache[branch];
            else
                logs = new StudioDeployLogs(branch);

            string deployHistory = await HistoryCache.GetDeployHistory(branch);

            if (logs.LastDeployHistory != deployHistory)
            {
                logs.LastDeployHistory = deployHistory;
                logs.UpdateLogs(deployHistory);
            }

            return logs;
        }
    }
}
