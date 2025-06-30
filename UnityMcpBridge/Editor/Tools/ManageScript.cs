using System;
using System.IO;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;
using UnityMcpBridge.Editor.Helpers;
using UnityMcpBridge.Editor.Models;
using System.Linq;

namespace UnityMcpBridge.Editor.Tools
{
    /// <summary>
    /// Handles CRUD operations for C# scripts within the Unity project.
    /// </summary>
    public static class ManageScript
    {
        /// <summary>
        /// Main handler for script management actions.
        /// </summary>
        public static string Handle(ManageScriptParams parameters)
        {
            // Extract parameters
            string action = parameters.action?.ToLower();
            string name = parameters.name;
            string path = parameters.path;
            string contents = parameters.contents;
            string scriptType = parameters.scriptType;
            string namespaceName = parameters.namespaceName;

            // Validate required parameters
            if (string.IsNullOrEmpty(action))
            {
                return JsonHelper.ToJson(Response.Error("Action parameter is required."));
            }
            if (string.IsNullOrEmpty(name))
            {
                return JsonHelper.ToJson(Response.Error("Name parameter is required."));
            }

            // Basic name validation (alphanumeric, underscores, cannot start with number)
            if (!Regex.IsMatch(name, @"^[a-zA-Z_][a-zA-Z0-9_]*$"))
            {
                return JsonHelper.ToJson(Response.Error($"Invalid script name: '{name}'. Use only letters, numbers, underscores, and don't start with a number."));
            }

            // Set default directory to "Scripts" if path is not provided
            string relativeDir = path ?? "Scripts";
            if (!string.IsNullOrEmpty(relativeDir))
            {
                relativeDir = relativeDir.Replace('\\', '/').Trim('/');
                if (relativeDir.StartsWith("Assets/", StringComparison.OrdinalIgnoreCase))
                {
                    relativeDir = relativeDir.Substring("Assets/".Length).TrimStart('/');
                }
            }
            if (string.IsNullOrEmpty(relativeDir))
            {
                relativeDir = "Scripts";
            }

            // Construct paths
            string scriptFileName = $"{name}.cs";
            string fullPathDir = Path.Combine(Application.dataPath, relativeDir);
            string fullPath = Path.Combine(fullPathDir, scriptFileName);
            string relativePath = Path.Combine("Assets", relativeDir, scriptFileName).Replace('\\', '/');

            // Ensure the target directory exists for create/update
            if (action == "create" || action == "update")
            {
                try
                {
                    Directory.CreateDirectory(fullPathDir);
                }
                catch (Exception e)
                {
                    return JsonHelper.ToJson(Response.Error($"Could not create directory '{fullPathDir}': {e.Message}"));
                }
            }

            // Route to specific action handlers
            switch (action)
            {
                case "create":
                    return JsonHelper.ToJson(CreateScript(fullPath, relativePath, name, contents, scriptType, namespaceName));
                case "read":
                    return JsonHelper.ToJson(ReadScript(fullPath, relativePath));
                case "update":
                    return JsonHelper.ToJson(UpdateScript(fullPath, relativePath, name, contents));
                case "delete":
                    return JsonHelper.ToJson(DeleteScript(fullPath, relativePath));
                default:
                    return JsonHelper.ToJson(Response.Error($"Unknown action: '{action}'. Valid actions are: create, read, update, delete."));
            }
        }

        private static object CreateScript(string fullPath, string relativePath, string name, string contents, string scriptType, string namespaceName)
        {
            // Check if script already exists
            if (File.Exists(fullPath))
            {
                return Response.Error($"Script already exists at '{relativePath}'. Use 'update' action to modify.");
            }

            // Generate default content if none provided
            if (string.IsNullOrEmpty(contents))
            {
                contents = GenerateDefaultScriptContent(name, scriptType, namespaceName);
            }

            try
            {
                File.WriteAllText(fullPath, contents);
                AssetDatabase.ImportAsset(relativePath);
                AssetDatabase.Refresh();
                return Response.Success($"Script '{name}.cs' created successfully at '{relativePath}'.", new { path = relativePath });
            }
            catch (Exception e)
            {
                return Response.Error($"Failed to create script '{relativePath}': {e.Message}");
            }
        }

        private static object ReadScript(string fullPath, string relativePath)
        {
            if (!File.Exists(fullPath))
            {
                return Response.Error($"Script not found at '{relativePath}'.");
            }

            try
            {
                string contents = File.ReadAllText(fullPath);
                var responseData = new
                {
                    path = relativePath,
                    contents = contents
                };

                return Response.Success($"Script '{Path.GetFileName(relativePath)}' read successfully.", responseData);
            }
            catch (Exception e)
            {
                return Response.Error($"Failed to read script '{relativePath}': {e.Message}");
            }
        }

        private static object UpdateScript(string fullPath, string relativePath, string name, string contents)
        {
            if (!File.Exists(fullPath))
            {
                return Response.Error($"Script not found at '{relativePath}'. Use 'create' action to add a new script.");
            }
            if (string.IsNullOrEmpty(contents))
            {
                return Response.Error("Content is required for the 'update' action.");
            }

            try
            {
                File.WriteAllText(fullPath, contents);
                AssetDatabase.ImportAsset(relativePath);
                AssetDatabase.Refresh();
                return Response.Success($"Script '{name}.cs' updated successfully at '{relativePath}'.", new { path = relativePath });
            }
            catch (Exception e)
            {
                return Response.Error($"Failed to update script '{relativePath}': {e.Message}");
            }
        }

        private static object DeleteScript(string fullPath, string relativePath)
        {
            if (!File.Exists(fullPath))
            {
                return Response.Error($"Script not found at '{relativePath}'. Cannot delete.");
            }

            try
            {
                bool deleted = AssetDatabase.MoveAssetToTrash(relativePath);
                if (deleted)
                {
                    AssetDatabase.Refresh();
                    return Response.Success($"Script '{Path.GetFileName(relativePath)}' moved to trash successfully.");
                }
                else
                {
                    return Response.Error($"Failed to move script '{relativePath}' to trash. It might be locked or in use.");
                }
            }
            catch (Exception e)
            {
                return Response.Error($"Error deleting script '{relativePath}': {e.Message}");
            }
        }

        /// <summary>
        /// Generates basic C# script content based on name and type.
        /// </summary>
        private static string GenerateDefaultScriptContent(string name, string scriptType, string namespaceName)
        {
            string usingStatements = "using UnityEngine;\nusing System.Collections;\n";
            string classDeclaration;
            string body = "\n    // Use this for initialization\n    void Start()\n    {\n\n    }\n\n    // Update is called once per frame\n    void Update()\n    {\n\n    }\n";

            string baseClass = "";
            if (!string.IsNullOrEmpty(scriptType))
            {
                if (scriptType.Equals("MonoBehaviour", StringComparison.OrdinalIgnoreCase))
                    baseClass = " : MonoBehaviour";
                else if (scriptType.Equals("ScriptableObject", StringComparison.OrdinalIgnoreCase))
                {
                    baseClass = " : ScriptableObject";
                    body = ""; // ScriptableObjects don't usually need Start/Update
                }
                else if (scriptType.Equals("Editor", StringComparison.OrdinalIgnoreCase))
                {
                    usingStatements += "using UnityEditor;\n";
                    baseClass = " : Editor";
                    body = ""; // Editor scripts have different structures
                }
            }

            classDeclaration = $"public class {name}{baseClass}";

            string fullContent = $"{usingStatements}\n";
            bool useNamespace = !string.IsNullOrEmpty(namespaceName);

            if (useNamespace)
            {
                fullContent += $"namespace {namespaceName}\n{{\n";
                classDeclaration = "    " + classDeclaration;
                body = string.Join("\n", body.Split('\n').Select(line => "    " + line));
            }

            fullContent += $"{classDeclaration}\n{{\n{body}\n}}";

            if (useNamespace)
            {
                fullContent += "\n}";
            }

            return fullContent.Trim() + "\n";
        }
    }
}
