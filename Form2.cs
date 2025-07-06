using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml;

namespace WindowsFormsApp2
{
    public partial class Form2 : Form
    {
        private string download_link = null;
        public string currentVersion = "3.1.0";

        public Form2()
        {
            InitializeComponent();
        }

        private void Form2_Load(object sender, EventArgs e)
        {
        }

        private void splitContainer1_Panel2_Paint(object sender, PaintEventArgs e)
        {
        }

        private void button2_Click(object sender, EventArgs e)
        {
            System.Diagnostics.Process.Start("https://yasserdivar.ir/best-dns-configuration/");
        }

        private void button1_Click(object sender, EventArgs e)
        {
            // this.Text = Application.ProductVersion.ToString();
            RunUpdateCheck();
        }

        public string CheckForUpdates()
        {
            var versionFile = "https://dl.yasserdivar.ir/software/dns-version.xml";
            var tempVersionFile = Path.Combine(Path.GetTempPath(), "appname_version.xml");

            using (var webClient = new WebClient())
            {
                try
                {
                    webClient.DownloadFile(versionFile, tempVersionFile);
                }
                catch (Exception)
                {
                    return "error";
                }
            }

            if (File.Exists(tempVersionFile))
            {
                XmlDocument xmlDoc = new XmlDocument();
                xmlDoc.Load(tempVersionFile);

                string latestVersion = $"{xmlDoc.SelectSingleNode("//currentVersion/major").InnerText}.{xmlDoc.SelectSingleNode("//currentVersion/minor").InnerText}.{xmlDoc.SelectSingleNode("//currentVersion/build").InnerText}";
                string downloadLink = xmlDoc.SelectSingleNode("//path").InnerText;
                //MessageBox.Show(latestVersion.ToString());
                if (currentVersion == latestVersion)
                {
                    return "updated";
                }
                else
                {
                    download_link = downloadLink;
                    return "needs_update";
                }
            }

            if (File.Exists(tempVersionFile))
            {
                File.Delete(tempVersionFile);
            }

            return "error";
        }

        public async void RunUpdateCheck()
        {
            labelUpdateState.Text = "Checking for updates. Please wait...";
            string result = await Task.Run(() => CheckForUpdates());

            switch (result)
            {
                case "error":

                    labelUpdateState.Text = ("Error checking for updates.");
                    break;

                case "updated":

                    labelUpdateState.Text = ("You are using the latest version.");
                    break;

                case "needs_update":
                    if (MessageBox.Show("A new version is available. Do you want to download it?", "Update Available", MessageBoxButtons.YesNo) == DialogResult.Yes)
                    {
                        Process.Start(new ProcessStartInfo(download_link) { UseShellExecute = true });
                    }
                    break;
            }

            download_link = null;

            labelUpdateState.Text = "Update check completed.";
        }
    }
}