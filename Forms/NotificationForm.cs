using GotifyClient.Models;

namespace GotifyClient.Forms
{
    public partial class NotificationForm : Form
    {
        private readonly GotifyMessage _message;
        private readonly System.Windows.Forms.Timer _autoCloseTimer;
        private readonly bool _autoHide;
        private readonly int _duration;
        private bool _isPinned = false;

        public NotificationForm(GotifyMessage message, bool autoHide = true, int duration = 5000)
        {
            _message = message;
            _autoHide = autoHide;
            _duration = duration;
            InitializeComponent();
            
            // 设置自动关闭定时器
            if (_autoHide && _duration > 0)
            {
                _autoCloseTimer = new System.Windows.Forms.Timer();
                _autoCloseTimer.Interval = _duration;
                _autoCloseTimer.Tick += AutoCloseTimer_Tick;
                _autoCloseTimer.Start();
            }
            else
            {
                _autoCloseTimer = new System.Windows.Forms.Timer(); // 创建但不启动
            }
        }

        private void InitializeComponent()
        {
            SuspendLayout();
            
            // 窗体设置
            Text = "";
            Size = new Size(420, 180);
            FormBorderStyle = FormBorderStyle.None;
            ShowInTaskbar = false;
            TopMost = true;
            StartPosition = FormStartPosition.Manual;
            
            // 设置窗体位置（右上角）
            var workingArea = Screen.PrimaryScreen?.WorkingArea ?? new Rectangle(0, 0, 1024, 768);
            Location = new Point(
                workingArea.Right - Width - 10,
                workingArea.Top + 10
            );

            // 设置窗体样式
            BackColor = Color.White;
            Paint += NotificationForm_Paint;

            // 创建标题栏
            var titlePanel = new Panel
            {
                Size = new Size(420, 35),
                Location = new Point(0, 0),
                BackColor = Color.FromArgb(70, 130, 180) // 钢蓝色
            };

            var iconLabel = new Label
            {
                Text = "📢",
                Font = new Font("Segoe UI Emoji", 14, FontStyle.Bold),
                ForeColor = Color.White,
                Location = new Point(10, 5),
                Size = new Size(30, 25),
                TextAlign = ContentAlignment.MiddleCenter
            };

            var titleLabel = new Label
            {
                Text = _message.Title ?? "Gotify 通知",
                Font = new Font("微软雅黑", 11, FontStyle.Bold),
                ForeColor = Color.White,
                Location = new Point(45, 8),
                Size = new Size(200, 20),
                TextAlign = ContentAlignment.MiddleLeft
            };

            // 添加应用名称标签
            var appNameLabel = new Label
            {
                Text = !string.IsNullOrEmpty(_message.AppName) ? $"[{_message.AppName}]" : "",
                Font = new Font("微软雅黑", 9, FontStyle.Regular),
                ForeColor = Color.FromArgb(220, 220, 220), // 浅灰色
                Location = new Point(250, 9),
                Size = new Size(130, 18),
                TextAlign = ContentAlignment.MiddleLeft
            };

            var closeButton = new Button
            {
                Text = "×",
                Size = new Size(30, 25),
                Location = new Point(385, 5),
                Font = new Font("微软雅黑", 10, FontStyle.Bold),
                ForeColor = Color.White,
                BackColor = Color.Transparent,
                FlatStyle = FlatStyle.Flat,
                FlatAppearance = { BorderSize = 0 }
            };
            closeButton.Click += (s, e) => Close();
            closeButton.MouseEnter += (s, e) => closeButton.BackColor = Color.FromArgb(255, 100, 100);
            closeButton.MouseLeave += (s, e) => closeButton.BackColor = Color.Transparent;

            titlePanel.Controls.Add(iconLabel);
            titlePanel.Controls.Add(titleLabel);
            titlePanel.Controls.Add(appNameLabel);
            titlePanel.Controls.Add(closeButton);

            // 主要内容区域
            var contentPanel = new Panel
            {
                Size = new Size(420, 110),
                Location = new Point(0, 35),
                BackColor = Color.White,
                Padding = new Padding(15)
            };

            var messageLabel = new Label
            {
                Text = _message.Message ?? "",
                Font = new Font("微软雅黑", 10),
                ForeColor = Color.FromArgb(60, 60, 60),
                Location = new Point(15, 10),
                Size = new Size(390, 60),
                TextAlign = ContentAlignment.TopLeft
            };

            // 底部状态栏
            var statusPanel = new Panel
            {
                Size = new Size(420, 35),
                Location = new Point(0, 145),
                BackColor = Color.FromArgb(248, 249, 250)
            };

            var timeLabel = new Label
            {
                Text = _message.Date.ToString("MM-dd HH:mm:ss"),
                Font = new Font("微软雅黑", 8),
                ForeColor = Color.Gray,
                Location = new Point(15, 8),
                Size = new Size(120, 20),
                TextAlign = ContentAlignment.MiddleLeft
            };

            var priorityLabel = new Label
            {
                Text = GetPriorityText(_message.Priority),
                Font = new Font("微软雅黑", 8, FontStyle.Bold),
                ForeColor = GetPriorityColor(_message.Priority),
                Location = new Point(145, 8),
                Size = new Size(60, 20),
                TextAlign = ContentAlignment.MiddleLeft
            };

            var autoHideLabel = new Label
            {
                Text = GetAutoHideText(),
                Font = new Font("微软雅黑", 8),
                ForeColor = _autoHide ? Color.Orange : Color.Blue,
                Location = new Point(220, 8),
                Size = new Size(120, 20),
                TextAlign = ContentAlignment.MiddleLeft
            };

            var pinButton = new Button
            {
                Text = "📌",
                Size = new Size(25, 20),
                Location = new Point(350, 7),
                Font = new Font("Segoe UI Emoji", 8),
                FlatStyle = FlatStyle.Flat,
                FlatAppearance = { BorderSize = 0 },
                BackColor = Color.Transparent
            };
            pinButton.Click += PinButton_Click;

            statusPanel.Controls.Add(timeLabel);
            statusPanel.Controls.Add(priorityLabel);
            statusPanel.Controls.Add(autoHideLabel);
            statusPanel.Controls.Add(pinButton);

            contentPanel.Controls.Add(messageLabel);

            // 添加控件到窗体
            Controls.Add(titlePanel);
            Controls.Add(contentPanel);
            Controls.Add(statusPanel);

            // 添加工具提示
            var toolTip = new ToolTip();
            toolTip.SetToolTip(pinButton, "点击固定/取消固定通知");

            ResumeLayout(false);
        }

        private void AutoCloseTimer_Tick(object? sender, EventArgs e)
        {
            // 如果通知被固定，不自动关闭
            if (!_isPinned)
            {
                _autoCloseTimer.Stop();
                Close();
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _autoCloseTimer?.Dispose();
            }
            base.Dispose(disposing);
        }

        // 窗体淡入效果
        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            
            // 简单的淡入效果
            Opacity = 0;
            var fadeTimer = new System.Windows.Forms.Timer();
            fadeTimer.Interval = 50;
            fadeTimer.Tick += (s, args) =>
            {
                Opacity += 0.1;
                if (Opacity >= 1.0)
                {
                    fadeTimer.Stop();
                    fadeTimer.Dispose();
                }
            };
            fadeTimer.Start();
        }

        private void NotificationForm_Paint(object? sender, PaintEventArgs e)
        {
            // 绘制边框阴影效果
            var g = e.Graphics;
            var rect = new Rectangle(0, 0, Width - 1, Height - 1);

            // 绘制外边框
            using var pen = new Pen(Color.FromArgb(200, 200, 200), 2);
            g.DrawRectangle(pen, rect);
        }

        private static string GetPriorityText(int priority)
        {
            return priority switch
            {
                0 => "最低",
                1 => "低",
                2 => "普通",
                3 => "中等",
                4 => "重要",
                5 => "高",
                6 => "紧急",
                7 => "严重",
                >= 8 => "危急",
                _ => "普通"
            };
        }

        private static Color GetPriorityColor(int priority)
        {
            return priority switch
            {
                0 => Color.FromArgb(34, 139, 34),      // 森林绿 - 最低
                1 => Color.FromArgb(0, 128, 0),        // 绿色 - 低
                2 => Color.FromArgb(30, 144, 255),     // 道奇蓝 - 普通
                3 => Color.FromArgb(70, 130, 180),     // 钢蓝色 - 中等
                4 => Color.FromArgb(255, 165, 0),      // 橙色 - 重要
                5 => Color.FromArgb(255, 140, 0),      // 深橙色 - 高
                6 => Color.FromArgb(255, 69, 0),       // 橙红色 - 紧急
                7 => Color.FromArgb(220, 20, 60),      // 深红色 - 严重
                >= 8 => Color.FromArgb(139, 0, 0),     // 暗红色 - 危急
                _ => Color.FromArgb(30, 144, 255)      // 默认道奇蓝
            };
        }

        private string GetAutoHideText()
        {
            if (_isPinned)
                return "已固定";
            
            return _autoHide ? $"自动关闭 ({_duration / 1000}秒)" : "手动关闭";
        }

        private void PinButton_Click(object? sender, EventArgs e)
        {
            _isPinned = !_isPinned;
            var button = (Button)sender!;
            
            if (_isPinned)
            {
                // 固定通知，停止自动关闭计时器
                _autoCloseTimer?.Stop();
                button.BackColor = Color.LightBlue;
                button.Text = "📌";
                
                // 更新状态标签
                UpdateAutoHideLabel();
            }
            else
            {
                // 取消固定，如果原本是自动关闭的，重新启动计时器
                if (_autoHide && _duration > 0)
                {
                    _autoCloseTimer?.Start();
                }
                button.BackColor = Color.Transparent;
                button.Text = "📌";
                
                // 更新状态标签
                UpdateAutoHideLabel();
            }
        }

        private void UpdateAutoHideLabel()
        {
            // 查找状态标签并更新文本
            foreach (Control control in Controls)
            {
                if (control is Panel panel && panel.Location.Y == 145)
                {
                    foreach (Control statusControl in panel.Controls)
                    {
                        if (statusControl is Label label && label.Location.X == 220)
                        {
                            label.Text = GetAutoHideText();
                            label.ForeColor = _isPinned ? Color.Blue : (_autoHide ? Color.Orange : Color.Blue);
                            break;
                        }
                    }
                    break;
                }
            }
        }
    }
}
