using System;
using UnityEditor;
using UnityMcpBridge.Editor.Helpers;
using UnityMcpBridge.Editor.Models;

namespace UnityMcpBridge.Editor.Tools
{
    public static class ExecuteMenuItem
    {
        public static string Handle(ExecuteMenuItemParams p)
        {
            if (string.IsNullOrEmpty(p.menuPath))
            {
                return JsonHelper.ToJson(Response.Error("menuPath parameter is required."));
            }

            try
            {
                EditorApplication.ExecuteMenuItem(p.menuPath);
                return JsonHelper.ToJson(Response.Success($"Menu item '{p.menuPath}' executed successfully."));
            }
            catch (Exception e)
            {
                return JsonHelper.ToJson(Response.Error($"An error occurred while executing menu item '{p.menuPath}': {e.Message}", e.StackTrace));
            }
        }
    }
} 
