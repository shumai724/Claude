# TowerDefenceGame

C# / WinForms で作ったタワーディフェンスゲーム。

- 7 種類のタワー（アーチャー・キャノン・フロスト・ブレイカー・ライトニング・金鉱・司令塔）と、項目ごとの強化・Lv5 の追加効果
- 4 ステージ × 6 段階の難易度レベル
- BGM・効果音（音声ファイルを使わず、起動時に合成）
- クリア結果を X で共有

## 動かし方

1. Visual Studio で `TowerDefenceGame.slnx` を開く（.NET 8 の Windows デスクトップ開発環境が必要）
2. スタートアップ プロジェクトを選んで実行する

| スタートアップ プロジェクト | 起動するもの |
| --- | --- |
| `TowerDefenceGame` | 製品版（遊ぶ人向け） |
| `TowerDefenceGame.Developer` | 開発者版（製品版に開発者ウィンドウを足したもの） |

## フォルダ構成

```
TowerDefenceGame/
├─ TowerDefenceGame.slnx
├─ TowerDefenceGame/             製品版。開発者向けのコードは含まない
│  ├─ Models/                     ゲームのルール（タワー・敵・ウェーブ・記録など）
│  ├─ Views/                      描画・画面・音
│  ├─ Controllers/                画面とルールのつなぎ
│  ├─ Assets/                     ステージ・タワー・敵・難易度の定義（JSON）
│  └─ ClassFigure.pu              クラス図（PlantUML）
└─ TowerDefenceGame.Developer/   開発者版。製品版を参照して機能を足す
   ├─ Bot/                        自動プレイ bot
   └─ ClassFigure.Developer.pu    クラス図（PlantUML）
```

## 開発者版でできること

起動すると開発者ウィンドウが開く（F12 で開閉）。

- 敵の数の倍率（0.1〜10 倍）
- 全レベル解放
- タワーごとのダメージ・撃破数・DPS の一覧と、マップ上のダメージ表示
- bot による自動プレイ（判断の理由をログに表示）

開発者版のクリア記録は製品版とは別のファイルに保存されるので、テストで遊んでも製品版の記録は変わらない。
