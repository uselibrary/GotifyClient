using System.Net.WebSockets;
using System.Text;
using Newtonsoft.Json;
using GotifyClient.Models;

namespace GotifyClient.Services
{
    public class GotifyWebSocketService : IDisposable
    {
        private ClientWebSocket? _webSocket;
        private CancellationTokenSource? _cancellationTokenSource;
        private readonly string _serverUrl;
        private readonly string _clientToken;
        private bool _isConnected;

        public event Action<GotifyMessage>? MessageReceived;
        public event Action<string>? ConnectionStatusChanged;

        public bool IsConnected => _isConnected;

        public GotifyWebSocketService(string serverUrl, string clientToken)
        {
            _serverUrl = serverUrl.TrimEnd('/');
            _clientToken = clientToken;
        }

        public async Task ConnectAsync()
        {
            try
            {
                _cancellationTokenSource = new CancellationTokenSource();
                _webSocket = new ClientWebSocket();

                var uri = new Uri($"{_serverUrl.Replace("http://", "ws://").Replace("https://", "wss://")}/stream?token={_clientToken}");
                
                await _webSocket.ConnectAsync(uri, _cancellationTokenSource.Token);
                
                // 确保连接状态正确设置
                if (_webSocket.State == WebSocketState.Open)
                {
                    _isConnected = true;
                    ConnectionStatusChanged?.Invoke("已连接");
                    
                    // 开始监听消息
                    _ = Task.Run(ListenForMessages, _cancellationTokenSource.Token);
                }
                else
                {
                    _isConnected = false;
                    ConnectionStatusChanged?.Invoke("连接失败: WebSocket状态异常");
                    throw new InvalidOperationException("WebSocket连接状态异常");
                }
            }
            catch (Exception ex)
            {
                _isConnected = false;
                ConnectionStatusChanged?.Invoke($"连接失败: {ex.Message}");
                throw;
            }
        }

        private async Task ListenForMessages()
        {
            var buffer = new byte[4096];
            var messageBuilder = new StringBuilder();

            try
            {
                while (_webSocket?.State == WebSocketState.Open && !_cancellationTokenSource!.Token.IsCancellationRequested)
                {
                    var result = await _webSocket.ReceiveAsync(
                        new ArraySegment<byte>(buffer), 
                        _cancellationTokenSource.Token);

                    if (result.MessageType == WebSocketMessageType.Text)
                    {
                        var messageText = Encoding.UTF8.GetString(buffer, 0, result.Count);
                        messageBuilder.Append(messageText);

                        if (result.EndOfMessage)
                        {
                            var fullMessage = messageBuilder.ToString();
                            messageBuilder.Clear();

                            try
                            {
                                var message = JsonConvert.DeserializeObject<GotifyMessage>(fullMessage);
                                if (message != null)
                                {
                                    MessageReceived?.Invoke(message);
                                }
                            }
                            catch (JsonException ex)
                            {
                                // 忽略JSON解析错误，可能是心跳包或其他格式
                                System.Diagnostics.Debug.WriteLine($"JSON解析错误: {ex.Message}");
                            }
                        }
                    }
                    else if (result.MessageType == WebSocketMessageType.Close)
                    {
                        break;
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // 正常取消操作
            }
            catch (Exception ex)
            {
                _isConnected = false;
                ConnectionStatusChanged?.Invoke($"连接断开: {ex.Message}");
            }
            finally
            {
                _isConnected = false;
                if (_webSocket?.State != WebSocketState.Closed)
                {
                    ConnectionStatusChanged?.Invoke("连接已断开");
                }
            }
        }

        public async Task DisconnectAsync()
        {
            try
            {
                _cancellationTokenSource?.Cancel();
                
                if (_webSocket?.State == WebSocketState.Open)
                {
                    await _webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "关闭连接", CancellationToken.None);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"断开连接时出错: {ex.Message}");
            }
            finally
            {
                _isConnected = false;
                ConnectionStatusChanged?.Invoke("已断开连接");
            }
        }

        public void Dispose()
        {
            _cancellationTokenSource?.Cancel();
            _webSocket?.Dispose();
            _cancellationTokenSource?.Dispose();
        }
    }
}
