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
            // 초기 UI 상태 설정
            UpdateUIForStoppedState();

            // 트레이 아이콘 설정
            trayIcon.Icon = this.Icon;
            trayIcon.Text = "LT Collector";
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

            // 시작하자마자 1회 즉시 실행
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
                // UI 스레드가 멈추지 않도록 Task.Run을 사용하여 백그라운드에서 실행
                await Task.Run(() => _collector.Execute());

                // UI 업데이트는 Invoke를 통해 안전하게 호출
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

        // X 버튼 클릭 시 종료 대신 트레이로 숨기기
        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (e.CloseReason == CloseReason.UserClosing)
            {
                e.Cancel = true;
                this.Hide();
                trayIcon.ShowBalloonTip(1000, "Collector", "The application is now running in the background.", ToolTipIcon.Info);
            }
        }

        // 트레이 아이콘 더블클릭 시 창 보이기
        private void trayIcon_DoubleClick(object sender, EventArgs e)
        {
            ShowForm();
        }

        // 트레이 메뉴 '열기'
        private void openToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ShowForm();
        }

        // 트레이 메뉴 '종료'
        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            // 타이머 정리
            _collectTimer?.Stop();
            _collectTimer?.Dispose();
            // 트레이 아이콘 정리
            trayIcon.Visible = false;
            trayIcon.Dispose();
            // 완전 종료
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
