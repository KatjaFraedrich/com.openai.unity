// Licensed under the MIT License. See LICENSE in the project root for license information.

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using UnityEngine;
using UnityEngine.Networking;

namespace OpenAI
{
    internal static class RequestPayloadUtility
    {
        public static string BuildJsonPayload(object request, JsonSerializer serializer, params string[] overrideJsonObjects)
        {
            var payload = JObject.FromObject(request, serializer);
            ApplyJsonOverrides(payload, overrideJsonObjects);
            return payload.ToString(Formatting.None);
        }

        public static JObject BuildJObjectPayload(object request, JsonSerializer serializer, params string[] overrideJsonObjects)
        {
            var payload = JObject.FromObject(request, serializer);
            ApplyJsonOverrides(payload, overrideJsonObjects);
            return payload;
        }

        public static void ApplyJsonOverrides(JObject payload, params string[] overrideJsonObjects)
        {
            if (payload == null)
                throw new ArgumentNullException(nameof(payload));

            if (overrideJsonObjects == null)
                return;

            foreach (string overrideJsonObject in overrideJsonObjects)
            {
                if (!TryParseObject(overrideJsonObject, out JObject overrides, out string error))
                {
                    if (!string.IsNullOrWhiteSpace(error))
                        Debug.LogWarning($"OpenAI request override JSON ignored: {error}");

                    continue;
                }

                MergeInto(payload, overrides);
            }
        }

        public static void MergeInto(JObject target, JObject source)
        {
            if (target == null)
                throw new ArgumentNullException(nameof(target));

            if (source == null)
                return;

            MergeObject(target, source);
        }

        public static void AddFormFields(WWWForm form, string overrideJsonObject)
        {
            if (form == null)
                throw new ArgumentNullException(nameof(form));

            if (!TryParseObject(overrideJsonObject, out JObject overrides, out string error))
            {
                if (!string.IsNullOrWhiteSpace(error))
                    Debug.LogWarning($"OpenAI form override JSON ignored: {error}");

                return;
            }

            foreach (JProperty property in overrides.Properties())
            {
                AddFormField(form, property.Name, property.Value);
            }
        }

        private static void MergeObject(JObject target, JObject source)
        {
            foreach (JProperty sourceProperty in source.Properties())
            {
                if (sourceProperty.Value.Type == JTokenType.Null)
                {
                    target.Property(sourceProperty.Name)?.Remove();
                    continue;
                }

                JProperty targetProperty = target.Property(sourceProperty.Name);

                if (targetProperty?.Value is JObject targetObject && sourceProperty.Value is JObject sourceObject)
                {
                    MergeObject(targetObject, sourceObject);
                    continue;
                }

                target[sourceProperty.Name] = sourceProperty.Value.DeepClone();
            }
        }

        private static bool TryParseObject(string json, out JObject result, out string error)
        {
            result = null;
            error = null;

            if (string.IsNullOrWhiteSpace(json))
                return true;

            try
            {
                result = JObject.Parse(json);
                return true;
            }
            catch (Exception ex)
            {
                error = ex.Message;
                return false;
            }
        }

        private static void AddFormField(WWWForm form, string name, JToken value)
        {
            if (value == null || value.Type == JTokenType.Null)
                return;

            if (value is JArray array)
            {
                foreach (JToken item in array)
                {
                    AddFormField(form, $"{name}[]", item);
                }

                return;
            }

            string stringValue = value.Type == JTokenType.String ? value.Value<string>() : value.ToString(Formatting.None);
            form.AddField(name, stringValue);
        }
    }
}
