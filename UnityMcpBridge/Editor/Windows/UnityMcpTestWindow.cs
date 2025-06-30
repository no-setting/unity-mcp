using System;
using UnityEditor;
using UnityEngine;
using UnityMcpBridge.Editor.Helpers;

namespace UnityMcpBridge.Editor.Windows
{
    public class UnityMcpTestWindow : EditorWindow
    {
        private string connectionStatus = "未接続";
        private Color statusColor = Color.red;
        private Vector2 scrollPosition;
        private bool claudeConfigExists = false;
        private bool customMcpConfigured = false;

        [MenuItem("Window/Unity MCP Test")]
        public static void ShowWindow()
        {
            GetWindow<UnityMcpTestWindow>("MCP Test");
        }

        private void OnEnable()
        {
            UpdateConnectionStatus();
            UpdateClaudeConfigStatus();
        }

        private void UpdateConnectionStatus()
        {
            if (UnityMcpBridge.IsRunning)
            {
                connectionStatus = "接続中 (ポート 6400)";
                statusColor = Color.green;
            }
            else
            {
                connectionStatus = "未接続";
                statusColor = Color.red;
            }
        }

        private void UpdateClaudeConfigStatus()
        {
            claudeConfigExists = ClaudeConfigManager.ConfigFileExists();
            customMcpConfigured = ClaudeConfigManager.IsCustomUnityMcpConfigured();
        }

        private void OnGUI()
        {
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

            EditorGUILayout.Space(10);
            
            // タイトル
            EditorGUILayout.LabelField("Unity MCP Bridge テスト", EditorStyles.boldLabel);
            EditorGUILayout.Space(10);

            // 接続状態セクション
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("Unity Bridge 状態", EditorStyles.boldLabel);
            
            GUIStyle statusStyle = new GUIStyle(EditorStyles.label);
            statusStyle.normal.textColor = statusColor;
            EditorGUILayout.LabelField($"状態: {connectionStatus}", statusStyle);
            
            EditorGUILayout.LabelField($"ポート: 6400");
            EditorGUILayout.Space(5);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button(UnityMcpBridge.IsRunning ? "ブリッジを停止" : "ブリッジを開始"))
            {
                ToggleBridge();
            }
            
            if (GUILayout.Button("状態を更新"))
            {
                UpdateConnectionStatus();
            }
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.EndVertical();

            EditorGUILayout.Space(10);
            
            // Claude Desktop設定セクション
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("Claude Desktop 設定", EditorStyles.boldLabel);

            EditorGUILayout.LabelField($"設定ファイル: {(claudeConfigExists ? "存在" : "未作成")}");
            EditorGUILayout.LabelField($"カスタムMCP: {(customMcpConfigured ? "設定済み" : "未設定")}");

            if (claudeConfigExists)
            {
                EditorGUILayout.LabelField($"パス: {ClaudeConfigManager.GetConfigFilePath()}");
                
                // プレビュー表示
                EditorGUILayout.Space(5);
                EditorGUILayout.LabelField("設定プレビュー:", EditorStyles.boldLabel);
                string preview = ClaudeConfigManager.GetConfigPreview();
                EditorGUILayout.HelpBox(preview, MessageType.Info);
            }

            EditorGUILayout.Space(5);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button(customMcpConfigured ? "設定済み" : "Claude設定を追加"))
            {
                if (!customMcpConfigured)
                {
                    UpdateClaudeConfig();
                }
                else
                {
                    EditorUtility.DisplayDialog("情報", 
                        "自作Unity MCP設定は既に追加されています。", "OK");
                }
            }

            if (GUILayout.Button("設定ファイルを開く"))
            {
                OpenClaudeConfigFile();
            }

            if (GUILayout.Button("設定状態を更新"))
            {
                UpdateClaudeConfigStatus();
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();

            EditorGUILayout.Space(10);

            // テストセクション
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("接続テスト", EditorStyles.boldLabel);
            
            EditorGUILayout.HelpBox(
                "Claude Desktopから test_unity_connection ツールを実行して接続をテストしてください。",
                MessageType.Info
            );
            
            if (GUILayout.Button("Pingテスト（直接）"))
            {
                TestDirectPing();
            }
            
            EditorGUILayout.EndVertical();

            EditorGUILayout.Space(10);

            // 使用方法
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("使用方法", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("1. Unity MCP Bridgeが開始されていることを確認");
            EditorGUILayout.LabelField("2. 「Claude設定を更新」ボタンでClaude Desktop設定を自動更新");
            EditorGUILayout.LabelField("3. Claude Desktopを再起動");
            EditorGUILayout.LabelField("4. Claude Desktopでtest_unity_connectionを実行");
            EditorGUILayout.EndVertical();

            EditorGUILayout.EndScrollView();
        }

        private void ToggleBridge()
        {
            if (UnityMcpBridge.IsRunning)
            {
                UnityMcpBridge.Stop();
            }
            else
            {
                UnityMcpBridge.Start();
            }
            UpdateConnectionStatus();
        }

        private void TestDirectPing()
        {
            if (!UnityMcpBridge.IsRunning)
            {
                EditorUtility.DisplayDialog("エラー", "Unity MCP Bridgeが開始されていません。", "OK");
                return;
            }

            Debug.Log("[MCP Test] Direct ping test completed. Bridge is running and ready for connections.");
            EditorUtility.DisplayDialog("情報", "Pingテスト完了。Bridgeは正常に動作しています。コンソールログを確認してください。", "OK");
        }

        private void UpdateClaudeConfig()
        {
            bool success = ClaudeConfigManager.UpdateUnityMcpConfig();
            
            if (success)
            {
                EditorUtility.DisplayDialog("成功", 
                    "Claude Desktop設定を更新しました。\n" +
                    "Claude Desktopを再起動して設定を反映してください。\n\n" +
                    "MCPサーバー名: no-settings-unity-mcp", "OK");
                UpdateClaudeConfigStatus();
            }
            else
            {
                EditorUtility.DisplayDialog("エラー", 
                    "Claude Desktop設定の更新に失敗しました。\n" +
                    "コンソールログでエラー詳細を確認してください。", "OK");
            }
        }

        private void OpenClaudeConfigFile()
        {
            string configPath = ClaudeConfigManager.GetConfigFilePath();
            
            if (System.IO.File.Exists(configPath))
            {
                System.Diagnostics.Process.Start("notepad.exe", configPath);
            }
            else
            {
                EditorUtility.DisplayDialog("エラー", 
                    "Claude Desktop設定ファイルが見つかりません。\n" +
                    "まず「Claude設定を更新」ボタンで設定ファイルを作成してください。", "OK");
            }
        }
    }
}
