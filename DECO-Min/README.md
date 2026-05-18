# ボンボンドロップシール ─ セットアップガイド
Unity **6000.3.11f1** 対応

---

## ディレクトリ構成

```
Assets/
├── Scripts/
│   ├── Core/
│   │   ├── GameManager.cs          # ゲームフェーズ管理（Singleton）
│   │   ├── UIManager.cs            # UI パネル切替
│   │   └── SceneBootstrapper.cs    # 実行時に必要オブジェクトを自動生成
│   ├── Player/
│   │   ├── PlayerController.cs     # 移動・インタラクション
│   │   └── StickerThrower.cs       # エイム・投げる
│   ├── Sticker/
│   │   ├── StickerData.cs          # ScriptableObject（シール定義）
│   │   ├── StickerBook.cs          # シール帳・在庫管理（Singleton）
│   │   ├── StickerProjectile.cs    # 投げられたシールの飛行物理
│   │   └── StickerizedObject.cs    # シール化オブジェクトの剥がし処理
│   ├── Field/
│   │   └── FieldObject.cs          # シールを貼れるフィールドオブジェクト
│   ├── Camera/
│   │   └── CameraController.cs     # サードパーソンカメラ
│   ├── UI/
│   │   ├── StickerBookUI.cs        # シール帳フェーズの UI
│   │   ├── StickerSlotUI.cs        # シール帳グリッドの各スロット
│   │   ├── ExplorationHUD.cs       # 探索中 HUD
│   │   └── FieldObjectProgressBar.cs # FieldObject 上のプログレスバー
│   └── Editor/                     # ← ビルドに含まれないエディタ専用
│       ├── FieldObjectEditor.cs    # SceneView に進捗表示
│       └── MinimalSceneSetup.cs    # テストシーン一発生成ツール
├── ScriptableObjects/
│   └── Stickers/                   # 作成した StickerData を置く
└── Prefabs/
    ├── Projectiles/                # StickerProjectile プレハブを置く
    └── FieldObjects/               # FieldObject プレハブを置く
```

---

## STEP 0: 必須パッケージの確認

| パッケージ | 確認方法 |
|---|---|
| TextMeshPro | Window → Package Manager → "TextMeshPro" がインストール済みか確認 |
| Universal RP | プロジェクト作成時に URP テンプレートを選んでいれば OK |

> TextMeshPro を初めて使う場合は `Window → TextMeshPro → Import TMP Essential Resources` を実行してください。

---

## STEP 1: スクリプトをインポート

1. Unity プロジェクトの `Assets/` にスクリプトをフォルダごとコピーする。
2. **Editor** フォルダは `Assets/Scripts/Editor/` に配置すること（これ以外の場所だとビルドエラーになります）。
3. コンパイルエラーが 0 件になるまで待つ。

---

## STEP 2: シールデータ（ScriptableObject）を作成

```
Project ウィンドウ右クリック
→ Create → BonbonDrop → シールデータ
```

作成した StickerData に以下を設定します：

| フィールド | 説明 |
|---|---|
| Sticker Name | シール名（例：ハートシール） |
| Icon | UI に表示する Sprite |
| Projectile Prefab | 投げたときに飛ぶプレハブ（後述） |
| Throw Force | 投げる速さ（12 前後推奨） |
| Arc Height | 放物線の高さ（1.5 前後推奨） |
| Sticker Power | 1 枚でシール化カウントに加算する値 |
| Rarity | コモン / アンコモン / レア / スペシャル |

---

## STEP 3: シールプロジェクタイルのプレハブを作成

1. `GameObject → 3D Object → Sphere`（または好きなメッシュ）
2. Scale を `(0.2, 0.2, 0.2)` 程度に小さくする
3. `Rigidbody` コンポーネントを追加（isKinematic = false）
4. `Sphere Collider`（または Mesh Collider）が付いていることを確認
5. **`StickerProjectile`** コンポーネントを追加
6. `Prefabs/Projectiles/` フォルダに Prefab として保存
7. StickerData の `Projectile Prefab` にドラッグ＆ドロップ

---

## STEP 4: テストシーンを自動生成（おすすめ）

Unity メニューバーから：

```
BonbonDrop → Setup Minimal Test Scene
```

このツールが以下を自動生成します：
- 地面（Plane）
- プレイヤー（Capsule + PlayerController + StickerThrower）
- カメラ（CameraController）
- GameManager / StickerBook / UIManager
- FieldObject × 3（シール化閾値 3 / 5 / 8）
- 最低限の Canvas / UI

> **注意**: StickerData アセットは自動生成できません。STEP 2 で作成したものを StickerBook の `Starting Stickers` に手動で設定してください。

---

## STEP 4a: 手動でシーン構築する場合

### 必須 GameObject の階層

```
Scene
├── [Managers]              ← 空の GameObject
│   ├── GameManager         ← GameManager.cs
│   ├── StickerBook         ← StickerBook.cs
│   └── UIManager           ← UIManager.cs
├── Player                  ← CharacterController + PlayerController + StickerThrower
│   └── ThrowOrigin         ← 空 GameObject（手の位置。StickerThrower の ThrowOrigin に設定）
├── Main Camera             ← Camera + CameraController
├── Canvas                  ← Canvas (Screen Space Overlay)
│   ├── StickerBookPanel    ← StickerBookUI.cs
│   │   ├── StickerGrid     ← Grid Layout Group
│   │   └── StartButton     ← Button（OnClick → UIManager.OnStartExplorationClicked）
│   ├── ExplorationHUD      ← ExplorationHUD.cs
│   │   ├── TimerText       ← TextMeshProUGUI
│   │   └── ReturnButton    ← Button（OnClick → UIManager.OnEndExplorationClicked）
│   └── ReturnPanel         ← 帰還中テキスト等
├── EventSystem             ← EventSystem + StandaloneInputModule
├── Ground                  ← Plane（Layer: Default）
└── FieldObjects
    ├── Object_A            ← Cube + BoxCollider + FieldObject.cs
    ├── Object_B            ← Cube + BoxCollider + FieldObject.cs
    └── Object_C            ← Cube + BoxCollider + FieldObject.cs
```

### Inspector の主な接続

#### GameManager
| フィールド | 設定値 |
|---|---|
| Exploration Time Limit Seconds | 300 |
| Use Time Limit | ✓ |

#### StickerBook
| フィールド | 設定値 |
|---|---|
| Starting Stickers | 作成した StickerData アセットをドラッグ |
| Starting Count | 5（デバッグ用） |

#### PlayerController
| フィールド | 設定値 |
|---|---|
| Ground Mask | Default レイヤーを含む LayerMask |

#### StickerThrower
| フィールド | 設定値 |
|---|---|
| Throw Origin | Player の ThrowOrigin 子 GameObject |
| Aim Target Mask | Default レイヤー（FieldObject が置かれるレイヤー） |

#### UIManager
| フィールド | 設定値 |
|---|---|
| Sticker Book Panel | Canvas 内の StickerBookPanel |
| Exploration Panel | Canvas 内の ExplorationHUD |
| Return Panel | Canvas 内の ReturnPanel |
| Sticker Book UI | StickerBookPanel の StickerBookUI コンポーネント |

#### CameraController
| フィールド | 設定値 |
|---|---|
| Target | （空白でも可、実行時に PlayerController を自動検索） |
| Collision Mask | Default |

#### FieldObject（各オブジェクトに設定）
| フィールド | 設定値 |
|---|---|
| Object Display Name | オブジェクトの名前（例：古い壺） |
| Sticker Threshold | シール化に必要な枚数（例：5） |
| Reward Sticker Data | 剥がしたときに入手できる StickerData |
| Reward Count | 獲得枚数（例：2） |

---

## STEP 5: レイヤー設定

`Edit → Project Settings → Tags and Layers` で確認：

- **Default** レイヤーに FieldObject・Ground・Player を配置（初期設定のまま OK）
- StickerThrower の `Aim Target Mask` に **Default** を含める

---

## 操作方法まとめ

| 入力 | 動作 |
|---|---|
| WASD | 移動 |
| Shift + WASD | ダッシュ |
| Space | ジャンプ |
| 左クリック | シールを投げる |
| 右クリック（長押し） | エイムモード（着弾点プレビュー） |
| マウスホイール | シール切替（前後） |
| 数字キー 1〜9 | シールをインデックス指定で選択 |
| E | 近くのシール化オブジェクトを剥がす |
| 中クリック＋ドラッグ | カメラ回転 |
| Q / E キー | カメラ水平回転 |
| 中クリック＋ホイール | カメラズーム |

---

## ゲームループ

```
[シール帳フェーズ]
  └─ StickerBook の在庫を確認
  └─ 「探索開始」ボタン → GameManager.StartExploration()

[探索フェーズ]
  └─ フィールドを歩き回る
  └─ シールを FieldObject に投げる（左クリック）
  └─ シール化したオブジェクトに近づいて E キーで剥がす → シール獲得
  └─ タイマー切れ or 帰還ボタン → GameManager.EndExploration()

[帰還フェーズ]
  └─ 2.5 秒後に自動でシール帳フェーズへ戻る
  └─ Day カウントが +1 される

 → シール帳フェーズに戻り、新しいシールを確認してから次の探索へ
```

---

## よくあるトラブル

| 症状 | 原因と対処 |
|---|---|
| プレイヤーが動かない | GameManager が存在するか確認。探索フェーズ開始前は動かないのが仕様 |
| シールが飛ばない | StickerData の `Projectile Prefab` が未設定 or StickerBook に在庫がない |
| シールがオブジェクトを通り抜ける | Rigidbody の Collision Detection を `Continuous` に変更 |
| TMPro が見つからないエラー | `Window → TextMeshPro → Import TMP Essential Resources` を実行 |
| UIManager.cs でエラー | ExplorationHUD / StickerBookUI を UIManager の Inspector に設定 |

---

## 今後の拡張ポイント

- **シール帳デコレーション UI**: StickerBookUI にドラッグ＆ドロップで配置機能を追加
- **シールのエフェクト**: StickerProjectile に Trail Renderer、着弾時にパーティクル
- **マップ**: NavMesh で AI エネミーや仲間キャラクターを追加
- **セーブ機能**: StickerBook の inventory を JSON でシリアライズして PlayerPrefs に保存
- **FieldObjectProgressBar**: World Space Canvas を使ったプレハブを作成して FieldObject に接続
