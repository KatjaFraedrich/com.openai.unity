// Licensed under the MIT License. See LICENSE in the project root for license information.

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using OpenAI.Realtime;
using System;
using System.Collections.Generic;

namespace OpenAI
{
    internal sealed class RealtimeSessionConfigurationConverter : JsonConverter<SessionConfiguration>
    {
        public override void WriteJson(JsonWriter writer, SessionConfiguration value, JsonSerializer serializer)
        {
            if (value == null)
            {
                writer.WriteNull();
                return;
            }

            ToJObject(value, serializer, includeClientSecret: value.ClientSecret != null).WriteTo(writer);
        }

        public override SessionConfiguration ReadJson(JsonReader reader, Type objectType, SessionConfiguration existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.Null)
            {
                return null;
            }

            var json = JObject.Load(reader);
            var audio = json["audio"] as JObject;
            var input = audio?["input"] as JObject;
            var output = audio?["output"] as JObject;

            return new SessionConfiguration(
                clientSecret: json["client_secret"]?.ToObject<ClientSecret>(serializer),
                modalities: ReadModalities(json),
                model: json.Value<string>("model"),
                instructions: json.Value<string>("instructions"),
                voice: output?.Value<string>("voice") ?? json.Value<string>("voice"),
                inputAudioFormat: ReadAudioFormat(input?["format"] ?? json["input_audio_format"]),
                outputAudioFormat: ReadAudioFormat(output?["format"] ?? json["output_audio_format"]),
                inputAudioTranscriptionSettings: (input?["transcription"] ?? json["input_audio_transcription"])?.ToObject<InputAudioTranscriptionSettings>(serializer),
                speed: output?.Value<float?>("speed") ?? json.Value<float?>("speed"),
                voiceActivityDetectionSettings: ReadVoiceActivityDetectionSettings(input?["turn_detection"] ?? json["turn_detection"], serializer),
                tools: json["tools"]?.ToObject<List<Function>>(serializer),
                toolChoice: json["tool_choice"]?.ToObject<object>(serializer),
                temperature: json.Value<float?>("temperature"),
                maxResponseOutputTokens: json["max_response_output_tokens"]?.ToObject<object>(serializer),
                inputAudioNoiseReductionSettings: (input?["noise_reduction"] ?? json["input_audio_noise_reduction"])?.ToObject<NoiseReductionSettings>(serializer),
                prompt: json["prompt"]?.ToObject<Prompt>(serializer));
        }

        internal static JObject ToJObject(SessionConfiguration value, JsonSerializer serializer, bool includeClientSecret)
        {
            var json = new JObject
            {
                ["type"] = "realtime"
            };

            WriteIfNotNull(json, "model", value.Model);
            WriteIfNotNull(json, "instructions", value.Instructions);
            WriteIfNotNull(json, "prompt", value.Prompt == null ? null : JToken.FromObject(value.Prompt, serializer));
            WriteIfNotNull(json, "output_modalities", WriteModalities(value.Modalities));
            WriteIfNotNull(json, "tools", value.Tools == null ? null : JToken.FromObject(value.Tools, serializer));
            WriteIfNotNull(json, "tool_choice", value.ToolChoice == null ? null : JToken.FromObject(value.ToolChoice, serializer));
            WriteIfNotNull(json, "temperature", value.Temperature);
            WriteIfNotNull(json, "max_response_output_tokens", value.MaxResponseOutputTokens == null ? null : JToken.FromObject(value.MaxResponseOutputTokens, serializer));

            if (includeClientSecret && value.ClientSecret != null)
            {
                json["client_secret"] = JToken.FromObject(value.ClientSecret, serializer);
            }

            var input = new JObject
            {
                ["format"] = WriteAudioFormat(value.InputAudioFormat)
            };
            WriteIfNotNull(input, "transcription", value.InputAudioTranscriptionSettings == null ? null : JToken.FromObject(value.InputAudioTranscriptionSettings, serializer));
            WriteIfNotNull(input, "turn_detection", value.VoiceActivityDetectionSettings == null ? null : JToken.FromObject(value.VoiceActivityDetectionSettings, serializer));
            WriteIfNotNull(input, "noise_reduction", value.InputAudioNoiseReduction == null ? null : JToken.FromObject(value.InputAudioNoiseReduction, serializer));

            var output = new JObject
            {
                ["format"] = WriteAudioFormat(value.OutputAudioFormat)
            };
            WriteIfNotNull(output, "voice", value.Voice);
            WriteIfNotNull(output, "speed", value.Speed);

            json["audio"] = new JObject
            {
                ["input"] = input,
                ["output"] = output
            };

            return json;
        }

        internal static void WriteIfNotNull(JObject json, string propertyName, object value)
        {
            if (value == null)
            {
                return;
            }

            json[propertyName] = value is JToken token ? token : JToken.FromObject(value);
        }

        private static JArray WriteModalities(Modality modalities)
        {
            var output = new JArray();

            if (modalities.HasFlag(Modality.Audio))
            {
                output.Add("audio");
            }
            else if (modalities.HasFlag(Modality.Text))
            {
                output.Add("text");
            }

            return output.Count > 0 ? output : null;
        }

        private static Modality ReadModalities(JObject json)
        {
            var token = json["output_modalities"] ?? json["modalities"];

            if (token is not JArray array)
            {
                return Modality.None;
            }

            var modalities = Modality.None;

            foreach (var item in array)
            {
                modalities |= item.Value<string>() switch
                {
                    "text" => Modality.Text,
                    "audio" => Modality.Audio,
                    _ => Modality.None
                };
            }

            return modalities;
        }

        internal static JObject WriteAudioFormat(RealtimeAudioFormat format)
        {
            var json = new JObject();

            switch (format)
            {
                case RealtimeAudioFormat.G771_uLaw:
                    json["type"] = "audio/pcmu";
                    break;
                case RealtimeAudioFormat.G771_ALaw:
                    json["type"] = "audio/pcma";
                    break;
                default:
                    json["type"] = "audio/pcm";
                    json["rate"] = 24000;
                    break;
            }

            return json;
        }

        internal static RealtimeAudioFormat ReadAudioFormat(JToken token)
        {
            var value = token switch
            {
                JObject json => json.Value<string>("type"),
                JValue scalar => scalar.Value?.ToString(),
                _ => null
            };

            return value switch
            {
                "audio/pcmu" or "g711_ulaw" or "g771_ulaw" => RealtimeAudioFormat.G771_uLaw,
                "audio/pcma" or "g711_alaw" or "g771_alaw" => RealtimeAudioFormat.G771_ALaw,
                _ => RealtimeAudioFormat.PCM16
            };
        }

        internal static IVoiceActivityDetectionSettings ReadVoiceActivityDetectionSettings(JToken token, JsonSerializer serializer)
        {
            if (token == null || token.Type == JTokenType.Null)
            {
                return null;
            }

            var type = token.Value<string>("type") ?? "disabled";

            return type switch
            {
                "server_vad" => token.ToObject<ServerVAD>(serializer),
                "semantic_vad" => token.ToObject<SemanticVAD>(serializer),
                _ => new DisabledVAD()
            };
        }
    }
}
