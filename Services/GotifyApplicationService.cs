using Newtonsoft.Json;
using GotifyClient.Models;

namespace GotifyClient.Services
{
    public class GotifyApplicationService(string serverUrl, string clientToken)
    {
        private readonly string _serverUrl = serverUrl.TrimEnd('/');
        private readonly string _clientToken = clientToken;
        private readonly Dictionary<int, string> _appNameCache = new();
        private readonly HttpClient _httpClient = new HttpClient();

        public async Task<string> GetApplicationNameAsync(int appId)
        {
            // 先检查缓存
            if (_appNameCache.TryGetValue(appId, out var cachedName))
            {
                return cachedName;
            }

            try
            {
                // 从服务器获取应用信息
                var applications = await GetApplicationsAsync();
                var app = applications?.Find(a => a.Id == appId);
                
                if (app != null && !string.IsNullOrEmpty(app.Name))
                {
                    _appNameCache[appId] = app.Name!;
                    return app.Name!;
                }
            }
            catch (Exception)
            {
                // 如果获取失败，使用默认名称
            }

            // 返回默认的应用名称
            var defaultName = $"应用 #{appId}";
            _appNameCache[appId] = defaultName;
            return defaultName;
        }

        private async Task<List<GotifyApplication>?> GetApplicationsAsync()
        {
            try
            {
                var url = $"{_serverUrl}/application?token={_clientToken}";
                var response = await _httpClient.GetStringAsync(url);
                return JsonConvert.DeserializeObject<List<GotifyApplication>>(response);
            }
            catch (Exception)
            {
                return null;
            }
        }

        public void Dispose()
        {
            _httpClient?.Dispose();
        }
    }
}
