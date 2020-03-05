using System.IO;
using System.Net;
using System.Threading.Tasks;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Roblox.Reflection
{
    public class ClientVersionInfo
    {
        public string Version;
        public string Guid;

        public static async Task<ClientVersionInfo> Get(string buildType = "WindowsStudio64", string branch = "roblox")
        {
            string jsonUrl = $"https://clientsettingscdn.{branch}.com/v1/client-version/{buildType}";

            using (WebClient http = new WebClient())
            {
                string jsonData = await http.DownloadStringTaskAsync(jsonUrl);

                using (TextReader reader = new StringReader(jsonData))
                {
                    var jsonReader = new JsonTextReader(reader);
                    var versionData = JObject.Load(jsonReader);

                    var versionInfo = new ClientVersionInfo()
                    {
                        Version = versionData.Value<string>("version"),
                        Guid = versionData.Value<string>("clientVersionUpload")
                    };

                    return versionInfo;
                }
            }
        }
    }
}
