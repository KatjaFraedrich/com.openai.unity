// Licensed under the MIT License. See LICENSE in the project root for license information.

using Newtonsoft.Json;
using UnityEngine.Scripting;

namespace OpenAI.Realtime
{
    [Preserve]
    public sealed class TranscriptionSessionResponse : BaseRealtimeEvent, IServerEvent
    {
        [Preserve]
        [JsonConstructor]
        internal TranscriptionSessionResponse(
            [JsonProperty("event_id")] string eventId,
            [JsonProperty("type")] string type,
            [JsonProperty("session")] RealtimeTranscriptionSessionConfiguration session)
        {
            EventId = eventId;
            Type = type;
            SessionConfiguration = session;
        }

        /// <inheritdoc />
        [Preserve]
        [JsonProperty("event_id")]
        public override string EventId { get; internal set; }

        /// <inheritdoc />
        [Preserve]
        [JsonProperty("type")]
        public override string Type { get; }

        /// <summary>
        /// The transcription session resource configuration.
        /// </summary>
        [Preserve]
        [JsonProperty("session")]
        public RealtimeTranscriptionSessionConfiguration SessionConfiguration { get; }
    }
}
