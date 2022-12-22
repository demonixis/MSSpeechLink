﻿namespace MSSpeechLink
{
    [Serializable]
    public enum MessageType
    {
        None = 0,
        VoiceRecognitionResult,
        Speak,
        SetLanguage,
        GetVoiceIndex,
        GetVoices,
        SetVoice,
        SetVoiceByIndex,
        SpeakStart,
        SpeakEnd
    }

    [Serializable]
    public struct MessageData
    {
        public MessageType MessageType;
        public string Message;
    }
}
