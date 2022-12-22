using Fleck;
using Newtonsoft.Json;

namespace MSSpeechLink
{
    public enum ServiceType
    {
        WebSocketServer = 0,
        SpeechRecognition,
        SpeechSynthesis
    }

    public partial class SpeechLink
    {
        private WebSocketServer _webSocketServer;
        private List<IWebSocketConnection> _clients;

        public SpeechLink(string lang, string ip, int port)
        {
            StartWebSocketServer(ip, port);
            StartSpeechRecognition(lang);
            StartSpeechSynthesis(lang);
        }

        private void StartWebSocketServer(string ip, int port)
        {
            _clients = new List<IWebSocketConnection>();

            _webSocketServer = new WebSocketServer($"ws://{ip}:{port}");
            _webSocketServer.Start(socket =>
            {
                socket.OnOpen += () =>
                {
                    _clients.Add(socket);

                    string str = GetVoices();

                    string json = JsonConvert.SerializeObject(new MessageData
                    {
                        MessageType = MessageType.GetVoices,
                        Message = str
                    });

                    socket.Send(json);
                };

                socket.OnClose += () =>
                {
                    _clients.Remove(socket);
                };

                socket.OnMessage += OnSocketMessage;
            });

            Log(ServiceType.WebSocketServer, "WebSocketServer Initialized: ws://{ip}:{port}");
        }

        private void SendMessage(MessageType messageType, string message)
        {
            var json = JsonConvert.SerializeObject(new MessageData
            {
                MessageType = messageType,
                Message = message
            });

            foreach (IWebSocketConnection client in _clients)
            {
                if (client != null && client.IsAvailable)
                    client.Send(json);
            }
        }

        private void OnSocketMessage(string message)
        {
            MessageData data = JsonConvert.DeserializeObject<MessageData>(message);

            if (string.IsNullOrEmpty(data.Message) || _speechSynthesizer == null) return;

            switch (data.MessageType)
            {
                case MessageType.GetVoices:
                    {
                        string str = GetVoices();

                        string json = JsonConvert.SerializeObject(new MessageData
                        {
                            MessageType = MessageType.GetVoices,
                            Message = str
                        });

                        foreach (IWebSocketConnection client in _clients)
                        {
                            if (client != null && client.IsAvailable)
                                client.Send(json);
                        }
                    }
                    break;

                case MessageType.Speak:
                    SpeakAsync(data.Message);
                    break;
                case MessageType.SetLanguage:
                    ChangeCulture(data.Message);
                    break;
                case MessageType.SetVoice:
                    SelectSpeechSynthesisVoice(data.Message);
                    break;
                case MessageType.SetVoiceByIndex:
                    SelectSpeechSynthesisVoiceByIndex(int.Parse(data.Message));
                    break;
            }
        }

        private void Log(ServiceType service, string message)
        {
            Console.WriteLine($"[{service}] {message}");
        }
    }
}
