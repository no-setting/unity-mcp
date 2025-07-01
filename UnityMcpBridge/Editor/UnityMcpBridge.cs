using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using UnityMcpBridge.Editor.Helpers;
using UnityMcpBridge.Editor.Models;
using UnityMcpBridge.Editor.Tools;
using Newtonsoft.Json;

namespace UnityMcpBridge.Editor
{
    [InitializeOnLoad]
    public static partial class UnityMcpBridge
    {
        private static TcpListener listener;
        private static bool isRunning = false;
        private static readonly object lockObj = new();
        private static Dictionary<string, (string commandJson, TaskCompletionSource<string> tcs)> commandQueue = new();
        private static readonly int unityPort = 6400; // Hardcoded port

        public static bool IsRunning => isRunning;

        static UnityMcpBridge()
        {
            Start();
            EditorApplication.quitting += Stop;
        }

        public static void Start()
        {
            Stop();

            if (isRunning)
            {
                return;
            }

            try
            {
                listener = new TcpListener(IPAddress.Loopback, unityPort);
                listener.Start();
                isRunning = true;
                Debug.Log($"UnityMcpBridge started on port {unityPort}.");
                Task.Run(ListenerLoop);
                EditorApplication.update += ProcessCommands;
            }
            catch (SocketException ex)
            {
                if (ex.SocketErrorCode == SocketError.AddressAlreadyInUse)
                {
                    Debug.LogError($"Port {unityPort} is already in use. Ensure no other instances are running or change the port.");
                }
                else
                {
                    Debug.LogError($"Failed to start TCP listener: {ex.Message}");
                }
            }
        }

        public static void Stop()
        {
            if (!isRunning)
            {
                return;
            }

            try
            {
                listener?.Stop();
                listener = null;
                isRunning = false;
                EditorApplication.update -= ProcessCommands;
                Debug.Log("UnityMcpBridge stopped.");
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error stopping UnityMcpBridge: {ex.Message}");
            }
        }

        private static async Task ListenerLoop()
        {
            while (isRunning)
            {
                try
                {
                    TcpClient client = await listener.AcceptTcpClientAsync();
                    // Enable basic socket keepalive
                    client.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.KeepAlive, true);

                    // Set longer receive timeout to prevent quick disconnections
                    client.ReceiveTimeout = 60000; // 60 seconds

                    // Fire and forget each client connection
                    _ = HandleClientAsync(client);
                }
                catch (Exception ex)
                {
                    if (isRunning)
                    {
                        Debug.LogError($"Listener error: {ex.Message}");
                    }
                }
            }
        }

        private static async Task HandleClientAsync(TcpClient client)
        {
            using (client)
            using (NetworkStream stream = client.GetStream())
            {
                byte[] buffer = new byte[8192];
                while (isRunning)
                {
                    try
                    {
                        int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);
                        if (bytesRead == 0)
                        {
                            break; // Client disconnected
                        }

                        string commandText = System.Text.Encoding.UTF8.GetString(buffer, 0, bytesRead);
                        string commandId = Guid.NewGuid().ToString();
                        TaskCompletionSource<string> tcs = new();

                        // Special handling for ping command to avoid JSON parsing
                        if (commandText.Trim() == "ping")
                        {
                            // Direct response to ping without going through JSON parsing
                            byte[] pingResponseBytes = System.Text.Encoding.UTF8.GetBytes(
                                /*lang=json,strict*/
                                "{\"status\":\"success\",\"result\":{\"message\":\"pong\"}}"
                            );
                            await stream.WriteAsync(pingResponseBytes, 0, pingResponseBytes.Length);
                            continue;
                        }

                        lock (lockObj)
                        {
                            commandQueue[commandId] = (commandText, tcs);
                        }

                        string response = await tcs.Task;
                        Debug.Log($"[HandleClientAsync] Sending response: {response}");
                        byte[] responseBytes = System.Text.Encoding.UTF8.GetBytes(response);
                        await stream.WriteAsync(responseBytes, 0, responseBytes.Length);
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError($"Client handler error: {ex.Message}");
                        break;
                    }
                }
            }
        }

        private static void ProcessCommands()
        {
            List<string> processedIds = new();
            lock (lockObj)
            {
                foreach (KeyValuePair<string, (string commandJson, TaskCompletionSource<string> tcs)> kvp in commandQueue.ToList())
                {
                    string id = kvp.Key;
                    string commandText = kvp.Value.commandJson;
                    TaskCompletionSource<string> tcs = kvp.Value.tcs;

                    try
                    {
                        // Special case handling
                        if (string.IsNullOrEmpty(commandText))
                        {
                            var emptyResponse = new
                            {
                                status = "error",
                                error = "Empty command received",
                            };
                            tcs.SetResult(JsonHelper.ToJson(emptyResponse));
                            processedIds.Add(id);
                            continue;
                        }

                        // Trim the command text to remove any whitespace
                        commandText = commandText.Trim();

                        // Non-JSON direct commands handling (like ping)
                        if (commandText == "ping")
                        {
                            var pingResponse = new
                            {
                                status = "success",
                                result = new { message = "pong" },
                            };
                            tcs.SetResult(JsonHelper.ToJson(pingResponse));
                            processedIds.Add(id);
                            continue;
                        }

                        // Check if the command is valid JSON before attempting to deserialize
                        if (!JsonHelper.IsValidJson(commandText))
                        {
                            var invalidJsonResponse = new
                            {
                                status = "error",
                                error = "Invalid JSON format",
                                receivedText = commandText.Length > 50 ? commandText[..50] + "..." : commandText,
                            };
                            tcs.SetResult(JsonHelper.ToJson(invalidJsonResponse));
                            processedIds.Add(id);
                            continue;
                        }

                        // Normal JSON command processing
                        Debug.Log($"[ProcessCommands] Raw command text: {commandText}");
                        // フィールド名を変換: "params" -> "parameters"
                        commandText = System.Text.RegularExpressions.Regex.Replace(
                            commandText,
                            "\\\"@params\\\"[ ]*:",
                            "\"parameters\":",
                            System.Text.RegularExpressions.RegexOptions.IgnoreCase
                        );
                        Debug.Log($"[ProcessCommands] After replacement: {commandText}");
                        Command command = JsonHelper.FromJson<Command>(commandText);
                        Debug.Log($"[ProcessCommands] Received command type: {command?.type ?? "null"}");
                        Debug.Log($"[ProcessCommands] Command text: {commandText}");
                        if (command == null)
                        {
                            Debug.LogError("[ProcessCommands] Failed to deserialize command");
                            var nullCommandResponse = new
                            {
                                status = "error",
                                error = "Command deserialized to null",
                                details = "The command was valid JSON but could not be deserialized to a Command object",
                            };
                            tcs.SetResult(JsonHelper.ToJson(nullCommandResponse));
                        }
                        else
                        {
                            Debug.Log($"[ProcessCommands] Command type: {command.type}");
                            Debug.Log($"[ProcessCommands] Parameters: {command.parameters}");
                            string responseJson = ExecuteCommand(command);
                            tcs.SetResult(responseJson);
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError($"Error processing command: {ex.Message}\n{ex.StackTrace}");

                        var response = new
                        {
                            status = "error",
                            error = ex.Message,
                            commandType = "Unknown (error during processing)",
                            receivedText = commandText?.Length > 50 ? commandText[..50] + "..." : commandText,
                        };
                        string responseJson = JsonHelper.ToJson(response);
                        tcs.SetResult(responseJson);
                    }

                    processedIds.Add(id);
                }

                foreach (string id in processedIds)
                {
                    commandQueue.Remove(id);
                }
            }
        }

        private static string ExecuteCommand(Command command)
        {
            Debug.Log($"[ExecuteCommand] Started - Type: {command.type}");
            Debug.Log($"[ExecuteCommand] Parameters: {command.parameters}");
            try
            {
                // parametersをJObjectからJSON文字列に変換
                string parametersJson = command.parameters != null ? command.parameters.ToString() : "{}";
                switch (command.type)
                {
                    case "manage_script":
                        Debug.Log("[ExecuteCommand] Processing manage_script");
                        var scriptParams = JsonConvert.DeserializeObject<ManageScriptParams>(parametersJson);
                        Debug.Log($"[ExecuteCommand] Parsed action: {scriptParams?.action}");
                        return ManageScript.Handle(scriptParams);
                    case "manage_scene":
                        Debug.Log("[ExecuteCommand] Processing manage_scene");
                        var sceneParams = JsonConvert.DeserializeObject<ManageSceneParams>(parametersJson);
                        Debug.Log($"[ExecuteCommand] Parsed action: {sceneParams?.action}");
                        return ManageScene.Handle(sceneParams);
                    case "manage_editor":
                        Debug.Log("[ExecuteCommand] Processing manage_editor");
                        var editorParams = JsonConvert.DeserializeObject<ManageEditorParams>(parametersJson);
                        Debug.Log($"[ExecuteCommand] Parsed action: {editorParams?.action}");
                        return ManageEditor.Handle(editorParams);
                    case "manage_gameobject":
                        Debug.Log("[ExecuteCommand] Processing manage_gameobject");
                        var gameObjectParams = JsonConvert.DeserializeObject<ManageGameObjectParams>(parametersJson);
                        Debug.Log($"[ExecuteCommand] Parsed action: {gameObjectParams?.action}");
                        return ManageGameObject.Handle(gameObjectParams);
                    case "manage_asset":
                        Debug.Log("[ExecuteCommand] Processing manage_asset");
                        var assetParams = JsonConvert.DeserializeObject<ManageAssetParams>(parametersJson);
                        Debug.Log($"[ExecuteCommand] Parsed action: {assetParams?.action}");
                        return ManageAsset.Handle(assetParams);
                    case "read_console":
                        Debug.Log("[ExecuteCommand] Processing read_console");
                        var consoleParams = JsonConvert.DeserializeObject<ReadConsoleParams>(parametersJson);
                        Debug.Log($"[ExecuteCommand] Parsed action: {consoleParams?.action}");
                        return ReadConsole.Handle(consoleParams);
                    case "execute_menu_item":
                        Debug.Log("[ExecuteCommand] Processing execute_menu_item");
                        var menuItemParams = JsonConvert.DeserializeObject<ExecuteMenuItemParams>(parametersJson);
                        Debug.Log($"[ExecuteCommand] Parsed menuPath: {menuItemParams?.menuPath}");
                        return ExecuteMenuItem.Handle(menuItemParams);
                    default:
                        Debug.LogError($"[ExecuteCommand] Unknown command type: {command.type}");
                        return JsonConvert.SerializeObject(Response.Error($"Unknown command type: {command.type}"));
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[ExecuteCommand] Exception: {ex.Message}\n{ex.StackTrace}");
                return JsonConvert.SerializeObject(Response.Error($"Error: {ex.Message}"));
            }
        }
    }
}
