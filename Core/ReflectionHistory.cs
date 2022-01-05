using System;
using System.Linq;
using System.Threading.Tasks;

using RobloxDeployHistory;

namespace RobloxApiDumpTool
{
    public static class ReflectionHistory
    {
        public static async Task<DeployLog> FindDeployLog(string branch, string versionGuid)
        {
            var deployLogs = await StudioDeployLogs.Get(branch);

            var result = deployLogs.CurrentLogs_x64
                .Where(log => log.VersionGuid == versionGuid)
                .FirstOrDefault();

            return result;
        }

        public static async Task<DeployLog> GetPreviousVersion(string branch, DeployLog log)
        {
            var deployLogs = await StudioDeployLogs.Get(branch);
            string versionGuid = log.VersionGuid;

            var currentLog = deployLogs.CurrentLogs_x64
                .Where(deployLog => deployLog.VersionGuid == versionGuid)
                .FirstOrDefault();

            if (currentLog == null)
                throw new Exception("Unknown version guid: " + versionGuid);

            var prevLog = deployLogs.CurrentLogs_x64
                .Where(deployLog => deployLog.Version < currentLog.Version)
                .OrderBy(deployLog => deployLog.Changelist)
                .LastOrDefault();

            if (prevLog == null)
                throw new Exception($"Could not resolve previous version for {versionGuid}");

            return prevLog;
        }

        public static async Task<string> GetPreviousVersionGuid(string branch, string versionGuid)
        {
            DeployLog current = await FindDeployLog(branch, versionGuid);
            DeployLog previous = await GetPreviousVersion(branch, current);

            return previous.VersionGuid;
        }
    }
}