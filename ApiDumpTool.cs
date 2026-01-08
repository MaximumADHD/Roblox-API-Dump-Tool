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

#pragma warning disable IDE1006 // Naming Styles

namespace RobloxApiDumpTool
{
    public partial class ApiDumpTool : Form
    {
        public static RegistryKey VersionRegistry => Program.GetMainRegistryKey("Current Versions");
        private const string API_DUMP_CSS_FILE = "api-dump.css";
        private const string LIVE = Program.LIVE;

        private delegate void StatusDelegate(string msg);
        private delegate string ItemDelegate(ComboBox comboBox);

        private static readonly WebClient http = new WebClient();
        private static TaskCompletionSource<Bitmap> renderFinished;

        private static BuildMetadata buildMetadata;

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

        private Channel getChannel()
        {
            return "LIVE";
            // return getSelectedItem(channel);
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

        public static async Task<DeployLog> GetLastDeployLog(Channel channel)
        {
            var history = await StudioDeployLogs.Get(channel);

            var latestDeploy = history.CurrentLogs_x64
                .OrderBy(log => log.TimeStamp)
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

        public static async Task<string> GetApiDumpFilePath(Channel channel, string versionGuid, bool full, Action<string> setStatus = null)
        {
            string coreBin = GetWorkDirectory();
            string fileName = full ? "Full-API-Dump" : "API-Dump";

            string apiUrl = $"{channel.BaseUrl}/{versionGuid}-{fileName}.json";
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

        public static async Task<string> GetApiDumpFilePath(Channel channel, int versionId, bool full, Action<string> setStatus = null)
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
                var logs = await StudioDeployLogs.Get(channel);

                var deployLog = logs.CurrentLogs_x64
                    .Where(log => log.Version == versionId)
                    .OrderBy(log => log.Changelist)
                    .LastOrDefault();

                if (deployLog == null)
                    throw new Exception("Unknown version id: " + versionId);

                string versionGuid = deployLog.VersionGuid;
                return await GetApiDumpFilePath(channel, versionGuid, full, setStatus);
            }
        }

        public static async Task<string> GetApiDumpFilePath(Channel channel, bool full, Action<string> setStatus = null, bool fetchPrevious = false)
        {
            setStatus?.Invoke("Checking for update...");
            string versionGuid = await GetVersion(channel);

            if (fetchPrevious)
                versionGuid = await ReflectionHistory.GetPreviousVersionGuid(channel, versionGuid);

            string file = await GetApiDumpFilePath(channel, versionGuid, full, setStatus);

            if (fetchPrevious)
                channel += "-prev";

            VersionRegistry.SetValue(channel, versionGuid);
            clearOldVersionFiles();

            return file;
        }

        private async Task<string> getApiDumpFilePath(Channel channel, bool full, bool fetchPrevious = false)
        {
            return await GetApiDumpFilePath(channel, full, setStatus, fetchPrevious);
        }

        private void channel_SelectedIndexChanged(object sender, EventArgs e)
        {
            Channel channel = getChannel();

            if (channel.Equals(LIVE))
                compareVersions.Text = "Compare Previous Version";
            else
                compareVersions.Text = "Compare to Production";

            Program.MainRegistry.SetValue("LastSelectedChannel", channel);
            updateEnabledStates();
        }

        private async void viewApiDumpClassic_Click(object sender, EventArgs e)
        {
            await lockWindowAndRunTask(async () =>
            {
                var channel = getChannel();
                string format = getApiDumpFormat();
                string apiFilePath = await getApiDumpFilePath(channel, fullDump.Checked);

                if (format == "JSON")
                {
                    Process.Start(apiFilePath);
                    return;
                }

                var api = new ReflectionDatabase(apiFilePath);
                var dumper = new ReflectionDumper(api);

                string result;

                if (format == "HTML" || format == "PNG")
                    result = dumper.DumpApi(ReflectionDumper.DumpUsingHtml, PostProcessHtml);
                else
                    result = dumper.DumpApi(ReflectionDumper.DumpUsingTxt);

                FileInfo info = new FileInfo(apiFilePath);
                string directory = info.DirectoryName;

                string resultPath = Path.Combine(directory, channel + "-api-dump." + format.ToLower());
                writeAndViewFile(resultPath, result);
            });
        }

        private async void compareVersions_Click(object sender, EventArgs e)
        {
            await lockWindowAndRunTask(async () =>
            {
                Channel newChannel = getChannel();
                bool fetchPrevious = newChannel.Equals(LIVE);
                bool full = fullDump.Checked;

                string newApiFilePath = await getApiDumpFilePath(newChannel, full);
                string oldApiFilePath = await getApiDumpFilePath(LIVE, full, fetchPrevious);

                var latestLog = await GetLastDeployLog(newChannel);
                string version = latestLog.VersionId;

                setStatus($"Reading the {(fetchPrevious ? "Previous" : "Production")} API...");
                var oldApi = new ReflectionDatabase(oldApiFilePath, LIVE, version);

                setStatus($"Reading the {(fetchPrevious ? "Production" : "New")} API...");
                var newApi = new ReflectionDatabase(newApiFilePath, newChannel, version);
                
                setStatus("Comparing APIs...");

                string format = getApiDumpFormat();
                string result = ReflectionDiffer.CompareDatabases(oldApi, newApi, format);

                if (result.Length > 0)
                {
                    FileInfo info = new FileInfo(newApiFilePath);
                    string dirName = info.DirectoryName;

                    string fileBase = Path.Combine(dirName, $"{newChannel}-diff.");
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
                }

                clearOldVersionFiles();
            });
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

        private async Task initVersionCache()
        {
            await lockWindowAndRunTask(async () =>
            {
                string[] channels = channel.Items.Cast<string>().ToArray();
                setStatus("Initializing version cache...");

                foreach (string channelName in channels)
                {
                    string versionGuid = await GetVersion(channelName);
                    VersionRegistry.SetValue(channelName, versionGuid);
                }

                Program.MainRegistry.SetValue("InitializedChannels", true);
            });
        }

        private async void ApiDumpTool_Load(object sender, EventArgs e)
        {
            WebRequest.DefaultWebProxy = null;

            if (!Program.GetRegistryBool("InitializedChannels"))
            {
                await initVersionCache();
                clearOldVersionFiles();
            }

            loadSelectedIndex(channel, "LastSelectedChannel");
            loadSelectedIndex(apiDumpFormat, "PreferredFormat");
        }

        private void apiDumpFormat_SelectedIndexChanged(object sender, EventArgs e)
        {
            string format = getApiDumpFormat();
            Program.MainRegistry.SetValue("PreferredFormat", format);

            updateEnabledStates();
        }

        private async void channel_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode != Keys.Enter)
                return;

            Channel input = channel.Text;
            e.SuppressKeyPress = true;

            foreach (var item in channel.Items)
            {
                Channel old = item.ToString();

                if (old.Name == input.Name)
                {
                    channel.SelectedItem = item;
                    return;
                }
            }

            try
            {
                var logs = await StudioDeployLogs.Get(input);

                if (logs.CurrentLogs_x64.Any())
                {
                    var addItem = new Action(() =>
                    {
                        var index = channel.Items.Add(channel.Text);
                        channel.SelectedIndex = index;
                    });

                    Invoke(addItem);
                    return;
                }

                throw new Exception("No channels to work with!");
            }
            catch
            {
                var reset = new Action(() => channel.SelectedIndex = 0);

                MessageBox.Show
                (
                    $"Channel '{input}' had no valid data on Roblox's CDN!",
                    "Invalid channel!",

                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error
                );

                Invoke(reset);
            }
        }
    }
}
