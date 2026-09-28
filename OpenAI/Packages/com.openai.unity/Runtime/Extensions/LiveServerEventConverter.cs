// Licensed under the MIT License. See LICENSE in the project root for license information.

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using OpenAI.Live;
using System;

namespace OpenAI
{
    internal class LiveServerEventConverter : JsonConverter
    {
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer) => serializer.Serialize(writer, value);

        public override bool CanConvert(Type objectType) => typeof(ILiveServerEvent) == objectType;

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            var jObject = JObject.Load(reader);
            var type = jObject["type"]!.Value<string>();
            return type switch
            {
                "error" => jObject.ToObject<LiveEventError>(serializer),
                "session.started" => jObject.ToObject<LiveSessionResponse>(serializer),
                "session.updated" => jObject.ToObject<LiveSessionResponse>(serializer),
                "session.closed" => jObject.ToObject<LiveSessionResponse>(serializer),
                "session.usage.updated" => jObject.ToObject<LiveSessionResponse>(serializer),
                "session.output_audio.delta" => jObject.ToObject<LiveAudioDeltaResponse>(serializer),
                "session.input_transcript.delta" => jObject.ToObject<LiveAudioDeltaResponse>(serializer),
                "session.output_transcript.delta" => jObject.ToObject<LiveAudioDeltaResponse>(serializer),
                "session.delegation.created" => jObject.ToObject<LiveDelegationResponse>(serializer),
                "response.event" => jObject.ToObject<LiveDelegationResponse>(serializer),
                _ when type.EndsWith(".appended") => jObject.ToObject<LiveAcknowledgementResponse>(serializer),
                _ when type.StartsWith("session.input_audio.") => jObject.ToObject<LiveAcknowledgementResponse>(serializer),
                _ => jObject.ToObject<LiveRawServerEvent>(serializer)
            };
        }
    }
}
