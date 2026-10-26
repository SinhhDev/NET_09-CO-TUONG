using System;
using System.Threading.Tasks;
using CoTuongOnline.Network;

namespace CoTuongOnline.Client
{
    public class ClientEventListener : IDisposable
    {
        private readonly SocketClient _socketClient;
        private readonly PacketParser _packetParser;

        public event Action<bool, string>? OnGameStart;
        public event Action<int, int, int, int>? OnOpponentMove;
        public event Action<string>? OnChatReceived;
        public event Action? OnOpponentSurrender;
        public event Action? OnOpponentTimeout;
        public event Action<string>? OnGameEnd;
        public event Action<Exception>? OnConnectionError;
        public event Action<string>? OnLoginOK;
        public event Action<string>? OnLoginFail;

        public ClientEventListener()
        {
            _socketClient = new SocketClient();
            _packetParser = new PacketParser();

            _socketClient.DataReceived += _packetParser.ReceiveData;
            _socketClient.ConnectionError += (ex) => OnConnectionError?.Invoke(ex);
            _packetParser.OnPacketParsed += HandlePacket;
        }

        public async Task ConnectAsync(string host, int port)
        {
            await _socketClient.ConnectAsync(host, port);
            _socketClient.StartReceiveLoop();
        }

        private void HandlePacket(PacketType type, string data)
        {
            switch (type)
            {
                case PacketType.LoginOK:
                    OnLoginOK?.Invoke(data);
                    break;

                case PacketType.LoginFail:
                    OnLoginFail?.Invoke(data);
                    break;

                case PacketType.GameStart:
                    if (PacketParser.ParseGameStart(data, out bool isRed, out string opponentName))
                        OnGameStart?.Invoke(isRed, opponentName);
                    break;

                case PacketType.Move:
                    var move = PacketParser.ParseMove(data);
                    if (move.HasValue)
                        OnOpponentMove?.Invoke(move.Value.fromRow, move.Value.fromCol, move.Value.toRow, move.Value.toCol);
                    break;

                case PacketType.Chat:
                    OnChatReceived?.Invoke(data);
                    break;

                case PacketType.Surrender:
                    OnOpponentSurrender?.Invoke();
                    break;

                case PacketType.Timeout:
                    OnOpponentTimeout?.Invoke();
                    break;

                case PacketType.GameEnd:
                    OnGameEnd?.Invoke(data);
                    break;
            }
        }

        public Task SendLoginAsync(string username)
        {
            byte[] packet = Protocol.CreateLogin(username);
            return _socketClient.SendAsync(packet);
        }

        public Task SendMoveAsync(int fromRow, int fromCol, int toRow, int toCol)
        {
            byte[] packet = Protocol.CreateMove(fromRow, fromCol, toRow, toCol);
            return _socketClient.SendAsync(packet);
        }

        public Task SendChatAsync(string message)
        {
            byte[] packet = Protocol.CreateChat(message);
            return _socketClient.SendAsync(packet);
        }

        public Task SendSurrenderAsync()
        {
            byte[] packet = Protocol.CreateSurrender();
            return _socketClient.SendAsync(packet);
        }

        public Task SendTimeoutAsync()
        {
            byte[] packet = Protocol.CreateTimeout();
            return _socketClient.SendAsync(packet);
        }

        public void Dispose()
        {
            _socketClient.Dispose();
        }
    }
}
