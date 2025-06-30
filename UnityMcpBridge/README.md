# UnityMcpBridge 実装者向けドキュメント

## 概要

UnityMcpBridgeは、Unity Editor内でTCPサーバーとして動作し、外部のMCPサーバー（Python実装）からのコマンドを受信・実行するC#パッケージです。Model Context Protocol（MCP）を通じて、AIアシスタントがUnityエディタを操作できるようにします。

## アーキテクチャ

### 通信フロー
```
[AI Assistant] → [MCP Client] → [Unity MCP Server (Python)] → [TCP:6400] → [UnityMcpBridge (Unity)]
```

### 主要コンポーネント

#### 1. UnityMcpBridge.cs（コアエンジン）
- **TCPサーバー管理**: ポート6400でTCPListenerを起動
- **非同期コマンド処理**: TaskCompletionSourceを使用した非同期パターン
- **コマンドルーティング**: 受信したコマンドを適切なハンドラーへ振り分け
- **メインスレッド実行**: EditorApplication.updateによるUnityメインスレッドでの処理

```csharp
// コマンド処理の基本フロー
private static string ExecuteCommand(Command command)
{
    string parametersJson = command.@params?.ToString() ?? "{}";
    
    switch (command.type)
    {
        case "manage_script":
            var scriptParams = JsonConvert.DeserializeObject<ManageScriptParams>(parametersJson);
            return ManageScript.Handle(scriptParams);
        // ... 他のコマンドタイプ
    }
}
```

#### 2. Models/（データモデル）

**Command.cs**
```csharp
public class Command
{
    public string type { get; set; }
    public JObject @params { get; set; }  // Newtonsoft.Json.Linq使用
}
```

**各種パラメータクラス**
- `ManageScriptParams`: スクリプト操作用
- `ManageSceneParams`: シーン操作用
- `ManageGameObjectParams`: GameObject操作用
- その他、各ツールに対応したパラメータクラス

#### 3. Tools/（ツールハンドラー）

各ツールは静的クラスとして実装され、`Handle`メソッドでコマンドを処理：

- **ManageScript.cs**: C#スクリプトのCRUD操作
- **ManageScene.cs**: シーンの作成、保存、ロード、階層取得
- **ManageEditor.cs**: エディタ状態制御（Play/Pause/Stop）、タグ・レイヤー管理
- **ManageGameObject.cs**: GameObject/コンポーネントの操作
- **ManageAsset.cs**: アセットのインポート、作成、削除、検索
- **ExecuteMenuItem.cs**: メニュー項目の実行
- **ReadConsole.cs**: コンソールログの取得・クリア

#### 4. Helpers/（ユーティリティ）

- **ConfigHelper.cs**: Claude Desktop設定の自動更新
- **Response.cs**: 統一されたレスポンス形式の生成

```csharp
public static class Response
{
    public static string Success(object data = null, string message = null)
    {
        return JsonConvert.SerializeObject(new {
            status = "success",
            data = data,
            message = message
        });
    }
}
```

## 実装詳細

### JSON処理

```csharp
// パッケージ依存関係（package.json）
"dependencies": {
    "com.unity.nuget.newtonsoft-json": "3.0.2"
}
```

### 非同期処理パターン

```csharp
// コマンドキューの定義
private static readonly Dictionary<string, (string commandJson, TaskCompletionSource<string> tcs)> commandQueue = new();

// メインスレッドでの処理
private static void ProcessCommands()
{
    lock (lockObj)
    {
        foreach (var kvp in commandQueue.ToList())
        {
            // コマンド処理
            string responseJson = ExecuteCommand(command);
            tcs.SetResult(responseJson);
        }
    }
}
```

### エラーハンドリング

すべてのツールで一貫したエラーハンドリング：

```csharp
try
{
    // 処理実行
    return Response.Success(result);
}
catch (Exception e)
{
    Debug.LogError($"[ToolName] Error: {e.Message}");
    return Response.Error($"Error: {e.Message}");
}
```

### デバッグログ

開発時のデバッグを容易にするため、詳細なログ出力：

```csharp
Debug.Log($"[ExecuteCommand] Processing {command.type}");
Debug.Log($"[ExecuteCommand] Parameters: {parametersJson}");
Debug.Log($"[ExecuteCommand] Response: {response}");
```

## 新機能の追加方法

### 1. 新しいツールの実装

1. **パラメータクラスの作成** (`Models/`ディレクトリ)
```csharp
[Serializable]
public class MyToolParams
{
    public string action;
    public string targetPath;
    // 他のパラメータ
}
```

2. **ツールハンドラーの作成** (`Tools/`ディレクトリ)
```csharp
public static class MyTool
{
    public static string Handle(MyToolParams parameters)
    {
        try
        {
            switch (parameters.action.ToLower())
            {
                case "create":
                    return CreateSomething(parameters);
                default:
                    return Response.Error($"Unknown action: {parameters.action}");
            }
        }
        catch (Exception e)
        {
            return Response.Error($"Error: {e.Message}");
        }
    }
}
```

3. **UnityMcpBridge.csへの登録**
```csharp
case "my_tool":
    var myToolParams = JsonConvert.DeserializeObject<MyToolParams>(parametersJson);
    return MyTool.Handle(myToolParams);
```

### 2. Python側の対応

`UnityMcpServer/src/tools/`に対応するツールを実装：

```python
def register_my_tool(mcp: FastMCP):
    @mcp.tool()
    async def my_tool(ctx: Context, action: str, target_path: str):
        params_dict = {
            "action": action,
            "targetPath": target_path
        }
        return get_unity_connection().send_command("my_tool", params_dict)
```

## トラブルシューティング

### よくある問題

1. **ポート6400が使用中**
   - 他のプロセスがポートを使用していないか確認
   - Unity Editorを再起動

2. **JSON解析エラー**
   - Newtonsoft.Jsonが正しくインストールされているか確認
   - パラメータ名のマッピングを確認（Python: snake_case → C#: camelCase）

3. **メインスレッドエラー**
   - Unity APIの呼び出しはメインスレッドで実行する必要あり
   - `EditorApplication.delayCall`を使用

### デバッグ方法

1. **Unityコンソールログ**
   - 各処理ステップでDebug.Logを出力
   - エラーはDebug.LogErrorで詳細を記録

2. **TCP通信の監視**
   - Wireshark等でポート6400の通信を確認
   - 送受信されるJSONの内容を検証

3. **ブレークポイント**
   - Visual StudioでUnityにアタッチ
   - コマンド処理の各ステップをステップ実行

## パフォーマンス考慮事項

1. **非同期処理**
   - TCPリスナーは別スレッドで動作
   - Unity APIへのアクセスはメインスレッドで実行

2. **メモリ管理**
   - 大量のアセット操作時はメモリ使用量に注意
   - 不要なオブジェクトは適切に破棄

3. **レスポンスタイム**
   - 重い処理は分割実行を検討
   - プログレスバーの表示を考慮

## セキュリティ考慮事項

1. **ローカル接続のみ**
   - デフォルトでlocalhost（127.0.0.1）のみ接続可能
   - リモート接続は無効化

2. **コマンド検証**
   - 危険な操作（ファイル削除等）は確認ダイアログを表示
   - パス操作は相対パスに制限

3. **ログ出力**
   - センシティブな情報はログに出力しない
   - 本番環境ではデバッグログを無効化

---

詳細な実装については、各ソースコードのコメントを参照してください。
質問や提案がある場合は、GitHubのIssuesまたはDiscussionsでお知らせください。
