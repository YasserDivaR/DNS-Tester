using System;
using System.IO;
using System.Management;
using System.Net.Http;
using System.Net.NetworkInformation;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace WindowsFormsApp2
{
    public partial class Form1 : Form
    {
        private static readonly HttpClient httpClient = new HttpClient();

        public Form1()
        {
            InitializeComponent();
            InitializeListView();
            GetAllNetworkInterface();
        }

        private void InitializeListView()
        {
            // Initialize the ListView

            //listViewResults.Dock = DockStyle.Fill;
            listViewResults.View = View.Details;
            listViewResults.FullRowSelect = true;
            listViewResults.GridLines = true;

            // Add columns to the ListView
            listViewResults.Columns.Add("Name", -2, HorizontalAlignment.Left);
            listViewResults.Columns.Add("DNS1", -2, HorizontalAlignment.Left);
            listViewResults.Columns.Add("DNS2", -2, HorizontalAlignment.Left);
            listViewResults.Columns.Add("Ping DNS1", -2, HorizontalAlignment.Left);
            listViewResults.Columns.Add("Ping DNS2", -2, HorizontalAlignment.Left);
            /////////////////////////////
            ///
            //listViewResults.ItemSelectionChanged += ListViewResults_ItemSelectionChanged;
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            this.Text = "DNS Tester By YasserDivar.ir " + new Form2().currentVersion.ToString();

            // Specify the path to your text file
            string filePath = "DNS_List.txt"; // Replace with your file path

            if (File.Exists(filePath))
            {
                // Read all lines from the file
                string[] lines = File.ReadAllLines(filePath);

                foreach (string line in lines)
                {
                    // Split the line into name and DNS parts
                    string[] parts = line.Split(new[] { '=' }, 2);
                    if (parts.Length == 2)
                    {
                        string name = parts[0].Trim();
                        string[] dnsParts = parts[1].Split(new[] { ',' }, 2);

                        if (dnsParts.Length == 2)
                        {
                            string dns1 = dnsParts[0].Trim();
                            string dns2 = dnsParts[1].Trim();
                            textBox1.Text = "items count : " + lines.Length.ToString();

                            // Add the data to the ListView
                            ListViewItem item = new ListViewItem(name);
                            item.SubItems.Add(dns1);
                            item.SubItems.Add(dns2);
                            listViewResults.Items.Add(item);
                        }
                    }
                }
                //  listViewResults.AutoResizeColumns(ColumnHeaderAutoResizeStyle.HeaderSize);
                listViewResults.Columns[0].Width = 255;
                listViewResults.Columns[1].Width = 145;
                listViewResults.Columns[2].Width = 145;
                listViewResults.Columns[3].Width = 187;
                listViewResults.Columns[4].Width = 187;
            }
        }

        private async void Check_DNS_Click(object sender, EventArgs e)
        {
            button1.Enabled = false;
            // Clear previous results
            listViewResults.Items.Clear();

            // Specify the path to your text file
            string filePath = "DNS_List.txt"; //    Replace with your file path

            if (File.Exists(filePath))
            {
                // Read all lines from the file
                string[] lines = File.ReadAllLines(filePath);

                // Show the ProgressBar
                progressBar1.Visible = true;
                progressBar1.Minimum = 0;
                progressBar1.Maximum = lines.Length;
                progressBar1.Value = 0;
                progressBar1.Step = 1;

                foreach (string line in lines)
                {
                    // Split the line into name and DNS parts
                    string[] parts = line.Split(new[] { '=' }, 2);
                    if (parts.Length == 2)
                    {
                        string name = parts[0].Trim();
                        string[] dnsParts = parts[1].Split(new[] { ',' }, 2);

                        if (dnsParts.Length == 2)
                        {
                            string dns1 = dnsParts[0].Trim();
                            string dns2 = dnsParts[1].Trim();

                            // Perform async ping tests
                            string pingDns1 = await PingAddressAsync(dns1);
                            string pingDns2 = await PingAddressAsync(dns2);

                            // Add the data to the ListView
                            ListViewItem item = new ListViewItem(name);
                            item.SubItems.Add(dns1);
                            item.SubItems.Add(dns2);
                            item.SubItems.Add(pingDns1);
                            item.SubItems.Add(pingDns2);
                            listViewResults.Items.Add(item);

                            // Update the ProgressBar
                            progressBar1.PerformStep();
                        }
                    }
                }

                // Hide the ProgressBar after completion
                //progressBar1.Visible = false;
            }
            else
            {
                lbl_status.Text = "File not found!";
            }
            listViewResults.ListViewItemSorter = new PingDnsComparer();
            listViewResults.Sort();
        }

        private async Task<string> PingAddressAsync(string address)
        {
            try
            {
                Ping pingSender = new Ping();
                PingReply reply = await pingSender.SendPingAsync(address);

                if (reply.Status == IPStatus.Success)
                {
                    return $"{reply.RoundtripTime} ms";
                }
                else
                {
                    return $"Failed ({reply.Status})";
                }
            }
            catch (Exception ex)
            {
                return $"Error ({ex.Message})";
            }
        }

        private void buttonSort_Click(object sender, EventArgs e)
        {
            // Sort the ListView by the "Ping DNS 1" column
            listViewResults.ListViewItemSorter = new PingDnsComparer();
            listViewResults.Sort();
        }

        public class PingDnsComparer : System.Collections.IComparer
        {
            public int Compare(object x, object y)
            {
                ListViewItem item1 = (ListViewItem)x;
                ListViewItem item2 = (ListViewItem)y;

                string pingDns1 = item1.SubItems[3].Text;
                string pingDns2 = item2.SubItems[3].Text;

                // Handle "Failed" and "Error" cases
                if (pingDns1.Contains("Failed") && !pingDns2.Contains("Failed"))
                {
                    return 1;
                }
                if (!pingDns1.Contains("Failed") && pingDns2.Contains("Failed"))
                {
                    return -1;
                }
                if (pingDns1.Contains("Error") && !pingDns2.Contains("Error"))
                {
                    return 1;
                }
                if (!pingDns1.Contains("Error") && pingDns2.Contains("Error"))
                {
                    return -1;
                }

                // Extract the numeric part and compare
                int time1 = ExtractTime(pingDns1);
                int time2 = ExtractTime(pingDns2);

                return time1.CompareTo(time2);
            }

            private int ExtractTime(string pingDns)
            {
                // Extract the numeric part from the ping result
                string[] parts = pingDns.Split(' ');
                if (parts.Length > 0 && int.TryParse(parts[0], out int time))
                {
                    return time;
                }
                return int.MaxValue; // Return a large value for non-numeric results
            }
        }

        private void GetAllNetworkInterface()
        {
            // Get all network interfaces
            NetworkInterface[] networkInterfaces = NetworkInterface.GetAllNetworkInterfaces();

            // Filter and add physical Ethernet and Wi-Fi adapters that are operational to the ComboBox
            foreach (NetworkInterface networkInterface in networkInterfaces)
            {
                if ((networkInterface.NetworkInterfaceType == NetworkInterfaceType.Ethernet ||
                networkInterface.NetworkInterfaceType == NetworkInterfaceType.Wireless80211) &&
                networkInterface.Description.IndexOf("Virtual", StringComparison.OrdinalIgnoreCase) < 0 &&
                networkInterface.Description.IndexOf("Pseudo", StringComparison.OrdinalIgnoreCase) < 0)
                {
                    comboBoxNetworkAdapters.Items.Add(networkInterface.Name);
                }
            }

            // Select the first item if available
            if (comboBoxNetworkAdapters.Items.Count > 0)
            {
                comboBoxNetworkAdapters.SelectedIndex = 0;
            }
        }

        private void SetDnsServers(string adapterId, string[] dnsServers)
        {
            try
            {
                ManagementClass managementClass = new ManagementClass("Win32_NetworkAdapterConfiguration");
                ManagementObjectCollection managementObjects = managementClass.GetInstances();

                foreach (ManagementObject managementObject in managementObjects)
                {
                    if ((string)managementObject["SettingID"] == adapterId)
                    {
                        ManagementBaseObject dnsServerSet = managementObject.GetMethodParameters("SetDNSServerSearchOrder");
                        dnsServerSet["DNSServerSearchOrder"] = dnsServers;
                        managementObject.InvokeMethod("SetDNSServerSearchOrder", dnsServerSet, null);
                        lbl_status.Text = ("DNS servers set successfully.");
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                lbl_status.Text = ($"Error setting DNS servers: {ex.Message}");
            }
        }

        private void dgvResults_CellClick(object sender, DataGridViewCellEventArgs e)
        {
        }

        private void dgvResults_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
        }

        private void button1_Click(object sender, EventArgs e)
        {
        }

        private void listViewResults_ColumnClick(object sender, ColumnClickEventArgs e)
        {
            if (e.Column == 3) // "Ping DNS1" column
            {
                // Sort the ListView by the "Ping DNS 1" column
                listViewResults.ListViewItemSorter = new PingDnsComparer();
                listViewResults.Sort();
            }
        }

        private void listViewResults_ColumnWidthChanged(object sender, ColumnWidthChangedEventArgs e)
        {
        }

        private void listViewResults_ColumnWidthChanging(object sender, ColumnWidthChangingEventArgs e)
        {
            e.Cancel = true;
            e.NewWidth = listViewResults.Columns[e.ColumnIndex].Width;
        }

        private void listViewResults_ItemSelectionChanged(object sender, ListViewItemSelectionChangedEventArgs e)
        {
            if (e.IsSelected)
            {
                // Get the selected item
                ListViewItem selectedItem = e.Item;

                // Extract DNS information
                string dns1 = selectedItem.SubItems[1].Text;
                string dns2 = selectedItem.SubItems[2].Text;

                // Display DNS information in the TextBoxes
                textBoxDns1.Text = dns1;
                textBoxDns2.Text = dns2;
            }
            button2.Enabled = true;
        }

        private void listViewResults_SelectedIndexChanged(object sender, EventArgs e)
        {
        }

        private void toolStripMenuItem1_Click(object sender, EventArgs e)
        {
            button2.PerformClick();
        }

        private void toolStripMenuItem6_Click(object sender, EventArgs e)
        {
            Check_DNS.PerformClick();
        }

        private void linkLabel1_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            System.Diagnostics.Process.Start("https://yasserdivar.ir/best-dns-configuration/");
        }

        private void button1_Click_1(object sender, EventArgs e)
        {
        }

        private void panel1_Paint(object sender, PaintEventArgs e)
        {
        }

        private void listViewResults_DrawItem(object sender, DrawListViewItemEventArgs e)
        {
        }

        private void LoadFileIntoListView(string filePath)
        {
            listViewResults.Items.Clear();
            if (File.Exists(filePath))
            {
                string[] lines = File.ReadAllLines(filePath);

                foreach (string line in lines)
                {
                    string[] parts = line.Split(new[] { '=' }, 2);
                    if (parts.Length == 2)
                    {
                        string name = parts[0].Trim();
                        string[] dnsParts = parts[1].Split(new[] { ',' }, 2);

                        if (dnsParts.Length == 2)
                        {
                            string dns1 = dnsParts[0].Trim();
                            string dns2 = dnsParts[1].Trim();
                            textBox1.Text = "items count : " + lines.Length.ToString();

                            ListViewItem item = new ListViewItem(name);
                            item.SubItems.Add(dns1);
                            item.SubItems.Add(dns2);
                            listViewResults.Items.Add(item);
                        }
                    }
                }

                listViewResults.Columns[0].Width = 255;
                listViewResults.Columns[1].Width = 145;
                listViewResults.Columns[2].Width = 145;
                listViewResults.Columns[3].Width = 187;
                listViewResults.Columns[4].Width = 187;
            }
            else
            {
                lbl_status.Text = ("File not found!");
            }
        }

        private async Task DownloadFileAsync(string url, string filePath)
        {
            try
            {
                HttpResponseMessage response = await httpClient.GetAsync(url);
                response.EnsureSuccessStatusCode();

                byte[] fileBytes = await response.Content.ReadAsByteArrayAsync();
                await Task.Run(() => File.WriteAllBytes(filePath, fileBytes));
            }
            catch (Exception ex)
            {
                lbl_status.Text = ($"Error downloading file: {ex.Message}");
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            if (textBoxDns1.Text == string.Empty && textBoxDns2.Text == string.Empty)
            {
                textBoxDns1.Text = "8.8.8.8"; textBoxDns2.Text = "8.8.4.4";
            }
            this.Enabled = false;
            string[] dnsServers = { textBoxDns1.Text, textBoxDns2.Text };

            NetworkInterface[] networkInterfaces = NetworkInterface.GetAllNetworkInterfaces();

            foreach (NetworkInterface networkInterface in networkInterfaces)
            {
                if ((networkInterface.NetworkInterfaceType == NetworkInterfaceType.Ethernet ||
                networkInterface.NetworkInterfaceType == NetworkInterfaceType.Wireless80211))

                {
                    SetDnsServers(networkInterface.Id, dnsServers);
                }
            }
            this.Enabled = true;
        }

        private void toolStripMenuItem5_Click(object sender, EventArgs e)
        {
            System.Diagnostics.Process.Start("https://yasserdivar.ir/best-dns-configuration/");
        }

        private void toolStripMenuItem3_Click(object sender, EventArgs e)
        {
            CopySelectedRowsColumnToClipboard(listViewResults, 1);
        }

        public static void CopySelectedRowsColumnToClipboard(System.Windows.Forms.ListView listView, int columnIndex)
        {
            if (listView == null || columnIndex < 0 || columnIndex >= listView.Columns.Count)
            {
                throw new ArgumentException("Invalid ListView or column index.");
            }

            // Create a string to hold the column data
            string columnData = string.Empty;

            // Iterate through each selected item in the ListView
            foreach (System.Windows.Forms.ListViewItem item in listView.SelectedItems)
            {
                // Check if the item has enough subitems
                if (item.SubItems.Count > columnIndex)
                {
                    // Append the text from the specified column
                    columnData += item.SubItems[columnIndex].Text + Environment.NewLine;
                }
            }

            // Copy the column data to the clipboard
            if (!string.IsNullOrEmpty(columnData))
            {
                Clipboard.SetText(columnData);
                // lbl_status.Text = ("Selected rows' column data copied to clipboard.");
            }
            else
            {
                MessageBox.Show("No data to copy.");
            }
        }

        private void toolStripMenuItem4_Click(object sender, EventArgs e)
        {
            CopySelectedRowsColumnToClipboard(listViewResults, 2);
        }

        private void toolStripMenuItem2_Click(object sender, EventArgs e)
        {
            CopySelectedRowsColumnsToClipboard(listViewResults, 1, 2);
        }

        public static void CopySelectedRowsColumnsToClipboard(System.Windows.Forms.ListView listView, int columnIndex1, int columnIndex2)
        {
            if (listView == null || columnIndex1 < 0 || columnIndex1 >= listView.Columns.Count || columnIndex2 < 0 || columnIndex2 >= listView.Columns.Count)
            {
                throw new ArgumentException("Invalid ListView or column index.");
            }

            // Create a string to hold the column data
            string columnData = string.Empty;

            // Iterate through each selected item in the ListView
            foreach (System.Windows.Forms.ListViewItem item in listView.SelectedItems)
            {
                // Check if the item has enough subitems
                if (item.SubItems.Count > Math.Max(columnIndex1, columnIndex2))
                {
                    // Append the text from the specified columns
                    columnData += item.SubItems[columnIndex1].Text + " , " + item.SubItems[columnIndex2].Text + Environment.NewLine;
                }
            }

            // Copy the column data to the clipboard
            if (!string.IsNullOrEmpty(columnData))
            {
                Clipboard.SetText(columnData);
                // MessageBox.Show("Selected rows' column data copied to clipboard.");
            }
            else
            {
                MessageBox.Show("No data to copy.");
            }
        }

        private async void button1_Click_3(object sender, EventArgs e)
        {
            string fileUrl = "https://dl.yasserdivar.ir/DNS_List.txt"; // Replace with your URL
            string filePath = Path.Combine(Application.StartupPath, "DNS_List.txt");

            await DownloadFileAsync(fileUrl, filePath);
            LoadFileIntoListView(filePath);
        }

        private void button3_Click(object sender, EventArgs e)
        {
            new Form2().ShowDialog();
        }
    }
}