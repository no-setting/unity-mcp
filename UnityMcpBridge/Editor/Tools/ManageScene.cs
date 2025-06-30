using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityMcpBridge.Editor.Helpers;
using UnityMcpBridge.Editor.Models;

namespace UnityMcpBridge.Editor.Tools
{
    public static class ManageScene
    {
        public static string Handle(ManageSceneParams parameters)
        {
            string action = parameters.action?.ToLower();
            if (string.IsNullOrEmpty(action))
            {
                return JsonHelper.ToJson(Response.Error("Action parameter is required for manage_scene."));
            }

            try
            {
                switch (action)
                {
                    case "create":
                        return JsonHelper.ToJson(CreateScene(parameters));
                    case "load":
                        return JsonHelper.ToJson(LoadScene(parameters));
                    case "save":
                        return JsonHelper.ToJson(SaveScene(parameters));
                    case "get_active_scene":
                        return JsonHelper.ToJson(GetActiveScene());
                    case "get_active":
                        return JsonHelper.ToJson(GetActiveScene());
                    case "get_hierarchy":
                        return JsonHelper.ToJson(GetHierarchy());
                    default:
                        return JsonHelper.ToJson(Response.Error($"Unknown action for manage_scene: '{action}'"));
                }
            }
            catch (Exception e)
            {
                return JsonHelper.ToJson(Response.Error($"Failed to execute scene action '{action}': {e.Message}", e.StackTrace));
            }
        }

        private static object CreateScene(ManageSceneParams p)
        {
            if (string.IsNullOrEmpty(p.name))
                return Response.Error("Scene name is required for 'create' action.");

            var newScene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
            string scenePath = $"Assets/{p.name}.unity";
            if (!string.IsNullOrEmpty(p.path))
            {
                var dir = p.path.Trim('/');
                if (!Directory.Exists(Path.Combine(Application.dataPath, dir)))
                {
                    Directory.CreateDirectory(Path.Combine(Application.dataPath, dir));
                }
                scenePath = $"Assets/{dir}/{p.name}.unity";
            }

            bool saved = EditorSceneManager.SaveScene(newScene, scenePath);
            return saved
                ? Response.Success($"Scene '{p.name}' created at '{scenePath}'.", new { path = scenePath })
                : Response.Error($"Failed to save new scene at '{scenePath}'.");
        }

        private static object LoadScene(ManageSceneParams p)
        {
            if (string.IsNullOrEmpty(p.name))
                return Response.Error("Scene name or path is required for 'load' action.");
            
            string scenePath = p.path ?? $"Assets/{p.name}.unity";
            if (!scenePath.EndsWith(".unity")) scenePath += ".unity";
            if (!scenePath.StartsWith("Assets/")) scenePath = "Assets/" + scenePath;

            if (!File.Exists(scenePath))
                return Response.Error($"Scene not found at path: {scenePath}");

            EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            return Response.Success($"Scene '{p.name}' loaded successfully.");
        }

        private static object SaveScene(ManageSceneParams p)
        {
            var activeScene = SceneManager.GetActiveScene();
            if (!activeScene.IsValid())
                return Response.Error("No active scene to save.");
            
            bool saved = string.IsNullOrEmpty(p.path)
                ? EditorSceneManager.SaveOpenScenes()
                : EditorSceneManager.SaveScene(activeScene, p.path);
                
            return saved
                ? Response.Success($"Scene '{activeScene.name}' saved successfully.")
                : Response.Error("Failed to save the active scene.");
        }

        private static object GetActiveScene()
        {
            var activeScene = SceneManager.GetActiveScene();
            if (!activeScene.IsValid())
                return Response.Error("No valid active scene found.");
            
            return Response.Success("Active scene retrieved.", new { name = activeScene.name, path = activeScene.path });
        }

        private static object GetHierarchy()
        {
            var activeScene = SceneManager.GetActiveScene();
            var rootObjects = activeScene.GetRootGameObjects();
            var hierarchy = rootObjects.Select(go => GetObjectData(go)).ToList();
            return Response.Success("Scene hierarchy retrieved.", hierarchy);
        }

        private static object GetObjectData(GameObject go)
        {
            return new
            {
                name = go.name,
                active = go.activeSelf,
                children = go.transform.childCount > 0
                    ? Enumerable.Range(0, go.transform.childCount)
                        .Select(i => GetObjectData(go.transform.GetChild(i).gameObject))
                        .ToList()
                    : new System.Collections.Generic.List<object>()
            };
        }
    }
} 
