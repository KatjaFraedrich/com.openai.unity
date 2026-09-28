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

namespace OpenAI.Live
{
    [Preserve]
    public sealed class LiveSession : IDisposable
    {
        [Preserve]
        public bool EnableDebug { get; set; }

        [Preserve]
        public int EventTimeout { get; set; } = 30;

        [Preserve]
        public LiveSessionConfiguration Configuration { get; internal set; }

        internal event Action<ILiveServerEvent> OnEventReceived;

        internal event Action<Exception> OnError;

        private readonly WebSocket websocketClient;
        private readonly ConcurrentQueue<ILiveEvent> events = new();
        private readonly object eventLock = new();
        private bool collectEvents;
        private bool isDisposed;

        [Preserve]
        internal LiveSession(WebSocket wsClient, bool enableDebug)
        {
            websocketClient = wsClient;
            EnableDebug = enableDebug;
            websocketClient.OnMessage += OnMessage;
        }

        [Preserve]
        private void OnMessage(DataFrame dataFrame)
        {
            if (dataFrame.Type != OpCode.Text) { return; }
            if (EnableDebug) { Debug.Log($"[LiveSession] received raw event:\n{RedactSecrets(dataFrame.Text)}"); }

            try
            {
                var @event = JsonConvert.DeserializeObject<ILiveServerEvent>(dataFrame.Text, OpenAIClient.JsonSerializationOptions);
                lock (eventLock) { if (collectEvents) { events.Enqueue(@event); } }
                OnEventReceived?.Invoke(@event);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                OnError?.Invoke(e);
            }
        }

        [Preserve]
        ~LiveSession() => Dispose(false);

        [Preserve]
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        [Preserve]
        private void Dispose(bool disposing)
        {
            if (isDisposed || !disposing) { return; }
            websocketClient.OnMessage -= OnMessage;
            websocketClient.Dispose();
            isDisposed = true;
        }

        [Preserve]
        internal async Task ConnectAsync(CancellationToken cancellationToken = default)
        {
            var connectTcs = new TaskCompletionSource<State>();
            websocketClient.OnOpen += OnWebsocketClientOnOpen;
            websocketClient.OnError += OnWebsocketClientOnError;

            try
            {
                websocketClient.Connect();
                await connectTcs.Task.WithCancellation(cancellationToken).ConfigureAwait(true);
            }
            finally
            {
                websocketClient.OnOpen -= OnWebsocketClientOnOpen;
                websocketClient.OnError -= OnWebsocketClientOnError;
            }

            return;

            void OnWebsocketClientOnError(Exception e) => connectTcs.TrySetException(e);
            void OnWebsocketClientOnOpen() => connectTcs.TrySetResult(websocketClient.State);
        }

        [Preserve]
        public async Task ReceiveUpdatesAsync<T>(Action<T> sessionEvent, CancellationToken cancellationToken) where T : ILiveEvent
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
                        lock (eventLock) { if (events.TryDequeue(out var dequeuedEvent) && dequeuedEvent is T typedEvent) { @event = typedEvent; } }
                        if (@event != null) { sessionEvent(@event); }
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
                lock (eventLock) { collectEvents = false; }
            }
        }

        [Preserve]
        public async void Send<T>(T @event) where T : ILiveClientEvent => await SendAsync(@event);

        [Preserve]
        public async Task<ILiveServerEvent> SendAsync<T>(T @event, CancellationToken cancellationToken = default) where T : ILiveClientEvent => await SendAsync(@event, null, cancellationToken);

        [Preserve]
        public async Task<ILiveServerEvent> SendAsync<T>(T @event, CancellationToken cancellationToken = default, params string[] requestOverrideJsonObjects) where T : ILiveClientEvent => await SendAsync(@event, null, cancellationToken, requestOverrideJsonObjects);

        [Preserve]
        public async Task<ILiveServerEvent> SendAsync<T>(T @event, Action<ILiveServerEvent> sessionEvents, CancellationToken cancellationToken = default) where T : ILiveClientEvent => await SendAsync(@event, sessionEvents, cancellationToken, null);

        [Preserve]
        public async Task<ILiveServerEvent> SendAsync<T>(T @event, Action<ILiveServerEvent> sessionEvents, CancellationToken cancellationToken = default, params string[] requestOverrideJsonObjects) where T : ILiveClientEvent
        {
            if (websocketClient.State != State.Open) { throw new Exception($"Websocket connection is not open! {websocketClient.State}"); }

            ILiveClientEvent clientEvent = @event;
            var typedPayload = JObject.FromObject(clientEvent, OpenAIClient.JsonSerializer);
            var payloadObject = new JObject();
            RequestPayloadUtility.ApplyJsonOverrides(payloadObject, requestOverrideJsonObjects);
            RequestPayloadUtility.MergeInto(payloadObject, typedPayload);
            var payload = payloadObject.ToString(Formatting.None);

            if (EnableDebug && @event is not LiveInputAudioAppendRequest) { Debug.Log(payload); }

            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(EventTimeout));
            using var eventCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, cts.Token);
            var tcs = new TaskCompletionSource<ILiveServerEvent>();
            eventCts.Token.Register(() => tcs.TrySetCanceled());
            OnEventReceived += EventCallback;

            lock (eventLock) { if (collectEvents) { events.Enqueue(clientEvent); } }

            var eventId = Guid.NewGuid().ToString("N");
            if (EnableDebug && @event is not LiveInputAudioAppendRequest) { Debug.Log($"[{eventId}] sending {clientEvent.Type}"); }
            await websocketClient.SendAsync(payload, cancellationToken);
            if (EnableDebug && @event is not LiveInputAudioAppendRequest) { Debug.Log($"[{eventId}] sent {clientEvent.Type}"); }

            if (@event is LiveInputAudioAppendRequest or LiveResponseItemCreateRequest)
            {
                OnEventReceived -= EventCallback;
                return null;
            }

            var response = await tcs.Task.WithCancellation(eventCts.Token).ConfigureAwait(true);
            if (EnableDebug) { Debug.Log($"[{eventId}] received {response.Type}"); }
            return response;

            void EventCallback(ILiveServerEvent serverEvent)
            {
                sessionEvents?.Invoke(serverEvent);

                try
                {
                    if (serverEvent is LiveEventError serverError)
                    {
                        tcs.TrySetException(serverError);
                        OnEventReceived -= EventCallback;
                        return;
                    }

                    switch (clientEvent)
                    {
                        case StartLiveSessionRequest when serverEvent is LiveSessionResponse { Type: "session.started" } sessionResponse:
                            Configuration = sessionResponse.SessionConfiguration;
                            Complete();
                            return;
                        case UpdateLiveSessionRequest when serverEvent is LiveSessionResponse { Type: "session.updated" } sessionResponse:
                            Configuration = sessionResponse.SessionConfiguration;
                            Complete();
                            return;
                        case CloseLiveSessionRequest when serverEvent is LiveSessionResponse { Type: "session.closed" }:
                        case LiveInputAudioMuteRequest when serverEvent is LiveAcknowledgementResponse { Type: "session.input_audio.muted" }:
                        case LiveInputAudioUnmuteRequest when serverEvent is LiveAcknowledgementResponse { Type: "session.input_audio.unmuted" }:
                        case LiveInstructionsAppendRequest when serverEvent is LiveAcknowledgementResponse { Type: "session.instructions.appended" }:
                        case LiveCommentaryAppendRequest when serverEvent is LiveAcknowledgementResponse { Type: "session.commentary.appended" }:
                        case LiveThinkingAppendRequest when serverEvent is LiveAcknowledgementResponse { Type: "session.thinking.appended" }:
                            Complete();
                            return;
                        case LiveResponseItemCreateRequest when serverEvent is LiveDelegationResponse:
                        case LiveResponseCreateRequest when serverEvent is LiveDelegationResponse:
                            Complete();
                            return;
                    }
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                }

                return;

                void Complete()
                {
                    if (EnableDebug) { Debug.Log($"{clientEvent.Type} -> {serverEvent.Type}"); }
                    tcs.TrySetResult(serverEvent);
                    OnEventReceived -= EventCallback;
                }
            }
        }

        private static string RedactSecrets(string json)
        {
            if (string.IsNullOrWhiteSpace(json)) { return json; }

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
                    foreach (var child in jArray) { Redact(child); }
                }
            }
        }
    }
}
