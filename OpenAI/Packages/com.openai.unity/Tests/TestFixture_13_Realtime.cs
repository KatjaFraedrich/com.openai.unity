// Licensed under the MIT License. See LICENSE in the project root for license information.

using NUnit.Framework;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using OpenAI.Extensions;
using OpenAI.Models;
using OpenAI.Realtime;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace OpenAI.Tests
{
    internal class TestFixture_13_Realtime : AbstractTestFixture
    {
        [Test]
        public async Task Test_01_RealtimeSession()
        {
            RealtimeSession session = null;
            Tool.ClearRegisteredTools();

            try
            {
                Assert.IsNotNull(OpenAIClient.RealtimeEndpoint);
                var cts = new CancellationTokenSource(TimeSpan.FromSeconds(20));
                var mutex = new object();
                var wasGoodbyeCalled = false;
                var tools = new List<Tool>
                {
                    Tool.FromFunc("goodbye", () =>
                    {
                        lock (mutex)
                        {
                           wasGoodbyeCalled = true;
                        }
                        cts.Cancel();
                        Debug.Log("Hanging up...");
                        return "Goodbye!";
                    })
                };

                var configuration = new SessionConfiguration(Model.GPT4oRealtime, tools: tools);
                session = await OpenAIClient.RealtimeEndpoint.CreateSessionAsync(configuration, cts.Token);
                Assert.IsNotNull(session);
                Assert.IsNotNull(session.Configuration);
                Assert.AreEqual(Model.GPT4oRealtime.Id, configuration.Model);
                Assert.AreEqual(configuration.Model, session.Configuration.Model);
                Assert.IsNotNull(session.Configuration.VoiceActivityDetectionSettings);
                Assert.IsInstanceOf<ServerVAD>(session.Configuration.VoiceActivityDetectionSettings);
                Assert.IsNotNull(configuration.Tools);
                Assert.IsNotEmpty(configuration.Tools);
                Assert.AreEqual(1, configuration.Tools.Count);
                Assert.AreEqual(configuration.Tools.Count, session.Configuration.Tools.Count);
                Assert.AreEqual(configuration.Tools[0].Name, session.Configuration.Tools[0].Name);
                Assert.AreEqual(Modality.Audio | Modality.Text, configuration.Modalities);
                Assert.AreEqual(Modality.Audio | Modality.Text, session.Configuration.Modalities);
                var responseTask = session.ReceiveUpdatesAsync<IServerEvent>(SessionEvents, cts.Token);

                await session.SendAsync(new ConversationItemCreateRequest("Hello!"), cts.Token);
                await session.SendAsync(new CreateResponseRequest(), cts.Token);
                await session.SendAsync(new InputAudioBufferAppendRequest(new ReadOnlyMemory<byte>(new byte[1024 * 4])), cts.Token);
                await session.SendAsync(new UpdateSessionRequest(new SessionConfiguration(model: Model.Transcribe_GPT_4o_Mini, instructions: "You are a fearsome dinosaur. Rawr!", tools: tools)), cts.Token);
                await session.SendAsync(new ConversationItemCreateRequest("Goodbye!"), cts.Token);
                await session.SendAsync(new CreateResponseRequest(), cts.Token);

                async void SessionEvents(IServerEvent @event)
                {
                    switch (@event)
                    {
                        case RealtimeResponse realtimeResponse:
                            realtimeResponse.Response.PrintUsage();
                            break;
                        case ResponseAudioTranscriptResponse transcriptResponse:
                            Debug.Log(transcriptResponse.ToString());
                            break;
                        case ResponseFunctionCallArgumentsResponse functionCallResponse:
                            if (functionCallResponse.IsDone)
                            {
                                Debug.Log($"tool_call: {functionCallResponse.Name}");
                                await functionCallResponse.InvokeFunctionAsync(cts.Token);
                            }

                            break;
                    }
                }

                await responseTask.ConfigureAwait(true);

                lock (mutex)
                {
                    Assert.IsTrue(wasGoodbyeCalled);
                }
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
                        throw;
                }
            }
            finally
            {
                session?.Dispose();
            }
        }

        [Test]
        public async Task Test_02_RealtimeSession_Semantic_VAD()
        {
            RealtimeSession session = null;
            Tool.ClearRegisteredTools();

            try
            {
                Assert.IsNotNull(OpenAIClient.RealtimeEndpoint);
                var cts = new CancellationTokenSource(TimeSpan.FromSeconds(20));
                var mutex = new object();
                var wasGoodbyeCalled = false;
                var tools = new List<Tool>
                {
                    Tool.FromFunc("goodbye", () =>
                    {
                        lock (mutex)
                        {
                            wasGoodbyeCalled = true;
                        }
                        cts.Cancel();
                        Debug.Log("Hanging up...");
                        return "Goodbye!";
                    })
                };

                var configuration = new SessionConfiguration(
                    model: Model.GPT4oRealtime,
                    turnDetectionSettings: new SemanticVAD(),
                    tools: tools);
                session = await OpenAIClient.RealtimeEndpoint.CreateSessionAsync(configuration, cts.Token);
                Assert.IsNotNull(session);
                Assert.IsNotNull(session.Configuration);
                Assert.AreEqual(Model.GPT4oRealtime.Id, configuration.Model);
                Assert.AreEqual(configuration.Model, session.Configuration.Model);
                Assert.IsNotNull(session.Configuration.VoiceActivityDetectionSettings);
                Assert.IsInstanceOf<SemanticVAD>(session.Configuration.VoiceActivityDetectionSettings);
                Assert.IsNotNull(configuration.Tools);
                Assert.IsNotEmpty(configuration.Tools);
                Assert.AreEqual(1, configuration.Tools.Count);
                Assert.AreEqual(configuration.Tools.Count, session.Configuration.Tools.Count);
                Assert.AreEqual(configuration.Tools[0].Name, session.Configuration.Tools[0].Name);
                Assert.AreEqual(Modality.Audio | Modality.Text, configuration.Modalities);
                Assert.AreEqual(Modality.Audio | Modality.Text, session.Configuration.Modalities);
                var responseTask = session.ReceiveUpdatesAsync<IServerEvent>(SessionEvents, cts.Token);

                await session.SendAsync(new ConversationItemCreateRequest("Hello!"), cts.Token);
                await session.SendAsync(new CreateResponseRequest(), cts.Token);
                await session.SendAsync(new InputAudioBufferAppendRequest(new ReadOnlyMemory<byte>(new byte[1024 * 4])), cts.Token);
                await session.SendAsync(new ConversationItemCreateRequest("Goodbye!"), cts.Token);
                await session.SendAsync(new CreateResponseRequest(), cts.Token);

                async void SessionEvents(IServerEvent @event)
                {
                    switch (@event)
                    {
                        case RealtimeResponse realtimeResponse:
                            realtimeResponse.Response.PrintUsage();
                            break;
                        case ResponseAudioTranscriptResponse transcriptResponse:
                            Debug.Log(transcriptResponse.ToString());
                            break;
                        case ResponseFunctionCallArgumentsResponse functionCallResponse:
                            if (functionCallResponse.IsDone)
                            {
                                Debug.Log($"tool_call: {functionCallResponse.Name}");
                                await functionCallResponse.InvokeFunctionAsync(cts.Token);
                            }

                            break;
                    }
                }

                await responseTask.ConfigureAwait(true);

                lock (mutex)
                {
                    Assert.IsTrue(wasGoodbyeCalled);
                }
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
                        throw;
                }
            }
            finally
            {
                session?.Dispose();
            }
        }

        [Test]
        public async Task Test_03_RealtimeSession_VAD_Disabled()
        {
            RealtimeSession session = null;
            Tool.ClearRegisteredTools();

            try
            {
                Assert.IsNotNull(OpenAIClient.RealtimeEndpoint);
                var cts = new CancellationTokenSource(TimeSpan.FromSeconds(20));
                var wasGoodbyeCalled = false;
                var mutex = new object();
                var tools = new List<Tool>
                {
                    Tool.FromFunc("goodbye", () =>
                    {
                        lock (mutex)
                        {
                            wasGoodbyeCalled = true;
                        }
                        cts.Cancel();
                        Debug.Log("Hanging up...");
                        return "Goodbye!";
                    })
                };

                var configuration = new SessionConfiguration(
                    model: Model.GPT4oRealtime,
                    tools: tools,
                    modalities: Modality.Text,
                    turnDetectionSettings: new DisabledVAD());
                session = await OpenAIClient.RealtimeEndpoint.CreateSessionAsync(configuration, cts.Token);
                Assert.IsNotNull(session);
                Assert.IsNotNull(session.Configuration);
                Assert.AreEqual(Model.GPT4oRealtime.Id, configuration.Model);
                Assert.AreEqual(configuration.Model, session.Configuration.Model);
                Assert.IsNull(session.Configuration.VoiceActivityDetectionSettings);
                Assert.IsNotNull(configuration.Tools);
                Assert.IsNotEmpty(configuration.Tools);
                Assert.AreEqual(1, configuration.Tools.Count);
                Assert.AreEqual(configuration.Tools.Count, session.Configuration.Tools.Count);
                Assert.AreEqual(configuration.Tools[0].Name, session.Configuration.Tools[0].Name);
                Assert.AreEqual(Modality.Text, configuration.Modalities);
                Assert.AreEqual(Modality.Text, session.Configuration.Modalities);
                var responseTask = session.ReceiveUpdatesAsync<IServerEvent>(SessionEvents, cts.Token);

                await session.SendAsync(new ConversationItemCreateRequest("Hello!"), cts.Token);
                await session.SendAsync(new CreateResponseRequest(), cts.Token);
                await session.SendAsync(new InputAudioBufferAppendRequest(new ReadOnlyMemory<byte>(new byte[1024 * 8])), cts.Token);
                await session.SendAsync(new ConversationItemCreateRequest("Goodbye!"), cts.Token);
                await session.SendAsync(new CreateResponseRequest(), cts.Token);

                async void SessionEvents(IServerEvent @event)
                {
                    switch (@event)
                    {
                        case RealtimeResponse realtimeResponse:
                            realtimeResponse.Response.PrintUsage();
                            break;
                        case ResponseAudioTranscriptResponse transcriptResponse:
                            Debug.Log(transcriptResponse.ToString());
                            break;
                        case ResponseFunctionCallArgumentsResponse functionCallResponse:
                            if (functionCallResponse.IsDone)
                            {
                                Debug.Log($"tool_call: {functionCallResponse.Name}");
                                await functionCallResponse.InvokeFunctionAsync(cts.Token);
                            }

                            break;
                    }
                }

                await responseTask.ConfigureAwait(true);

                lock (mutex)
                {
                    Assert.IsTrue(wasGoodbyeCalled);
                }
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
                        throw;
                }
            }
            finally
            {
                session?.Dispose();
            }
        }

        [Test]
        public void Test_04_RealtimeTranscriptionSessionConfiguration()
        {
            var configuration = new RealtimeTranscriptionSessionConfiguration(
                model: Model.GPT_Realtime_Whisper,
                language: "en",
                delay: RealtimeTranscriptionDelay.Low,
                include: new[] { "item.input_audio_transcription.logprobs" });
            configuration.ClientSecret = null;

            var json = JObject.FromObject(configuration, OpenAIClient.JsonSerializer);

            Assert.AreEqual("transcription", json.Value<string>("type"));
            Assert.AreEqual("audio/pcm", json["audio"]?["input"]?["format"]?.Value<string>("type"));
            Assert.AreEqual(24000, json["audio"]?["input"]?["format"]?.Value<int>("rate"));
            Assert.AreEqual(Model.GPT_Realtime_Whisper.Id, json["audio"]?["input"]?["transcription"]?.Value<string>("model"));
            Assert.AreEqual("en", json["audio"]?["input"]?["transcription"]?.Value<string>("language"));
            Assert.AreEqual("low", json["audio"]?["input"]?["transcription"]?.Value<string>("delay"));
            Assert.AreEqual("item.input_audio_transcription.logprobs", json["include"]?[0]?.Value<string>());

            var serverEvent = JsonConvert.DeserializeObject<IServerEvent>(
                @"{""type"":""transcription_session.created"",""session"":{""type"":""transcription"",""audio"":{""input"":{""format"":{""type"":""audio/pcm"",""rate"":24000},""transcription"":{""model"":""gpt-realtime-whisper""}}}}}",
                OpenAIClient.JsonSerializationOptions);
            Assert.IsInstanceOf<TranscriptionSessionResponse>(serverEvent);
        }

        [Test]
        public void Test_05_RealtimeResponseOutputAudioEvents()
        {
            var audioEvent = JsonConvert.DeserializeObject<IServerEvent>(
                @"{""event_id"":""event_Dyw0HYf3eMRwqhyccDNDn"",""type"":""response.output_audio.delta"",""response_id"":""resp_123"",""item_id"":""item_123"",""output_index"":0,""content_index"":0,""delta"":""AA==""}",
                OpenAIClient.JsonSerializationOptions);

            Assert.IsInstanceOf<ResponseAudioResponse>(audioEvent);
            var audioResponse = (ResponseAudioResponse)audioEvent;
            Assert.AreEqual("response.output_audio.delta", audioResponse.Type);
            Assert.AreEqual("AA==", audioResponse.Delta);

            var transcriptEvent = JsonConvert.DeserializeObject<IServerEvent>(
                @"{""event_id"":""event_transcript"",""type"":""response.output_audio_transcript.delta"",""response_id"":""resp_123"",""item_id"":""item_123"",""output_index"":0,""content_index"":0,""delta"":""hello""}",
                OpenAIClient.JsonSerializationOptions);

            Assert.IsInstanceOf<ResponseAudioTranscriptResponse>(transcriptEvent);
        }
    }
}
