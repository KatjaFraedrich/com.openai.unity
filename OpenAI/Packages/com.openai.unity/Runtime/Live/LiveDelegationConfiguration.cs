// Licensed under the MIT License. See LICENSE in the project root for license information.

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using OpenAI.Extensions;
using OpenAI.Models;
using System.Collections.Generic;
using UnityEngine.Scripting;

namespace OpenAI.Live
{
    [Preserve]
    public sealed class LiveDelegationConfiguration
    {
        [Preserve]
        public LiveDelegationConfiguration(LiveResponsesDelegation responses = null)
        {
            Type = responses == null ? "client" : "responses";
            Responses = responses;
        }

        [Preserve]
        [JsonConstructor]
        internal LiveDelegationConfiguration([JsonProperty("type")] string type, [JsonProperty("responses")] LiveResponsesDelegation responses)
        {
            Type = string.IsNullOrWhiteSpace(type) ? (responses == null ? "client" : "responses") : type;
            Responses = responses;
        }

        [Preserve]
        [JsonProperty("type")]
        public string Type { get; }

        [Preserve]
        [JsonProperty("responses", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public LiveResponsesDelegation Responses { get; }
    }

    [Preserve]
    public sealed class LiveResponsesDelegation
    {
        [Preserve]
        public LiveResponsesDelegation(Model model = null, string instructions = null, IEnumerable<Tool> tools = null, string toolChoice = null, bool? parallelToolCalls = null)
        {
            Model = string.IsNullOrWhiteSpace(model?.Id) ? Models.Model.GPT4_1_Mini : model;
            Instructions = instructions;

            if (tools == null && string.IsNullOrWhiteSpace(toolChoice))
            {
                Tools = null;
                ToolChoice = null;
            }
            else
            {
                tools.ProcessTools<ITool>(toolChoice, out var toolList, out var activeTool);
                Tools = toolList;
                ToolChoice = activeTool;
            }

            ParallelToolCalls = parallelToolCalls;
        }

        [Preserve]
        [JsonConstructor]
        internal LiveResponsesDelegation([JsonProperty("model")] string model, [JsonProperty("instructions")] string instructions, [JsonProperty("tools")] IReadOnlyList<ITool> tools, [JsonProperty("tool_choice")] object toolChoice, [JsonProperty("parallel_tool_calls")] bool? parallelToolCalls)
        {
            Model = model;
            Instructions = instructions;
            Tools = tools;
            ToolChoice = toolChoice;
            ParallelToolCalls = parallelToolCalls;
        }

        [Preserve]
        [JsonProperty("model", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string Model { get; }

        [Preserve]
        [JsonProperty("instructions", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string Instructions { get; }

        [Preserve]
        [JsonProperty("tools", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public IReadOnlyList<ITool> Tools { get; }

        [Preserve]
        [JsonProperty("tool_choice", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public object ToolChoice { get; }

        [Preserve]
        [JsonProperty("parallel_tool_calls", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public bool? ParallelToolCalls { get; }

        [Preserve]
        [JsonExtensionData]
        public IDictionary<string, JToken> AdditionalProperties { get; private set; }
    }
}