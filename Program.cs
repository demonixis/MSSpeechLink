using Fleck;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Speech.Recognition;
using System.Speech.Synthesis;
using System.Text;
using System.Threading;

namespace MSSpeechLink
{
    public enum ServiceType
    {
        WebSocketServer = 0,
        SpeechRecognition,
        SpeechSynthesis
    }

    internal class Program
    {
        private WebSocketServer _webSocketServer;
        private List<IWebSocketConnection> _clients;
        private SpeechRecognitionEngine _speechRecognitionEngine;
        private SpeechSynthesizer _speechSynthesizer;

        private void StartService(string lang = "fr-FR", int port = 8831)
        {
            StartWebSocketServer(port);
            StartSpeechRecognition(lang);
            StartSpeechSynthesis(lang);

            while (true)
                Thread.Sleep(100);
        }

        private void StartWebSocketServer(int port)
        {
            _clients = new List<IWebSocketConnection>();

            _webSocketServer = new WebSocketServer($"ws://127.0.0.1:{port}");
            _webSocketServer.Start(socket =>
            {
                socket.OnOpen += () =>
                {
                    _clients.Add(socket);
                };

                socket.OnClose += () =>
                {
                    _clients.Remove(socket);
                };

                socket.OnMessage += OnSocketMessage;
            });

            Console.WriteLine("WebSocketServer Initialized");
        }

        private void OnSocketMessage(string message)
        {
            var data = JsonConvert.DeserializeObject<MessageData>(message);

            if (string.IsNullOrEmpty(data.Message) || _speechSynthesizer == null) return;

            switch (data.MessageType)
            {
                case MessageType.ListVoices:
                    {
                        var voices = _speechSynthesizer.GetInstalledVoices();
                        var sb = new StringBuilder();

                        foreach (var voice in voices)
                        {
                            sb.Append(voice.VoiceInfo.Name);
                            sb.Append("#");
                            sb.Append(voice.VoiceInfo.Culture.Name);
                            sb.Append("|");
                        }

                        var str = sb.ToString();
                        foreach (var client in _clients)
                        {
                            if (client != null && client.IsAvailable)
                                client.Send(str);
                        }
                    }
                    break;

                case MessageType.TextToSpeech:
                    _speechSynthesizer.SpeakAsync(data.Message);
                    break;
                case MessageType.ChangeLang:
                    ChangeCulture(data.Message);
                    break;
                case MessageType.SelectVoice:
                    SelectSpeechSynthesisVoice(data.Message);
                    break;
            }
        }

        private void ChangeCulture(string lang)
        {
            _speechRecognitionEngine.Dispose();
            StartSpeechRecognition(lang);
            SelectSpeechSynthesisVoice(lang);
        }

        private void StartSpeechRecognition(string lang)
        {
            _speechRecognitionEngine = new SpeechRecognitionEngine(new System.Globalization.CultureInfo(lang));
            _speechRecognitionEngine.LoadGrammar(new DictationGrammar());
            _speechRecognitionEngine.SpeechRecognized += OnSpeechRecognized;
            _speechRecognitionEngine.SetInputToDefaultAudioDevice();
            _speechRecognitionEngine.RecognizeAsync(RecognizeMode.Multiple);

            Log(ServiceType.SpeechRecognition, $"Initialized with {lang} culture");
        }

        private void StartSpeechSynthesis(string lang)
        {
            _speechSynthesizer = new SpeechSynthesizer();
            _speechSynthesizer.SetOutputToDefaultAudioDevice();

            SelectSpeechSynthesisVoice(lang);

            Log(ServiceType.SpeechSynthesis, $"Initialized with {lang} culture");
        }

        private void SelectSpeechSynthesisVoice(string lang)
        {
            var voice = GetBestVoice(lang);
            if (voice != null)
            {
                _speechSynthesizer.SelectVoice(voice.VoiceInfo.Name);

                Log(ServiceType.SpeechSynthesis, $"{voice.VoiceInfo.Name} voice was selected");
            }
        }

        private InstalledVoice GetBestVoice(string lang)
        {
            var voices = _speechSynthesizer.GetInstalledVoices();
            foreach (var voice in voices)
            {
                if (voice.VoiceInfo.Culture.Name == lang)
                    return voice;
            }

            return voices.Count > 0 ? voices[0] : null;
        }

        private void OnSpeechRecognized(object sender, SpeechRecognizedEventArgs e)
        {
            Log(ServiceType.SpeechRecognition, $"Detected: {e.Result.Text}");

            var json = JsonConvert.SerializeObject(new MessageData
            {
                MessageType = MessageType.VoiceRecognized,
                Message = e.Result.Text
            });

            foreach (var client in _clients)
            {
                if (client != null && client.IsAvailable)
                    client.Send(json);
            }
        }

        private void Log(ServiceType service, string message)
        {
            Console.WriteLine($"[{service}] {message}");
        }

        static void Main(string[] args)
        {
            var p = new Program();
            p.StartService();
        }
    }
}
