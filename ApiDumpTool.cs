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

namespace RobloxApiDumpTool
{
    public partial class ApiDumpTool : Form
    {
        public static RegistryKey VersionRegistry => Program.GetMainRegistryKey("Current Versions");
        private const string API_DUMP_CSS_FILE = "api-dump.css";

        private delegate void StatusDelegate(string msg);
        private delegate string ItemDelegate(ComboBox comboBox);

        private static WebClient http = new WebClient();
        private static TaskCompletionSource<Bitmap> renderFinished;

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

            return result?.ToString() ?? "roblox";
        }

        private string getBranch()
        {
            return getSelectedItem(branch);
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
                compareVersions.Enabled = (format != "JSON");
            }
            catch
            {
                // ¯\_(ツ)_/¯
            }
        }

        public static async Task<DeployLog> GetLastDeployLog(string branch)
        {
            var history = await StudioDeployLogs.Get(branch);

            var latestDeploy = history.CurrentLogs_x64
                .OrderBy(log => log.Changelist)
                .Last();

            return latestDeploy;
        }

        public static async Task<string> GetVersion(string branch)
        {
            var log = await GetLastDeployLog(branch);
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

        public static async Task<string> GetApiDumpFilePath(string branch, string versionGuid, Action<string> setStatus = null)
        {
            string apiUrl = $"https://s3.amazonaws.com/setup.{branch}.com/{versionGuid}-API-Dump.json";
            
            string coreBin = GetWorkDirectory();
            string file = Path.Combine(coreBin, versionGuid + ".json");

            if (!File.Exists(file))
            {
                setStatus?.Invoke("Grabbing API Dump for " + branch);
                string apiDump = await http.DownloadStringTaskAsync(apiUrl);
                File.WriteAllText(file, apiDump);
            }
            else
            {
                setStatus?.Invoke("Already up to date!");
            }

            return file;
        }

        public static async Task<string> GetApiDumpFilePath(string branch, int versionId, Action<string> setStatus = null)
        {
            setStatus?.Invoke("Fetching deploy logs for " + branch);
            var logs = await StudioDeployLogs.Get(branch);

            var deployLog = logs.CurrentLogs_x64
                .Where(log => log.Version == versionId)
                .OrderBy(log => log.Changelist)
                .LastOrDefault();

            if (deployLog == null)
                throw new Exception("Unknown version id: " + versionId);

            string versionGuid = deployLog.VersionGuid;
            return await GetApiDumpFilePath(branch, versionGuid, setStatus);
        }

        public static async Task<string> GetApiDumpFilePath(string branch, Action<string> setStatus = null, bool fetchPrevious = false)
        {
            setStatus?.Invoke("Checking for update...");
            string versionGuid = await GetVersion(branch);

            if (fetchPrevious)
                versionGuid = await ReflectionHistory.GetPreviousVersionGuid(branch, versionGuid);

            string file = await GetApiDumpFilePath(branch, versionGuid, setStatus);

            if (fetchPrevious)
                branch += "-prev";

            VersionRegistry.SetValue(branch, versionGuid);
            clearOldVersionFiles();

            return file;
        }

        private async Task<string> getApiDumpFilePath(string branch, bool fetchPrevious = false)
        {
            return await GetApiDumpFilePath(branch, setStatus, fetchPrevious);
        }

        private void branch_SelectedIndexChanged(object sender, EventArgs e)
        {
            string branch = getBranch();

            if (branch == "roblox")
                compareVersions.Text = "Compare Previous Version";
            else
                compareVersions.Text = "Compare to Production";

            Program.MainRegistry.SetValue("LastSelectedBranch", branch);
            updateEnabledStates();
        }

        private async void viewApiDumpClassic_Click(object sender, EventArgs e)
        {
            await lockWindowAndRunTask(async () =>
            {
                string branch = getBranch();
                string format = getApiDumpFormat();

                string apiFilePath = await getApiDumpFilePath(branch);

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

                string resultPath = Path.Combine(directory, branch + "-api-dump." + format.ToLower());
                writeAndViewFile(resultPath, result);
            });
        }

        private async void compareVersions_Click(object sender, EventArgs e)
        {
            await lockWindowAndRunTask(async () =>
            {
                string newBranch = getBranch();
                bool fetchPrevious = (newBranch == "roblox");

                string newApiFilePath = await getApiDumpFilePath(newBranch);
                string oldApiFilePath = await getApiDumpFilePath("roblox", fetchPrevious);

                setStatus("Reading the " + (fetchPrevious ? "Previous" : "Production") + " API...");

                ReflectionDatabase oldApi = new ReflectionDatabase(oldApiFilePath);
                oldApi.Branch = fetchPrevious ? "roblox-prev" : "roblox";

                setStatus("Reading the " + (fetchPrevious ? "Production" : "New") + " API...");

                ReflectionDatabase newApi = new ReflectionDatabase(newApiFilePath);
                newApi.Branch = newBranch;
                
                setStatus("Comparing APIs...");

                string format = getApiDumpFormat();
                string result = await ReflectionDiffer.CompareDatabases(oldApi, newApi, format);

                if (result.Length > 0)
                {
                    FileInfo info = new FileInfo(newApiFilePath);
                    string dirName = info.DirectoryName;

                    string fileBase = Path.Combine(dirName, $"{newBranch}-diff.");
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
                .Select(branch => Program.GetRegistryString(VersionRegistry, branch))
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
                string[] branches = branch.Items.Cast<string>().ToArray();
                setStatus("Initializing version cache...");

                // Fetch the version guids for roblox and sitetest1-sitetest3
                foreach (string branchName in branches)
                {
                    string versionGuid = await GetVersion(branchName);
                    VersionRegistry.SetValue(branchName, versionGuid);
                }

                Program.MainRegistry.SetValue("InitializedVersions", true);
            });
        }

        private async void ApiDumpTool_Load(object sender, EventArgs e)
        {
            WebRequest.DefaultWebProxy = null;

            if (!Program.GetRegistryBool("InitializedVersions"))
            {
                await initVersionCache();
                clearOldVersionFiles();
            }

            loadSelectedIndex(branch, "LastSelectedBranch");
            loadSelectedIndex(apiDumpFormat, "PreferredFormat");
        }

        private void apiDumpFormat_SelectedIndexChanged(object sender, EventArgs e)
        {
            string format = getApiDumpFormat();
            Program.MainRegistry.SetValue("PreferredFormat", format);

            updateEnabledStates();
        }
    }
}
