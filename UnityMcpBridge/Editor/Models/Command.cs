using System;
using System.Collections.Generic;
using UnityEngine;
using Newtonsoft.Json.Linq;

namespace UnityMcpBridge.Editor.Models
{
    /// <summary>
    /// Represents a command received from the MCP client
    /// </summary>
    [Serializable]
    public class Command
    {
        /// <summary>
        /// The type of command to execute
        /// </summary>
        public string type;

        /// <summary>
        /// The parameters for the command as JObject (flexible JSON object)
        /// </summary>
        public JObject parameters; // This will be a JSON object
        
        /// <summary>
        /// Get parameter value by key (simple string values only)
        /// </summary>
        public string GetParam(string key)
        {
            if (string.IsNullOrEmpty(parameters.ToString()))
                return null;
                
            try
            {
                // Simple JSON parsing for key-value pairs
                string searchKey = $"\"{key}\":";
                int keyIndex = parameters.ToString().IndexOf(searchKey);
                if (keyIndex == -1) return null;
                
                int valueStart = keyIndex + searchKey.Length;
                
                // Skip whitespace
                while (valueStart < parameters.ToString().Length && char.IsWhiteSpace(parameters.ToString()[valueStart]))
                    valueStart++;
                
                if (valueStart >= parameters.ToString().Length) return null;
                
                // Handle string values (quoted)
                if (parameters.ToString()[valueStart] == '"')
                {
                    valueStart++; // Skip opening quote
                    int valueEnd = parameters.ToString().IndexOf('"', valueStart);
                    if (valueEnd == -1) return null;
                    return parameters.ToString().Substring(valueStart, valueEnd - valueStart);
                }
                
                // Handle non-string values (numbers, booleans)
                int valueEnd2 = valueStart;
                while (valueEnd2 < parameters.ToString().Length && 
                       parameters.ToString()[valueEnd2] != ',' && 
                       parameters.ToString()[valueEnd2] != '}' && 
                       parameters.ToString()[valueEnd2] != ']')
                {
                    valueEnd2++;
                }
                
                return parameters.ToString().Substring(valueStart, valueEnd2 - valueStart).Trim();
            }
            catch
            {
                return null;
            }
        }
        
        /// <summary>
        /// Check if parameter exists
        /// </summary>
        public bool HasParam(string key)
        {
            if (string.IsNullOrEmpty(parameters.ToString()))
                return false;
                
            return parameters.ToString().Contains($"\"{key}\":");
        }
    }

    // Parameter classes for each command type
    [Serializable]
    public class ManageScriptParams
    {
        public string action;
        public string name;
        public string path;
        public string contents;
        public string scriptType;
        public string namespaceName;
    }

    [Serializable]
    public class ManageSceneParams
    {
        public string action;
        public string name;
        public string path;
        public int? buildIndex;
    }

    [Serializable]
    public class ManageEditorParams
    {
        public string action;
        public bool? waitForCompletion;
        public string toolName;
        public string tagName;
        public string layerName;
    }

    [Serializable]
    public class PropertyModification
    {
        public string propertyName;
        public string propertyValue; // Using string for simplicity, handler will parse it
    }

    [Serializable]
    public class ComponentModification
    {
        public string componentName;
        public System.Collections.Generic.List<PropertyModification> properties;
    }

    [Serializable]
    public class ManageGameObjectParams
    {
        public string action;
        public string target;
        public string searchMethod;
        public string name;
        public string tag;
        public string layer;
        public string parent;
        public float[] position;
        public float[] rotation;
        public float[] scale;
        public string primitiveType;
        public System.Collections.Generic.List<string> componentsToAdd;
        public System.Collections.Generic.List<string> componentsToRemove;
        public System.Collections.Generic.List<ComponentModification> componentProperties;
        public bool? setActive;
        public bool? saveAsPrefab;
        public string prefabPath;
    }

    [Serializable]
    public class ManageAssetParams
    {
        public string action;
        public string path;
        public string assetType;
        public string destination;
        public string searchPattern;
        // Properties will be handled as a JSON string for flexibility
        public string properties;
    }

    [Serializable]
    public class ReadConsoleParams
    {
        public string action;
        public System.Collections.Generic.List<string> types;
        public string filterText;
    }

    [Serializable]
    public class ExecuteMenuItemParams
    {
        public string menuPath;
    }
}
