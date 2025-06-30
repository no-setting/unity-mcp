using System;
using UnityEngine;
using UnityEditor;
using UnityEditorInternal;
using UnityMcpBridge.Editor.Helpers;
using UnityMcpBridge.Editor.Models;

namespace UnityMcpBridge.Editor.Tools
{
    public static class ManageEditor
    {
        public static string Handle(ManageEditorParams parameters)
        {
            string action = parameters.action?.ToLower();
            if (string.IsNullOrEmpty(action))
            {
                return JsonHelper.ToJson(Response.Error("Action is required for manage_editor."));
            }

            try
            {
                switch (action)
                {
                    case "play":
                        EditorApplication.EnterPlaymode();
                        return JsonHelper.ToJson(Response.Success("Editor entering play mode."));
                    case "pause":
                        EditorApplication.isPaused = true;
                        return JsonHelper.ToJson(Response.Success("Editor paused."));
                    case "stop":
                        EditorApplication.ExitPlaymode();
                        return JsonHelper.ToJson(Response.Success("Editor exiting play mode."));
                    case "get_state":
                        var response = Response.Success("Editor state retrieved.", new
                        {
                            isPlaying = EditorApplication.isPlaying,
                            isPaused = EditorApplication.isPaused,
                            isCompiling = EditorApplication.isCompiling
                        });
                        
                        string jsonResponse = JsonHelper.ToJson(response);
                        Debug.Log($"[ManageEditor] get_state response: {jsonResponse}");
                        
                        return jsonResponse;
                    case "add_tag":
                        if (string.IsNullOrEmpty(parameters.tagName)) return JsonHelper.ToJson(Response.Error("Tag name is required to add a tag."));
                        InternalEditorUtility.AddTag(parameters.tagName);
                        return JsonHelper.ToJson(Response.Success($"Tag '{parameters.tagName}' added."));
                    case "add_layer":
                        // This is more complex than a single method call.
                        // Requires modifying the TagManager.asset file.
                        // Placeholder for now.
                        return JsonHelper.ToJson(Response.Error("Add layer is not implemented yet."));
                    default:
                        return JsonHelper.ToJson(Response.Error($"Unknown action for manage_editor: '{action}'"));
                }
            }
            catch (Exception e)
            {
                return JsonHelper.ToJson(Response.Error($"Failed to execute editor action '{action}': {e.Message}", e.StackTrace));
            }
        }
    }
} 
