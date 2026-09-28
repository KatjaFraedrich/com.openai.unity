// Licensed under the MIT License. See LICENSE in the project root for license information.

using Newtonsoft.Json;
using UnityEngine.Scripting;

namespace OpenAI.Live
{
    [Preserve]
    public sealed class LiveAudioFormat
    {
        [Preserve]
        [JsonConstructor]
        public LiveAudioFormat(string type = "audio/pcm", int? rate = 24000)
        {
            Type = type;
            Rate = rate;
        }

        [Preserve]
        [JsonProperty("type")]
        public string Type { get; }

        [Preserve]
        [JsonProperty("rate", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public int? Rate { get; }
    }
}
