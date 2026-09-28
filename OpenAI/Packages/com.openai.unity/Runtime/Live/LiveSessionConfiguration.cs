// Licensed under the MIT License. See LICENSE in the project root for license information.

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using OpenAI.Models;
using System.Collections.Generic;
using UnityEngine.Scripting;

namespace OpenAI.Live
{
    [Preserve]
    public sealed class LiveSessionConfiguration
    {
        [Preserve]
        public LiveSessionConfiguration(Model model = null, string instructions = null, LiveAudioConfiguration audio = null, LiveDelegationConfiguration delegation = null, IEnumerable<LiveInputItem> input = null, bool? store = null)
        {
            Model = string.IsNullOrWhiteSpace(model?.Id) ? Models.Model.GPT_Live_1 : model;
            Instructions = instructions;
            Audio = audio ?? new LiveAudioConfiguration();
            Delegation = delegation;
            Input = input;
            Store = store;
        }

        [Preserve]
        [JsonConstructor]
        internal LiveSessionConfiguration([JsonProperty("id")] string id, [JsonProperty("model")] string model, [JsonProperty("instructions")] string instructions, [JsonProperty("audio")] LiveAudioConfiguration audio, [JsonProperty("delegation")] LiveDelegationConfiguration delegation, [JsonProperty("input")] IEnumerable<LiveInputItem> input, [JsonProperty("store")] bool? store)
        {
            Id = id;
            Model = model;
            Instructions = instructions;
            Audio = audio;
            Delegation = delegation;
            Input = input;
            Store = store;
        }

        [Preserve]
        [JsonProperty("id", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string Id { get; }

        [Preserve]
        [JsonProperty("model", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string Model { get; }

        [Preserve]
        [JsonProperty("instructions", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string Instructions { get; }

        [Preserve]
        [JsonProperty("audio", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public LiveAudioConfiguration Audio { get; }

        [Preserve]
        [JsonProperty("delegation", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public LiveDelegationConfiguration Delegation { get; }

        [Preserve]
        [JsonProperty("input", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public IEnumerable<LiveInputItem> Input { get; }

        [Preserve]
        [JsonProperty("store", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public bool? Store { get; }

        [Preserve]
        [JsonExtensionData]
        public IDictionary<string, JToken> AdditionalProperties { get; private set; }
    }
}
