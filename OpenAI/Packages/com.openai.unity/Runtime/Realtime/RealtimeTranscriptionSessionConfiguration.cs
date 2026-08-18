// Licensed under the MIT License. See LICENSE in the project root for license information.

using Newtonsoft.Json;
using OpenAI.Models;
using System.Collections.Generic;
using UnityEngine.Scripting;

namespace OpenAI.Realtime
{
    [Preserve]
    [JsonConverter(typeof(RealtimeTranscriptionSessionConfigurationConverter))]
    public sealed class RealtimeTranscriptionSessionConfiguration
    {
        [Preserve]
        public RealtimeTranscriptionSessionConfiguration(
            Model model = null,
            string language = null,
            RealtimeTranscriptionDelay? delay = null,
            RealtimeAudioFormat inputAudioFormat = RealtimeAudioFormat.PCM16,
            NoiseReductionSettings inputAudioNoiseSettings = null,
            IVoiceActivityDetectionSettings turnDetectionSettings = null,
            IEnumerable<string> include = null,
            int? expiresAfter = null)
            : this(
                new RealtimeTranscriptionSettings(model, language, delay),
                inputAudioFormat,
                inputAudioNoiseSettings,
                turnDetectionSettings,
                include,
                expiresAfter)
        {
        }

        [Preserve]
        public RealtimeTranscriptionSessionConfiguration(
            RealtimeTranscriptionSettings transcriptionSettings,
            RealtimeAudioFormat inputAudioFormat = RealtimeAudioFormat.PCM16,
            NoiseReductionSettings inputAudioNoiseSettings = null,
            IVoiceActivityDetectionSettings turnDetectionSettings = null,
            IEnumerable<string> include = null,
            int? expiresAfter = null)
        {
            ClientSecret = new ClientSecret(expiresAfter);
            InputAudioFormat = inputAudioFormat;
            InputAudioNoiseReduction = inputAudioNoiseSettings;
            TranscriptionSettings = transcriptionSettings ?? new RealtimeTranscriptionSettings();
            VoiceActivityDetectionSettings = turnDetectionSettings;
            Include = include == null ? null : new List<string>(include);
        }

        [Preserve]
        internal RealtimeTranscriptionSessionConfiguration(
            string id,
            string @object,
            string type,
            ClientSecret clientSecret,
            RealtimeAudioFormat inputAudioFormat,
            NoiseReductionSettings inputAudioNoiseSettings,
            RealtimeTranscriptionSettings transcriptionSettings,
            IVoiceActivityDetectionSettings voiceActivityDetectionSettings,
            IReadOnlyList<string> include)
        {
            Id = id;
            Object = @object;
            Type = type;
            ClientSecret = clientSecret;
            InputAudioFormat = inputAudioFormat;
            InputAudioNoiseReduction = inputAudioNoiseSettings;
            TranscriptionSettings = transcriptionSettings;
            VoiceActivityDetectionSettings = voiceActivityDetectionSettings;
            Include = include;
        }

        [Preserve]
        [JsonProperty("id", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string Id { get; private set; }

        [Preserve]
        [JsonProperty("object", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string Object { get; private set; }

        [Preserve]
        [JsonProperty("type", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string Type { get; private set; } = "transcription";

        [Preserve]
        [JsonProperty("client_secret", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public ClientSecret ClientSecret { get; internal set; }

        [Preserve]
        [JsonProperty("input_audio_format", DefaultValueHandling = DefaultValueHandling.Include)]
        public RealtimeAudioFormat InputAudioFormat { get; private set; }

        [Preserve]
        [JsonProperty("input_audio_noise_reduction", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public NoiseReductionSettings InputAudioNoiseReduction { get; private set; }

        [Preserve]
        [JsonProperty("input_audio_transcription", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public RealtimeTranscriptionSettings TranscriptionSettings { get; private set; }

        [Preserve]
        [JsonProperty("turn_detection", DefaultValueHandling = DefaultValueHandling.Ignore)]
        [JsonConverter(typeof(VoiceActivityDetectionSettingsConverter))]
        public IVoiceActivityDetectionSettings VoiceActivityDetectionSettings { get; private set; }

        [Preserve]
        [JsonProperty("include", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public IReadOnlyList<string> Include { get; private set; }
    }
}
