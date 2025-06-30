using System;
using System.IO;
using System.Text.RegularExpressions;
using UnityEngine;

namespace UnityMcpBridge.Editor.Helpers
{
    /// <summary>
    /// Claude Desktop設定ファイルの管理
    /// </summary>
    public static class ClaudeConfigManager
    {
        private static readonly string ConfigFileName = "claude_desktop_config.json";
        private static readonly string CustomMcpServerKey = "no-settings-unity-mcp";
        
        /// <summary>
        /// Claude Desktop設定ファイルのパスを取得
        /// </summary>
        public static string GetConfigFilePath()
        {
            string appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            return Path.Combine(appDataPath, "Claude", ConfigFileName);
        }
        
        /// <summary>
        /// 設定ファイルが存在するかチェック
        /// </summary>
        public static bool ConfigFileExists()
        {
            return File.Exists(GetConfigFilePath());
        }
        
        /// <summary>
        /// 自作Unity MCPサーバーの設定を追加/更新
        /// </summary>
        public static bool UpdateUnityMcpConfig()
        {
            try
            {
                string configPath = GetConfigFilePath();
                string projectPath = GetProjectPath();
                
                // 設定ファイルのディレクトリが存在しない場合は作成
                string configDir = Path.GetDirectoryName(configPath);
                if (!Directory.Exists(configDir))
                {
                    Directory.CreateDirectory(configDir);
                }
                
                string jsonContent;
                
                // 既存ファイルがある場合は読み込み、ない場合は空のJSON作成
                if (File.Exists(configPath))
                {
                    jsonContent = File.ReadAllText(configPath);
                    
                    // 既に自作設定が存在するかチェック
                    if (IsCustomUnityMcpConfiguredInJson(jsonContent))
                    {
                        Debug.Log($"Custom Unity MCP server '{CustomMcpServerKey}' already configured.");
                        return true; // 既に設定済み
                    }
                }
                else
                {
                    jsonContent = "{}";
                }
                
                // 自作Unity MCPサーバー設定を追加
                string updatedJson = AddCustomUnityMcpConfig(jsonContent, projectPath);
                
                // ファイルに書き込み
                File.WriteAllText(configPath, updatedJson);
                
                Debug.Log($"Added custom Unity MCP server '{CustomMcpServerKey}' to Claude config.");
                return true;
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to update Claude config: {e.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// JSONに自作Unity MCP設定を追加
        /// </summary>
        private static string AddCustomUnityMcpConfig(string originalJson, string projectPath)
        {
            // 自作Unity MCP設定
            string customMcpConfig = $@"    ""{CustomMcpServerKey}"": {{
      ""command"": ""uv"",
      ""args"": [
        ""run"",
        ""--directory"",
        ""{projectPath.Replace("\\", "\\\\")}"",
        ""server.py""
      ]
    }}";
            
            // mcpServersセクションが存在するかチェック
            if (originalJson.Contains("\"mcpServers\""))
            {
                // mcpServersセクションの終了位置を見つけて設定を追加
                return AddToExistingMcpServers(originalJson, customMcpConfig);
            }
            else
            {
                // mcpServersセクション自体を作成
                return AddMcpServersSection(originalJson, customMcpConfig);
            }
        }
        
        /// <summary>
        /// 既存のmcpServersセクションに設定を追加
        /// </summary>
        private static string AddToExistingMcpServers(string originalJson, string customMcpConfig)
        {
            // mcpServersセクションの終了括弧を見つける
            int mcpServersStart = originalJson.IndexOf("\"mcpServers\"");
            int braceStart = originalJson.IndexOf("{", mcpServersStart);
            
            // mcpServersセクション内の最後の閉じ括弧を見つける
            int braceCount = 0;
            int mcpServersEnd = -1;
            
            for (int i = braceStart; i < originalJson.Length; i++)
            {
                if (originalJson[i] == '{') braceCount++;
                else if (originalJson[i] == '}') braceCount--;
                
                if (braceCount == 0)
                {
                    mcpServersEnd = i;
                    break;
                }
            }
            
            if (mcpServersEnd == -1)
            {
                throw new Exception("Invalid JSON: mcpServers section not properly closed");
            }
            
            // 最後の設定の後にカンマが必要かチェック
            string beforeClosing = originalJson.Substring(braceStart + 1, mcpServersEnd - braceStart - 1).Trim();
            bool needsComma = beforeClosing.Length > 0 && !beforeClosing.EndsWith(",");
            
            // 設定を挿入
            string beforeMcpEnd = originalJson.Substring(0, mcpServersEnd);
            string afterMcpEnd = originalJson.Substring(mcpServersEnd);
            
            string comma = needsComma ? ",\n" : "\n";
            
            return beforeMcpEnd + comma + customMcpConfig + "\n  " + afterMcpEnd;
        }
        
        /// <summary>
        /// mcpServersセクション自体を作成
        /// </summary>
        private static string AddMcpServersSection(string originalJson, string customMcpConfig)
        {
            // 空のJSONの場合
            if (originalJson.Trim() == "{}" || originalJson.Trim() == "")
            {
                return $@"{{
  ""mcpServers"": {{
{customMcpConfig}
  }}
}}";
            }
            
            // 既存の設定がある場合は末尾に追加
            int lastBraceIndex = originalJson.LastIndexOf("}");
            if (lastBraceIndex == -1)
            {
                throw new Exception("Invalid JSON format");
            }
            
            string beforeLastBrace = originalJson.Substring(0, lastBraceIndex);
            
            // 既存設定の後にカンマが必要かチェック
            bool needsComma = beforeLastBrace.Trim().Length > 1; // "{" より長い場合
            
            string comma = needsComma ? ",\n" : "\n";
            
            return beforeLastBrace + comma + $@"  ""mcpServers"": {{
{customMcpConfig}
  }}" + "\n}";
        }
        
        /// <summary>
        /// JSON文字列内で自作Unity MCPサーバーが設定されているかチェック
        /// </summary>
        private static bool IsCustomUnityMcpConfiguredInJson(string json)
        {
            return json.Contains($"\"{CustomMcpServerKey}\"");
        }
        
        /// <summary>
        /// プロジェクトのUnityMcpServer/srcパスを取得
        /// </summary>
        private static string GetProjectPath()
        {
            // 現在のUnityプロジェクトのパスを取得
            string unityProjectPath = Application.dataPath;
            string projectRoot = Directory.GetParent(unityProjectPath).FullName;
            
            // Unity MCPプロジェクトのパスを構築
            // Unityプロジェクトと同じレベルにunity-mcpフォルダがあると仮定
            string unityMcpRoot = Path.Combine(Directory.GetParent(projectRoot).FullName, "unity-mcp");
            string serverPath = Path.Combine(unityMcpRoot, "UnityMcpServer", "src");
            
            // Debug.Log($"[ClaudeConfigManager] Detected server path: {serverPath}");
            
            // パスが存在するかチェック
            if (!Directory.Exists(serverPath))
            {
                Debug.LogWarning($"[ClaudeConfigManager] Server path does not exist: {serverPath}");
                // フォールバック: 直接指定
                serverPath = @"C:\Users\image\Documents\unity-mcp\UnityMcpServer\src";
                Debug.Log($"[ClaudeConfigManager] Using fallback path: {serverPath}");
            }
            
            return serverPath;
        }
        
        /// <summary>
        /// 自作Unity MCPサーバーが設定されているかチェック
        /// </summary>
        public static bool IsCustomUnityMcpConfigured()
        {
            if (!ConfigFileExists()) return false;
            
            try
            {
                string content = File.ReadAllText(GetConfigFilePath());
                return IsCustomUnityMcpConfiguredInJson(content);
            }
            catch
            {
                return false;
            }
        }
        
        /// <summary>
        /// 設定内容をプレビュー用に取得
        /// </summary>
        public static string GetConfigPreview()
        {
            try
            {
                string configPath = GetConfigFilePath();
                if (!File.Exists(configPath))
                {
                    return "設定ファイルが存在しません";
                }
                
                string content = File.ReadAllText(configPath);
                
                // 自作設定の部分のみ抽出してプレビュー
                if (IsCustomUnityMcpConfiguredInJson(content))
                {
                    string projectPath = GetProjectPath();
                    return $"設定済み:\nサーバー名: {CustomMcpServerKey}\nパス: {projectPath}";
                }
                else
                {
                    return "自作Unity MCP設定はまだ追加されていません";
                }
            }
            catch (Exception e)
            {
                return $"設定確認エラー: {e.Message}";
            }
        }
    }
}
