// Licensed under the MIT License. See LICENSE in the project root for license information.

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using UnityEngine.Scripting;

namespace OpenAI.Live
{
    [Preserve]
    public sealed class LiveEventError : BaseLiveEvent, ILiveServerEvent
    {
        [Preserve]
        [JsonConstructor]
        internal LiveEventError([JsonProperty("event_id")] string eventId, [JsonProperty("type")] string type, [JsonProperty("client_event_id")] string clientEventId, [JsonProperty("error")] Error error)
        {
            EventId = eventId;
            Type = type;
            ClientEventId = clientEventId;
            Error = error;
        }

        [Preserve]
        [JsonProperty("event_id")]
        public override string EventId { get; internal set; }

        [Preserve]
        [JsonProperty("type")]
        public override string Type { get; }

        [Preserve]
        [JsonProperty("client_event_id", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string ClientEventId { get; }

        [Preserve]
        [JsonProperty("error")]
        public Error Error { get; }

        [Preserve]
        public override string ToString() => Error.ToString();

        [Preserve]
        public static implicit operator Exception(LiveEventError error) => error.Error?.Exception ?? new Exception(error.ToString());
    }

    [Preserve]
    public sealed class LiveSessionResponse : BaseLiveEvent, ILiveServerEvent
    {
        [Preserve]
        [JsonConstructor]
        internal LiveSessionResponse([JsonProperty("event_id")] string eventId, [JsonProperty("type")] string type, [JsonProperty("client_event_id")] string clientEventId, [JsonProperty("session")] LiveSessionConfiguration session, [JsonProperty("reason")] string reason, [JsonProperty("usage")] LiveSessionUsage usage)
        {
            EventId = eventId;
            Type = type;
            ClientEventId = clientEventId;
            SessionConfiguration = session;
            Reason = reason;
            Usage = usage;
        }

        [Preserve]
        [JsonProperty("event_id")]
        public override string EventId { get; internal set; }

        [Preserve]
        [JsonProperty("type")]
        public override string Type { get; }

        [Preserve]
        [JsonProperty("client_event_id", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string ClientEventId { get; }

        [Preserve]
        [JsonProperty("session", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public LiveSessionConfiguration SessionConfiguration { get; }

        [Preserve]
        [JsonProperty("reason", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string Reason { get; }

        [Preserve]
        [JsonProperty("usage", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public LiveSessionUsage Usage { get; }
    }

    [Preserve]
    public sealed class LiveAudioDeltaResponse : BaseLiveEvent, ILiveServerEvent
    {
        [Preserve]
        [JsonConstructor]
        internal LiveAudioDeltaResponse([JsonProperty("event_id")] string eventId, [JsonProperty("type")] string type, [JsonProperty("client_event_id")] string clientEventId, [JsonProperty("delta")] string delta, [JsonProperty("start_ms")] int? startMs, [JsonProperty("end_ms")] int? endMs)
        {
            EventId = eventId;
            Type = type;
            ClientEventId = clientEventId;
            Delta = delta;
            StartMs = startMs;
            EndMs = endMs;
        }

        [Preserve]
        [JsonProperty("event_id")]
        public override string EventId { get; internal set; }

        [Preserve]
        [JsonProperty("type")]
        public override string Type { get; }

        [Preserve]
        [JsonProperty("client_event_id", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string ClientEventId { get; }

        [Preserve]
        [JsonProperty("delta", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string Delta { get; }

        [Preserve]
        [JsonProperty("start_ms", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public int? StartMs { get; }

        [Preserve]
        [JsonProperty("end_ms", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public int? EndMs { get; }
    }

    [Preserve]
    public sealed class LiveAcknowledgementResponse : BaseLiveEvent, ILiveServerEvent
    {
        [Preserve]
        [JsonConstructor]
        internal LiveAcknowledgementResponse([JsonProperty("event_id")] string eventId, [JsonProperty("type")] string type, [JsonProperty("client_event_id")] string clientEventId)
        {
            EventId = eventId;
            Type = type;
            ClientEventId = clientEventId;
        }

        [Preserve]
        [JsonProperty("event_id")]
        public override string EventId { get; internal set; }

        [Preserve]
        [JsonProperty("type")]
        public override string Type { get; }

        [Preserve]
        [JsonProperty("client_event_id", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string ClientEventId { get; }

        [Preserve]
        [JsonExtensionData]
        public System.Collections.Generic.IDictionary<string, JToken> AdditionalProperties { get; private set; }
    }

    [Preserve]
    public sealed class LiveDelegationResponse : BaseLiveEvent, ILiveServerEvent
    {
        [Preserve]
        [JsonConstructor]
        internal LiveDelegationResponse([JsonProperty("event_id")] string eventId, [JsonProperty("type")] string type, [JsonProperty("delegation_id")] string delegationId, [JsonProperty("response_id")] string responseId, [JsonProperty("event")] JToken responseEvent)
        {
            EventId = eventId;
            Type = type;
            DelegationId = delegationId;
            ResponseId = responseId;
            ResponseEvent = responseEvent;
        }

        [Preserve]
        [JsonProperty("event_id")]
        public override string EventId { get; internal set; }

        [Preserve]
        [JsonProperty("type")]
        public override string Type { get; }

        [Preserve]
        [JsonProperty("delegation_id", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string DelegationId { get; }

        [Preserve]
        [JsonProperty("response_id", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string ResponseId { get; }

        [Preserve]
        [JsonProperty("event", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public JToken ResponseEvent { get; }

        [Preserve]
        [JsonExtensionData]
        public System.Collections.Generic.IDictionary<string, JToken> AdditionalProperties { get; private set; }
    }

    [Preserve]
    public sealed class LiveSessionUsage
    {
        [Preserve]
        [JsonConstructor]
        internal LiveSessionUsage([JsonProperty("seconds")] double? seconds, [JsonProperty("input_seconds")] double? inputSeconds, [JsonProperty("output_seconds")] double? outputSeconds)
        {
            Seconds = seconds;
            InputSeconds = inputSeconds;
            OutputSeconds = outputSeconds;
        }

        [Preserve]
        [JsonProperty("seconds", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public double? Seconds { get; }

        [Preserve]
        [JsonProperty("input_seconds", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public double? InputSeconds { get; }

        [Preserve]
        [JsonProperty("output_seconds", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public double? OutputSeconds { get; }

        [Preserve]
        [JsonExtensionData]
        public System.Collections.Generic.IDictionary<string, JToken> AdditionalProperties { get; private set; }
    }

    [Preserve]
    public sealed class LiveRawServerEvent : BaseLiveEvent, ILiveServerEvent
    {
        [Preserve]
        [JsonConstructor]
        internal LiveRawServerEvent([JsonProperty("event_id")] string eventId, [JsonProperty("type")] string type)
        {
            EventId = eventId;
            Type = type;
        }

        [Preserve]
        [JsonProperty("event_id")]
        public override string EventId { get; internal set; }

        [Preserve]
        [JsonProperty("type")]
        public override string Type { get; }

        [Preserve]
        [JsonExtensionData]
        public System.Collections.Generic.IDictionary<string, JToken> AdditionalProperties { get; private set; }
    }
}
