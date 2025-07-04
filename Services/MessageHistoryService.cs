using Newtonsoft.Json;
using GotifyClient.Models;

namespace GotifyClient.Services
{
    public class MessageHistoryService
    {
        private readonly string _historyPath;
        private List<GotifyMessage> _messages;
        private readonly object _lock = new object();
        private const int MAX_MESSAGES = 1000; // 最大保存消息数量

        public MessageHistoryService()
        {
            var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var configDir = Path.Combine(appDataPath, "GotifyClient");
            
            if (!Directory.Exists(configDir))
            {
                Directory.CreateDirectory(configDir);
            }

            _historyPath = Path.Combine(configDir, "message_history.json");
            _messages = LoadMessages();
        }

        public List<GotifyMessage> GetMessages()
        {
            lock (_lock)
            {
                return new List<GotifyMessage>(_messages);
            }
        }

        public void AddMessage(GotifyMessage message)
        {
            lock (_lock)
            {
                // 添加到列表开头（最新消息在前）
                _messages.Insert(0, message);
                
                // 限制消息数量
                if (_messages.Count > MAX_MESSAGES)
                {
                    _messages.RemoveAt(_messages.Count - 1);
                }
                
                // 异步保存，避免阻塞UI
                _ = Task.Run(SaveMessages);
            }
        }

        public void ClearMessages()
        {
            lock (_lock)
            {
                _messages.Clear();
                _ = Task.Run(SaveMessages);
            }
        }

        private List<GotifyMessage> LoadMessages()
        {
            try
            {
                if (File.Exists(_historyPath))
                {
                    var json = File.ReadAllText(_historyPath);
                    var messages = JsonConvert.DeserializeObject<List<GotifyMessage>>(json);
                    return messages ?? new List<GotifyMessage>();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"加载消息历史失败: {ex.Message}");
            }

            return new List<GotifyMessage>();
        }

        private void SaveMessages()
        {
            try
            {
                lock (_lock)
                {
                    var json = JsonConvert.SerializeObject(_messages, Formatting.Indented);
                    File.WriteAllText(_historyPath, json);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"保存消息历史失败: {ex.Message}");
            }
        }
    }
}
