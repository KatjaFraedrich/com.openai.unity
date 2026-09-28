// Licensed under the MIT License. See LICENSE in the project root for license information.

using Newtonsoft.Json;
using UnityEngine.Scripting;

namespace OpenAI.Live
{
    [Preserve]
    public sealed class LiveAudioConfiguration
    {
        [Preserve]
        [JsonConstructor]
        public LiveAudioConfiguration(LiveAudioFormat format = null, LiveAudioOutput output = null, bool omitDefaultFormat = false)
        {
            Format = omitDefaultFormat ? format : format ?? new LiveAudioFormat();
            Output = output ?? new LiveAudioOutput();
        }

        [Preserve]
        [JsonProperty("format", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public LiveAudioFormat Format { get; }

        [Preserve]
        [JsonProperty("output", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public LiveAudioOutput Output { get; }
    }

    [Preserve]
    public sealed class LiveAudioOutput
    {
        [Preserve]
        [JsonConstructor]
        public LiveAudioOutput(string voice = null) => Voice = string.IsNullOrWhiteSpace(voice) ? "marin" : voice;

        [Preserve]
        [JsonProperty("voice", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string Voice { get; }
    }
}
