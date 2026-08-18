// Licensed under the MIT License. See LICENSE in the project root for license information.

using Newtonsoft.Json;
using OpenAI.Models;
using UnityEngine.Scripting;

namespace OpenAI.Realtime
{
    [Preserve]
    public sealed class RealtimeTranscriptionSettings
    {
        [Preserve]
        public RealtimeTranscriptionSettings(
            Model model = null,
            string language = null,
            RealtimeTranscriptionDelay? delay = null,
            string prompt = null)
        {
            Model = string.IsNullOrWhiteSpace(model?.Id) ? Models.Model.GPT_Realtime_Whisper : model;
            Language = language;
            Delay = delay;
            Prompt = prompt;
        }

        [Preserve]
        [JsonConstructor]
        internal RealtimeTranscriptionSettings(
            [JsonProperty("model")] string model,
            [JsonProperty("language")] string language = null,
            [JsonProperty("delay")] RealtimeTranscriptionDelay? delay = null,
            [JsonProperty("prompt")] string prompt = null)
        {
            Model = string.IsNullOrWhiteSpace(model) ? Models.Model.GPT_Realtime_Whisper : model;
            Language = language;
            Delay = delay;
            Prompt = prompt;
        }

        [Preserve]
        [JsonProperty("model")]
        public string Model { get; }

        [Preserve]
        [JsonProperty("language", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string Language { get; }

        [Preserve]
        [JsonProperty("delay", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public RealtimeTranscriptionDelay? Delay { get; }

        [Preserve]
        [JsonProperty("prompt", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string Prompt { get; }
    }
}
