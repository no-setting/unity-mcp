# TCP通信テストガイド

このガイドは、Unity MCP Bridge（Unityエディタ側）と Unity MCP Server（Pythonサーバー側）のTCP通信が正しく動作するかを確認するための手順です。

---

## 1. Unity側の準備
1. Unity Editorで本プロジェクトを開く
2. メニューから `Window > Unity MCP Test` を選択し、テストウィンドウを開く
3. 「ブリッジを開始」ボタンをクリック
4. 状態が「接続中 (ポート 6400)」になっていることを確認

---

## 2. Pythonサーバーの起動
1. コマンドプロンプトまたはターミナルを開く
2. Python 3.12以上がインストールされていることを確認
3. 必要な依存パッケージをインストール（初回のみ）
   ```bash
   cd "C:\Users\image\Documents\unity-mcp\UnityMcpServer\src"
   pip install -r requirements.txt  # または pyproject.toml/uv で管理
   ```
4. サーバーを起動
   ```bash
   uv run server.py
   ```

---

## 3. 接続テスト
- サーバー起動時に「Unity MCP Server starting up」や「Connected to Unity on startup」等のログが表示されることを確認
- Unity MCP Testウィンドウで「接続中」と表示されていれば、TCP通信が確立しています

---

## 4. トラブルシューティング
- ポート6400が他のプロセスで使用されていないか確認
- Unity側で「ブリッジを開始」後にエラーが出る場合は、Unityのコンソールログを確認
- Python側で接続エラーが出る場合は、Unity Editorが起動しているか、ブリッジが開始されているか確認

---

## 備考
- 詳細な機能やAPIの使い方はREADME.mdを参照してください
