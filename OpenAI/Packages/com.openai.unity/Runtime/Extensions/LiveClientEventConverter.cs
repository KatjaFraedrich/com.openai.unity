// Licensed under the MIT License. See LICENSE in the project root for license information.

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using OpenAI.Live;
using System;

namespace OpenAI
{
    internal class LiveClientEventConverter : JsonConverter
    {
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer) => serializer.Serialize(writer, value);

        public override bool CanConvert(Type objectType) => typeof(ILiveClientEvent) == objectType;

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            var jObject = JObject.Load(reader);
            var type = jObject["type"]!.Value<string>();
            return type switch
            {
                "session.start" => jObject.ToObject<StartLiveSessionRequest>(serializer),
                "session.update" => jObject.ToObject<UpdateLiveSessionRequest>(serializer),
                "session.close" => jObject.ToObject<CloseLiveSessionRequest>(serializer),
                "session.input_audio.append" => jObject.ToObject<LiveInputAudioAppendRequest>(serializer),
                "session.input_audio.mute" => jObject.ToObject<LiveInputAudioMuteRequest>(serializer),
                "session.input_audio.unmute" => jObject.ToObject<LiveInputAudioUnmuteRequest>(serializer),
                "session.instructions.append" => jObject.ToObject<LiveInstructionsAppendRequest>(serializer),
                "session.commentary.append" => jObject.ToObject<LiveCommentaryAppendRequest>(serializer),
                "session.thinking.append" => jObject.ToObject<LiveThinkingAppendRequest>(serializer),
                "response.item.create" => jObject.ToObject<LiveResponseItemCreateRequest>(serializer),
                "response.create" => jObject.ToObject<LiveResponseCreateRequest>(serializer),
                _ => throw new NotImplementedException($"Unknown live event type: {type}")
            };
        }
    }
}
