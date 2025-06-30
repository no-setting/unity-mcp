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
    public static class ManageGameObject
    {
        public static string Handle(ManageGameObjectParams p)
        {
            if (string.IsNullOrEmpty(p.action)) return JsonHelper.ToJson(Response.Error("Action is required."));
            try
            {
                switch (p.action.ToLower())
                {
                    case "create": return JsonHelper.ToJson(CreateGameObject(p));
                    case "find": return JsonHelper.ToJson(FindGameObject(p));
                    case "modify": return JsonHelper.ToJson(ModifyGameObject(p));
                    case "delete": return JsonHelper.ToJson(DeleteGameObject(p));
                    default: return JsonHelper.ToJson(Response.Error($"Unknown action: {p.action}"));
                }
            }
            catch (Exception e)
            {
                return JsonHelper.ToJson(Response.Error($"Failed to execute action {p.action}: {e.Message}", e.StackTrace));
            }
        }

        private static object CreateGameObject(ManageGameObjectParams p)
        {
            GameObject go;
            if (!string.IsNullOrEmpty(p.primitiveType))
            {
                var type = (PrimitiveType)Enum.Parse(typeof(PrimitiveType), p.primitiveType, true);
                go = GameObject.CreatePrimitive(type);
            }
            else
            {
                go = new GameObject();
            }

            if (!string.IsNullOrEmpty(p.name)) go.name = p.name;
            
            // Further modifications can be done via the ModifyGameObject logic
            ModifyGameObject(p, go);

            return Response.Success($"GameObject '{go.name}' created.", new { name = go.name, instanceId = go.GetInstanceID() });
        }

        private static object FindGameObject(ManageGameObjectParams p)
        {
            if (string.IsNullOrEmpty(p.target) && string.IsNullOrEmpty(p.searchMethod))
                return Response.Error("Target or searchMethod is required for find action.");

            List<GameObject> found = new List<GameObject>();
            string method = p.searchMethod?.ToLower() ?? "by_name";

            switch (method)
            {
                case "by_name":
                    if (!string.IsNullOrEmpty(p.target))
                    {
                        var go = GameObject.Find(p.target);
                        if (go != null) found.Add(go);
                    }
                    break;
                case "by_tag":
                    if (!string.IsNullOrEmpty(p.target))
                    {
                        found.AddRange(GameObject.FindGameObjectsWithTag(p.target));
                    }
                    break;
                case "by_layer":
                    if (!string.IsNullOrEmpty(p.target))
                    {
                        int layer = LayerMask.NameToLayer(p.target);
                        found.AddRange(GameObject.FindObjectsOfType<GameObject>().Where(go => go.layer == layer));
                    }
                    break;
                case "by_path":
                    if (!string.IsNullOrEmpty(p.target))
                    {
                        var go = GameObject.Find(p.target); // Unityのパス指定（"Parent/Child"）
                        if (go != null) found.Add(go);
                    }
                    break;
                case "by_id":
                    if (int.TryParse(p.target, out int id))
                    {
                        var go = EditorUtility.InstanceIDToObject(id) as GameObject;
                        if (go != null) found.Add(go);
                    }
                    break;
                case "by_component":
                    if (!string.IsNullOrEmpty(p.target))
                    {
                        var type = Type.GetType(p.target) ??
                            AppDomain.CurrentDomain.GetAssemblies()
                                .SelectMany(a => a.GetTypes())
                                .FirstOrDefault(t => t.Name == p.target);
                        if (type != null && typeof(Component).IsAssignableFrom(type))
                        {
                            found.AddRange(GameObject.FindObjectsOfType<GameObject>().Where(go => go.GetComponent(type) != null));
                        }
                    }
                    break;
                case "by_parent":
                    if (!string.IsNullOrEmpty(p.target))
                    {
                        var parentGo = GameObject.Find(p.target);
                        if (parentGo != null)
                        {
                            foreach (Transform child in parentGo.transform)
                                found.Add(child.gameObject);
                        }
                    }
                    break;
                case "by_all":
                    found.AddRange(GameObject.FindObjectsOfType<GameObject>());
                    break;
                default:
                    return Response.Error($"Unknown searchMethod: {p.searchMethod}");
            }

            if (found.Count == 0)
                return Response.Error($"No GameObject found by {method} for '{p.target}'");
            if (found.Count == 1)
                return Response.Success("GameObject found.", new { name = found[0].name, instanceId = found[0].GetInstanceID() });
            return Response.Success($"{found.Count} GameObjects found.", found.Select(go => new { name = go.name, instanceId = go.GetInstanceID() }).ToList());
        }

        private static object ModifyGameObject(ManageGameObjectParams p)
        {
             if (string.IsNullOrEmpty(p.target)) return Response.Error("Target is required for modify action.");
             var go = GameObject.Find(p.target);
             if (go == null) return Response.Error($"GameObject '{p.target}' not found to modify.");
             
             return ModifyGameObject(p, go);
        }
        
        private static object ModifyGameObject(ManageGameObjectParams p, GameObject go)
        {
            // Modify transform
            if (p.position != null && p.position.Length == 3) go.transform.position = new Vector3(p.position[0], p.position[1], p.position[2]);
            if (p.rotation != null && p.rotation.Length == 3) go.transform.rotation = Quaternion.Euler(p.rotation[0], p.rotation[1], p.rotation[2]);
            if (p.scale != null && p.scale.Length == 3) go.transform.localScale = new Vector3(p.scale[0], p.scale[1], p.scale[2]);

            // Modify other properties
            if (!string.IsNullOrEmpty(p.name) && go.name != p.name) go.name = p.name;
            if (!string.IsNullOrEmpty(p.tag)) go.tag = p.tag;
            if (!string.IsNullOrEmpty(p.layer)) go.layer = LayerMask.NameToLayer(p.layer);
            if (p.setActive.HasValue) go.SetActive(p.setActive.Value);

            // Add/Remove components and modify properties would go here
            // This is a complex part left for future implementation.

            // --- コンポーネント追加 ---
            if (p.componentsToAdd != null)
            {
                foreach (var componentName in p.componentsToAdd)
                {
                    var type = Type.GetType(componentName) ??
                        AppDomain.CurrentDomain.GetAssemblies()
                            .SelectMany(a => a.GetTypes())
                            .FirstOrDefault(t => t.Name == componentName);
                    if (type != null && typeof(Component).IsAssignableFrom(type))
                    {
                        if (go.GetComponent(type) == null)
                            go.AddComponent(type);
                    }
                }
            }

            // --- コンポーネント削除 ---
            if (p.componentsToRemove != null)
            {
                foreach (var componentName in p.componentsToRemove)
                {
                    var type = Type.GetType(componentName) ??
                        AppDomain.CurrentDomain.GetAssemblies()
                            .SelectMany(a => a.GetTypes())
                            .FirstOrDefault(t => t.Name == componentName);
                    if (type != null)
                    {
                        var comp = go.GetComponent(type);
                        if (comp != null)
                            UnityEngine.Object.DestroyImmediate(comp);
                    }
                }
            }

            // --- コンポーネントプロパティ設定 ---
            if (p.componentProperties != null)
            {
                foreach (var mod in p.componentProperties)
                {
                    var comp = go.GetComponent(mod.componentName) ??
                        go.GetComponents<Component>().FirstOrDefault(c => c.GetType().Name == mod.componentName);
                    if (comp == null) continue;
                    foreach (var prop in mod.properties)
                    {
                        var propertyInfo = comp.GetType().GetProperty(prop.propertyName);
                        if (propertyInfo != null && propertyInfo.CanWrite)
                        {
                            try
                            {
                                object value = Convert.ChangeType(prop.propertyValue, propertyInfo.PropertyType);
                                propertyInfo.SetValue(comp, value);
                            }
                            catch { /* 型変換失敗時は無視 */ }
                        }
                        else
                        {
                            var fieldInfo = comp.GetType().GetField(prop.propertyName);
                            if (fieldInfo != null)
                            {
                                try
                                {
                                    object value = Convert.ChangeType(prop.propertyValue, fieldInfo.FieldType);
                                    fieldInfo.SetValue(comp, value);
                                }
                                catch { }
                            }
                        }
                    }
                }
            }

            // --- 親子関係の設定 ---
            if (!string.IsNullOrEmpty(p.parent))
            {
                var parentGo = GameObject.Find(p.parent);
                if (parentGo != null)
                {
                    go.transform.SetParent(parentGo.transform);
                }
            }

            // --- プレハブ保存 ---
#if UNITY_EDITOR
            if (p.saveAsPrefab == true && !string.IsNullOrEmpty(p.prefabPath))
            {
                string path = p.prefabPath;
                if (!path.EndsWith(".prefab")) path += ".prefab";
                UnityEditor.PrefabUtility.SaveAsPrefabAsset(go, path);
            }
#endif

            return Response.Success($"GameObject '{go.name}' modified.");
        }

        private static object DeleteGameObject(ManageGameObjectParams p)
        {
            if (string.IsNullOrEmpty(p.target)) return Response.Error("Target is required for delete action.");
            var go = GameObject.Find(p.target);
            if (go == null) return Response.Error($"GameObject '{p.target}' not found to delete.");

            UnityEngine.Object.DestroyImmediate(go);
            return Response.Success($"GameObject '{p.target}' deleted.");
        }
    }
} 
