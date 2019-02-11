using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

using Roblox.Reflection;
using Microsoft.Win32;

namespace Roblox
{
    public partial class ApiDumpTool : Form
    {
        public static RegistryKey VersionRegistry => Program.GetRegistryKey(Program.MainRegistry, "Current Versions");
        
        private const string VERSION_API_KEY = "76e5a40c-3ae1-4028-9f10-7c62520bd94f";
        private const string API_DUMP_CSS_FILE = "api-dump-v1-4.css";

        private delegate void StatusDelegate(string msg);
        private delegate string ItemDelegate(ComboBox comboBox);
        private static WebClient http = new WebClient();

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

            return result.ToString();
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

        private static async Task<string> getLiveVersion(string branch, string binaryType)
        {
            string versionUrl = "https://versioncompatibility.api."
                                + branch + ".com/GetCurrentClientVersionUpload?binaryType=" 
                                + binaryType + "&apiKey=" + VERSION_API_KEY;

            string version = await http.DownloadStringTaskAsync(versionUrl);
            version = version.Replace('"', ' ').Trim();

            return version;
        }

        private static async Task<string> getDeployedVersion(string branch, string versionType)
        {
            string versionUrl = "https://s3.amazonaws.com/setup." + branch + ".com/" + versionType;
            return await http.DownloadStringTaskAsync(versionUrl);
        }

        public static async Task<string> GetVersion(string branch)
        {
            bool useDeployed = Program.GetRegistryBool("UseDeployedVersion");
            string result;

            if (useDeployed)
                result = await getDeployedVersion(branch, "versionQTStudio");
            else
                result = await getLiveVersion(branch, "WindowsStudio");

            return result;
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

        private static void writeAndViewFile(string path, string contents)
        {
            if (!File.Exists(path) || File.ReadAllText(path, Encoding.UTF8) != contents)
                File.WriteAllText(path, contents, Encoding.UTF8);

            Process.Start(path);
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

            if (!File.Exists(apiDumpCss))
                File.WriteAllText(apiDumpCss, Properties.Resources.ApiDumpStyler);

            return "<head>\n"
                 + "\t<link rel=\"stylesheet\" href=\"" + API_DUMP_CSS_FILE + "\">\n"
                 + "</head>\n\n"
                 + result.Trim();
        }

        public static async Task<string> GetApiDumpFilePath(string branch, string versionGuid, Action<string> setStatus = null)
        {
            string setupUrl = "https://s3.amazonaws.com/setup." + branch + ".com/";

            DeployLog deployLog = await ReflectionHistory.FindDeployLog(branch, versionGuid);
            string version = deployLog.ToString();

            string coreBin = GetWorkDirectory();
            string file = Path.Combine(coreBin, versionGuid + ".json");

            if (!File.Exists(file))
            {
                setStatus?.Invoke("Grabbing API Dump for " + version);

                string apiDump = await http.DownloadStringTaskAsync(setupUrl + versionGuid + "-API-Dump.json");
                File.WriteAllText(file, apiDump);
            }
            else
            {
                setStatus?.Invoke("Already up to date!");
            }

            return file;
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
            viewApiDump.Enabled = true;
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

                ReflectionDatabase api = new ReflectionDatabase(apiFilePath);
                ReflectionDumper dumper = new ReflectionDumper(api);

                string result;

                if (format == "HTML")
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

                ReflectionDiffer differ = new ReflectionDiffer();
                string result = await differ.CompareDatabases(oldApi, newApi, format);

                if (result.Length > 0)
                {
                    FileInfo info = new FileInfo(newApiFilePath);

                    string directory = info.DirectoryName;
                    string resultPath = Path.Combine(directory, newBranch + "-diff." + format.ToLower());

                    writeAndViewFile(resultPath, result);
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

                // Fetch the version guids for roblox, and gametest1-gametest5
                foreach (string branchName in branches)
                {
                    string versionGuid = await getLiveVersion(branchName, "WindowsStudio");
                    VersionRegistry.SetValue(branchName, versionGuid);
                }

                // Fetch the previous version guid for roblox.
                string robloxGuid = Program.GetRegistryString(VersionRegistry, "roblox");
                string prevGuid = await ReflectionHistory.GetPreviousVersionGuid("roblox", robloxGuid);
                VersionRegistry.SetValue("roblox-prev", prevGuid);

                // Done.
                Program.MainRegistry.SetValue("InitializedVersions", true);
            });
        }

        private async void ApiDumpTool_Load(object sender, EventArgs e)
        {
            bool initVersions = Program.GetRegistryBool("InitializedVersions");

            if (!initVersions)
            {
                await initVersionCache();
                clearOldVersionFiles();
            }

            bool useLatest = Program.GetRegistryBool("UseDeployedVersion");
            useLatestDeployed.Checked = useLatest;

            // Load combobox selections.
            loadSelectedIndex(branch, "LastSelectedBranch");
            loadSelectedIndex(apiDumpFormat, "PreferredFormat");
        }

        private void useLatestDeployed_CheckedChanged(object sender, EventArgs e)
        {
            bool useLatest = useLatestDeployed.Checked;
            Program.MainRegistry.SetValue("UseDeployedVersion", useLatest);
        }

        private void apiDumpFormat_SelectedIndexChanged(object sender, EventArgs e)
        {
            string format = getApiDumpFormat();
            Program.MainRegistry.SetValue("PreferredFormat", format);
            compareVersions.Enabled = (format != "JSON");
        }
    }
}
