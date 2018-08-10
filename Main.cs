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
        private WebClient http = new WebClient();

        public Main()
        {
            InitializeComponent();
            branch.SelectedIndex = 0;
        }

        private string getBranch()
        {
            return branch.SelectedItem.ToString();
        }

        private void setWindowLocked(bool locked)
        {
            if (!locked)
                clearStatus();

            Enabled = !locked;
            UseWaitCursor = locked;
        }

        private async Task setStatus(string msg = "")
        {
            status.Text = "Status: " + msg;
            await Task.Delay(10);
        }

        private void clearStatus()
        {
            status.Text = "Status: Ready!";
        }

        private async Task<string> getApiDumpFilePath(string branch)
        {
            await setStatus("Checking for update...");
            string localAppData = Environment.GetEnvironmentVariable("LocalAppData");

            string coreBin = Path.Combine(localAppData,"RobloxApiDumpFiles");
            Directory.CreateDirectory(coreBin);

            string setupUrl = "http://setup." + branch + ".com/";
            string version = await http.DownloadStringTaskAsync(setupUrl + "versionQTStudio");
            string file = Path.Combine(coreBin, version + ".json");

            if (!File.Exists(file))
            {
                await setStatus("Grabbing the API Dump from " + branch + ".com");
                string apiDump = await http.DownloadStringTaskAsync(setupUrl + version + "-API-Dump.json");
                File.WriteAllText(file, apiDump);
            }
            else
            {
                await setStatus("Already up to date!");
            }

            return file;
        }

        private void branch_SelectedIndexChanged(object sender, EventArgs e)
        {
            viewApiDumpJson.Enabled = true;
            viewApiDumpClassic.Enabled = true;

            compareToProduction.Enabled = (getBranch() != "roblox");
        }

        private async void viewApiDumpJson_Click(object sender, EventArgs e)
        {
            setWindowLocked(true);

            string branch = getBranch();
            string filePath = await getApiDumpFilePath(branch);
            Process.Start(filePath);

            setWindowLocked(false);
        }

        private async void compareToProduction_Click(object sender, EventArgs e)
        {
            setWindowLocked(true);

            string branch = getBranch();
            
            string oldApiFilePath = await getApiDumpFilePath("roblox");
            string newApiFilePath = await getApiDumpFilePath(branch);

            await setStatus("Reading Production API...");
            string oldApiJson = File.ReadAllText(oldApiFilePath);
            ReflectionDatabase oldApi = ReflectionDatabase.Load(oldApiJson);

            await setStatus("Reading New API...");
            string newApiJson = File.ReadAllText(newApiFilePath);
            ReflectionDatabase newApi = ReflectionDatabase.Load(newApiJson);

            await setStatus("Comparing APIs...");

            ReflectionDiffer differ = new ReflectionDiffer();
            string result = differ.CompareDatabases(oldApi, newApi);

            if (result.Length > 0)
            {
                FileInfo info = new FileInfo(newApiFilePath);
                string directory = info.DirectoryName;
                string resultFile = Path.Combine(directory, branch + "-diff.txt");

                File.WriteAllText(resultFile, result);
                Process.Start(resultFile);
            }
            else
            {
                MessageBox.Show("No differences were found!", "Well, this is awkward...", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

            setWindowLocked(false);
        }

        private async void viewApiDumpClassic_Click(object sender, EventArgs e)
        {
            setWindowLocked(true);

            string branch = getBranch();
            string apiFilePath = await getApiDumpFilePath(branch);
            string apiJson = File.ReadAllText(apiFilePath);

            ReflectionDatabase api = ReflectionDatabase.Load(apiJson);
            ReflectionDumper dumper = new ReflectionDumper(api);

            string result = dumper.Run();

            FileInfo info = new FileInfo(apiFilePath);
            string directory = info.DirectoryName;
            string resultFile = Path.Combine(directory, branch + "-api-dump.txt");

            File.WriteAllText(resultFile, result);
            Process.Start(resultFile);

            setWindowLocked(false);
        }
    }
}
