// Licensed under the MIT License. See LICENSE in the project root for license information.

using UnityEngine.Scripting;

namespace OpenAI.Realtime
{
    [Preserve]
    public sealed class RealtimeWebRTCSessionResponse
    {
        [Preserve]
        public RealtimeWebRTCSessionResponse(string sdp, long statusCode = 0)
        {
            Sdp = sdp;
            StatusCode = statusCode;
        }

        [Preserve]
        public string Sdp { get; }

        [Preserve]
        public long StatusCode { get; }
    }
}
