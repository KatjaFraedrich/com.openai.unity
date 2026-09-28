// Licensed under the MIT License. See LICENSE in the project root for license information.

using Newtonsoft.Json;
using UnityEngine.Scripting;

namespace OpenAI.Responses
{
    /// <summary>
    /// This tool searches the web for relevant results to use in a response.
    /// </summary>
    [Preserve]
    public sealed class WebSearchTool : ITool
    {
        [Preserve]
        public static implicit operator Tool(WebSearchTool webSearchTool) => new(webSearchTool as ITool);

        [Preserve]
        public WebSearchTool(SearchContextSize searchContextSize = SearchContextSize.Medium)
        {
            SearchContextSize = searchContextSize;
        }

        [Preserve]
        [JsonConstructor]
        internal WebSearchTool([JsonProperty("type")] string type, [JsonProperty("search_context_size")] SearchContextSize searchContextSize)
        {
            Type = string.IsNullOrWhiteSpace(type) ? "web_search" : type;
            SearchContextSize = searchContextSize;
        }

        [Preserve]
        [JsonProperty("type")]
        public string Type { get; } = "web_search";

        /// <summary>
        /// High level guidance for the amount of context window space to use for the search.
        /// </summary>
        [Preserve]
        [JsonProperty("search_context_size")]
        public SearchContextSize SearchContextSize { get; }
    }
}
