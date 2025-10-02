// MainForm.cs
using System;
using System.Drawing;
using System.Windows.Forms;
using System.Threading.Tasks;

namespace Onto.LampLifeCollector
{
    public partial class MainForm : Form
    {
        private System.Timers.Timer _collectTimer;
        private readonly Collector _collector;

        public MainForm()
        {
            InitializeComponent();
            _collector = new Collector();
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            // Set initial UI state
            UpdateUIForStoppedState();
            
            // Configure the tray icon
            trayIcon.Icon = this.Icon;
            trayIcon.Text = "Onto LampLife Collector";
            trayIcon.Visible = true;
        }

        private void btnStart_Click(object sender, EventArgs e)
        {
            double interval = (double)numInterval.Value * 60 * 1000;
            if (interval <= 0)
            {
                MessageBox.Show("Collection interval must be at least 1 minute.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            _collectTimer = new System.Timers.Timer(interval);
            _collectTimer.Elapsed += OnTimerElapsed;
            _collectTimer.AutoReset = true;
            _collectTimer.Start();

            UpdateUIForRunningState();

            // Immediately run the first collection
            ExecuteCollectionAsync();
        }

        private void btnStop_Click(object sender, EventArgs e)
        {
            _collectTimer?.Stop();
            _collectTimer?.Dispose();

            UpdateUIForStoppedState();
        }

        private void OnTimerElapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            ExecuteCollectionAsync();
        }

        private async void ExecuteCollectionAsync()
        {
            try
            {
                // Run in a background thread using Task.Run to avoid blocking the UI
                await Task.Run(() => _collector.Execute());

                // Safely update the UI from the UI thread
                this.Invoke((MethodInvoker)delegate {
                    lblStatus.Text = $"Last Collection: {DateTime.Now:HH:mm:ss}";
                    lblStatus.ForeColor = Color.Green;
                });
            }
            catch (Exception ex)
            {
                 this.Invoke((MethodInvoker)delegate {
                    lblStatus.Text = $"Error occurred: {DateTime.Now:HH:mm:ss}";
                    lblStatus.ForeColor = Color.Red;
                });
            }
        }

        private void UpdateUIForRunningState()
        {
            lblStatus.Text = "Status: Running...";
            lblStatus.ForeColor = Color.Blue;
            btnStart.Enabled = false;
            btnStop.Enabled = true;
            numInterval.Enabled = false;
        }
        
        private void UpdateUIForStoppedState()
        {
            lblStatus.Text = "Status: Stopped";
            lblStatus.ForeColor = Color.DarkGray;
            btnStart.Enabled = true;
            btnStop.Enabled = false;
            numInterval.Enabled = true;
        }

        // Hide the form to the system tray instead of closing
        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (e.CloseReason == CloseReason.UserClosing)
            {
                e.Cancel = true;
                this.Hide();
                trayIcon.ShowBalloonTip(1000, "Collector", "The application is now running in the background.", ToolTipIcon.Info);
            }
        }

        // Show the form when the tray icon is double-clicked
        private void trayIcon_DoubleClick(object sender, EventArgs e)
        {
            ShowForm();
        }

        // 'Open' from tray menu
        private void openToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ShowForm();
        }
        
        // 'Exit' from tray menu
        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            // Clean up resources
            _collectTimer?.Stop();
            _collectTimer?.Dispose();
            trayIcon.Visible = false;
            trayIcon.Dispose();
            // Exit the application
            Application.Exit();
        }
        
        private void ShowForm()
        {
            this.Show();
            if (this.WindowState == FormWindowState.Minimized)
            {
                this.WindowState = FormWindowState.Normal;
            }
            this.Activate();
        }
    }
}
