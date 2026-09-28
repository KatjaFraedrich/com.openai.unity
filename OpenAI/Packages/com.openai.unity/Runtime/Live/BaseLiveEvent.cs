// Licensed under the MIT License. See LICENSE in the project root for license information.

using Newtonsoft.Json;
using UnityEngine.Scripting;

namespace OpenAI.Live
{
    [Preserve]
    public abstract class BaseLiveEvent : ILiveEvent
    {
        public abstract string EventId { get; internal set; }

        public abstract string Type { get; }

        [Preserve]
        public virtual string ToJsonString() => JsonConvert.SerializeObject(this, OpenAIClient.JsonSerializationOptions);
    }
}
