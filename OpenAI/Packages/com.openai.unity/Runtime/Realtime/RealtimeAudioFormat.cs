// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Runtime.Serialization;

namespace OpenAI.Realtime
{
    public enum RealtimeAudioFormat
    {
        [EnumMember(Value = "pcm16")]
        PCM16,
        [EnumMember(Value = "g711_ulaw")]
        G771_uLaw,
        [EnumMember(Value = "g711_alaw")]
        G771_ALaw,
    }
}
