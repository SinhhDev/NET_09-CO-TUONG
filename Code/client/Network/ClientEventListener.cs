using System;
using System.Threading.Tasks;
using System.Windows.Forms;
using CoTuongOnline.Network;

namespace CoTuongOnline.Client
{
    public class ClientEventListener : IDisposable
    {
        private readonly SocketClient _socketClient;
        private readonly PacketParser _packetParser;

        public event Action<bool>? OnGameStart;
        public event Action<int, int, int, int>? OnOpponentMove;
        public event Action<string>? OnChatReceived;
        public event Action? OnOpponentSurrender;
        public event Action<string>? OnGameEnd;

        public ClientEventListener()
        {
            _socketClient = new SocketClient();
            _packetParser = new PacketParser();

            _socketClient.DataReceived += _packetParser.ReceiveData;
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
                case PacketType.GameStart:
                    if (PacketParser.ParseGameStart(data, out bool isRed))
                        OnGameStart?.Invoke(isRed);
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

                case PacketType.GameEnd:
                    OnGameEnd?.Invoke(data);
                    break;
            }
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

        public void Dispose()
        {
            _socketClient.Dispose();
        }
    }
}
