using System.Speech.Synthesis;
using System.Text;

namespace MSSpeechLink
{
    public partial class SpeechLink
    {
        private SpeechSynthesizer _speechSynthesizer;
        private InstalledVoice[] _voices;
        private int _voiceIndex;

        private void StartSpeechSynthesis(string lang)
        {
            _speechSynthesizer = new SpeechSynthesizer();
            _speechSynthesizer.SetOutputToDefaultAudioDevice();
            _speechSynthesizer.SpeakStarted += (_, _) =>
            {
                SendMessage(MessageType.SpeakStart, string.Empty);
            };
            _speechSynthesizer.SpeakCompleted += (_, _) =>
            {
                SendMessage(MessageType.SpeakEnd, string.Empty);
            };
            SpeechApiReflectionHelper.InjectOneCoreVoices(_speechSynthesizer);

            var voices = _speechSynthesizer.GetInstalledVoices();
            _voices = voices != null ? voices.ToArray() : Array.Empty<InstalledVoice>();

            Log(ServiceType.SpeechSynthesis, $"{_voices.Length} voices founds");

            SelectSpeechSynthesisVoice(lang);

            Log(ServiceType.SpeechSynthesis, $"Initialized with {lang} culture");
        }

        private void SpeakAsync(string message)
        {
            _speechSynthesizer.SpeakAsync(message);
        }

        private string GetVoices()
        {
            StringBuilder sb = new();

            sb.Append($"{_voiceIndex}_");

            foreach (var voice in _voices)
            {
                sb.Append(voice.VoiceInfo.Name);
                sb.Append("#");
                sb.Append(voice.VoiceInfo.Culture.Name);
                sb.Append("|");
            }

            return sb.ToString();
        }

        private void SelectSpeechSynthesisVoice(string lang)
        {
            if (TryGetBestVoice(lang, out InstalledVoice voice, out int index))
            {
                _speechSynthesizer.SelectVoice(voice.VoiceInfo.Name);
                _voiceIndex = index;
                Log(ServiceType.SpeechSynthesis, $"{voice.VoiceInfo.Name}/{voice.VoiceInfo.Culture.Name} voice was selected");
            }
        }

        private void SelectSpeechSynthesisVoiceByIndex(int index)
        {
            if (index > 0 && index < _voices.Length)
                _speechSynthesizer.SelectVoice(_voices[index].VoiceInfo.Name);
        }

        private bool TryGetBestVoice(string lang, out InstalledVoice voice, out int index)
        {
            for (var i = 0; i < _voices.Length; i++)
            {
                if (_voices[i].VoiceInfo.Culture.Name == lang)
                {
                    index = i;
                    voice = _voices[i];
                    return true;
                }
            }

            if (_voices != null && _voices.Length > 0)
            {
                index = 0;
                voice = _voices[0];
                return true;
            }

            index = 0;
            voice = null;
            return false;
        }
    }
}
