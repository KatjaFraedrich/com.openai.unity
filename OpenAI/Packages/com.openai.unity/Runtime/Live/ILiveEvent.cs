// Licensed under the MIT License. See LICENSE in the project root for license information.

using Newtonsoft.Json;
using UnityEngine.Scripting;

namespace OpenAI.Live
{
    [Preserve]
    public interface ILiveEvent
    {
        [Preserve]
        [JsonProperty("event_id")]
        public string EventId { get; }

        [Preserve]
        [JsonProperty("type")]
        public string Type { get; }

        [Preserve]
        public string ToJsonString();
    }

    [Preserve]
    public interface ILiveClientEvent : ILiveEvent
    {
    }

    [Preserve]
    public interface ILiveServerEvent : ILiveEvent
    {
    }
}
