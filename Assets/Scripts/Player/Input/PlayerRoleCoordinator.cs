using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class PlayerRoleCoordinator : MonoBehaviour
{
    // 各能力コンポーネントやPlayerIdentityの参照を保持する変数群
    private PlayerIdentity identity;
    private PlayerMotor2D motor;
    private PlayerDash dash;
    private PlayerCannon cannon;
    private PlayerDrawing drawing;
    private PlayerNameplate nameplate;
    private Renderer[] renderers;
    private Collider2D[] colliders;
    private void Awake()
    {
        identity = GetComponent<PlayerIdentity>();
        motor = GetComponent<PlayerMotor2D>();
        dash = GetComponent<PlayerDash>();
        cannon = GetComponent<PlayerCannon>();
        drawing = GetComponent<PlayerDrawing>();
        nameplate = GetComponent<PlayerNameplate>();
        // ブロッカー本体の見た目と判定を消すために子オブジェクトも含めて取得
        renderers = GetComponentsInChildren<Renderer>(true);
        colliders = GetComponentsInChildren<Collider2D>(true);
    }

    private void Start()
    {
        if (GameManager.Instance != null)
        {
            // GameManagerのイベント(OnMatchPhaseChanged)を購読
            GameManager.Instance.OnMatchPhaseChanged += HandlePhaseChanged;
        }
        // 初回状態の反映
        RefreshAbilities();
    }

    private void OnDestroy()
    {
        // イベントの購読解除
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnMatchPhaseChanged -= HandlePhaseChanged;

        }
    }

    private void HandlePhaseChanged(MatchPhase newPhase)
    {
        RefreshAbilities();
    }

    // 陣営が変更されたときに外部(PlayerControllerなど)から呼ばれる想定、あるいはイベント購読
    public void OnSideChanged()
    {
        RefreshAbilities();
    }

    private void RefreshAbilities()
    {
        // GameManagerの現在のフェーズと、現在の陣営を取得
        // 以下の条件に従って各コンポーネントの enabled を切り替える

        // - Drawフェーズ：描画機能のみON、移動や大砲はOFF
        // - Placeフェーズ：大砲の角度調整等があるので特定機能のみON（要調整）
        // - Raceフェーズ：
        //   - GoalRunner: 移動・ダッシュON
        //   - Blocker: 移動・ダッシュOFF、大砲ON、本体のレンダラーとコライダーOFF
    }
}