using GotifyClient.Models;

namespace GotifyClient.Controls
{
    public class MessageItem : Panel
    {
        private readonly GotifyMessage _message;
        private readonly string _appName;
        private readonly Label _titleLabel;
        private readonly Label _messageLabel;
        private readonly Label _timeLabel;
        private readonly Label _priorityLabel;
        private readonly Panel _priorityBar;

        public MessageItem(GotifyMessage message, string? appName = null)
        {
            _message = message;
            _appName = appName ?? $"应用 #{message.AppId}";
            
            // 在构造函数中初始化所有 readonly 字段
            _titleLabel = new Label();
            _messageLabel = new Label();
            _timeLabel = new Label();
            _priorityLabel = new Label();
            _priorityBar = new Panel();
            
            InitializeComponents();
            LoadMessageData();
        }

        private void InitializeComponents()
        {
            // 设置面板属性 - 调整为更紧凑的布局，确保不超出父容器
            Size = new Size(650, 100);
            Margin = new Padding(2);
            BackColor = Color.White;
            BorderStyle = BorderStyle.None;
            Padding = new Padding(10);

            // 配置优先级指示条 - 更现代的设计
            _priorityBar.Size = new Size(5, 80);
            _priorityBar.Location = new Point(0, 10);
            _priorityBar.BackColor = GetPriorityColor(_message.Priority);
            
            // 添加圆角效果
            _priorityBar.Paint += (s, e) =>
            {
                var rect = new Rectangle(0, 0, _priorityBar.Width, _priorityBar.Height);
                using (var brush = new SolidBrush(GetPriorityColor(_message.Priority)))
                {
                    e.Graphics.FillRectangle(brush, rect);
                }
            };

            // 配置标题标签 - 更大更突出，增加宽度
            _titleLabel.Font = new Font("微软雅黑", 11, FontStyle.Bold);
            _titleLabel.ForeColor = Color.FromArgb(33, 37, 41);
            _titleLabel.Location = new Point(20, 12);
            _titleLabel.Size = new Size(520, 22);
            _titleLabel.AutoEllipsis = true;

            // 配置消息内容标签 - 支持多行显示，更好的间距，增加宽度
            _messageLabel.Font = new Font("微软雅黑", 9);
            _messageLabel.ForeColor = Color.FromArgb(108, 117, 125);
            _messageLabel.Location = new Point(20, 50);
            _messageLabel.Size = new Size(520, 35);
            _messageLabel.AutoEllipsis = false;
            _messageLabel.AutoSize = false;

            // 添加应用名称标签
            var appNameLabel = new Label
            {
                Text = _appName,
                Font = new Font("微软雅黑", 8),
                ForeColor = Color.FromArgb(153, 153, 153),
                Location = new Point(20, 32),
                Size = new Size(200, 15),
                AutoEllipsis = true
            };

            // 配置时间标签 - 右对齐，位置更靠左
            _timeLabel.Font = new Font("微软雅黑", 8);
            _timeLabel.ForeColor = Color.FromArgb(153, 153, 153);
            _timeLabel.Location = new Point(550, 12);
            _timeLabel.Size = new Size(90, 16);
            _timeLabel.TextAlign = ContentAlignment.TopRight;

            // 配置优先级色块 - 只显示背景色，不显示文字，稍微下移
            _priorityLabel.Font = new Font("微软雅黑", 9, FontStyle.Bold);
            _priorityLabel.Location = new Point(580, 32); // Y坐标由20改为32，下移12px
            _priorityLabel.Size = new Size(30, 25);
            _priorityLabel.TextAlign = ContentAlignment.MiddleCenter;
            _priorityLabel.BackColor = GetPriorityColor(_message.Priority);
            _priorityLabel.ForeColor = Color.White;
            _priorityLabel.Text = ""; // 不显示文字
            
            // 添加圆角效果到优先级色块
            _priorityLabel.Paint += (s, e) =>
            {
                var rect = new Rectangle(0, 0, _priorityLabel.Width - 1, _priorityLabel.Height - 1);
                using (var brush = new SolidBrush(GetPriorityColor(_message.Priority)))
                {
                    e.Graphics.FillRectangle(brush, rect);
                }
            };

            // 添加优先度数字显示 - 位于色块下方，整体下移
            var iconLabel = new Label
            {
                Font = new Font("微软雅黑", 12, FontStyle.Bold),
                Text = GetAppIcon(_message.Priority),
                Location = new Point(580, 62), // Y坐标由50改为62，下移12px
                Size = new Size(30, 25),
                ForeColor = GetPriorityColor(_message.Priority),
                TextAlign = ContentAlignment.MiddleCenter
            };

            // 添加控件
            Controls.Add(_priorityBar);
            Controls.Add(_titleLabel);
            Controls.Add(appNameLabel);
            Controls.Add(_messageLabel);
            Controls.Add(_timeLabel);
            Controls.Add(_priorityLabel);
            Controls.Add(iconLabel);

            // 添加悬停效果 - 更微妙的变化
            MouseEnter += (s, e) => 
            {
                BackColor = Color.FromArgb(248, 249, 250);
                // 增强优先级条的视觉效果
                _priorityBar.BackColor = Color.FromArgb(255, GetPriorityColor(_message.Priority).R,
                    GetPriorityColor(_message.Priority).G, GetPriorityColor(_message.Priority).B);
            };
            MouseLeave += (s, e) => 
            {
                BackColor = Color.White;
                _priorityBar.BackColor = GetPriorityColor(_message.Priority);
            };
            
            // 为所有子控件添加事件传递
            var allControls = new Control[] { _titleLabel, appNameLabel, _messageLabel, _timeLabel, _priorityLabel, iconLabel };
            foreach (Control control in allControls)
            {
                control.MouseEnter += (s, e) => 
                {
                    BackColor = Color.FromArgb(248, 249, 250);
                    _priorityBar.BackColor = Color.FromArgb(255, GetPriorityColor(_message.Priority).R,
                        GetPriorityColor(_message.Priority).G, GetPriorityColor(_message.Priority).B);
                };
                control.MouseLeave += (s, e) => 
                {
                    BackColor = Color.White;
                    _priorityBar.BackColor = GetPriorityColor(_message.Priority);
                };
            }

            // 添加边框绘制
            Paint += MessageItem_Paint;
        }

        private void MessageItem_Paint(object? sender, PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            
            // 绘制底部分隔线
            using (var pen = new Pen(Color.FromArgb(240, 240, 240), 1))
            {
                g.DrawLine(pen, 10, Height - 1, Width - 10, Height - 1);
            }

            // 绘制左侧阴影效果（增强视觉层次）
            using var brush = new System.Drawing.Drawing2D.LinearGradientBrush(
                new Point(0, 0), new Point(10, 0),
                Color.FromArgb(10, 0, 0, 0), Color.Transparent);
            g.FillRectangle(brush, 0, 0, 10, Height);
        }

        private void LoadMessageData()
        {
            // 显示标题，如果没有标题则显示应用名称
            var title = !string.IsNullOrEmpty(_message.Title) ? _message.Title : _appName;
            _titleLabel.Text = title;
            
            // 支持多行消息显示
            var messageText = _message.Message ?? "";
            if (messageText.Length > 80)
            {
                // 对长消息进行换行处理
                var lines = new List<string>();
                var words = messageText.Split(' ');
                var currentLine = "";
                
                foreach (var word in words)
                {
                    if ((currentLine + " " + word).Length > 40)
                    {
                        if (!string.IsNullOrEmpty(currentLine))
                        {
                            lines.Add(currentLine);
                            currentLine = word;
                        }
                        else
                        {
                            lines.Add(word);
                        }
                    }
                    else
                    {
                        currentLine = string.IsNullOrEmpty(currentLine) ? word : currentLine + " " + word;
                    }
                }
                
                if (!string.IsNullOrEmpty(currentLine))
                    lines.Add(currentLine);
                
                _messageLabel.Text = string.Join(Environment.NewLine, lines.Take(2));
                if (lines.Count > 2)
                    _messageLabel.Text += "...";
            }
            else
            {
                _messageLabel.Text = messageText;
            }
            
            _timeLabel.Text = _message.Date.ToString("MM-dd HH:mm:ss");
            // 优先度色块不需要设置文字
            _priorityLabel.BackColor = GetPriorityColor(_message.Priority);
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

        private static string GetAppIcon(int priority)
        {
            return priority.ToString(); // 显示优先度数字
        }
    }
}
