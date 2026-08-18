// Licensed under the MIT License. See LICENSE in the project root for license information.

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using OpenAI.Realtime;
using System;
using System.Collections.Generic;

namespace OpenAI
{
    internal sealed class RealtimeTranscriptionSessionConfigurationConverter : JsonConverter<RealtimeTranscriptionSessionConfiguration>
    {
        public override void WriteJson(JsonWriter writer, RealtimeTranscriptionSessionConfiguration value, JsonSerializer serializer)
        {
            if (value == null)
            {
                writer.WriteNull();
                return;
            }

            ToJObject(value, serializer, includeClientSecret: value.ClientSecret != null).WriteTo(writer);
        }

        public override RealtimeTranscriptionSessionConfiguration ReadJson(JsonReader reader, Type objectType, RealtimeTranscriptionSessionConfiguration existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.Null)
            {
                return null;
            }

            var json = JObject.Load(reader);
            var input = (json["audio"] as JObject)?["input"] as JObject;

            return new RealtimeTranscriptionSessionConfiguration(
                id: json.Value<string>("id"),
                @object: json.Value<string>("object"),
                type: json.Value<string>("type"),
                clientSecret: json["client_secret"]?.ToObject<ClientSecret>(serializer),
                inputAudioFormat: RealtimeSessionConfigurationConverter.ReadAudioFormat(input?["format"] ?? json["input_audio_format"]),
                inputAudioNoiseSettings: (input?["noise_reduction"] ?? json["input_audio_noise_reduction"])?.ToObject<NoiseReductionSettings>(serializer),
                transcriptionSettings: (input?["transcription"] ?? json["input_audio_transcription"])?.ToObject<RealtimeTranscriptionSettings>(serializer),
                voiceActivityDetectionSettings: RealtimeSessionConfigurationConverter.ReadVoiceActivityDetectionSettings(input?["turn_detection"] ?? json["turn_detection"], serializer),
                include: json["include"]?.ToObject<List<string>>(serializer));
        }

        internal static JObject ToJObject(RealtimeTranscriptionSessionConfiguration value, JsonSerializer serializer, bool includeClientSecret)
        {
            var json = new JObject
            {
                ["type"] = "transcription"
            };

            if (includeClientSecret && value.ClientSecret != null)
            {
                json["client_secret"] = JToken.FromObject(value.ClientSecret, serializer);
            }

            RealtimeSessionConfigurationConverter.WriteIfNotNull(json, "include", value.Include == null ? null : JToken.FromObject(value.Include, serializer));

            var input = new JObject
            {
                ["format"] = RealtimeSessionConfigurationConverter.WriteAudioFormat(value.InputAudioFormat)
            };
            RealtimeSessionConfigurationConverter.WriteIfNotNull(input, "transcription", value.TranscriptionSettings == null ? null : JToken.FromObject(value.TranscriptionSettings, serializer));
            RealtimeSessionConfigurationConverter.WriteIfNotNull(input, "turn_detection", value.VoiceActivityDetectionSettings == null ? null : JToken.FromObject(value.VoiceActivityDetectionSettings, serializer));
            RealtimeSessionConfigurationConverter.WriteIfNotNull(input, "noise_reduction", value.InputAudioNoiseReduction == null ? null : JToken.FromObject(value.InputAudioNoiseReduction, serializer));

            json["audio"] = new JObject
            {
                ["input"] = input
            };

            return json;
        }
    }
}
