using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using System.Windows.Forms;

using Roblox.Reflection;

namespace Roblox
{
    public partial class Main : Form
    {
        private const string VERSION_API_KEY = "76e5a40c-3ae1-4028-9f10-7c62520bd94f";

        private delegate void StatusDelegate(string msg);
        private delegate string BranchDelegate();

        private WebClient http = new WebClient();

        public Main()
        {
            InitializeComponent();
            branch.SelectedIndex = 0;
        }

        private string getBranch()
        {
            if (InvokeRequired)
                return Invoke(new BranchDelegate(getBranch)).ToString();
            else
                return branch.SelectedItem.ToString();
        }

        private async Task<string> getLiveVersion(string branch, string endPoint, string binaryType)
        {
            string versionUrl = "https://versioncompatibility.api."
                                + branch + ".com/" + endPoint + "?binaryType=" 
                                + binaryType + "&apiKey=" + VERSION_API_KEY;

            string version = await http.DownloadStringTaskAsync(versionUrl);
            version = version.Replace('"', ' ').Trim();

            return version;
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

        private void writeAndViewFile(string path, string contents)
        {
            if (!File.Exists(path) || File.ReadAllText(path) != contents)
                File.WriteAllText(path, contents);

            Process.Start(path);
        }

        private async Task<string> getApiDumpFilePath(string branch, bool fetchPrevious = false)
        {
            string localAppData = Environment.GetEnvironmentVariable("LocalAppData");

            string coreBin = Path.Combine(localAppData, "RobloxApiDumpFiles");
            Directory.CreateDirectory(coreBin);

            string setupUrl = "https://s3.amazonaws.com/setup." + branch + ".com/";
            setStatus("Checking for update...");

            string version = await getLiveVersion(branch, "GetCurrentClientVersionUpload", "WindowsStudio");

            if (fetchPrevious)
                version = await ReflectionHistory.GetPreviousVersionGuid(branch, version);

            string file = Path.Combine(coreBin, version + ".json");

            if (!File.Exists(file))
            {
                setStatus("Grabbing the" + (fetchPrevious ? " previous " : " ") + "API Dump from " + branch);
                string apiDump = await http.DownloadStringTaskAsync(setupUrl + version + "-API-Dump.json");
                File.WriteAllText(file, apiDump);
            }
            else
            {
                setStatus("Already up to date!");
            }

            return file;
        }

        private void branch_SelectedIndexChanged(object sender, EventArgs e)
        {
            viewApiDumpJson.Enabled = true;
            viewApiDumpClassic.Enabled = true;
            compareVersions.Enabled = true;

            if (getBranch() == "roblox")
                compareVersions.Text = "Compare Previous Version";
            else
                compareVersions.Text = "Compare to Production";

        }

        private async void viewApiDumpJson_Click(object sender, EventArgs e)
        {
            await lockWindowAndRunTask(async () =>
            {
                string branch = getBranch();
                string filePath = await getApiDumpFilePath(branch);
                Process.Start(filePath);
            });
        }

        private async void viewApiDumpClassic_Click(object sender, EventArgs e)
        {
            await lockWindowAndRunTask(async () =>
            {
                string branch = getBranch();
                string apiFilePath = await getApiDumpFilePath(branch);
                string apiJson = File.ReadAllText(apiFilePath);

                ReflectionDatabase api = ReflectionDatabase.Load(apiJson);
                ReflectionDumper dumper = new ReflectionDumper(api);

                string result = dumper.Run();

                FileInfo info = new FileInfo(apiFilePath);
                string directory = info.DirectoryName;

                string resultPath = Path.Combine(directory, branch + "-api-dump.txt");
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
                string oldApiJson = File.ReadAllText(oldApiFilePath);
                ReflectionDatabase oldApi = ReflectionDatabase.Load(oldApiJson);

                setStatus("Reading the " + (fetchPrevious ? "Production" : "New") + " API...");
                string newApiJson = File.ReadAllText(newApiFilePath);
                ReflectionDatabase newApi = ReflectionDatabase.Load(newApiJson);

                setStatus("Comparing APIs...");
                ReflectionDiffer differ = new ReflectionDiffer();
                string result = differ.CompareDatabases(oldApi, newApi);

                if (result.Length > 0)
                {
                    FileInfo info = new FileInfo(newApiFilePath);

                    string directory = info.DirectoryName;
                    string resultPath = Path.Combine(directory, newBranch + "-diff.txt");

                    writeAndViewFile(resultPath, result);
                }
                else
                {
                    MessageBox.Show("No differences were found!", "Well, this is awkward...", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            });
        }
    }
}
