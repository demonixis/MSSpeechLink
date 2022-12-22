using System.Speech.Synthesis;
using System.Text;

namespace MSSpeechLink
{
    public partial class SpeechLink
    {
        private SpeechSynthesizer _speechSynthesizer;

        private void StartSpeechSynthesis(string lang)
        {
            _speechSynthesizer = new SpeechSynthesizer();
            _speechSynthesizer.SetOutputToDefaultAudioDevice();

            SpeechApiReflectionHelper.InjectOneCoreVoices(_speechSynthesizer);

            SelectSpeechSynthesisVoice(lang);

            Log(ServiceType.SpeechSynthesis, $"Initialized with {lang} culture");
        }

        private string GetVoices()
        {
            System.Collections.ObjectModel.ReadOnlyCollection<InstalledVoice> voices = _speechSynthesizer.GetInstalledVoices();
            StringBuilder sb = new();

            foreach (InstalledVoice? voice in voices)
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
            InstalledVoice voice = GetBestVoice(lang);
            if (voice != null)
            {
                _speechSynthesizer.SelectVoice(voice.VoiceInfo.Name);

                Log(ServiceType.SpeechSynthesis, $"{voice.VoiceInfo.Name}/{voice.VoiceInfo.Culture.Name} voice was selected");
            }
        }

        private void SelectSpeechSynthesisVoiceByIndex(int index)
        {
            var voices = _speechSynthesizer.GetInstalledVoices();

            if (index > 0 && index < voices.Count)
                _speechSynthesizer.SelectVoice(voices[index].VoiceInfo.Name);
        }

        private InstalledVoice GetBestVoice(string lang)
        {
            System.Collections.ObjectModel.ReadOnlyCollection<InstalledVoice> voices = _speechSynthesizer.GetInstalledVoices();
            foreach (InstalledVoice? voice in voices)
            {
                if (voice.VoiceInfo.Culture.Name == lang)
                {
                    return voice;
                }
            }

            return voices.Count > 0 ? voices[0] : null;
        }
    }
}
