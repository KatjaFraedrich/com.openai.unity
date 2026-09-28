// Licensed under the MIT License. See LICENSE in the project root for license information.

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Scripting;
using Utilities.Audio;
using Utilities.Extensions;

namespace OpenAI.Live
{
    [Preserve]
    public sealed class StartLiveSessionRequest : BaseLiveEvent, ILiveClientEvent
    {
        [Preserve]
        public StartLiveSessionRequest(LiveSessionConfiguration session = null, string eventId = null)
        {
            EventId = eventId;
            Session = session ?? new LiveSessionConfiguration();
        }

        [Preserve]
        [JsonProperty("event_id", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public override string EventId { get; internal set; }

        [Preserve]
        [JsonProperty("type")]
        public override string Type { get; } = "session.start";

        [Preserve]
        [JsonProperty("session")]
        public LiveSessionConfiguration Session { get; }
    }

    [Preserve]
    public sealed class UpdateLiveSessionRequest : BaseLiveEvent, ILiveClientEvent
    {
        [Preserve]
        public UpdateLiveSessionRequest(LiveSessionConfiguration session, string eventId = null)
        {
            EventId = eventId;
            Session = session;
        }

        [Preserve]
        [JsonProperty("event_id", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public override string EventId { get; internal set; }

        [Preserve]
        [JsonProperty("type")]
        public override string Type { get; } = "session.update";

        [Preserve]
        [JsonProperty("session")]
        public LiveSessionConfiguration Session { get; }
    }

    [Preserve]
    public sealed class CloseLiveSessionRequest : BaseLiveEvent, ILiveClientEvent
    {
        [Preserve]
        public CloseLiveSessionRequest(string eventId = null) => EventId = eventId;

        [Preserve]
        [JsonProperty("event_id", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public override string EventId { get; internal set; }

        [Preserve]
        [JsonProperty("type")]
        public override string Type { get; } = "session.close";
    }

    [Preserve]
    public sealed class LiveInputAudioAppendRequest : BaseLiveEvent, ILiveClientEvent
    {
        [Preserve]
        public LiveInputAudioAppendRequest(AudioClip audioClip) => Audio = Convert.ToBase64String(audioClip.EncodeToPCM(outputSampleRate: 24000).ToArray());

        [Preserve]
        public LiveInputAudioAppendRequest(ReadOnlyMemory<byte> audioData) : this(audioData.Span) { }

        [Preserve]
        public LiveInputAudioAppendRequest(NativeArray<byte> audioData) : this(audioData.AsSpan()) { }

        [Preserve]
        public LiveInputAudioAppendRequest(ReadOnlySpan<byte> audioData) => Audio = Convert.ToBase64String(audioData);

        [Preserve]
        public LiveInputAudioAppendRequest(byte[] audioData) => Audio = Convert.ToBase64String(audioData);

        [Preserve]
        [JsonProperty("event_id", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public override string EventId { get; internal set; }

        [Preserve]
        [JsonProperty("type")]
        public override string Type { get; } = "session.input_audio.append";

        [Preserve]
        [JsonProperty("audio")]
        public string Audio { get; }
    }

    [Preserve]
    public sealed class LiveInputAudioMuteRequest : BaseLiveEvent, ILiveClientEvent
    {
        [Preserve]
        public LiveInputAudioMuteRequest(string eventId = null) => EventId = eventId;

        [Preserve]
        [JsonProperty("event_id", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public override string EventId { get; internal set; }

        [Preserve]
        [JsonProperty("type")]
        public override string Type { get; } = "session.input_audio.mute";
    }

    [Preserve]
    public sealed class LiveInputAudioUnmuteRequest : BaseLiveEvent, ILiveClientEvent
    {
        [Preserve]
        public LiveInputAudioUnmuteRequest(string eventId = null) => EventId = eventId;

        [Preserve]
        [JsonProperty("event_id", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public override string EventId { get; internal set; }

        [Preserve]
        [JsonProperty("type")]
        public override string Type { get; } = "session.input_audio.unmute";
    }

    [Preserve]
    public sealed class LiveInstructionsAppendRequest : BaseLiveEvent, ILiveClientEvent
    {
        [Preserve]
        public LiveInstructionsAppendRequest(string instructions, string delegationId = null, string eventId = null)
        {
            EventId = eventId;
            DelegationId = delegationId;
            Instructions = instructions;
        }

        [Preserve]
        [JsonProperty("event_id", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public override string EventId { get; internal set; }

        [Preserve]
        [JsonProperty("type")]
        public override string Type { get; } = "session.instructions.append";

        [Preserve]
        [JsonProperty("delegation_id", NullValueHandling = NullValueHandling.Include)]
        public string DelegationId { get; }

        [Preserve]
        [JsonProperty("instructions")]
        public string Instructions { get; }
    }

    [Preserve]
    public sealed class LiveCommentaryAppendRequest : BaseLiveEvent, ILiveClientEvent
    {
        [Preserve]
        public LiveCommentaryAppendRequest(string content, string delegationId = null, string eventId = null)
        {
            EventId = eventId;
            DelegationId = delegationId;
            Content = content;
        }

        [Preserve]
        [JsonProperty("event_id", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public override string EventId { get; internal set; }

        [Preserve]
        [JsonProperty("type")]
        public override string Type { get; } = "session.commentary.append";

        [Preserve]
        [JsonProperty("delegation_id", NullValueHandling = NullValueHandling.Include)]
        public string DelegationId { get; }

        [Preserve]
        [JsonProperty("content")]
        public string Content { get; }
    }

    [Preserve]
    public sealed class LiveThinkingAppendRequest : BaseLiveEvent, ILiveClientEvent
    {
        [Preserve]
        public LiveThinkingAppendRequest(string content, string delegationId = null, string eventId = null)
        {
            EventId = eventId;
            DelegationId = delegationId;
            Content = content;
        }

        [Preserve]
        [JsonProperty("event_id", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public override string EventId { get; internal set; }

        [Preserve]
        [JsonProperty("type")]
        public override string Type { get; } = "session.thinking.append";

        [Preserve]
        [JsonProperty("delegation_id", NullValueHandling = NullValueHandling.Include)]
        public string DelegationId { get; }

        [Preserve]
        [JsonProperty("content")]
        public string Content { get; }
    }

    [Preserve]
    public sealed class LiveResponseItemCreateRequest : BaseLiveEvent, ILiveClientEvent
    {
        [Preserve]
        public LiveResponseItemCreateRequest(JToken item, string eventId = null)
        {
            EventId = eventId;
            Item = item;
        }

        [Preserve]
        [JsonProperty("event_id", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public override string EventId { get; internal set; }

        [Preserve]
        [JsonProperty("type")]
        public override string Type { get; } = "response.item.create";

        [Preserve]
        [JsonProperty("item")]
        public JToken Item { get; }
    }

    [Preserve]
    public sealed class LiveResponseCreateRequest : BaseLiveEvent, ILiveClientEvent
    {
        [Preserve]
        public LiveResponseCreateRequest(string eventId = null) => EventId = eventId;

        [Preserve]
        [JsonProperty("event_id", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public override string EventId { get; internal set; }

        [Preserve]
        [JsonProperty("type")]
        public override string Type { get; } = "response.create";
    }
}
