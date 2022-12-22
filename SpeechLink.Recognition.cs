using Newtonsoft.Json;
using System.Globalization;
using System.Speech.Recognition;

namespace MSSpeechLink
{
    public partial class SpeechLink
    {
        private SpeechRecognitionEngine _speechRecognitionEngine;

        private void ChangeCulture(string lang)
        {
            _speechRecognitionEngine.Dispose();
            StartSpeechRecognition(lang);
        }

        private void StartSpeechRecognition(string lang)
        {
            CultureInfo culture = new(lang);
            try
            {
                _speechRecognitionEngine = new SpeechRecognitionEngine(culture);
            }
            catch (Exception)
            {
                culture = CultureInfo.CurrentCulture;
                _speechRecognitionEngine = new SpeechRecognitionEngine(culture);
                Log(ServiceType.SpeechRecognition, $"Wasn't able to change language {lang} for Speech Recognition, using system language");
            }

            _speechRecognitionEngine.LoadGrammar(new DictationGrammar());
            _speechRecognitionEngine.SpeechRecognized += OnSpeechRecognized;
            _speechRecognitionEngine.SetInputToDefaultAudioDevice();
            _speechRecognitionEngine.RecognizeAsync(RecognizeMode.Multiple);

            Log(ServiceType.SpeechRecognition, $"Initialized with {culture.Name} culture");
        }

        private void OnSpeechRecognized(object sender, SpeechRecognizedEventArgs e)
        {
            Log(ServiceType.SpeechRecognition, $"Detected: {e.Result.Text}");

            string json = JsonConvert.SerializeObject(new MessageData
            {
                MessageType = MessageType.VoiceRecognitionResult,
                Message = e.Result.Text
            });

            foreach (Fleck.IWebSocketConnection client in _clients)
            {
                if (client != null && client.IsAvailable)
                    client.Send(json);
            }
        }
    }
}
