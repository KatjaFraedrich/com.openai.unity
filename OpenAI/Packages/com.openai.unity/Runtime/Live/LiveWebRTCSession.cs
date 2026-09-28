// Licensed under the MIT License. See LICENSE in the project root for license information.

using Newtonsoft.Json;
using UnityEngine.Scripting;

namespace OpenAI.Live
{
    [Preserve]
    public sealed class LiveWebRTCSessionRequest
    {
        [Preserve]
        public LiveWebRTCSessionRequest(string sdp, LiveSessionConfiguration session = null)
        {
            Session = session ?? new LiveSessionConfiguration();
            Transport = new LiveTransport(sdp);
        }

        [Preserve]
        [JsonConstructor]
        internal LiveWebRTCSessionRequest([JsonProperty("session")] LiveSessionConfiguration session, [JsonProperty("transport")] LiveTransport transport)
        {
            Session = session;
            Transport = transport;
        }

        [Preserve]
        [JsonProperty("session")]
        public LiveSessionConfiguration Session { get; }

        [Preserve]
        [JsonProperty("transport")]
        public LiveTransport Transport { get; }
    }

    [Preserve]
    public sealed class LiveWebRTCSessionResponse
    {
        [Preserve]
        [JsonConstructor]
        internal LiveWebRTCSessionResponse([JsonProperty("session")] LiveSessionConfiguration session, [JsonProperty("transport")] LiveTransport transport)
        {
            Session = session;
            Transport = transport;
        }

        [Preserve]
        [JsonProperty("session")]
        public LiveSessionConfiguration Session { get; }

        [Preserve]
        [JsonProperty("transport")]
        public LiveTransport Transport { get; }
    }

    [Preserve]
    public sealed class LiveTransport
    {
        [Preserve]
        [JsonConstructor]
        public LiveTransport(string sdp, string type = "webrtc")
        {
            Sdp = sdp;
            Type = type;
        }

        [Preserve]
        [JsonProperty("sdp")]
        public string Sdp { get; }

        [Preserve]
        [JsonProperty("type")]
        public string Type { get; }
    }
}
