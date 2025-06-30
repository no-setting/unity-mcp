using System;
using System.Collections.Generic;
using UnityEngine;
using Newtonsoft.Json;

namespace UnityMcpBridge.Editor.Helpers
{
    /// <summary>
    /// Helper class for JSON operations using Unity's JsonUtility
    /// </summary>
    public static class JsonHelper
    {
        /// <summary>
        /// Convert object to JSON string
        /// </summary>
        public static string ToJson(object obj)
        {
            try
            {
                return Newtonsoft.Json.JsonConvert.SerializeObject(obj, Newtonsoft.Json.Formatting.Indented);
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to serialize object to JSON: {e.Message}");
                return "{}";
            }
        }

        /// <summary>
        /// Convert JSON string to specified type
        /// </summary>
        public static T FromJson<T>(string json)
        {
            try
            {
                return JsonConvert.DeserializeObject<T>(json);
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to deserialize JSON to {typeof(T).Name}: {e.Message}");
                return default(T);
            }
        }

        /// <summary>
        /// Check if string is valid JSON
        /// </summary>
        public static bool IsValidJson(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
                return false;

            json = json.Trim();
            return (json.StartsWith("{") && json.EndsWith("}")) ||
                   (json.StartsWith("[") && json.EndsWith("]"));
        }

        /// <summary>
        /// Extract string value from JSON by key (simple implementation)
        /// </summary>
        public static string GetStringValue(string json, string key)
        {
            if (string.IsNullOrEmpty(json) || string.IsNullOrEmpty(key))
                return null;

            try
            {
                string searchKey = $"\"{key}\":";
                int keyIndex = json.IndexOf(searchKey);
                if (keyIndex == -1) return null;
                
                int valueStart = keyIndex + searchKey.Length;
                
                // Skip whitespace
                while (valueStart < json.Length && char.IsWhiteSpace(json[valueStart]))
                    valueStart++;
                
                if (valueStart >= json.Length) return null;
                
                // Handle string values (quoted)
                if (json[valueStart] == '"')
                {
                    valueStart++; // Skip opening quote
                    int valueEnd = json.IndexOf('"', valueStart);
                    if (valueEnd == -1) return null;
                    return json.Substring(valueStart, valueEnd - valueStart);
                }
                
                return null;
            }
            catch
            {
                return null;
            }
        }
    }
}
