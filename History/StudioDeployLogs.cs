using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Roblox.Reflection
{
    public class StudioDeployLogs
    {
        private const string LogPattern = "New Studio64 (version-[a-f\\d]+) at \\d+/\\d+/\\d+ \\d+:\\d+:\\d+ [A,P]M, file version: (\\d+), (\\d+), (\\d+), (\\d+)";
        private const int EarliestChangelist = 338804; // The earliest acceptable changelist of Roblox Studio, with explicit 64-bit versions declared via DeployHistory.txt
        
        public string Branch { get; private set; }

        public Dictionary<string, DeployLog> LookupFromGuid { get; private set; }
        public Dictionary<int, DeployLog> LookupFromVersion { get; private set; }

        private static Dictionary<string, StudioDeployLogs> LogCache = new Dictionary<string, StudioDeployLogs>();
        private string LastDeployHistory = "";

        private static readonly CultureInfo invariant = CultureInfo.InvariantCulture;

        private StudioDeployLogs(string branch)
        {
            LookupFromGuid = new Dictionary<string, DeployLog>();
            LookupFromVersion = new Dictionary<int, DeployLog>();

            Branch = branch;
            LogCache[branch] = this;
        }

        private void Add(DeployLog deployLog)
        {
            if (deployLog.Changelist >= EarliestChangelist)
            {
                // Add by version info
                int version = deployLog.Version;
                int currentPatch = -1;

                if (LookupFromVersion.ContainsKey(version))
                    currentPatch = LookupFromVersion[version].Patch;

                if (deployLog.Patch > currentPatch)
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

                DeployLog deployLog = new DeployLog()
                {
                    VersionGuid = data[1],
                    MajorRev    = int.Parse(data[2], invariant),
                    Version     = int.Parse(data[3], invariant),
                    Patch       = int.Parse(data[4], invariant),
                    Changelist  = int.Parse(data[5], invariant)
                };

                Add(deployLog);
            }
        }

        public static async Task<StudioDeployLogs> GetDeployLogs(string branch)
        {
            StudioDeployLogs logs;

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
