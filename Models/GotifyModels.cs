using Newtonsoft.Json;

namespace GotifyClient.Models
{
    public class GotifyMessage
    {
        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("appid")]
        public int AppId { get; set; }

        [JsonProperty("message")]
        public string Message { get; set; } = string.Empty;

        [JsonProperty("title")]
        public string Title { get; set; } = string.Empty;

        [JsonProperty("priority")]
        public int Priority { get; set; }

        [JsonProperty("date")]
        public DateTime Date { get; set; }

        // 应用名称，如果消息中包含，则使用；否则基于AppId生成
        [JsonProperty("appname")]
        public string? AppName { get; set; }
    }

    public class GotifyApplication
    {
        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("token")]
        public string Token { get; set; } = string.Empty;

        [JsonProperty("name")]
        public string Name { get; set; } = string.Empty;
    }

    public class GotifyConfig
    {
        public string ServerUrl { get; set; } = string.Empty;
        public string ClientToken { get; set; } = string.Empty;
        public bool ShowCustomNotification { get; set; } = true; // 启用自定义通知
        public bool PlaySound { get; set; } = true;
        public int NotificationDuration { get; set; } = 5000;
        public bool NotificationAutoHide { get; set; } = true; // 是否自动隐藏
        public bool NotificationNeverClose { get; set; } = false; // 永不自动关闭
        public bool MinimizeToTray { get; set; } = true;
        public bool ShowMainFormOnStartup { get; set; } = true; // 启动时显示主界面
    }
}
