using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityMcpBridge.Editor.Helpers;
using UnityMcpBridge.Editor.Models;

namespace UnityMcpBridge.Editor.Tools
{
    public static class ReadConsole
    {
        private static Type logEntriesType;
        private static MethodInfo getCountMethod;
        private static MethodInfo getEntryMethod;
        private static MethodInfo startGettingEntriesMethod;
        private static MethodInfo endGettingEntriesMethod;
        private static object logEntry;

        static ReadConsole()
        {
            // Use reflection to get internal UnityEditor log reading types/methods
            var assembly = Assembly.GetAssembly(typeof(EditorWindow));
            logEntriesType = assembly.GetType("UnityEditor.LogEntries");
            getCountMethod = logEntriesType.GetMethod("GetCount");
            getEntryMethod = logEntriesType.GetMethod("GetEntryInternal");
            startGettingEntriesMethod = logEntriesType.GetMethod("StartGettingEntries");
            endGettingEntriesMethod = logEntriesType.GetMethod("EndGettingEntries");
            
            var logEntryType = assembly.GetType("UnityEditor.LogEntry");
            logEntry = Activator.CreateInstance(logEntryType);
        }
        
        public static string Handle(ReadConsoleParams p)
        {
            if (string.IsNullOrEmpty(p.action)) return JsonHelper.ToJson(Response.Error("Action is required."));
            try
            {
                switch (p.action.ToLower())
                {
                    case "get":
                        return JsonHelper.ToJson(GetLogs(p));
                    case "clear":
                        logEntriesType.GetMethod("Clear").Invoke(null, null);
                        return JsonHelper.ToJson(Response.Success("Console cleared."));
                    default:
                        return JsonHelper.ToJson(Response.Error($"Unknown action: {p.action}"));
                }
            }
            catch (Exception e)
            {
                return JsonHelper.ToJson(Response.Error($"Failed to execute console action {p.action}: {e.Message}", e.StackTrace));
            }
        }
        
        private static object GetLogs(ReadConsoleParams p)
        {
            var messages = new List<object>();
            startGettingEntriesMethod.Invoke(null, null);
            int count = (int)getCountMethod.Invoke(null, null);

            for (int i = 0; i < count; i++)
            {
                getEntryMethod.Invoke(null, new object[] { i, logEntry });
                var message = (string)logEntry.GetType().GetField("message").GetValue(logEntry);
                var mode = (int)logEntry.GetType().GetField("mode").GetValue(logEntry);

                // Simple filtering
                if (!string.IsNullOrEmpty(p.filterText) && !message.Contains(p.filterText)) continue;
                
                // Type filtering would go here (by checking 'mode')

                messages.Add(new { message, mode });
            }
            endGettingEntriesMethod.Invoke(null, null);

            return Response.Success("Logs retrieved.", new { logs = messages });
        }
    }
} 
