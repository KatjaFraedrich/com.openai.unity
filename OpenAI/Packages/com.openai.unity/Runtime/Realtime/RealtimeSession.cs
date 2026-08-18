// Licensed under the MIT License. See LICENSE in the project root for license information.

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Scripting;
using Utilities.Async;
using Utilities.WebSockets;

namespace OpenAI.Realtime
{
    [Preserve]
    public sealed class RealtimeSession : IDisposable
    {
        /// <summary>
        /// Enable or disable logging.
        /// </summary>
        [Preserve]
        public bool EnableDebug { get; set; }

        /// <summary>
        /// The timeout in seconds to wait for a response from the server.
        /// </summary>
        [Preserve]
        public int EventTimeout { get; set; } = 30;

        [Obsolete("Use RealtimeSession.Configuration")]
        public SessionConfiguration Options => Configuration;

        /// <summary>
        /// The configuration options for the session.
        /// </summary>
        [Preserve]
        public SessionConfiguration Configuration { get; internal set; }

        /// <summary>
        /// The configuration options for a realtime transcription session.
        /// </summary>
        [Preserve]
        public RealtimeTranscriptionSessionConfiguration TranscriptionConfiguration { get; internal set; }

        #region Internal

        internal event Action<IServerEvent> OnEventReceived;

        internal event Action<Exception> OnError;

        private readonly WebSocket websocketClient;
        private readonly ConcurrentQueue<IRealtimeEvent> events = new();
        private readonly object eventLock = new();

        private bool collectEvents;

        [Preserve]
        internal RealtimeSession(WebSocket wsClient, bool enableDebug)
        {
            websocketClient = wsClient;
            EnableDebug = enableDebug;
            websocketClient.OnMessage += OnMessage;
        }

        [Preserve]
        private void OnMessage(DataFrame dataFrame)
        {
            if (dataFrame.Type == OpCode.Text)
            {
                if (EnableDebug)
                {
                    Debug.Log($"[RealtimeSession] received raw event:\n{RedactSecrets(dataFrame.Text)}");
                }

                try
                {
                    var @event = JsonConvert.DeserializeObject<IServerEvent>(dataFrame.Text, OpenAIClient.JsonSerializationOptions);

                    lock (eventLock)
                    {
                        if (collectEvents)
                        {
                            events.Enqueue(@event);
                        }
                    }

                    OnEventReceived?.Invoke(@event);
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                    OnError?.Invoke(e);
                }
            }
        }

        [Preserve]
        ~RealtimeSession()
            => Dispose(false);

        private static string RedactSecrets(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                return json;
            }

            try
            {
                var token = Newtonsoft.Json.Linq.JToken.Parse(json);
                Redact(token);
                return token.ToString(Formatting.Indented);
            }
            catch
            {
                return json;
            }

            static void Redact(Newtonsoft.Json.Linq.JToken token)
            {
                if (token is Newtonsoft.Json.Linq.JObject jObject)
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
                else if (token is Newtonsoft.Json.Linq.JArray jArray)
                {
                    foreach (var child in jArray)
                    {
                        Redact(child);
                    }
                }
            }
        }

        #region IDisposable

        private bool isDisposed;

        [Preserve]
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        [Preserve]
        private void Dispose(bool disposing)
        {
            if (!isDisposed && disposing)
            {
                websocketClient.OnMessage -= OnMessage;
                websocketClient.Dispose();
                isDisposed = true;
            }
        }

        #endregion IDisposable

        [Preserve]
        internal async Task ConnectAsync(CancellationToken cancellationToken = default)
        {
            var connectTcs = new TaskCompletionSource<State>();
            websocketClient.OnOpen += OnWebsocketClientOnOpen;
            websocketClient.OnError += OnWebsocketClientOnError;

            try
            {
                // ReSharper disable once MethodHasAsyncOverloadWithCancellation
                // don't call async because it is blocking until connection is closed.
                websocketClient.Connect();
                await connectTcs.Task.WithCancellation(cancellationToken).ConfigureAwait(true);
            }
            finally
            {
                websocketClient.OnOpen -= OnWebsocketClientOnOpen;
                websocketClient.OnError -= OnWebsocketClientOnError;
            }

            return;

            void OnWebsocketClientOnError(Exception e)
                => connectTcs.TrySetException(e);
            void OnWebsocketClientOnOpen()
                => connectTcs.TrySetResult(websocketClient.State);
        }

        #endregion Internal

        /// <summary>
        /// Receive updates from the server.
        /// </summary>
        /// <typeparam name="T"><see cref="IRealtimeEvent"/> to subscribe for updates to.</typeparam>
        /// <param name="sessionEvent">The event to receive updates for.</param>
        /// <param name="cancellationToken">Optional, <see cref="CancellationToken"/>.</param>
        /// <returns><see cref="Task"/>.</returns>
        [Preserve]
        public async Task ReceiveUpdatesAsync<T>(Action<T> sessionEvent, CancellationToken cancellationToken) where T : IRealtimeEvent
        {
            try
            {
                lock (eventLock)
                {
                    if (collectEvents)
                    {
                        Debug.LogWarning($"{nameof(ReceiveUpdatesAsync)} is already running!");
                        return;
                    }

                    collectEvents = true;
                }

                do
                {
                    try
                    {
                        T @event = default;

                        lock (eventLock)
                        {
                            if (events.TryDequeue(out var dequeuedEvent) &&
                                dequeuedEvent is T typedEvent)
                            {
                                @event = typedEvent;
                            }
                        }

                        if (@event != null)
                        {
                            sessionEvent(@event);
                        }

                        await Task.Yield();
                    }
                    catch (Exception e)
                    {
                        switch (e)
                        {
                            case TaskCanceledException:
                            case OperationCanceledException:
                                break;
                            default:
                                Debug.LogException(e);
                                break;
                        }
                    }
                } while (!cancellationToken.IsCancellationRequested && websocketClient.State == State.Open);
            }
            finally
            {
                lock (eventLock)
                {
                    collectEvents = false;
                }
            }
        }

        /// <summary>
        /// Send a client event to the server.
        /// </summary>
        /// <typeparam name="T"><see cref="IClientEvent"/> to send to the server.</typeparam>
        /// <param name="event">The event to send.</param>
        [Preserve]
        public async void Send<T>(T @event) where T : IClientEvent
            => await SendAsync(@event);

        /// <summary>
        /// Send a client event to the server.
        /// </summary>
        /// <typeparam name="T"><see cref="IClientEvent"/> to send to the server.</typeparam>
        /// <param name="event">The event to send.</param>
        /// <param name="cancellationToken">Optional, <see cref="CancellationToken"/>.</param>
        /// <returns><see cref="Task{IServerEvent}"/>.</returns>
        [Preserve]
        public async Task<IServerEvent> SendAsync<T>(T @event, CancellationToken cancellationToken = default) where T : IClientEvent
            => await SendAsync(@event, null, cancellationToken);

        [Preserve]
        public async Task<IServerEvent> SendAsync<T>(T @event, CancellationToken cancellationToken = default, params string[] requestOverrideJsonObjects) where T : IClientEvent
            => await SendAsync(@event, null, cancellationToken, requestOverrideJsonObjects);

        /// <summary>
        /// Send a client event to the server.
        /// </summary>
        /// <typeparam name="T"><see cref="IClientEvent"/> to send to the server.</typeparam>
        /// <param name="event">The event to send.</param>
        /// <param name="sessionEvents">Optional, <see cref="Action{IServerEvent}"/>.</param>
        /// <param name="cancellationToken">Optional, <see cref="CancellationToken"/>.</param>
        /// <returns><see cref="Task{IServerEvent}"/>.</returns>
        [Preserve]
        public async Task<IServerEvent> SendAsync<T>(T @event, Action<IServerEvent> sessionEvents, CancellationToken cancellationToken = default) where T : IClientEvent
            => await SendAsync(@event, sessionEvents, cancellationToken, null);

        [Preserve]
        public async Task<IServerEvent> SendAsync<T>(T @event, Action<IServerEvent> sessionEvents, CancellationToken cancellationToken = default, params string[] requestOverrideJsonObjects) where T : IClientEvent
        {
            if (websocketClient.State != State.Open)
            {
                throw new Exception($"Websocket connection is not open! {websocketClient.State}");
            }

            IClientEvent clientEvent = @event;
            var typedPayload = JObject.FromObject(clientEvent, OpenAIClient.JsonSerializer);
            var payloadObject = new JObject();
            RequestPayloadUtility.ApplyJsonOverrides(payloadObject, requestOverrideJsonObjects);
            RequestPayloadUtility.MergeInto(payloadObject, typedPayload);
            var payload = payloadObject.ToString(Formatting.None);

            if (EnableDebug)
            {
                if (@event is not InputAudioBufferAppendRequest)
                {
                    Debug.Log(payload);
                }
            }

            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(EventTimeout));
            using var eventCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, cts.Token);
            var tcs = new TaskCompletionSource<IServerEvent>();
            eventCts.Token.Register(() => tcs.TrySetCanceled());
            OnEventReceived += EventCallback;

            lock (eventLock)
            {
                if (collectEvents)
                {
                    events.Enqueue(clientEvent);
                }
            }

            var eventId = Guid.NewGuid().ToString("N");

            if (EnableDebug)
            {
                if (@event is not InputAudioBufferAppendRequest)
                {
                    Debug.Log($"[{eventId}] sending {clientEvent.Type}");
                }
            }

            await websocketClient.SendAsync(payload, cancellationToken);

            if (EnableDebug)
            {
                if (@event is not InputAudioBufferAppendRequest)
                {
                    Debug.Log($"[{eventId}] sent {clientEvent.Type}");
                }
            }

            if (@event is InputAudioBufferAppendRequest)
            {
                // no response for this client event
                return null;
            }

            var response = await tcs.Task.WithCancellation(eventCts.Token).ConfigureAwait(true);

            if (EnableDebug)
            {
                Debug.Log($"[{eventId}] received {response.Type}");
            }

            return response;

            void EventCallback(IServerEvent serverEvent)
            {
                sessionEvents?.Invoke(serverEvent);

                try
                {
                    if (serverEvent is RealtimeEventError serverError)
                    {
                        tcs.TrySetException(serverError);
                        OnEventReceived -= EventCallback;
                        return;
                    }

                    switch (clientEvent)
                    {
                        case UpdateSessionRequest when serverEvent is SessionResponse sessionResponse:
                            Configuration = sessionResponse.SessionConfiguration;
                            Complete();
                            return;
                        case UpdateTranscriptionSessionRequest when serverEvent is TranscriptionSessionResponse transcriptionSessionResponse:
                            TranscriptionConfiguration = transcriptionSessionResponse.SessionConfiguration;
                            Complete();
                            return;
                        case InputAudioBufferCommitRequest when serverEvent is InputAudioBufferCommittedResponse:
                        case InputAudioBufferClearRequest when serverEvent is InputAudioBufferClearedResponse:
                        case ConversationItemCreateRequest when serverEvent is ConversationItemCreatedResponse:
                        case ConversationItemTruncateRequest when serverEvent is ConversationItemTruncatedResponse:
                        case ConversationItemDeleteRequest when serverEvent is ConversationItemDeletedResponse:
                            Complete();
                            return;
                        case CreateResponseRequest when serverEvent is RealtimeResponse serverResponse:
                        {
                            if (serverResponse.Response.Status == RealtimeResponseStatus.InProgress)
                            {
                                return;
                            }

                            if (serverResponse.Response.Status != RealtimeResponseStatus.Completed)
                            {
                                var message = serverResponse.Response.StatusDetails?.Error?.ToString() ??
                                              $"ServerResponse {serverResponse.Response.Status}!";
                                tcs.TrySetException(new Exception(message));
                            }
                            else
                            {
                                Complete();
                            }

                            break;
                        }
                    }
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                }

                return;

                void Complete()
                {
                    if (EnableDebug)
                    {
                        Debug.Log($"{clientEvent.Type} -> {serverEvent.Type}");
                    }

                    tcs.TrySetResult(serverEvent);
                    OnEventReceived -= EventCallback;
                }
            }
        }
    }
}
