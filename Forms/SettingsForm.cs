using GotifyClient.Models;
using GotifyClient.Services;

namespace GotifyClient.Forms
{
    public partial class SettingsForm : Form
    {
        private readonly ConfigService _configService;
        private TextBox _serverUrlTextBox = null!;
        private TextBox _clientTokenTextBox = null!;
        private CheckBox _showCustomNotificationCheckBox = null!;
        private CheckBox _playSoundCheckBox = null!;
        private CheckBox _notificationAutoHideCheckBox = null!;
        private CheckBox _notificationNeverCloseCheckBox = null!;
        private CheckBox _minimizeToTrayCheckBox = null!;
        private CheckBox _showMainFormOnStartupCheckBox = null!;
        private NumericUpDown _notificationDurationNumeric = null!;

        public event Action<GotifyConfig>? ConfigChanged;

        public SettingsForm(ConfigService configService)
        {
            _configService = configService;
            InitializeComponent();
            LoadSettings();
        }

        private void InitializeComponent()
        {
            SuspendLayout();

            // 窗体设置
            Text = "Gotify 客户端设置";
            Size = new Size(520, 565);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            StartPosition = FormStartPosition.CenterScreen; // 将窗体启动位置设置为屏幕中央
            ShowInTaskbar = false;

            // 创建控件
            var serverLabel = new Label
            {
                Text = "服务器地址:",
                Location = new Point(20, 20),
                Size = new Size(100, 23)
            };

            _serverUrlTextBox = new TextBox
            {
                Location = new Point(130, 18),
                Size = new Size(280, 23),
                PlaceholderText = "http://your-gotify-server.com"
            };

            var tokenLabel = new Label
            {
                Text = "客户端令牌:",
                Location = new Point(20, 55),
                Size = new Size(100, 23)
            };

            _clientTokenTextBox = new TextBox
            {
                Location = new Point(130, 53),
                Size = new Size(240, 23),
                UseSystemPasswordChar = true
            };

            var showTokenButton = new Button
            {
                Text = "显示",
                Location = new Point(380, 53),
                Size = new Size(50, 23)
            };
            showTokenButton.Click += ShowTokenButton_Click;

            // 通知设置组
            var notificationGroupBox = new GroupBox
            {
                Text = "通知设置",
                Location = new Point(20, 95),
                Size = new Size(460, 220)
            };

            _showCustomNotificationCheckBox = new CheckBox
            {
                Text = "显示自定义弹窗通知",
                Location = new Point(15, 25),
                Size = new Size(200, 23)
            };



            _playSoundCheckBox = new CheckBox
            {
                Text = "播放提示音",
                Location = new Point(250, 25),
                Size = new Size(150, 23)
            };

            _notificationAutoHideCheckBox = new CheckBox
            {
                Text = "通知自动消失",
                Location = new Point(250, 55),
                Size = new Size(150, 23)
            };
            _notificationAutoHideCheckBox.CheckedChanged += NotificationAutoHideCheckBox_CheckedChanged;

            _notificationNeverCloseCheckBox = new CheckBox
            {
                Text = "永不自动关闭",
                Location = new Point(15, 85),
                Size = new Size(200, 23),
                ForeColor = Color.Red
            };
            _notificationNeverCloseCheckBox.CheckedChanged += NotificationNeverCloseCheckBox_CheckedChanged;

            var durationLabel = new Label
            {
                Text = "通知持续时间(毫秒):",
                Location = new Point(15, 115),
                Size = new Size(150, 23)
            };

            _notificationDurationNumeric = new NumericUpDown
            {
                Location = new Point(170, 113),
                Size = new Size(100, 23),
                Minimum = 1000,
                Maximum = 30000,
                Increment = 1000,
                Value = 5000
            };

            var durationHelpLabel = new Label
            {
                Text = "（仅在自动消失启用时有效）",
                Location = new Point(280, 115),
                Size = new Size(160, 23),
                Font = new Font("微软雅黑", 8),
                ForeColor = Color.Gray
            };

            var noteLabel = new Label
            {
                Text = "提示：永不自动关闭的通知可通过点击固定按钮来切换状态",
                Location = new Point(15, 145),
                Size = new Size(430, 40),
                Font = new Font("微软雅黑", 8),
                ForeColor = Color.Blue
            };

            var reconnectCheckBox = new CheckBox
            {
                Text = "启用主动重连",
                Location = new Point(15, 55), // 调整位置到 "显示自定义弹窗通知" 下方
                Size = new Size(200, 23),
                Checked = _configService.Config.EnableReconnect // 从配置加载默认值
            };
            reconnectCheckBox.CheckedChanged += (sender, args) =>
            {
                _configService.Config.EnableReconnect = reconnectCheckBox.Checked;
                _configService.SaveConfig();
            };

            notificationGroupBox.Controls.Add(_showCustomNotificationCheckBox);
            notificationGroupBox.Controls.Add(_playSoundCheckBox);
            notificationGroupBox.Controls.Add(_notificationAutoHideCheckBox);
            notificationGroupBox.Controls.Add(_notificationNeverCloseCheckBox);
            notificationGroupBox.Controls.Add(durationLabel);
            notificationGroupBox.Controls.Add(_notificationDurationNumeric);
            notificationGroupBox.Controls.Add(durationHelpLabel);
            notificationGroupBox.Controls.Add(noteLabel);
            notificationGroupBox.Controls.Add(reconnectCheckBox);

            // 其他设置组
            var otherGroupBox = new GroupBox
            {
                Text = "其他设置",
                Location = new Point(20, 330),
                Size = new Size(460, 85)
            };

            _minimizeToTrayCheckBox = new CheckBox
            {
                Text = "最小化到系统托盘",
                Location = new Point(15, 25),
                Size = new Size(200, 23)
            };

            _showMainFormOnStartupCheckBox = new CheckBox
            {
                Text = "启动时显示主界面",
                Location = new Point(15, 50),
                Size = new Size(200, 23)
            };

            otherGroupBox.Controls.Add(_minimizeToTrayCheckBox);
            otherGroupBox.Controls.Add(_showMainFormOnStartupCheckBox);

            // 底部按钮区域
            var buttonPanel = new Panel
            {
                Location = new Point(20, 435),
                Size = new Size(460, 50),
                Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right
            };

            var testButton = new Button
            {
                Text = "测试连接",
                Location = new Point(190, 10),
                Size = new Size(80, 30)
            };
            testButton.Click += TestButton_Click;

            var saveButton = new Button
            {
                Text = "保存",
                Location = new Point(280, 10),
                Size = new Size(80, 30)
            };
            saveButton.Click += SaveButton_Click;

            var cancelButton = new Button
            {
                Text = "取消",
                Location = new Point(370, 10),
                Size = new Size(80, 30)
            };
            cancelButton.Click += (s, e) => Close();

            buttonPanel.Controls.Add(testButton);
            buttonPanel.Controls.Add(saveButton);
            buttonPanel.Controls.Add(cancelButton);

            // 版本信息标签
            var versionLabel = new Label
            {
                Text = "版本: v0.1.2",
                Location = new Point(20, 500),
                Size = new Size(200, 23),
                ForeColor = Color.Gray
            };

            // 添加控件到窗体
            Controls.Add(serverLabel);
            Controls.Add(_serverUrlTextBox);
            Controls.Add(tokenLabel);
            Controls.Add(_clientTokenTextBox);
            Controls.Add(showTokenButton);
            Controls.Add(notificationGroupBox);
            Controls.Add(otherGroupBox);
            Controls.Add(buttonPanel);
            Controls.Add(versionLabel);

            ResumeLayout(false);
        }

        private void LoadSettings()
        {
            var config = _configService.Config;
            _serverUrlTextBox.Text = config.ServerUrl;
            _clientTokenTextBox.Text = config.ClientToken;
            _showCustomNotificationCheckBox.Checked = config.ShowCustomNotification;
            _playSoundCheckBox.Checked = config.PlaySound;
            _notificationAutoHideCheckBox.Checked = config.NotificationAutoHide;
            _notificationNeverCloseCheckBox.Checked = config.NotificationNeverClose;
            _minimizeToTrayCheckBox.Checked = config.MinimizeToTray;
            _showMainFormOnStartupCheckBox.Checked = config.ShowMainFormOnStartup;
            _notificationDurationNumeric.Value = config.NotificationDuration;
            
            // 根据设置状态更新控件
            UpdateNotificationControls();
        }

        private void ShowTokenButton_Click(object? sender, EventArgs e)
        {
            _clientTokenTextBox.UseSystemPasswordChar = !_clientTokenTextBox.UseSystemPasswordChar;
            ((Button)sender!).Text = _clientTokenTextBox.UseSystemPasswordChar ? "显示" : "隐藏";
        }

        private async void TestButton_Click(object? sender, EventArgs e)
        {
            var button = (Button)sender!;
            button.Enabled = false;
            button.Text = "测试中...";

            try
            {
                using var httpClient = new HttpClient();
                httpClient.Timeout = TimeSpan.FromSeconds(10);
                
                var url = _serverUrlTextBox.Text.TrimEnd('/');
                var token = _clientTokenTextBox.Text.Trim();
                
                var response = await httpClient.GetAsync($"{url}/application?token={token}");
                
                if (response.IsSuccessStatusCode)
                {
                    MessageBox.Show("连接测试成功！", "测试结果", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else
                {
                    MessageBox.Show($"连接测试失败: HTTP {(int)response.StatusCode} {response.ReasonPhrase}", 
                        "测试结果", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"连接测试失败: {ex.Message}", "测试结果", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                button.Enabled = true;
                button.Text = "测试连接";
            }
        }

        private void SaveButton_Click(object? sender, EventArgs e)
        {
            try
            {
                var config = new GotifyConfig
                {
                    ServerUrl = _serverUrlTextBox.Text.Trim(),
                    ClientToken = _clientTokenTextBox.Text.Trim(),
                    ShowCustomNotification = _showCustomNotificationCheckBox.Checked,
                    PlaySound = _playSoundCheckBox.Checked,
                    NotificationAutoHide = _notificationAutoHideCheckBox.Checked,
                    NotificationNeverClose = _notificationNeverCloseCheckBox.Checked,
                    MinimizeToTray = _minimizeToTrayCheckBox.Checked,
                    ShowMainFormOnStartup = _showMainFormOnStartupCheckBox.Checked,
                    NotificationDuration = (int)_notificationDurationNumeric.Value
                };

                _configService.UpdateConfig(config);
                ConfigChanged?.Invoke(config);

                MessageBox.Show("设置已保存！", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"保存设置失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void NotificationAutoHideCheckBox_CheckedChanged(object? sender, EventArgs e)
        {
            UpdateNotificationControls();
        }

        private void NotificationNeverCloseCheckBox_CheckedChanged(object? sender, EventArgs e)
        {
            if (_notificationNeverCloseCheckBox.Checked)
            {
                _notificationAutoHideCheckBox.Checked = false;
            }
            UpdateNotificationControls();
        }

        private void UpdateNotificationControls()
        {
            // 如果选择了"永不自动关闭"，则禁用"自动消失"和持续时间设置
            if (_notificationNeverCloseCheckBox.Checked)
            {
                _notificationAutoHideCheckBox.Enabled = false;
                _notificationDurationNumeric.Enabled = false;
            }
            else
            {
                _notificationAutoHideCheckBox.Enabled = true;
                _notificationDurationNumeric.Enabled = _notificationAutoHideCheckBox.Checked;
            }
        }
    }
}
