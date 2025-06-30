# Unity MCP - Unity Editor Integration via Model Context Protocol

[![Unity Version](https://img.shields.io/badge/Unity-2020.3%2B-blue.svg)](https://unity3d.com/get-unity/download)
[![Python Version](https://img.shields.io/badge/Python-3.12%2B-blue.svg)](https://www.python.org/downloads/)
[![License](https://img.shields.io/badge/License-MIT-green.svg)](LICENSE)

Unity MCPは、AIアシスタント（Claude Desktop、Cursor等）からUnityエディタを直接操作できるようにするMCP（Model Context Protocol）サーバー実装です。

## 🎯 特徴

- **AIアシスタントとの統合**: Claude DesktopやCursorから直接Unityエディタを操作
- **包括的なAPI**: スクリプト管理、シーン操作、GameObject制御、アセット管理など
- **自動設定**: Claude Desktop設定の自動更新機能
- **リアルタイム通信**: TCP通信による高速レスポンス
- **拡張可能**: 新しいツールの追加が容易

## 📋 動作要件

### Unity側
- Unity 2020.3 LTS以降
- Newtonsoft.Json for Unity (com.unity.nuget.newtonsoft-json: 3.0.2)

### Python側
- Python 3.12以降
- uv（Pythonパッケージマネージャー）
- Windows/macOS/Linux対応

## 🚀 クイックスタート

### 1. Package Manager経由

1. Unity Editorで Window > Package Manager を開く
2. + ボタンをクリックし、Add package from git URL... を選択
3. 以下のURLを入力：
```
https://github.com/no-setting/unity-mcp.git?path=/UnityMcpBridge
```
4. Add をクリック

### 2. Unity MCP Bridgeの起動

1. Unity Editorで Window > Unity MCP を開く
2. 「Start Bridge」ボタンをクリック
3. ステータスが「Connected」になることを確認

### 3. Unity MCP Server（Python側）のセットアップ

```bash
cd UnityMcpServer/src
# uvのインストール（未インストールの場合）
pip install uv
# 依存関係のインストール
uv pip install -r pyproject.toml
```

### 4. Claude Desktop設定（自動）

Unity MCP Testウィンドウで「Claude設定を追加」ボタンをクリックすると、自動的に設定が追加されます。

### 5. サーバーの起動

```bash
cd UnityMcpServer/src
uv run server.py
```

## 📁 プロジェクト構成

```
unity-mcp/
├── UnityMcpBridge/          # Unity側パッケージ
│   ├── Editor/
│   │   ├── Tools/          # 各種操作ツール実装
│   │   ├── Helpers/        # ヘルパークラス
│   │   ├── Models/         # データモデル
│   │   ├── Windows/        # EditorWindow実装
│   │   └── UnityMcpBridge.cs  # メインブリッジ
│   └── package.json
│
├── UnityMcpServer/          # Python側サーバー
│   └── src/
│       ├── server.py       # MCPサーバー本体
│       ├── unity_connection.py  # Unity通信管理
│       ├── config.py       # 設定管理
│       ├── tools/          # ツール実装
│       └── pyproject.toml  # Python依存関係
│
└── README.md               # このファイル
```

## 🔧 利用可能なツール

### manage_script
C#スクリプトの管理（作成、読み取り、更新、削除）

```python
# 例: MonoBehaviourスクリプトの作成
manage_script(
    action="create",
    name="PlayerController",
    script_type="MonoBehaviour",
    namespace="MyGame"
)
```

### manage_scene
シーンの管理（作成、保存、ロード、階層取得）

```python
# 例: 新しいシーンの作成
manage_scene(
    action="create",
    name="GameScene",
    path="Assets/Scenes"
)
```

### manage_editor
エディタの制御（再生/停止、状態取得、タグ/レイヤー管理）

```python
# 例: プレイモードの開始
manage_editor(action="play")
```

### manage_gameobject
GameObjectの操作（作成、変更、削除、コンポーネント管理）

```python
# 例: プレイヤーオブジェクトの作成
manage_gameobject(
    action="create",
    name="Player",
    position=[0, 1, 0],
    components_to_add=["Rigidbody", "BoxCollider"]
)
```

### manage_asset
アセットの管理（作成、削除、移動、検索）

```python
# 例: マテリアルの作成
manage_asset(
    action="create",
    path="Assets/Materials/PlayerMaterial.mat",
    asset_type="Material"
)
```

### read_console
コンソールログの読み取りとクリア

```python
# 例: エラーログの取得
read_console(
    action="get",
    types=["error"]
)
```

### execute_menu_item
Unityメニュー項目の実行

```python
# 例: プロジェクトの保存
execute_menu_item(menu_path="File/Save Project")
```

## 💡 使用例

### Claude Desktopでの使用例

```
あなた: Unityで簡単なプレイヤーキャラクターを作成してください

Claude: Unity MCPを使用してプレイヤーキャラクターを作成します。

1. まず接続を確認します
[test_unity_connection実行]

2. 新しいシーンを作成します
[manage_scene: create "GameScene"]

3. プレイヤーオブジェクトを作成します
[manage_gameobject: create "Player" with Rigidbody and CapsuleCollider]

4. PlayerControllerスクリプトを作成します
[manage_script: create "PlayerController" as MonoBehaviour]

5. スクリプトをアタッチします
[コンポーネント追加処理]

プレイヤーキャラクターの基本セットアップが完了しました！
```

## 🐛 トラブルシューティング

### Unity側の問題

**Q: ブリッジが開始できない**
- A: ポート6400が他のプロセスで使用されていないか確認してください
- A: Unityコンソールでエラーログを確認してください

**Q: コマンドが実行されない**
- A: プレイモード中は一部の操作が制限されます
- A: コンパイルエラーがないか確認してください

### Python側の問題

**Q: サーバーが起動しない**
- A: Python 3.12以上がインストールされているか確認してください
- A: 依存関係が正しくインストールされているか確認してください
- A: Unity側のブリッジが起動しているか確認してください

### Claude Desktop設定の問題

**Q: MCPサーバーが認識されない**
- A: Claude Desktopを再起動してください
- A: 設定ファイルのパスが正しいか確認してください
- A: `%APPDATA%\Claude\claude_desktop_config.json`を確認してください

## 🛠️ 開発者向け情報

### 新しいツールの追加

1. Python側: `UnityMcpServer/src/tools/`に新しいツールファイルを作成
2. Unity側: `UnityMcpBridge/Editor/Tools/`に対応するハンドラーを実装
3. `Models/Command.cs`にパラメータクラスを追加
4. `UnityMcpBridge.cs`のExecuteCommandメソッドにルーティングを追加

### テスト

詳細なテスト手順は[自作UnityMcpのテスト計画書](docs/test-plan.md)を参照してください。

### デバッグ

- Unity側: コンソールログでデバッグ情報を確認
- Python側: `config.py`でログレベルを`DEBUG`に設定
- TCP通信: Wireshark等でポート6400の通信を監視

## 🤝 貢献

1. このリポジトリをフォーク
2. 機能ブランチを作成 (`git checkout -b feature/amazing-feature`)
3. 変更をコミット (`git commit -m 'Add amazing feature'`)
4. ブランチにプッシュ (`git push origin feature/amazing-feature`)
5. プルリクエストを作成

## 📄 ライセンス

このプロジェクトはMITライセンスの下で公開されています。詳細は[LICENSE](LICENSE)ファイルを参照してください。

## 🙏 謝辞

- オリジナル実装: [justinpbarnett/unity-mcp](https://github.com/justinpbarnett/unity-mcp)
- [Anthropic MCP](https://github.com/anthropics/mcp) - Model Context Protocol仕様
---

**注意**: このプロジェクトは開発中です。APIや機能は変更される可能性があります。
