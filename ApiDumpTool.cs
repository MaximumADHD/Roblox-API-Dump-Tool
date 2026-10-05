using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

using RobloxDeployHistory;
using Microsoft.Win32;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;

#pragma warning disable IDE1006 // Naming Styles

namespace RobloxApiDumpTool
{
    public enum ApiDumpSchema
    {
        V1_Partial,
        V1_Full,
        V2,
    }

    public partial class ApiDumpTool : Form
    {
        public static RegistryKey VersionRegistry => Program.GetMainRegistryKey("Current Versions");
        private const string API_DUMP_CSS_FILE = "api-dump.css";
        private const string LIVE = Program.LIVE;
        private const string CDN_COMMON_BASE_URL = "https://setup.rbxcdn.com/channel/common";

        private delegate void StatusDelegate(string msg);
        private delegate string ItemDelegate(ComboBox comboBox);

        private static readonly WebClient http = new WebClient();
        private static TaskCompletionSource<Bitmap> renderFinished;

        private static BuildMetadata buildMetadata;

        private static readonly IReadOnlyDictionary<ApiDumpSchema, string> SchemaMap = new Dictionary<ApiDumpSchema, string>()
        {
            { ApiDumpSchema.V1_Partial, "API-Dump" },
            { ApiDumpSchema.V1_Full, "Full-API-Dump" },
            { ApiDumpSchema.V2, "API-Dump-2" }
        };

        private class ChannelResolution
        {
            public Channel Channel;
            public string VersionGuid;
            public string VersionId;
            public bool IsEarlyAccess;
        }

        public ApiDumpTool()
        {
            InitializeComponent();
        }

        private string getSelectedItem(ComboBox comboBox)
        {
            object result;

            if (InvokeRequired)
            {
                ItemDelegate itemDelegate = new ItemDelegate(getSelectedItem);
                result = Invoke(itemDelegate, comboBox);
            }
            else
            {
                result = comboBox.SelectedItem;
            }

            return result?.ToString() ?? LIVE;
        }

        private string getChannelName()
        {
            string text = channel.Text.Trim();
            return string.IsNullOrEmpty(text) ? LIVE : text;
        }

        private string getChannelToken()
        {
            return channelToken.Text.Trim();
        }

        private string getApiDumpFormat()
        {
            return getSelectedItem(apiDumpFormat);
        }

        private void loadSelectedIndex(ComboBox comboBox, string registryKey)
        {
            string value = Program.GetRegistryString(registryKey);
            comboBox.SelectedIndex = Math.Max(0, comboBox.Items.IndexOf(value));
        }

        private void updateEnabledStates()
        {
            try
            {
                string format = getApiDumpFormat();
                viewApiDump.Enabled = (format != "PNG");
                compareVersions.Enabled = true;
            }
            catch
            {
                // ¯\_(ツ)_/¯
            }
        }

        public static async Task<DeployLog> GetLastDeployLog(string channel)
        {
            var history = await StudioDeployLogs.Get(true, channel);

            var latestDeploy = history.CurrentLogs
                .OrderBy(log => log.CommitId)
                .Last();

            return latestDeploy;
        }

        public static async Task<string> GetVersion(Channel channel)
        {
            var log = await GetLastDeployLog(channel);
            return log.VersionGuid;
        }

        private void setStatus(string msg = "")
        {
            if (InvokeRequired)
            {
                StatusDelegate status = new StatusDelegate(setStatus);
                Invoke(status, msg);
            }
            else
            {
                status.Text = "Status: " + msg;
                status.Refresh();
            }
        }

        private async Task lockWindowAndRunTask(Func<Task> task)
        {
            Enabled = false;
            UseWaitCursor = true;

            await Task.Run(task);

            Enabled = true;
            UseWaitCursor = false;

            setStatus("Ready!");
        }

        private static bool writeFile(string path, string contents)
        {
            if (!File.Exists(path) || File.ReadAllText(path, Encoding.UTF8) != contents)
            {
                File.WriteAllText(path, contents, Encoding.UTF8);
                return true;
            }

            return false;
        }

        private static void writeAndViewFile(string path, string contents)
        {
            writeFile(path, contents);
            Process.Start(path);
        }

        private static Bitmap renderApiDumpImpl(WebBrowser browser)
        {
            var body = browser.Document.Body;
            body.Style = "zoom:150%";

            const int extraWidth = 21;
            Rectangle size = body.ScrollRectangle;

            int width = size.Width + extraWidth;
            browser.Width = width + 8;

            int height = size.Height;
            browser.Height = height + 24;

            Bitmap apiRender = new Bitmap(width, height);
            browser.DrawToBitmap(apiRender, size);

            // Fill in some extra space on the right that we missed.
            using (Graphics graphics = Graphics.FromImage(apiRender))
            {
                Color backColor = apiRender.GetPixel(0, 0);

                using (Brush brush = new SolidBrush(backColor))
                {
                    Rectangle fillArea = new Rectangle
                    (
                        width - extraWidth, 0,
                        extraWidth, height
                    );

                    graphics.FillRectangle(brush, fillArea);
                }
            }

            // Apply some random noise and transparency to the edges.
            // Doing this so websites like Twitter can't force the image
            // to use lossy compression. Its a nice little hack :)

            Random rng = new Random();

            var addNoise = new Action<int, int>((x, y) =>
            {
                const int alpha = (224 << 24);

                int lum = 10 + (int)(rng.NextDouble() * 30);
                int argb = alpha | (lum << 16) | (lum << 8) | lum;

                Color pixel = Color.FromArgb(argb);
                apiRender.SetPixel(x, y, pixel);
            });

            for (int x = 0; x < width; x++)
            {
                addNoise(x, 0);
                addNoise(x, height - 1);
            }

            for (int y = 0; y < height; y++)
            {
                addNoise(0, y);
                addNoise(width - 1, y);
            }

            return apiRender;
        }

        private static void onDocumentComplete(object sender, WebBrowserDocumentCompletedEventArgs e)
        {
            var browser = sender as WebBrowser;

            Bitmap image = renderApiDumpImpl(browser);
            renderFinished.SetResult(image);

            browser.Dispose();
            Application.ExitThread();
        }

        public static string GetWorkDirectory()
        {
            string localAppData = Environment.GetEnvironmentVariable("LocalAppData");

            string workDir = Path.Combine(localAppData, "RobloxApiDumpFiles");
            Directory.CreateDirectory(workDir);

            return workDir;
        }

        public static string PostProcessHtml(string result, string workDir = "")
        {
            // Preload the API Dump CSS file.
            if (workDir == "")
                workDir = GetWorkDirectory();

            string apiDumpCss = Path.Combine(workDir, API_DUMP_CSS_FILE);
            File.WriteAllText(apiDumpCss, Properties.Resources.ApiDumpStyler);

            return "<head>\n"
                 + "\t<link rel=\"stylesheet\" href=\"" + API_DUMP_CSS_FILE + "\">\n"
                 + "</head>\n\n"
                 + result.Trim();
        }

        public static async Task<Bitmap> RenderApiDump(string htmlFilePath)
        {
            var docReady = new WebBrowserDocumentCompletedEventHandler(onDocumentComplete);
            string fileUrl = "file://" + htmlFilePath.Replace('\\', '/');

            Thread renderThread = new Thread(() =>
            {
                var renderer = new WebBrowser()
                {
                    Url = new Uri(fileUrl),
                    ScrollBarsEnabled = false,
                };

                renderer.DocumentCompleted += docReady;
                Application.Run();
            });

            renderFinished = new TaskCompletionSource<Bitmap>();

            renderThread.SetApartmentState(ApartmentState.STA);
            renderThread.Start();

            await renderFinished.Task;
            var apiRender = renderFinished.Task.Result;

            return apiRender;
        }

        public static async Task<string> GetApiDumpFilePath(Channel channel, string versionGuid, ApiDumpSchema schema, Action<string> setStatus = null, bool useLegacyChannelUrl = false)
        {
            string coreBin = GetWorkDirectory();
            string fileName = SchemaMap[schema];

            string baseUrl = useLegacyChannelUrl ? channel.BaseUrl : CDN_COMMON_BASE_URL;
            string apiUrl = $"{baseUrl}/{versionGuid}-{fileName}.json";
            string file = Path.Combine(coreBin, $"{versionGuid}-{fileName}.json");

            if (!File.Exists(file))
            {
                setStatus?.Invoke("Grabbing API Dump for " + channel);
                string apiDump = await http.DownloadStringTaskAsync(apiUrl);
                File.WriteAllText(file, apiDump);
            }
            else
            {
                setStatus?.Invoke("Already up to date!");
            }

            return file;
        }

        public static async Task<BuildMetadata> GetBuildMetadata()
        {
            if (buildMetadata == null)
                buildMetadata = await BuildArchive.GetBuildMetadata();

            return buildMetadata;
        }

        public static async Task<string> GetApiDumpFilePath(Channel channel, int versionId, ApiDumpSchema format, Action<string> setStatus = null, bool useLegacyChannelUrl = false)
        {
            if (versionId < 350)
            {
                setStatus?.Invoke("Fetching build metadata...");
                await GetBuildMetadata();

                var buildId = versionId.ToString();
                setStatus?.Invoke("Finding version guid for " + versionId);

                var buildInfo = buildMetadata.Builds
                    .Where(build => build.Version
                        .Substring(2)
                        .StartsWith(buildId)
                    ).OrderBy(build => build.Date.Ticks)
                     .Last();

                var workDir = GetWorkDirectory();
                string path = Path.Combine(workDir, $"{buildInfo.Guid}.json");

                if (!File.Exists(path))
                {
                    setStatus?.Invoke("Fetching API Dump...");
                    string json = await BuildArchive.GetFile(buildInfo.Guid, "API-Dump.json");
                    File.WriteAllText(path, json);
                }

                return path;
            }
            else
            {
                setStatus?.Invoke("Fetching deploy logs for " + channel);
                var logs = await StudioDeployLogs.Get(true, channel);

                var deployLog = logs.CurrentLogs
                    .Where(log => log.Version == versionId)
                    .OrderBy(log => log.CommitId)
                    .LastOrDefault();

                if (deployLog == null)
                    throw new Exception("Unknown version id: " + versionId);

                string versionGuid = deployLog.VersionGuid;
                return await GetApiDumpFilePath(channel, versionGuid, format, setStatus, useLegacyChannelUrl);
            }
        }

        public static async Task<string> GetApiDumpFilePath(Channel channel, ApiDumpSchema format, Action<string> setStatus = null, bool fetchPrevious = false, bool useLegacyChannelUrl = false)
        {
            setStatus?.Invoke("Checking for update...");
            string versionGuid = await GetVersion(channel);

            if (fetchPrevious)
                versionGuid = await ReflectionHistory.GetPreviousVersionGuid(channel, versionGuid);

            string file = await GetApiDumpFilePath(channel, versionGuid, format, setStatus, useLegacyChannelUrl);

            if (fetchPrevious)
                channel += "-prev";

            VersionRegistry.SetValue(channel, versionGuid);
            clearOldVersionFiles();

            return file;
        }

        private async Task<string> getApiDumpFilePath(Channel channel, ApiDumpSchema schema, bool fetchPrevious = false)
        {
            return await GetApiDumpFilePath(channel, schema, setStatus, fetchPrevious);
        }

        private void channel_TextChanged(object sender, EventArgs e)
        {
            Channel resolvedChannel = getChannelName();
            compareVersions.Text = resolvedChannel.Equals(LIVE) ? "Compare Previous Version" : "Compare to Production";
        }


        // Resolves a channel either through the public API or early access API,
        // depending on whether or not a token was provided.
        private async Task<ChannelResolution> resolveChannel(string channelName, string token)
        {
            if (string.IsNullOrEmpty(channelName))
                channelName = LIVE;

            var channelObj = new Channel(channelName);
            bool isEarlyAccess = !string.IsNullOrEmpty(token);

            setStatus($"Resolving channel '{channelName}'...");

            if (isEarlyAccess)
            {
                var versionInfo = await ClientVersionInfo.Get(channelName, "WindowsStudio64", token);

                if (!versionInfo.Success)
                {
                    string reason = versionInfo.Errors.FirstOrDefault()?.Message ?? "the channel name or token was rejected";
                    throw new Exception(reason);
                }

                return new ChannelResolution
                {
                    Channel = channelObj,
                    VersionGuid = versionInfo.VersionGuid,
                    VersionId = versionInfo.Version,
                    IsEarlyAccess = true,
                };
            }
            else
            {
                string url = "https://clientsettingscdn.roblox.com/v2/client-version/WindowsStudio64";

                if (!channelObj.Equals(LIVE))
                    url += $"/channel/{channelName}";

                string json = await http.DownloadStringTaskAsync(url);
                var data = JObject.Parse(json);

                return new ChannelResolution
                {
                    Channel = channelObj,
                    VersionGuid = data.Value<string>("clientVersionUpload"),
                    VersionId = data.Value<string>("version"),
                    IsEarlyAccess = false,
                };
            }
        }

        private async Task<string> downloadDump(ChannelResolution resolved, ApiDumpSchema schema, string earlyAccessTempDir)
        {
            if (resolved.IsEarlyAccess)
                return await downloadEarlyAccessDump(resolved.VersionGuid, schema, earlyAccessTempDir);

            return await GetApiDumpFilePath(resolved.Channel, resolved.VersionGuid, schema, setStatus);
        }

        private async void viewApiDumpClassic_Click(object sender, EventArgs e)
        {
            string channelName = getChannelName();
            string token = getChannelToken();
            bool isEarlyAccess = !string.IsNullOrEmpty(token);

            // Early access channels can be deleted or access can be revoked at any time,
            // so everything downloaded lives in its own folder, and nothing about it is ever written to the registry.
            string tempDir = isEarlyAccess
                ? Path.Combine(Path.GetTempPath(), "RobloxApiDumpTool-EarlyAccess-" + Guid.NewGuid().ToString("N"))
                : null;

            try
            {
                await lockWindowAndRunTask(async () =>
                {
                    if (isEarlyAccess)
                        Directory.CreateDirectory(tempDir);

                    string format = getApiDumpFormat();
                    var schema = fullDump.Checked ? ApiDumpSchema.V1_Full : ApiDumpSchema.V1_Partial;

                    var resolved = await resolveChannel(channelName, token);
                    string apiFilePath = await downloadDump(resolved, schema, tempDir);

                    if (format == "JSON")
                    {
                        Process.Start(apiFilePath);
                        return;
                    }

                    var api = new ReflectionDatabase(apiFilePath, schema)
                    {
                        Channel = resolved.Channel,
                        Version = resolved.VersionId,
                    };

                    var dumper = new ReflectionDumper(api);

                    string apiFilePath2 = await downloadDump(resolved, ApiDumpSchema.V2, tempDir);
                    api.MungeV2(apiFilePath2);

                    if (isEarlyAccess)
                    {
                        deleteFileQuietly(apiFilePath);
                        deleteFileQuietly(apiFilePath2);
                    }

                    string result;

                    if (format == "HTML" || format == "PNG")
                        result = dumper.DumpApi(ReflectionDumper.DumpUsingHtml, PostProcessHtml);
                    else
                        result = dumper.DumpApi(ReflectionDumper.DumpUsingTxt);

                    string directory;

                    if (isEarlyAccess)
                    {
                        directory = tempDir;

                        if (format == "HTML" || format == "PNG")
                            File.WriteAllText(Path.Combine(tempDir, API_DUMP_CSS_FILE), Properties.Resources.ApiDumpStyler);
                    }
                    else
                    {
                        directory = new FileInfo(apiFilePath).DirectoryName;
                    }

                    string resultPath = Path.Combine(directory, resolved.Channel + "-api-dump." + format.ToLower());
                    writeAndViewFile(resultPath, result);
                });
            }
            catch (Exception ex)
            {
                Enabled = true;
                UseWaitCursor = false;
                setStatus("Ready!");

                if (isEarlyAccess)
                    deleteDirectoryQuietly(tempDir);

                MessageBox.Show
                (
                    $"Could not view the API dump for that channel:\n{ex.Message}",
                    "Lookup failed",

                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error
                );
            }
            finally
            {
                if (isEarlyAccess)
                    channelToken.Clear();
            }
        }

        private async void compareVersions_Click(object sender, EventArgs e)
        {
            string channelName = getChannelName();
            string token = getChannelToken();
            bool isEarlyAccess = !string.IsNullOrEmpty(token);

            string tempDir = isEarlyAccess
                ? Path.Combine(Path.GetTempPath(), "RobloxApiDumpTool-EarlyAccess-" + Guid.NewGuid().ToString("N"))
                : null;

            try
            {
                await lockWindowAndRunTask(async () =>
                {
                    if (isEarlyAccess)
                        Directory.CreateDirectory(tempDir);

                    bool full = fullDump.Checked;
                    var schema = full ? ApiDumpSchema.V1_Full : ApiDumpSchema.V1_Partial;

                    var resolved = await resolveChannel(channelName, token);
                    bool fetchPrevious = !isEarlyAccess && resolved.Channel.Equals(LIVE);

                    string oldApiFilePath = await getApiDumpFilePath(LIVE, schema, fetchPrevious);
                    string oldApiFilePath2 = await getApiDumpFilePath(LIVE, ApiDumpSchema.V2, fetchPrevious);

                    string newApiFilePath = await downloadDump(resolved, schema, tempDir);
                    string newApiFilePath2 = await downloadDump(resolved, ApiDumpSchema.V2, tempDir);

                    setStatus($"Reading the {(fetchPrevious ? "Previous" : "Production")} API...");

                    var oldApi = new ReflectionDatabase(oldApiFilePath, schema)
                    {
                        Channel = LIVE,
                        Version = resolved.VersionId,
                    };

                    oldApi.MungeV2(oldApiFilePath2);
                    setStatus($"Reading the {(fetchPrevious ? "Production" : "New")} API...");

                    var newApi = new ReflectionDatabase(newApiFilePath, schema)
                    {
                        Channel = resolved.Channel,
                        Version = resolved.VersionId,
                    };

                    newApi.MungeV2(newApiFilePath2);

                    if (isEarlyAccess)
                    {
                        deleteFileQuietly(newApiFilePath);
                        deleteFileQuietly(newApiFilePath2);
                    }

                    setStatus("Comparing APIs...");

                    string format = getApiDumpFormat();
                    string result = ReflectionDiffer.CompareDatabases(oldApi, newApi, format);

                    string dirName = isEarlyAccess ? tempDir : new FileInfo(newApiFilePath).DirectoryName;

                    if (result.Length > 0)
                    {
                        if (isEarlyAccess && (format == "HTML" || format == "PNG"))
                        {
                            // Writing the CSS to the early-access channel's own folder so the styling is applied properly
                            File.WriteAllText(Path.Combine(tempDir, API_DUMP_CSS_FILE), Properties.Resources.ApiDumpStyler);
                        }

                        string fileBase = Path.Combine(dirName, $"{resolved.Channel}-diff.");
                        string filePath = fileBase + format.ToLower();

                        if (format == "PNG")
                        {
                            string htmlPath = $"{fileBase}.html";

                            writeFile(htmlPath, result);
                            setStatus("Rendering Image...");

                            Bitmap apiRender = await RenderApiDump(htmlPath);
                            apiRender.Save(filePath);

                            Process.Start(filePath);
                        }
                        else
                        {
                            writeAndViewFile(filePath, result);
                        }
                    }
                    else
                    {
                        MessageBox.Show("No differences were found!", "Well, this is awkward...", MessageBoxButtons.OK, MessageBoxIcon.Error);

                        if (isEarlyAccess)
                            deleteDirectoryQuietly(tempDir);
                    }

                    if (!isEarlyAccess)
                        clearOldVersionFiles();
                });
            }
            catch (Exception ex)
            {
                Enabled = true;
                UseWaitCursor = false;
                setStatus("Ready!");

                if (isEarlyAccess)
                    deleteDirectoryQuietly(tempDir);

                MessageBox.Show
                (
                    $"Could not compare that channel:\n{ex.Message}",
                    "Comparison failed",

                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error
                );
            }
            finally
            {
                if (isEarlyAccess)
                    channelToken.Clear();
            }
        }

        private static void clearOldVersionFiles()
        {
            string workDir = GetWorkDirectory();

            string[] activeVersions = VersionRegistry.GetValueNames()
                .Select(channel => Program.GetRegistryString(VersionRegistry, channel))
                .ToArray();

            string[] oldFiles = Directory.GetFiles(workDir, "version-*.json")
                .Select(file => new FileInfo(file))
                .Where(fileInfo => !activeVersions.Contains(fileInfo.Name.Substring(0, 24)))
                .Select(fileInfo => fileInfo.FullName)
                .ToArray();

            foreach (string oldFile in oldFiles)
            {
                try
                {
                    File.Delete(oldFile);
                }
                catch
                {
                    Console.WriteLine("Could not delete file {0}", oldFile);
                }
            }
        }

        private void ApiDumpTool_Load(object sender, EventArgs e)
        {
            WebRequest.DefaultWebProxy = null;

            channel_TextChanged(this, EventArgs.Empty);
            loadSelectedIndex(apiDumpFormat, "PreferredFormat");
        }

        private void apiDumpFormat_SelectedIndexChanged(object sender, EventArgs e)
        {
            string format = getApiDumpFormat();
            Program.MainRegistry.SetValue("PreferredFormat", format);

            updateEnabledStates();
        }

        private static async Task<string> downloadEarlyAccessDump(string versionGuid, ApiDumpSchema schema, string tempDir)
        {
            string fileName = SchemaMap[schema];
            string apiUrl = $"{CDN_COMMON_BASE_URL}/{versionGuid}-{fileName}.json";
            string file = Path.Combine(tempDir, $"{versionGuid}-{fileName}.json");

            string apiDump = await http.DownloadStringTaskAsync(apiUrl);
            File.WriteAllText(file, apiDump);

            return file;
        }

        private static void deleteFileQuietly(string path)
        {
            try
            {
                if (File.Exists(path))
                    File.Delete(path);
            }
            catch
            {
                // Best-effort cleanup only.
            }
        }

        private static void deleteDirectoryQuietly(string path)
        {
            try
            {
                if (Directory.Exists(path))
                    Directory.Delete(path, true);
            }
            catch
            {
                // Best-effort cleanup only.
            }
        }
    }
}
