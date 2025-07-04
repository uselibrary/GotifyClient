using GotifyClient.Models;
using GotifyClient.Services;
using GotifyClient.Forms;
using GotifyClient.Controls;

namespace GotifyClient
{
    public partial class MainForm : Form
    {
        private readonly ConfigService _configService;
        private readonly MessageHistoryService _messageHistoryService;
        private NotificationService _notificationService;
        private GotifyWebSocketService? _webSocketService;
        private GotifyApplicationService? _applicationService;
        private NotifyIcon _notifyIcon = null!;
        private ContextMenuStrip _contextMenu = null!;
        private Label _statusLabel = null!;
        private Button _connectButton = null!;
        private Button _settingsButton = null!;
        private Panel _messagesPanel = null!;
        private readonly List<MessageItem> _messageItems = new List<MessageItem>();
        private bool _isConnected = false;
        private bool _forceExit = false;

        public MainForm()
        {
            _configService = new ConfigService();
            _messageHistoryService = new MessageHistoryService();
            InitializeComponent();
            
            // 从嵌入资源加载图标
            try
            {
                var assembly = System.Reflection.Assembly.GetExecutingAssembly();
                using (var stream = assembly.GetManifestResourceStream("GotifyClient.gotify.ico"))
                {
                    if (stream != null)
                    {
                        Icon = new Icon(stream);
                    }
                }
            }
            catch
            {
                // 如果加载图标失败，使用默认图标
            }
            
            InitializeTrayIcon();
            
            _notificationService = new NotificationService(_notifyIcon, _configService.Config);
            
            // 窗口加载完成后再加载历史消息
            this.Load += (s, e) => _ = Task.Run(LoadHistoryMessages);
            
            // 根据配置决定是否在启动时显示主界面
            if (!_configService.Config.ShowMainFormOnStartup)
            {
                this.WindowState = FormWindowState.Minimized;
                this.ShowInTaskbar = false;
                this.Visible = false;
            }
            
            // 如果配置完整，自动连接
            if (!string.IsNullOrEmpty(_configService.Config.ServerUrl) && 
                !string.IsNullOrEmpty(_configService.Config.ClientToken))
            {
                ConnectToGotify();
            }
        }

        private void InitializeComponent()
        {
            SuspendLayout();

            // 窗体设置
            Text = "Gotify 客户端";
            Size = new Size(750, 600);
            StartPosition = FormStartPosition.CenterScreen;
            MinimumSize = new Size(650, 450);

            // 顶部按钮区布局
            var topPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 55
            };

            _statusLabel = new Label
            {
                Text = "未连接",
                Location = new Point(20, 15),
                Size = new Size(220, 25),
                Font = new Font("微软雅黑", 10),
                ForeColor = Color.Red,
                Anchor = AnchorStyles.Left | AnchorStyles.Top
            };

            _connectButton = new Button
            {
                Text = "连接",
                Size = new Size(80, 30),
                Location = new Point(260, 12),
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };
            _connectButton.Click += ConnectButton_Click;

            _settingsButton = new Button
            {
                Text = "设置",
                Size = new Size(80, 30),
                Location = new Point(350, 12),
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };
            _settingsButton.Click += SettingsButton_Click;

            topPanel.Controls.Add(_statusLabel);
            topPanel.Controls.Add(_connectButton);
            topPanel.Controls.Add(_settingsButton);

            // 清空消息按钮
            var clearButton = new Button
            {
                Text = "清空消息",
                Size = new Size(90, 28),
                Anchor = AnchorStyles.Bottom | AnchorStyles.Right
            };
            clearButton.Click += ClearButton_Click;

            // 底部按钮区布局
            var bottomPanel = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 45
            };
            clearButton.Location = new Point(bottomPanel.Width - clearButton.Width - 20, 8);
            clearButton.Anchor = AnchorStyles.Right | AnchorStyles.Top;
            bottomPanel.Controls.Add(clearButton);
            bottomPanel.Resize += (s, e) =>
            {
                clearButton.Location = new Point(bottomPanel.Width - clearButton.Width - 20, 8);
            };

            // 主区布局
            var mainPanel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(245, 248, 250)
            };

            // 创建一个带标题的消息区域
            var messageAreaPanel = new Panel
            {
                Location = new Point(15, 10),
                Size = new Size(mainPanel.Width - 30, mainPanel.Height - 20),
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
                BackColor = Color.White,
                BorderStyle = BorderStyle.None
            };

            // 添加圆角效果和阴影
            messageAreaPanel.Paint += (s, e) =>
            {
                var rect = new Rectangle(0, 0, messageAreaPanel.Width - 1, messageAreaPanel.Height - 1);
                using (var brush = new SolidBrush(Color.White))
                {
                    e.Graphics.FillRectangle(brush, rect);
                }
                using (var pen = new Pen(Color.FromArgb(230, 230, 230), 1))
                {
                    e.Graphics.DrawRectangle(pen, rect);
                }
            };

            // 美化消息标题区域
            var messageHeaderPanel = new Panel
            {
                Location = new Point(0, 0),
                Size = new Size(messageAreaPanel.Width, 40),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
                BackColor = Color.FromArgb(248, 249, 250)
            };

            var messageHeaderLabel = new Label
            {
                Text = "📬 历史消息",
                Font = new Font("微软雅黑", 11, FontStyle.Bold),
                ForeColor = Color.FromArgb(51, 51, 51),
                Location = new Point(15, 10),
                Size = new Size(200, 20),
                TextAlign = ContentAlignment.MiddleLeft
            };

            var messageCountLabel = new Label
            {
                Text = "0 条消息",
                Font = new Font("微软雅黑", 9),
                ForeColor = Color.Gray,
                Location = new Point(messageHeaderPanel.Width - 100, 12),
                Size = new Size(80, 16),
                TextAlign = ContentAlignment.MiddleRight,
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };

            messageHeaderPanel.Controls.Add(messageHeaderLabel);
            messageHeaderPanel.Controls.Add(messageCountLabel);

            // 创建滚动面板来容纳消息项
            _messagesPanel = new Panel
            {
                Location = new Point(5, 45),
                Size = new Size(messageAreaPanel.Width - 10, messageAreaPanel.Height - 50),
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
                AutoScroll = true,
                BackColor = Color.White
            };
            
            // 防止水平滚动条出现
            _messagesPanel.HorizontalScroll.Enabled = false;
            _messagesPanel.HorizontalScroll.Visible = false;

            // 添加空状态提示
            var emptyStateLabel = new Label
            {
                Text = "暂无消息\n\n等待接收新的通知消息...",
                Font = new Font("微软雅黑", 10),
                ForeColor = Color.Gray,
                TextAlign = ContentAlignment.MiddleCenter,
                Dock = DockStyle.Fill,
                Visible = true
            };
            _messagesPanel.Controls.Add(emptyStateLabel);
            _messagesPanel.Tag = emptyStateLabel; // 保存引用以便后续控制显示

            messageAreaPanel.Controls.Add(messageHeaderPanel);
            messageAreaPanel.Controls.Add(_messagesPanel);

            mainPanel.Controls.Add(messageAreaPanel);
            mainPanel.Resize += (s, e) =>
            {
                messageAreaPanel.Size = new Size(mainPanel.Width - 30, mainPanel.Height - 20);
                messageCountLabel.Location = new Point(messageHeaderPanel.Width - 100, 12);
                
                // 动态调整所有消息项的宽度以适应新的面板大小
                if (_messagesPanel != null && _messageItems.Count > 0)
                {
                    var newWidth = _messagesPanel.ClientSize.Width - 30;
                    if (newWidth > 600)
                    {
                        foreach (var item in _messageItems)
                        {
                            item.Width = newWidth;
                        }
                    }
                }
            };

            // 添加控件
            Controls.Add(mainPanel);
            Controls.Add(bottomPanel);
            Controls.Add(topPanel);

            // 窗体事件
            Resize += MainForm_Resize;
            FormClosing += MainForm_FormClosing;

            ResumeLayout(false);
        }

        private void InitializeTrayIcon()
        {
            // 创建托盘图标
            _notifyIcon = new NotifyIcon
            {
                Text = "Gotify 客户端",
                Visible = true
            };
            
            // 从嵌入资源加载托盘图标
            try
            {
                var assembly = System.Reflection.Assembly.GetExecutingAssembly();
                using (var stream = assembly.GetManifestResourceStream("GotifyClient.gotify.ico"))
                {
                    if (stream != null)
                    {
                        _notifyIcon.Icon = new Icon(stream);
                    }
                }
            }
            catch
            {
                // 如果加载图标失败，使用默认图标
                _notifyIcon.Icon = SystemIcons.Application;
            }
            
            _notifyIcon.MouseClick += NotifyIcon_MouseClick;

            // 创建右键菜单
            _contextMenu = new ContextMenuStrip();
            
            var showItem = new ToolStripMenuItem("显示主窗口");
            showItem.Click += (s, e) => ShowMainWindow();
            
            var settingsItem = new ToolStripMenuItem("设置");
            settingsItem.Click += SettingsButton_Click;
            
            var exitItem = new ToolStripMenuItem("退出");
            exitItem.Click += (s, e) => {
                _forceExit = true;
                Close();
            };

            _contextMenu.Items.Add(showItem);
            _contextMenu.Items.Add(settingsItem);
            _contextMenu.Items.Add(new ToolStripSeparator());
            _contextMenu.Items.Add(exitItem);

            _notifyIcon.ContextMenuStrip = _contextMenu;
        }

        private async void ConnectButton_Click(object? sender, EventArgs e)
        {
            if (_isConnected)
            {
                await DisconnectFromGotify();
            }
            else
            {
                ConnectToGotify();
            }
        }

        private async void ConnectToGotify()
        {
            try
            {
                var config = _configService.Config;
                if (string.IsNullOrEmpty(config.ServerUrl) || string.IsNullOrEmpty(config.ClientToken))
                {
                    MessageBox.Show("请先配置服务器地址和客户端令牌！", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    SettingsButton_Click(null, EventArgs.Empty);
                    return;
                }

                _connectButton.Enabled = false;
                _connectButton.Text = "连接中..";
                _statusLabel.Text = "正在连接...";
                _statusLabel.ForeColor = Color.Orange;

                _webSocketService = new GotifyWebSocketService(config.ServerUrl, config.ClientToken);
                _webSocketService.MessageReceived += OnMessageReceived;
                _webSocketService.ConnectionStatusChanged += OnConnectionStatusChanged;

                // 初始化应用服务
                _applicationService = new GotifyApplicationService(config.ServerUrl, config.ClientToken);

                await _webSocketService.ConnectAsync();
                
                // 连接状态由 OnConnectionStatusChanged 事件处理，不在这里直接设置
                _connectButton.Enabled = true;
            }
            catch (Exception ex)
            {
                _isConnected = false;
                _connectButton.Text = "连接";
                _connectButton.Enabled = true;
                _statusLabel.Text = $"连接失败: {ex.Message}";
                _statusLabel.ForeColor = Color.Red;

                MessageBox.Show($"连接失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async Task DisconnectFromGotify()
        {
            try
            {
                if (_webSocketService != null)
                {
                    await _webSocketService.DisconnectAsync();
                    _webSocketService.Dispose();
                    _webSocketService = null;
                }

                _isConnected = false;
                _connectButton.Text = "连接";
                _statusLabel.Text = "未连接";
                _statusLabel.ForeColor = Color.Red;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"断开连接时出错: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async void OnMessageReceived(GotifyMessage message)
        {
            // 异步获取应用名称
            string appName = "正在加载...";
            if (_applicationService != null)
            {
                try
                {
                    appName = await _applicationService.GetApplicationNameAsync(message.AppId);
                    // 将应用名称设置到消息对象中
                    message.AppName = appName;
                }
                catch (Exception)
                {
                    appName = $"应用 #{message.AppId}";
                    message.AppName = appName;
                }
            }
            else
            {
                appName = $"应用 #{message.AppId}";
                message.AppName = appName;
            }

            // 保存包含应用名称的消息到历史记录
            _messageHistoryService.AddMessage(message);

            // 在UI线程上更新界面
            Invoke(new Action(() =>
            {
                // 添加美化的消息项，传递应用名称
                AddMessageItem(message, appName);
            }));

            // 显示通知
            _notificationService.ShowNotification(message);
        }

        private void OnConnectionStatusChanged(string status)
        {
            Invoke(new Action(() =>
            {
                if (status.Contains("连接断开") || status.Contains("连接失败") || status.Contains("已断开连接"))
                {
                    _isConnected = false;
                    _connectButton.Text = "连接";
                    _connectButton.Enabled = true;
                    _statusLabel.Text = status;
                    _statusLabel.ForeColor = Color.Red;
                }
                else if (status.Contains("已连接"))
                {
                    _isConnected = true;
                    _connectButton.Text = "断开";
                    _connectButton.Enabled = true;
                    _statusLabel.Text = status;
                    _statusLabel.ForeColor = Color.Green;
                }
                else
                {
                    _statusLabel.Text = status;
                    _statusLabel.ForeColor = Color.Orange;
                }
            }));
        }

        private void SettingsButton_Click(object? sender, EventArgs e)
        {
            var settingsForm = new SettingsForm(_configService);
            settingsForm.ConfigChanged += async (config) =>
            {
                // 更新通知服务的配置
                _notificationService?.Dispose();
                _notificationService = new NotificationService(_notifyIcon, config);
                
                // 配置更改后重新连接
                if (_isConnected)
                {
                    await DisconnectFromGotify();
                    ConnectToGotify();
                }
                else
                {
                    // 如果没有连接，但现在有了完整配置，则尝试连接
                    if (!string.IsNullOrEmpty(config.ServerUrl) && !string.IsNullOrEmpty(config.ClientToken))
                    {
                        ConnectToGotify();
                    }
                }
            };
            settingsForm.ShowDialog(this);
        }

        private void MainForm_Resize(object? sender, EventArgs e)
        {
            if (_configService.Config.MinimizeToTray && WindowState == FormWindowState.Minimized)
            {
                Hide();
                ShowInTaskbar = false;
                _notificationService.StopBlinking();
            }
        }

        private void MainForm_FormClosing(object? sender, FormClosingEventArgs e)
        {
            if (!_forceExit && _configService.Config.MinimizeToTray)
            {
                e.Cancel = true;
                Hide();
                ShowInTaskbar = false;
            }
            else
            {
                // 执行真正的退出
                _webSocketService?.DisconnectAsync();
                _notifyIcon.Visible = false;
                Application.Exit();
            }
        }

        private void NotifyIcon_MouseClick(object? sender, MouseEventArgs e)
        {
            // 只响应左键单击，右键用于显示上下文菜单
            if (e.Button == MouseButtons.Left)
            {
                ShowMainWindow();
            }
        }

        private void ShowMainWindow()
        {
            Show();
            ShowInTaskbar = true;
            WindowState = FormWindowState.Normal;
            Activate();
            _notificationService.StopBlinking();
        }

        private void ClearButton_Click(object? sender, EventArgs e)
        {
            // 清除历史记录
            _messageHistoryService.ClearMessages();
            
            // 清除所有消息项（但保留空状态标签）
            _messagesPanel.Controls.Clear();
            _messageItems.Clear();
            
            // 重新添加空状态标签
            if (_messagesPanel.Tag is Label emptyStateLabel)
            {
                _messagesPanel.Controls.Add(emptyStateLabel);
                emptyStateLabel.Visible = true;
            }
            
            // 更新消息计数
            UpdateMessageCount();
        }

        private void AddMessageItem(GotifyMessage message, string? appName = null)
        {
            // 隐藏空状态标签
            var emptyStateLabel = _messagesPanel.Tag as Label;
            if (emptyStateLabel != null)
            {
                emptyStateLabel.Visible = false;
            }

            var messageItem = new MessageItem(message, appName);
            
            // 动态调整消息项宽度以适应面板，确保不会超出
            var availableWidth = _messagesPanel.ClientSize.Width - 30; // 减去边距和滚动条预留空间
            messageItem.Width = Math.Max(600, availableWidth); // 设置合适的最小宽度
            
            // 插入到顶部
            messageItem.Location = new Point(5, 5);
            
            // 移动现有项目向下
            foreach (Control control in _messagesPanel.Controls)
            {
                if (control != emptyStateLabel) // 跳过空状态标签
                {
                    control.Location = new Point(control.Location.X, control.Location.Y + messageItem.Height + 8);
                }
            }
            
            _messagesPanel.Controls.Add(messageItem);
            _messageItems.Insert(0, messageItem);
            
            // 限制消息数量
            if (_messageItems.Count > 100)
            {
                var lastItem = _messageItems[_messageItems.Count - 1];
                _messagesPanel.Controls.Remove(lastItem);
                _messageItems.RemoveAt(_messageItems.Count - 1);
                lastItem.Dispose();
            }
            
            // 更新消息计数
            UpdateMessageCount();
            
            // 确保滚动到顶部显示最新消息
            _messagesPanel.VerticalScroll.Value = 0;
            _messagesPanel.PerformLayout();
        }

        private async void LoadHistoryMessages()
        {
            try
            {
                var historyMessages = _messageHistoryService.GetMessages();
                
                // 如果有配置且历史消息不为空，初始化应用服务以获取应用名称
                GotifyApplicationService? tempApplicationService = null;
                var config = _configService.Config;
                if (historyMessages.Count > 0 && 
                    !string.IsNullOrEmpty(config.ServerUrl) && 
                    !string.IsNullOrEmpty(config.ClientToken))
                {
                    try
                    {
                        tempApplicationService = new GotifyApplicationService(config.ServerUrl, config.ClientToken);
                    }
                    catch (Exception)
                    {
                        // 如果初始化失败，将使用默认应用名称
                    }
                }
                
                // 按时间倒序处理历史消息（最新的在前）
                var sortedMessages = historyMessages.OrderByDescending(m => m.Date).ToList();
                
                // 在UI线程上添加历史消息
                foreach (var message in sortedMessages)
                {
                    // 如果消息已经有应用名称，直接使用；否则异步获取
                    string appName = message.AppName ?? $"应用 #{message.AppId}";
                    
                    // 如果消息没有应用名称且有可用的应用服务，尝试获取
                    if (string.IsNullOrEmpty(message.AppName) && tempApplicationService != null)
                    {
                        try
                        {
                            appName = await tempApplicationService.GetApplicationNameAsync(message.AppId);
                        }
                        catch (Exception)
                        {
                            // 如果获取失败，使用默认名称
                        }
                    }
                    
                    // 添加到界面，但不重复保存到历史记录
                    AddMessageItemFromHistory(message, appName);
                }
                
                // 清理临时应用服务
                tempApplicationService?.Dispose();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"加载历史消息失败: {ex.Message}");
            }
        }

        private void AddMessageItemFromHistory(GotifyMessage message, string? appName = null)
        {
            // 类似AddMessageItem，但不保存到历史记录
            if (_messagesPanel.InvokeRequired)
            {
                _messagesPanel.Invoke(new Action(() => AddMessageItemFromHistory(message, appName)));
                return;
            }
            
            // 隐藏空状态标签
            var emptyStateLabel = _messagesPanel.Tag as Label;
            if (emptyStateLabel != null)
            {
                emptyStateLabel.Visible = false;
            }

            var messageItem = new MessageItem(message, appName);
            
            // 动态调整消息项宽度以适应面板，确保不会超出
            var availableWidth = _messagesPanel.ClientSize.Width - 30; // 减去边距和滚动条预留空间
            messageItem.Width = Math.Max(600, availableWidth); // 设置合适的最小宽度
            
            // 历史消息添加到底部，但保持最新的在上面
            var yPosition = 5;
            if (_messageItems.Count > 0)
            {
                var lastItem = _messageItems[_messageItems.Count - 1];
                yPosition = lastItem.Location.Y + lastItem.Height + 8;
            }
            
            messageItem.Location = new Point(5, yPosition);
            
            _messagesPanel.Controls.Add(messageItem);
            _messageItems.Add(messageItem);
            
            // 更新消息计数
            UpdateMessageCount();
        }

        private void UpdateMessageCount()
        {
            // 查找并更新消息计数标签
            foreach (Control control in Controls)
            {
                if (control is Panel mainPanel && mainPanel.Dock == DockStyle.Fill)
                {
                    foreach (Control subControl in mainPanel.Controls)
                    {
                        if (subControl is Panel messageAreaPanel)
                        {
                            foreach (Control headerControl in messageAreaPanel.Controls)
                            {
                                if (headerControl is Panel headerPanel && headerPanel.Location.Y == 0)
                                {
                                    foreach (Control labelControl in headerPanel.Controls)
                                    {
                                        if (labelControl is Label countLabel && countLabel.TextAlign == ContentAlignment.MiddleRight)
                                        {
                                            countLabel.Text = $"{_messageItems.Count} 条消息";
                                            return;
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _webSocketService?.Dispose();
                _applicationService?.Dispose();
                _notificationService?.Dispose();
                _notifyIcon?.Dispose();
                _contextMenu?.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
