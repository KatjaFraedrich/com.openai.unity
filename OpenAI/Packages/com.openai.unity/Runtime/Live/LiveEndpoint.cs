// Licensed under the MIT License. See LICENSE in the project root for license information.

using OpenAI.Models;
using OpenAI.Extensions;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine.Scripting;
using Utilities.WebRequestRest;
using Utilities.WebSockets;

namespace OpenAI.Live
{
    [Preserve]
    public sealed class LiveEndpoint : OpenAIBaseEndpoint
    {
        [Preserve]
        public LiveEndpoint(OpenAIClient client) : base(client) { }

        protected override string Root => "live";

        [Preserve]
        public async Task<LiveSession> CreateSessionAsync(LiveSessionConfiguration configuration = null, CancellationToken cancellationToken = default) => await CreateSessionAsync(configuration, cancellationToken, null);

        [Preserve]
        public async Task<LiveSession> CreateSessionAsync(LiveSessionConfiguration configuration = null, CancellationToken cancellationToken = default, params string[] sessionOverrideJsonObjects)
        {
            configuration ??= new LiveSessionConfiguration(Model.GPT_Live_1);
            var headers = new Dictionary<string, string>(client.DefaultRequestHeaders);
#if !PLATFORM_WEBGL
            headers["User-Agent"] = "OpenAI-DotNet";
#endif
            if (headers.TryGetValue("api-key", out var apiKey))
            {
                headers.Remove("api-key");
                headers["Authorization"] = Rest.GetBearerOAuthToken(apiKey);
            }

            var websocket = new WebSocket(GetWebsocketUri("/sessions"), headers, new List<string>());
            var session = new LiveSession(websocket, EnableDebug);

            try
            {
                await session.ConnectAsync(cancellationToken).ConfigureAwait(true);
                await session.SendAsync(new StartLiveSessionRequest(configuration), cancellationToken, sessionOverrideJsonObjects).ConfigureAwait(true);
            }
            catch
            {
                session.Dispose();
                throw;
            }

            return session;
        }

        [Preserve]
        public async Task<LiveWebRTCSessionResponse> CreateWebRTCSessionAsync(string sdp, LiveSessionConfiguration configuration = null, CancellationToken cancellationToken = default) => await CreateWebRTCSessionAsync(new LiveWebRTCSessionRequest(sdp, configuration), cancellationToken);

        [Preserve]
        public async Task<LiveWebRTCSessionResponse> CreateWebRTCSessionAsync(LiveWebRTCSessionRequest request, CancellationToken cancellationToken = default)
        {
            var payload = JsonConvert.SerializeObject(request, OpenAIClient.JsonSerializationOptions);
            var response = await Rest.PostAsync(GetUrl("/sessions"), payload, new RestParameters(client.DefaultRequestHeaders), cancellationToken);
            response.Validate(EnableDebug);
            return response.Deserialize<LiveWebRTCSessionResponse>(client);
        }

        [Preserve]
        public async Task<LiveWebRTCSessionResponse> ForkWebRTCSessionAsync(string sessionId, string sdp, LiveSessionConfiguration configuration = null, CancellationToken cancellationToken = default) => await ForkWebRTCSessionAsync(sessionId, new LiveWebRTCSessionRequest(sdp, configuration), cancellationToken);

        [Preserve]
        public async Task<LiveWebRTCSessionResponse> ForkWebRTCSessionAsync(string sessionId, LiveWebRTCSessionRequest request, CancellationToken cancellationToken = default)
        {
            var payload = JsonConvert.SerializeObject(request, OpenAIClient.JsonSerializationOptions);
            var response = await Rest.PostAsync(GetUrl($"/sessions/{sessionId}/fork"), payload, new RestParameters(client.DefaultRequestHeaders), cancellationToken);
            response.Validate(EnableDebug);
            return response.Deserialize<LiveWebRTCSessionResponse>(client);
        }

        [Preserve]
        public async Task HangupSessionAsync(string sessionId, CancellationToken cancellationToken = default)
        {
            var response = await Rest.PostAsync(GetUrl($"/sessions/{sessionId}/hangup"), new RestParameters(client.DefaultRequestHeaders), cancellationToken);
            response.Validate(EnableDebug);
        }

    }
}
