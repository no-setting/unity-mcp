# UnityMcpServer 実装者向けドキュメント

## 概要

UnityMcpServerは、Model Context Protocol（MCP）を使用してAIアシスタント（Claude、Cursor等）とUnity Editorを接続するPythonサーバー実装です。FastMCPフレームワークを使用し、Unity Editor内のMCP Bridgeと通信して様々な操作を実行します。

## アーキテクチャ

### 通信フロー
```
[AI Assistant] <--> [MCP Client] <--(stdio)--> [Unity MCP Server] <--(TCP:6400)--> [Unity MCP Bridge]
                                               (このサーバー)                        (Unity Editor内)
```

### 主要コンポーネント

1. **MCPサーバー層**: FastMCPを使用したツール登録とリクエスト処理
2. **Unity接続層**: TCP通信によるUnity Bridgeとの連携
3. **ツール実装層**: 各種Unity操作のビジネスロジック

## ディレクトリ構成

```
UnityMcpServer/
└── src/
    ├── __init__.py
    ├── server.py              # MCPサーバーメイン
    ├── unity_connection.py    # Unity通信管理
    ├── config.py             # 設定管理
    ├── pyproject.toml        # プロジェクト設定
    ├── uv.lock              # 依存関係ロック
    ├── .python-version      # Python 3.12
    └── tools/               # ツール実装
        ├── __init__.py
        ├── manage_script.py
        ├── manage_scene.py
        ├── manage_editor.py
        ├── manage_gameobject.py
        ├── manage_asset.py
        ├── read_console.py
        └── execute_menu_item.py
```

## 環境設定

### 必要要件
- Python 3.12以上
- uv（Pythonパッケージマネージャー）
- Unity Editor側でMCP Bridgeが起動していること

### インストール方法

```bash
cd UnityMcpServer/src

# uvを使用する場合（推奨）
pip install uv
uv pip install -r pyproject.toml

# または通常のpipを使用
pip install -e .
```

### サーバー起動

```bash
# 開発時（直接実行）
uv run server.py

# MCPクライアント経由（Claude Desktop等）
# claude_desktop_config.jsonに設定済みの場合、自動起動
```

## 実装詳細

### 1. server.py - MCPサーバー本体

```python
# FastMCPの初期化とライフサイクル管理
mcp = FastMCP(
    "unity-mcp-server",
    description="Unity Editor integration via Model Context Protocol",
    lifespan=server_lifespan
)

# サーバーライフサイクル
@asynccontextmanager
async def server_lifespan(server: FastMCP) -> AsyncIterator[Dict[str, Any]]:
    """サーバー起動時にUnity接続を確立"""
    global _unity_connection
    _unity_connection = get_unity_connection()
    yield {"bridge": _unity_connection}
    # シャットダウン時のクリーンアップ
```

**主な機能:**
- Unity接続の初期化とクリーンアップ
- ツールの登録管理
- プロンプトの提供
- stdio通信によるMCPプロトコル実装

### 2. unity_connection.py - Unity通信管理

```python
class UnityConnection:
    def __init__(self):
        self.config = config
        self.sock = None
        
    def connect(self) -> bool:
        """Unity Bridgeへの接続確立"""
        
    def send_command(self, command_type: str, params: Dict[str, Any]) -> Dict[str, Any]:
        """コマンド送信とレスポンス受信"""
        
    def receive_full_response(self, sock: socket.socket) -> bytes:
        """大きなレスポンスの完全受信"""
```

**通信仕様:**
- TCP Socket（ポート6400）
- JSON形式のメッセージ
- 16MBまでのバッファサイズ対応
- 自動再接続機能

### 3. config.py - 設定管理

```python
@dataclass
class ServerConfig:
    unity_host: str = "localhost"
    unity_port: int = 6400
    mcp_port: int = 6500
    connection_timeout: float = 86400.0  # 24時間
    buffer_size: int = 16 * 1024 * 1024  # 16MB
    log_level: str = "INFO"
    log_format: str = "%(asctime)s - %(name)s - %(levelname)s - %(message)s"
```

### 4. tools/ - ツール実装

各ツールはFastMCPの`@mcp.tool()`デコレータで登録：

#### ツール実装パターン

```python
def register_tool_name_tools(mcp: FastMCP):
    @mcp.tool()
    def tool_name(
        ctx: Context,
        required_param: str,
        optional_param: Optional[str] = None
    ) -> Dict[str, Any]:
        """ツールの説明（MCPクライアントに表示される）"""
        
        # Unity接続の取得
        bridge = get_unity_connection()
        
        # パラメータの準備（snake_case → camelCase変換）
        params_dict = {
            "requiredParam": required_param,
            "optionalParam": optional_param
        }
        
        # None値の除去
        params_dict = {k: v for k, v in params_dict.items() if v is not None}
        
        # コマンド送信
        return bridge.send_command("tool_name", params_dict)
```

#### 利用可能なツール

1. **manage_script** - C#スクリプト管理
   - 作成、読み取り、更新、削除
   - 名前空間、スクリプトタイプ指定

2. **manage_scene** - シーン管理
   - 作成、保存、ロード
   - シーン階層の取得
   - ビルド設定管理

3. **manage_editor** - エディタ制御
   - Play/Pause/Stop
   - エディタ状態取得
   - タグ・レイヤー管理

4. **manage_gameobject** - GameObject操作
   - 作成、変更、削除、検索
   - コンポーネント管理
   - Transform操作

5. **manage_asset** - アセット管理
   - インポート、作成、削除
   - 検索、移動、複製
   - プロパティ設定

6. **read_console** - コンソール操作
   - ログ取得（エラー、警告、通常）
   - フィルタリング
   - コンソールクリア

7. **execute_menu_item** - メニュー実行
   - 任意のメニュー項目の実行

## 開発ガイド

### 新しいツールの追加

1. **ツールファイルの作成**
```python
# tools/my_new_tool.py
from typing import Dict, Any, Optional
from mcp.server.fastmcp import FastMCP, Context
from unity_connection import get_unity_connection

def register_my_new_tool_tools(mcp: FastMCP):
    @mcp.tool()
    def my_new_tool(
        ctx: Context,
        action: str,
        target: str
    ) -> Dict[str, Any]:
        """新しいツールの説明"""
        bridge = get_unity_connection()
        
        params_dict = {
            "action": action.lower(),
            "target": target
        }
        
        return bridge.send_command("my_new_tool", params_dict)
```

2. **tools/__init__.pyへの登録**
```python
from .my_new_tool import register_my_new_tool_tools

def register_all_tools(mcp: FastMCP):
    # ... 既存のツール
    register_my_new_tool_tools(mcp)
```

3. **Unity側の対応実装**
- `UnityMcpBridge/Editor/Tools/MyNewTool.cs`を作成
- `UnityMcpBridge.cs`にルーティング追加

### パラメータ命名規則

Python（snake_case） → C#（camelCase）の変換：
- `script_type` → `scriptType`
- `filter_text` → `filterText`
- `include_stacktrace` → `includeStacktrace`

### エラーハンドリング

```python
try:
    result = bridge.send_command("command", params)
    return {
        "success": True,
        "data": result.get("data"),
        "message": result.get("message")
    }
except Exception as e:
    logger.error(f"Error in tool: {str(e)}")
    return {
        "success": False,
        "error": str(e)
    }
```

### デバッグ

1. **ログレベルの設定**
```python
# config.pyでDEBUGに変更
log_level: str = "DEBUG"
```

2. **通信ログの確認**
```python
logger.debug(f"Sending command: {command_type}")
logger.debug(f"Parameters: {params_dict}")
logger.debug(f"Response: {response}")
```

3. **Unity側との連携確認**
- Unity Consoleでエラーログ確認
- TCP通信の監視（Wireshark等）

## トラブルシューティング

### よくある問題

1. **Unity接続エラー**
   - Unity EditorでMCP Bridgeが起動しているか確認
   - ポート6400が他のプロセスで使用されていないか確認
   - ファイアウォール設定を確認

2. **コマンド実行エラー**
   - パラメータ名のマッピングを確認
   - Unity側のプレイモード状態を確認
   - JSONシリアライゼーションエラーをチェック

3. **大きなデータの処理**
   - バッファサイズ（16MB）の制限を確認
   - 必要に応じてconfig.pyで調整

### パフォーマンス最適化

1. **接続の再利用**
   - グローバル接続インスタンスを使用
   - 不要な再接続を避ける

2. **バッチ処理**
   - 複数の操作をまとめて実行
   - 頻繁な小さいリクエストを避ける

3. **非同期処理**
   - 現在は同期的だが、将来的に非同期化予定
   - 長時間実行タスクの考慮

## セキュリティ考慮事項

1. **ローカル接続のみ**
   - デフォルトでlocalhost接続
   - 外部接続は無効

2. **入力検証**
   - パラメータの型チェック
   - パス操作の検証

3. **危険な操作の制限**
   - ファイルシステムへの直接アクセス制限
   - 実行可能なメニュー項目の制限

---

詳細な実装については各ソースコードのドキュメントストリングとコメントを参照してください。
質問や提案がある場合は、GitHubのIssuesまたはDiscussionsでお知らせください。
