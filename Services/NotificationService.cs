using System.Media;
using GotifyClient.Models;

namespace GotifyClient.Services
{
    public class NotificationService
    {
        private readonly NotifyIcon _notifyIcon;
        private readonly System.Windows.Forms.Timer _blinkTimer;
        private readonly Icon _normalIcon;
        private readonly Icon _alertIcon;
        private bool _isBlinking = false;
        private bool _blinkState = false;
        private readonly GotifyConfig _config;

        public NotificationService(NotifyIcon notifyIcon, GotifyConfig config)
        {
            _notifyIcon = notifyIcon;
            _config = config;
            
            // 直接使用托盘图标的当前图标作为正常状态
            _normalIcon = _notifyIcon.Icon ?? SystemIcons.Application;
            _alertIcon = SystemIcons.Exclamation;
            
            _blinkTimer = new System.Windows.Forms.Timer();
            _blinkTimer.Interval = 500; // 500ms闪烁间隔
            _blinkTimer.Tick += BlinkTimer_Tick;
        }

        public void ShowNotification(GotifyMessage message)
        {
            try
            {
                // 播放声音
                if (_config.PlaySound)
                {
                    SystemSounds.Asterisk.Play();
                }

                // 显示自定义通知窗口（默认启用）
                if (_config.ShowCustomNotification)
                {
                    ShowCustomNotification(message);
                }

                // 开始图标闪烁
                StartBlinking();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"显示通知时出错: {ex.Message}");
            }
        }

        private void ShowCustomNotification(GotifyMessage message)
        {
            // 在主线程上创建和显示通知窗口
            if (Application.OpenForms.Count > 0)
            {
                var mainForm = Application.OpenForms[0];
                if (mainForm != null)
                {
                    mainForm.Invoke(new Action(() =>
                    {
                        var notificationForm = new GotifyClient.Forms.NotificationForm(
                            message, 
                            _config.NotificationAutoHide, 
                            _config.NotificationDuration);
                        notificationForm.Show();
                    }));
                }
            }
        }

        private void StartBlinking()
        {
            if (!_isBlinking)
            {
                _isBlinking = true;
                _blinkState = false;
                _blinkTimer.Start();
            }
        }

        public void StopBlinking()
        {
            if (_isBlinking)
            {
                _isBlinking = false;
                _blinkTimer.Stop();
                _notifyIcon.Icon = _normalIcon;
            }
        }

        private void BlinkTimer_Tick(object? sender, EventArgs e)
        {
            _blinkState = !_blinkState;
            _notifyIcon.Icon = _blinkState ? _alertIcon : _normalIcon;
        }

        public void Dispose()
        {
            _blinkTimer?.Dispose();
        }
    }
}
