// Licensed under the MIT License. See LICENSE in the project root for license information.

using Newtonsoft.Json;
using UnityEngine.Scripting;

namespace OpenAI.Realtime
{
    [Preserve]
    public sealed class UpdateTranscriptionSessionRequest : BaseRealtimeEvent, IClientEvent
    {
        [Preserve]
        public UpdateTranscriptionSessionRequest(RealtimeTranscriptionSessionConfiguration session)
        {
            Session = session;
            if (Session != null)
            {
                Session.ClientSecret = null;
            }
        }

        /// <inheritdoc />
        [Preserve]
        [JsonProperty("event_id")]
        public override string EventId { get; internal set; }

        /// <inheritdoc />
        [Preserve]
        [JsonProperty("type")]
        public override string Type { get; } = "session.update";

        [Preserve]
        [JsonProperty("session")]
        public RealtimeTranscriptionSessionConfiguration Session { get; }
    }
}
