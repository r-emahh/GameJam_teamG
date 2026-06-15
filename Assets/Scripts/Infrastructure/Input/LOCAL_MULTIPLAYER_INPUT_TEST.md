# ローカル2人入力 手動テスト

対象シーン: `Assets/Scenes/Rema.unity`

## 事前確認

1. Unity Console を開き、既存ログを Clear する。
2. `PlayerActions.inputactions` に `Gamepad` と `Keyboard&Mouse` の Control Scheme が存在することを確認する。
3. Play Mode 開始後、Console の `Player 1 input:` / `Player 2 input:` ログで実際の割り当てを確認する。

## ゲームパッド2台

1. ゲームパッドを2台接続して Play Mode を開始する。
2. Console で Player 1 と Player 2 が異なるデバイス名に割り当てられていることを確認する。
3. 1台目だけを操作し、Player 1 だけが反応することを確認する。
4. 2台目だけを操作し、Player 2 だけが反応することを確認する。
5. 同じ操作で両プレイヤーが同時に反応しないことを確認する。

## キーボード + ゲームパッド

1. ゲームパッドを1台だけ接続して Play Mode を開始する。
2. Console で Player 1 が `Keyboard&Mouse`、Player 2 が `Gamepad` になっていることを確認する。
3. WASD、Space、Enter、Left Shift で Player 1 だけが反応することを確認する。
4. ゲームパッドで Player 2 だけが反応することを確認する。

## ラウンド交替

1. 上記いずれかの構成でラウンド終了条件を満たす。
2. Goal Runner と Blocker が交替した後も、Player 1 と Player 2 の物理デバイスが入れ替わっていないことを確認する。
3. 交替後も各デバイスが片方のプレイヤーだけを操作することを確認する。

## 未接続・切断

1. ゲームパッドを接続せず Play Mode を開始し、NullReferenceException が出ないことを確認する。
2. ゲームパッド1台構成で Play Mode 中にゲームパッドを抜く。
3. Console に未割り当て警告は出ても、NullReferenceException が出ずゲームが継続することを確認する。
4. ゲームパッドを再接続し、割り当てログが再表示されて入力が復帰することを確認する。
