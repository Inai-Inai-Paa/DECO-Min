# クラス設計 (Class Design)

本プロジェクト（DECO-Min）のコアとなるクラス設計の概要です。
キャラクターの制御、ステートマシン（状態遷移）、およびカメラ制御が主な基盤となっています。

## 1. キャラクター（Character）システム
キャラクター（プレイヤーや敵など）の共通機能とステータスを管理する基底クラス群です。

- **`Character` (MonoBehaviour)**
  - 全キャラクターの基底クラス。
  - 物理挙動（`Rigidbody` の速度制御）とステートマシン（`StateMachine`）の更新を行います。
  - 参照するステータスデータとして `CharacterStatus` を保持します。

- **`CharacterStatus` (ScriptableObject)**
  - キャラクターの最大体力・現在体力・移動速度倍率・攻撃力倍率などの共通ステータスを定義するデータコンテナです。

### 1.1 プレイヤー（Player）
- **`Player` (Character を継承)**
  - プレイヤー固有の制御を行うクラス。接地判定（Raycast）やカメラの向きに基づく移動方向の算出を行います。
  - `PlayerInputData` を通じて入力情報を管理します。
  - 固有のステートマシン（`PlayerState`）を用いて状態遷移を管理します。

- **`PlayerStatus` (CharacterStatus を継承)**
  - プレイヤー固有のステータス。総シール数（`TotalSealCount`）や現在のシール数（`CurrentSealCount`）を追加で保持します。

- **`PlayerInputData` & `Player.Input.cs`**
  - 新しい Input System (`PlayerInput`) をラップし、移動ベクトル（`Move`）やジャンプ入力（`JumpHeld`, `JumpPressed`）、先行入力判定などを管理します。

### 1.2 エネミー（Enemy）
- **`Enemy` (Character を継承)**
  - 敵キャラクター固有の制御を行うクラス。プレイヤーと同様に接地判定などを持ちます。
  
- **`EnemyStatus` (CharacterStatus を継承)**
  - 敵固有のステータスデータ。現在は `CharacterStatus` と同等ですが、将来的に敵専用のパラメータを追加するための拡張枠です。

## 2. ステートマシン（StateMachine）パターン
各キャラクターの状態（移動、攻撃、待機など）をスクリプタブルオブジェクト（ScriptableObject）ベースで管理する設計です。

- **`State` (ScriptableObject)**
  - 全ステートの基底クラス。`Enter()`, `Exit()`, `Update()`, `FixedUpdate()` の仮想メソッドを提供します。
  
- **`StateMachine` (Class)**
  - 現在の `State` を保持し、状態遷移（`ChangeState`）および更新処理（`Update`, `FixedUpdate`）を委譲します。

- **`PlayerState` & `PlayerMove`**
  - `PlayerState` は `Player` と `StateMachine` への参照を初期化（Initialize）するプレイヤー専用ステートの基底クラス。
  - `PlayerMove` は具体的な移動ステートで、プレイヤーの入力（`playerInputData`）を基に速度の計算やジャンプ処理を実装します。

- **`EnemyState`**
  - 敵専用ステートの基底クラス（PlayerStateのEnemy版）。

## 3. カメラ（Camera）システム
プレイヤーに追従し、特定のエリア（Volume）に応じてカメラの位置や振る舞いを動的にブレンドするシステムです。

- **`CameraVolume` (MonoBehaviour)**
  - シーン内に配置される空間（Volume）。半径（`radius`）とカメラパラメータ（オフセット、視野角、追従の有無など）を定義します。
  - `GetPreviewTransform` によって、Raycastを用いた地面基準のカメラターゲット位置を算出する機能も持ちます。

- **`CameraController` (MonoBehaviour)**
  - プレイヤーの現在位置と各 `CameraVolume` の距離を計算し、影響範囲内のVolumeの重み（Weight）に応じてカメラの位置・回転・ターゲット位置を滑らかにブレンド（Lerp/Slerp）します。

## 4. その他・ポストエフェクト
- **`Seal` (MonoBehaviour)**
  - シール（Sticker）オブジェクトの基底。独自の `StateMachine` を持ち、シールの振る舞い（飛翔、貼り付き等）をステートで管理する想定のクラスです。

- **`CustomPostProcessEffectRendererFeature` (ScriptableRendererFeature)**
  - Unity 6 / URPのRenderGraph APIを用いたカスタムポストプロセス（ノイズやブルームなど）を描画するためのレンダラーフィーチャーです。Reflectionを用いて `VolumeComponent` の値をシェーダーマテリアルに動的に適用します。
