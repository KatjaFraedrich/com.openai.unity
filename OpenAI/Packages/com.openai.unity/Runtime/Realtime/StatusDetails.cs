// Licensed under the MIT License. See LICENSE in the project root for license information.

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Text;
using UnityEngine.Scripting;

namespace OpenAI.Realtime
{
    public sealed class StatusDetails
    {
        /// <summary>
        /// The type of error that caused the response to fail, corresponding with the status field (cancelled, incomplete, failed).
        /// </summary>
        [Preserve]
        [JsonProperty("type")]
        public RealtimeResponseStatus Type { get; }

        /// <summary>
        /// The reason the Response did not complete.
        /// For a cancelled Response, one of turn_detected (the server VAD detected a new start of speech) or
        /// client_cancelled (the client sent a cancel event).
        /// For an incomplete Response, one of max_output_tokens or content_filter
        /// (the server-side safety filter activated and cut off the response).
        /// </summary>
        [Preserve]
        [JsonProperty("reason")]
        public string Reason { get; }

        /// <summary>
        /// A description of the error that caused the response to fail, populated when the status is failed.
        /// </summary>
        [Preserve]
        [JsonProperty("error")]
        public Error Error { get; }

        [Preserve]
        [JsonExtensionData]
        public IDictionary<string, JToken> AdditionalProperties { get; private set; }

        [Preserve]
        public override string ToString()
        {
            var builder = new StringBuilder();

            if (Type != default)
            {
                builder.Append($"Type: {Type}");
            }

            if (!string.IsNullOrWhiteSpace(Reason))
            {
                if (builder.Length > 0) { builder.Append(", "); }
                builder.Append($"Reason: {Reason}");
            }

            if (Error != null)
            {
                if (builder.Length > 0) { builder.Append(", "); }
                builder.Append($"Error: {Error}");
            }

            if (AdditionalProperties != null)
            {
                foreach (var property in AdditionalProperties)
                {
                    if (builder.Length > 0) { builder.Append(", "); }
                    builder.Append($"{property.Key}: {property.Value}");
                }
            }

            return builder.Length > 0 ? builder.ToString() : "(empty status_details)";
        }
    }
}
