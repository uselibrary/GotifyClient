using Newtonsoft.Json;
using GotifyClient.Models;

namespace GotifyClient.Services
{
    public class ConfigService
    {
        private readonly string _configPath;
        private GotifyConfig _config;

        public ConfigService()
        {
            var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var configDir = Path.Combine(appDataPath, "GotifyClient");
            
            if (!Directory.Exists(configDir))
            {
                Directory.CreateDirectory(configDir);
            }

            _configPath = Path.Combine(configDir, "config.json");
            _config = LoadConfig();
        }

        public GotifyConfig Config => _config;

        private GotifyConfig LoadConfig()
        {
            try
            {
                if (File.Exists(_configPath))
                {
                    var json = File.ReadAllText(_configPath);
                    var config = JsonConvert.DeserializeObject<GotifyConfig>(json);
                    return config ?? new GotifyConfig();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"加载配置失败: {ex.Message}");
            }

            return new GotifyConfig();
        }

        public void SaveConfig()
        {
            try
            {
                var json = JsonConvert.SerializeObject(_config, Formatting.Indented);
                File.WriteAllText(_configPath, json);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"保存配置失败: {ex.Message}");
                throw new InvalidOperationException($"保存配置失败: {ex.Message}");
            }
        }

        public void UpdateConfig(GotifyConfig newConfig)
        {
            _config = newConfig;
            SaveConfig();
        }
    }
}
