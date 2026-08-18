// Licensed under the MIT License. See LICENSE in the project root for license information.

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using OpenAI.Extensions;
using OpenAI.Models;
using System;
using System.Collections.Generic;
using System.Security.Authentication;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using Utilities.Async;
using Utilities.WebRequestRest;
using Utilities.WebSockets;

namespace OpenAI.Realtime
{
    public sealed class RealtimeEndpoint : OpenAIBaseEndpoint
    {
        public RealtimeEndpoint(OpenAIClient client) : base(client) { }

        protected override string Root => "realtime";

        /// <summary>
        /// Creates a new realtime session with the provided <see cref="SessionConfiguration"/> options.
        /// </summary>
        /// <param name="configuration"><see cref="SessionConfiguration"/>.</param>
        /// <param name="cancellationToken">Optional, <see cref="CancellationToken"/>.</param>
        /// <returns><see cref="RealtimeSession"/>.</returns>
        public async Task<RealtimeSession> CreateSessionAsync(SessionConfiguration configuration = null, CancellationToken cancellationToken = default)
            => await CreateSessionAsync(configuration, cancellationToken, null);

        public async Task<RealtimeSession> CreateSessionAsync(SessionConfiguration configuration = null, CancellationToken cancellationToken = default, params string[] sessionOverrideJsonObjects)
        {
            configuration ??= new SessionConfiguration(Model.GPT_Realtime);
            string model = string.IsNullOrWhiteSpace(configuration.Model) ? Model.GPT_Realtime : configuration.Model;
            var queryParameters = new Dictionary<string, string>();

            if (client.Settings.Info.IsAzureOpenAI)
            {
                queryParameters["deployment"] = model;
            }
            else
            {
                queryParameters["model"] = model;
            }

            var clientSecret = configuration.ClientSecret ?? new ClientSecret();
            var typedSessionPayload = RealtimeSessionConfigurationConverter.ToJObject(configuration, OpenAIClient.JsonSerializer, includeClientSecret: false);
            var sessionPayload = new JObject();
            RequestPayloadUtility.ApplyJsonOverrides(sessionPayload, sessionOverrideJsonObjects);
            RequestPayloadUtility.MergeInto(sessionPayload, typedSessionPayload);
            var request = new ClientSecretRequest(clientSecret.ExpiresAfter, sessionPayload);
            var payload = JsonConvert.SerializeObject(request, OpenAIClient.JsonSerializationOptions);
            var createSessionResponse = await Rest.PostAsync(GetUrl("/client_secrets"), payload, new RestParameters(client.DefaultRequestHeaders), cancellationToken);
            createSessionResponse.Validate(EnableDebug);
            var createSession = createSessionResponse.Deserialize<ClientSecretResponse>(client);

            if (createSession == null ||
                string.IsNullOrWhiteSpace(createSession.ClientSecret?.EphemeralApiKey))
            {
                throw new InvalidOperationException("Failed to create a session. Ensure the configuration is valid and the API key is set.");
            }

            var websocket = new WebSocket(GetWebsocketUri(queryParameters: queryParameters), new Dictionary<string, string>
            {
#if !PLATFORM_WEBGL
                { "User-Agent", "OpenAI-DotNet" },
                { "Authorization", $"Bearer {createSession.ClientSecret!.EphemeralApiKey}" }
#endif
            }, new List<string>
            {
#if PLATFORM_WEBGL // Web browsers do not support headers. https://github.com/openai/openai-realtime-api-beta/blob/339e9553a757ef1cf8c767272fc750c1e62effbb/lib/api.js#L76-L80
                "realtime",
                $"openai-insecure-api-key.{createSession.ClientSecret!.EphemeralApiKey}"
#endif
            });
            var session = new RealtimeSession(websocket, EnableDebug);
            var sessionCreatedTcs = new TaskCompletionSource<SessionResponse>();

            try
            {
                session.OnEventReceived += OnEventReceived;
                session.OnError += OnError;
                await session.ConnectAsync(cancellationToken).ConfigureAwait(true);
                var sessionResponse = await sessionCreatedTcs.Task.WithCancellation(cancellationToken).ConfigureAwait(true);
                session.Configuration = sessionResponse.SessionConfiguration;
            }
            finally
            {
                session.OnError -= OnError;
                session.OnEventReceived -= OnEventReceived;
            }

            return session;

            void OnError(Exception e)
                => sessionCreatedTcs.TrySetException(e);

            void OnEventReceived(IRealtimeEvent @event)
            {
                try
                {
                    switch (@event)
                    {
                        case SessionResponse sessionResponse:
                            if (sessionResponse.Type == "session.created")
                            {
                                sessionCreatedTcs.TrySetResult(sessionResponse);
                            }

                            break;
                        case RealtimeEventError realtimeEventError:
                            sessionCreatedTcs.TrySetException(realtimeEventError.Error.Code is "invalid_session_token" or "invalid_api_key"
                                ? new AuthenticationException(realtimeEventError.Error.Message)
                                : new Exception(realtimeEventError.Error.Message));
                            break;
                    }
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                    sessionCreatedTcs.TrySetException(e);
                }
            }
        }

        /// <summary>
        /// Creates a new realtime transcription session with the provided <see cref="RealtimeTranscriptionSessionConfiguration"/> options.
        /// </summary>
        /// <param name="configuration"><see cref="RealtimeTranscriptionSessionConfiguration"/>.</param>
        /// <param name="cancellationToken">Optional, <see cref="CancellationToken"/>.</param>
        /// <returns><see cref="RealtimeSession"/>.</returns>
        public async Task<RealtimeSession> CreateTranscriptionSessionAsync(RealtimeTranscriptionSessionConfiguration configuration = null, CancellationToken cancellationToken = default)
            => await CreateTranscriptionSessionAsync(configuration, cancellationToken, null);

        public async Task<RealtimeSession> CreateTranscriptionSessionAsync(RealtimeTranscriptionSessionConfiguration configuration = null, CancellationToken cancellationToken = default, params string[] sessionOverrideJsonObjects)
        {
            configuration ??= new RealtimeTranscriptionSessionConfiguration();
            var queryParameters = new Dictionary<string, string>
            {
                ["intent"] = "transcription"
            };

            var clientSecret = configuration.ClientSecret ?? new ClientSecret();
            var typedSessionPayload = RealtimeTranscriptionSessionConfigurationConverter.ToJObject(configuration, OpenAIClient.JsonSerializer, includeClientSecret: false);
            var sessionPayload = new JObject();
            RequestPayloadUtility.ApplyJsonOverrides(sessionPayload, sessionOverrideJsonObjects);
            RequestPayloadUtility.MergeInto(sessionPayload, typedSessionPayload);
            var request = new ClientSecretRequest(clientSecret.ExpiresAfter, sessionPayload);
            var payload = JsonConvert.SerializeObject(request, OpenAIClient.JsonSerializationOptions);
            var clientSecretUrl = GetUrl("/client_secrets");
            Debug.Log($"[RealtimeEndpoint] Creating realtime transcription client secret: POST {clientSecretUrl}\n{RedactSecrets(payload)}");
            var createSessionResponse = await Rest.PostAsync(clientSecretUrl, payload, new RestParameters(client.DefaultRequestHeaders), cancellationToken);
            createSessionResponse.Validate(EnableDebug);
            Debug.Log($"[RealtimeEndpoint] Realtime transcription client secret response:\n{RedactSecrets(createSessionResponse.Body)}");
            var createSession = createSessionResponse.Deserialize<TranscriptionClientSecretResponse>(client);

            if (createSession == null ||
                string.IsNullOrWhiteSpace(createSession.ClientSecret?.EphemeralApiKey))
            {
                throw new InvalidOperationException("Failed to create a transcription session. Ensure the configuration is valid and the API key is set.");
            }

            var websocketUri = GetWebsocketUri(queryParameters: queryParameters);
            Debug.Log($"[RealtimeEndpoint] Connecting realtime transcription WebSocket: {websocketUri}");
            var websocket = new WebSocket(websocketUri, new Dictionary<string, string>
            {
#if !PLATFORM_WEBGL
                { "User-Agent", "OpenAI-DotNet" },
                { "Authorization", $"Bearer {createSession.ClientSecret!.EphemeralApiKey}" }
#endif
            }, new List<string>
            {
#if PLATFORM_WEBGL
                "realtime",
                $"openai-insecure-api-key.{createSession.ClientSecret!.EphemeralApiKey}"
#endif
            });
            var session = new RealtimeSession(websocket, EnableDebug);
            session.TranscriptionConfiguration = createSession.Session;

            await session.ConnectAsync(cancellationToken).ConfigureAwait(true);
            Debug.Log($"[RealtimeEndpoint] Realtime transcription WebSocket connected; using REST-created session:\n{RedactSecrets(JsonConvert.SerializeObject(createSession.Session, OpenAIClient.JsonSerializationOptions))}");

            return session;
        }

        private sealed class ClientSecretRequest
        {
            public ClientSecretRequest(ExpiresAfter expiresAfter, object session)
            {
                ExpiresAfter = expiresAfter;
                Session = session;
            }

            [JsonProperty("expires_after", DefaultValueHandling = DefaultValueHandling.Ignore)]
            public ExpiresAfter ExpiresAfter { get; }

            [JsonProperty("session", DefaultValueHandling = DefaultValueHandling.Ignore)]
            public object Session { get; }
        }

        private sealed class ClientSecretResponse
        {
            [JsonConstructor]
            public ClientSecretResponse(
                [JsonProperty("client_secret")] ClientSecret clientSecret,
                [JsonProperty("value")] string ephemeralApiKey,
                [JsonProperty("expires_at")] int? expiresAtUnixTimeSeconds,
                [JsonProperty("session")] SessionConfiguration session)
            {
                ClientSecret = clientSecret ?? session?.ClientSecret ?? new ClientSecret(ephemeralApiKey, expiresAtUnixTimeSeconds);
                Session = session;
            }

            [JsonProperty("client_secret")]
            public ClientSecret ClientSecret { get; }

            [JsonProperty("session")]
            public SessionConfiguration Session { get; }
        }

        private sealed class TranscriptionClientSecretResponse
        {
            [JsonConstructor]
            public TranscriptionClientSecretResponse(
                [JsonProperty("client_secret")] ClientSecret clientSecret,
                [JsonProperty("value")] string ephemeralApiKey,
                [JsonProperty("expires_at")] int? expiresAtUnixTimeSeconds,
                [JsonProperty("session")] RealtimeTranscriptionSessionConfiguration session)
            {
                ClientSecret = clientSecret ?? session?.ClientSecret ?? new ClientSecret(ephemeralApiKey, expiresAtUnixTimeSeconds);
                Session = session;
            }

            [JsonProperty("client_secret")]
            public ClientSecret ClientSecret { get; }

            [JsonProperty("session")]
            public RealtimeTranscriptionSessionConfiguration Session { get; }
        }

        private static string RedactSecrets(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                return json;
            }

            try
            {
                var token = JToken.Parse(json);
                Redact(token);
                return token.ToString(Formatting.Indented);
            }
            catch
            {
                return json;
            }

            static void Redact(JToken token)
            {
                if (token is JObject jObject)
                {
                    foreach (var property in jObject.Properties())
                    {
                        if (property.Name == "value" || property.Name == "ephemeral_api_key")
                        {
                            property.Value = "<redacted>";
                            continue;
                        }

                        Redact(property.Value);
                    }
                }
                else if (token is JArray jArray)
                {
                    foreach (var child in jArray)
                    {
                        Redact(child);
                    }
                }
            }
        }

    }
}
