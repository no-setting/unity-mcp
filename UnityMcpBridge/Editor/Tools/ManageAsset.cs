using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityMcpBridge.Editor.Helpers;
using UnityMcpBridge.Editor.Models;

namespace UnityMcpBridge.Editor.Tools
{
    public static class ManageAsset
    {
        public static string Handle(ManageAssetParams p)
        {
            if (string.IsNullOrEmpty(p.action)) return JsonHelper.ToJson(Response.Error("Action is required."));
            try
            {
                switch (p.action.ToLower())
                {
                    case "create": return JsonHelper.ToJson(CreateAsset(p));
                    case "delete": return JsonHelper.ToJson(DeleteAsset(p));
                    case "move": return JsonHelper.ToJson(MoveAsset(p));
                    case "search": return JsonHelper.ToJson(SearchAssets(p));
                    default: return JsonHelper.ToJson(Response.Error($"Unknown action: {p.action}"));
                }
            }
            catch (Exception e)
            {
                return JsonHelper.ToJson(Response.Error($"Failed to execute asset action {p.action}: {e.Message}", e.StackTrace));
            }
        }

        private static object CreateAsset(ManageAssetParams p)
        {
            if (string.IsNullOrEmpty(p.path)) return Response.Error("Path is required for create action.");
            if (string.IsNullOrEmpty(p.assetType)) return Response.Error("Asset type is required for create action.");

            // Ensure the directory exists
            var directory = Path.GetDirectoryName(p.path);
            if (!Directory.Exists(Path.Combine(Application.dataPath, directory.Substring("Assets/".Length))))
            {
                Directory.CreateDirectory(Path.Combine(Application.dataPath, directory.Substring("Assets/".Length)));
            }

            if (p.assetType.Equals("Material", StringComparison.OrdinalIgnoreCase))
            {
                var material = new Material(Shader.Find("Standard"));
                AssetDatabase.CreateAsset(material, p.path);
                // Further property modifications would go here, by parsing p.properties
                ApplyAssetProperties(material, p.properties);
                return Response.Success("Material created.", new { path = p.path });
            }

            // ScriptableObject作成
            if (p.assetType.Equals("ScriptableObject", StringComparison.OrdinalIgnoreCase))
            {
                if (string.IsNullOrEmpty(p.properties))
                    return Response.Error("'properties'にScriptableObjectの型名（className）が必要です。");
                var propDict = JsonHelper.FromJson<Dictionary<string, string>>(p.properties);
                if (!propDict.ContainsKey("className"))
                    return Response.Error("'className'が必要です。");
                var soType = Type.GetType(propDict["className"]) ??
                    AppDomain.CurrentDomain.GetAssemblies().SelectMany(a => a.GetTypes()).FirstOrDefault(t => t.Name == propDict["className"]);
                if (soType == null || !typeof(ScriptableObject).IsAssignableFrom(soType))
                    return Response.Error($"ScriptableObject型 '{propDict["className"]}' が見つかりません。");
                var so = ScriptableObject.CreateInstance(soType);
                AssetDatabase.CreateAsset(so, p.path);
                ApplyAssetProperties(so, p.properties);
                return Response.Success("ScriptableObject created.", new { path = p.path });
            }

            // Texture2D作成
            if (p.assetType.Equals("Texture", StringComparison.OrdinalIgnoreCase) || p.assetType.Equals("Texture2D", StringComparison.OrdinalIgnoreCase))
            {
                var tex = new Texture2D(128, 128);
                AssetDatabase.CreateAsset(tex, p.path);
                ApplyAssetProperties(tex, p.properties);
                return Response.Success("Texture2D created.", new { path = p.path });
            }

            // Prefab作成
            if (p.assetType.Equals("Prefab", StringComparison.OrdinalIgnoreCase))
            {
                if (string.IsNullOrEmpty(p.properties))
                    return Response.Error("'properties'にGameObject名（gameObjectName）が必要です。");
                var propDict = JsonHelper.FromJson<Dictionary<string, string>>(p.properties);
                if (!propDict.ContainsKey("gameObjectName"))
                    return Response.Error("'gameObjectName'が必要です。");
                var go = GameObject.Find(propDict["gameObjectName"]);
                if (go == null) return Response.Error($"GameObject '{propDict["gameObjectName"]}' が見つかりません。");
                string path = p.path;
                if (!path.EndsWith(".prefab")) path += ".prefab";
                var prefab = PrefabUtility.SaveAsPrefabAsset(go, path);
                return Response.Success("Prefab created.", new { path = path });
            }

            // フォルダ作成
            if (p.assetType.Equals("Folder", StringComparison.OrdinalIgnoreCase))
            {
                if (!AssetDatabase.IsValidFolder(p.path))
                {
                    AssetDatabase.CreateFolder(Path.GetDirectoryName(p.path), Path.GetFileName(p.path));
                }
                return Response.Success("Folder created.", new { path = p.path });
            }

            return Response.Error($"Asset type '{p.assetType}' creation not supported yet.");
        }

        private static object DeleteAsset(ManageAssetParams p)
        {
            if (string.IsNullOrEmpty(p.path)) return Response.Error("Path is required for delete action.");
            if (!AssetDatabase.MoveAssetToTrash(p.path))
            {
                return Response.Error("Failed to delete asset. It may not exist or be locked.");
            }
            return Response.Success($"Asset at '{p.path}' moved to trash.");
        }

        private static object MoveAsset(ManageAssetParams p)
        {
            if (string.IsNullOrEmpty(p.path)) return Response.Error("Source path is required.");
            if (string.IsNullOrEmpty(p.destination)) return Response.Error("Destination path is required.");

            string validationError = AssetDatabase.ValidateMoveAsset(p.path, p.destination);
            if (!string.IsNullOrEmpty(validationError))
            {
                return Response.Error($"Invalid asset move: {validationError}");
            }
            AssetDatabase.MoveAsset(p.path, p.destination);
            return Response.Success($"Asset moved from '{p.path}' to '{p.destination}'.");
        }

        private static object SearchAssets(ManageAssetParams p)
        {
            if (string.IsNullOrEmpty(p.searchPattern)) return Response.Error("Search pattern is required.");
            
            string[] searchInFolders = string.IsNullOrEmpty(p.path) ? null : new[] { p.path };
            var assetGuids = AssetDatabase.FindAssets(p.searchPattern, searchInFolders);
            var assetPaths = assetGuids.Select(AssetDatabase.GUIDToAssetPath).ToList();

            return Response.Success("Asset search complete.", new { count = assetPaths.Count, paths = assetPaths });
        }

        // --- アセットプロパティ適用ヘルパー ---
        private static void ApplyAssetProperties(UnityEngine.Object asset, string propertiesJson)
        {
            if (string.IsNullOrEmpty(propertiesJson) || asset == null) return;
            try
            {
                var dict = JsonHelper.FromJson<Dictionary<string, string>>(propertiesJson);
                foreach (var kv in dict)
                {
                    if (kv.Key == "className" || kv.Key == "gameObjectName") continue;
                    var prop = asset.GetType().GetProperty(kv.Key);
                    if (prop != null && prop.CanWrite)
                    {
                        try
                        {
                            object value = Convert.ChangeType(kv.Value, prop.PropertyType);
                            prop.SetValue(asset, value);
                        }
                        catch { }
                    }
                    else
                    {
                        var field = asset.GetType().GetField(kv.Key);
                        if (field != null)
                        {
                            try
                            {
                                object value = Convert.ChangeType(kv.Value, field.FieldType);
                                field.SetValue(asset, value);
                            }
                            catch { }
                        }
                    }
                }
                EditorUtility.SetDirty(asset);
            }
            catch { }
        }
    }
} 
