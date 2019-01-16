using System;
using System.Threading.Tasks;

namespace Roblox.Reflection
{
    public static class ReflectionHistory
    {
        public static async Task<DeployLog> FindDeployLog(string branch, string versionGuid)
        {
            StudioDeployLogs deployLogs = await StudioDeployLogs.GetDeployLogs(branch);
            DeployLog result = null;

            if (deployLogs.LookupFromGuid.ContainsKey(versionGuid))
                result = deployLogs.LookupFromGuid[versionGuid];

            return result;
        }

        public static async Task<DeployLog> GetPreviousVersion(string branch, DeployLog log)
        {
            StudioDeployLogs deployLogs = await StudioDeployLogs.GetDeployLogs(branch);
            string versionGuid = log.VersionGuid;

            if (deployLogs.LookupFromGuid.ContainsKey(versionGuid))
            {
                DeployLog currentLog = deployLogs.LookupFromGuid[versionGuid];
                int previousVersion = currentLog.Version - 1;

                DeployLog previousLog = null;

                if (deployLogs.LookupFromVersion.ContainsKey(previousVersion))
                    previousLog = deployLogs.LookupFromVersion[previousVersion];
                else
                    Console.WriteLine("Could not resolve previous version for {0}", versionGuid);

                return previousLog;
            }
            else
            {
                throw new Exception("Unknown version guid: " + versionGuid);
            }
        }

        public static async Task<string> GetPreviousVersionGuid(string branch, string versionGuid)
        {
            DeployLog current = await FindDeployLog(branch, versionGuid);
            DeployLog previous = await GetPreviousVersion(branch, current);

            return previous.VersionGuid;
        }
    }
}