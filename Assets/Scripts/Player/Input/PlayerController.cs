using UnityEngine;
using UnityEngine.InputSystem;

[DisallowMultipleComponent]
[RequireComponent(typeof(Rigidbody2D), typeof(BoxCollider2D), typeof(PlayerInput))]
[RequireComponent(typeof(PlayerIdentity), typeof(PlayerContactSensor2D), typeof(PlayerMotor2D))]
[RequireComponent(typeof(PlayerDash), typeof(PlayerStun), typeof(PlayerCannon))]
[RequireComponent(typeof(PlayerDrawing))]
// 入力メッセージを各プレイヤー能力へ振り分ける互換コンポーネント。
public sealed class PlayerController : MonoBehaviour
{
	// 各機能コンポーネントをキャッシュする。
	private PlayerIdentity identity;
	// 接地と壁接触を監視する。
	private PlayerContactSensor2D contactSensor;
	// 移動とジャンプを制御する。
	private PlayerMotor2D motor;
	// ダッシュ状態を管理する。
	private PlayerDash dash;
	// スタンを適用する。
	private PlayerStun stun;
	// 大砲の選択と発射を管理する。
	private PlayerCannon cannon;
	// 描画入力とスタンプ生成を管理する。
	private PlayerDrawing drawing;
	// Race 中に非表示化するための描画コンポーネント群を保持する。
	private Renderer[] renderers;
	// Race 中に無効化するための当たり判定群を保持する。
	private Collider2D[] colliders;
	// 現在ブロッカーが Race 中に隠れているかを保持する。
	private bool isRaceBlockerHidden;
	// 現在の移動入力を保持する。
	private Vector2 moveInput;

	// このプレイヤーが担当する陣営を返す。
	public MatchSide ControlledSide => identity != null ? identity.ControlledSide : MatchSide.GoalRunner;
	// 現在地上にいるかを返す。
	public bool IsGrounded => contactSensor != null && contactSensor.IsGrounded;
	// ダッシュ中かを返す。
	public bool IsDashing => dash != null && dash.IsDashing;
	// 現在選択中の大砲番号を返す。
	public int SelectedCannonOrder => cannon != null ? cannon.SelectedMountOrder : -1;
	// 現在選択中の描画形状を返す。
	public DrawingStampShape CurrentDrawingShape => drawing != null ? drawing.CurrentShape : DrawingStampShape.Square;

	// 必要なコンポーネントをキャッシュし、入力登録を試みる。
	private void Awake()
	{
		identity = GetComponent<PlayerIdentity>();
		contactSensor = GetComponent<PlayerContactSensor2D>();
		motor = GetComponent<PlayerMotor2D>();
		dash = GetComponent<PlayerDash>();
		stun = GetComponent<PlayerStun>();
		cannon = GetComponent<PlayerCannon>();
		drawing = GetComponent<PlayerDrawing>();
		renderers = GetComponentsInChildren<Renderer>(true);
		colliders = GetComponentsInChildren<Collider2D>(true);
		drawing.ConfigureControlledSide(identity.ControlledSide);
		RefreshRaceBlockerState();
		RegisterWithInputManager();
	}

	// 起動直後にも再登録を試みる。
	private void Start()
	{
		RefreshRaceBlockerState();
		RegisterWithInputManager();
	}

	// 毎フレーム、Race 中のブロッカー表示と操作を同期する。
	private void Update()
	{
		RefreshRaceBlockerState();
	}

	// 固定更新で移動系の処理をまとめて進める。
	private void FixedUpdate()
	{
		drawing.TickFixed();
		RefreshRaceBlockerState();
		if (isRaceBlockerHidden)
		{
			motor.SetMoveInput(Vector2.zero);
			dash.SetPreferredDirection(Vector2.zero);
			motor.SetMovementLocked(true);
			motor.Stop();
			dash.ResetAvailability();
			return;
		}

		bool isDrawPhase = IsDrawPhase();
		contactSensor.Refresh();
		if (isDrawPhase)
		{
			motor.SetMovementLocked(true);
			motor.Stop();
			dash.ResetAvailability();
			return;
		}

		motor.SetMovementLocked(false);
		motor.SetMoveInput(moveInput);
		dash.SetPreferredDirection(moveInput);
		dash.TickFixed(moveInput, contactSensor.IsGrounded);
		motor.TickFixed(contactSensor, dash.IsDashing);
	}

	// 破棄時に入力管理から外す。
	private void OnDestroy()
	{
		InputManager.Instance?.UnregisterPlayer(this);
	}

	// InputManager が存在すればこのプレイヤーを登録する。
	private void RegisterWithInputManager()
	{
		InputManager.Instance?.RegisterPlayer(this);
	}

	// 移動入力を各移動系コンポーネントへ渡す。
	public void OnMove(InputValue inputValue)
	{
		Vector2 value = inputValue.Get<Vector2>();
		moveInput = value;
		drawing.SetMoveInput(value);
		if (IsRaceBlockerHidden())
		{
			return;
		}

		if (!IsDrawPhase())
		{
			motor.SetMoveInput(value);
			dash.SetPreferredDirection(value);
		}
	}

	// ジャンプ入力を移動コンポーネントへ渡す。
	public void OnJump(InputValue inputValue)
	{
		if (inputValue.isPressed && !IsDrawPhase() && !IsRaceBlockerHidden())
		{
			motor.TryJump(contactSensor);
		}
	}

	// ダッシュ入力を処理する。
	public void OnSprint(InputValue inputValue)
	{
		if (inputValue.isPressed && !IsDrawPhase() && !IsRaceBlockerHidden())
		{
			dash.TryDash(moveInput);
		}
	}

	// 前の大砲へ切り替える。
	public void OnPrevious(InputValue inputValue)
	{
		if (inputValue.isPressed)
		{
			cannon.SelectPrevious();
		}
	}

	// 次の大砲へ切り替える。
	public void OnNext(InputValue inputValue)
	{
		if (inputValue.isPressed)
		{
			cannon.SelectNext();
		}
	}

	// 攻撃入力を描画と大砲へ振り分ける。
	public void OnAttack(InputValue inputValue)
	{
		if (!inputValue.isPressed)
		{
			return;
		}

		if (IsDrawPhase())
		{
			if (GameManager.Instance != null && GameManager.Instance.TryConsumeShape(identity.ControlledSide))
			{
				drawing.PlaceStamp();
			}
			return;
		}

		cannon.TryAttack(identity.ControlledSide);
	}

	// 図形切り替え入力を処理する。
	public void OnCycleShape(InputValue inputValue)
	{
		if (inputValue.isPressed)
		{
			drawing.CycleShape();
		}
	}

	// しゃがみ入力で発射中の弾を止める。
	public void OnCrouch(InputValue inputValue)
	{
		if (inputValue.isPressed)
		{
			cannon.StopActiveProjectile();
		}
	}

	// 外部からダッシュを試行する。
	public bool TryDash() => dash.TryDash(moveInput);
	// 陣営を設定する。
	public void AssignControlledSide(MatchSide side)
	{
		identity.AssignSide(side);
		drawing.ConfigureControlledSide(side);
	}
	// ローカル入力スキームを設定する。
	public void ConfigureLocalInput(MatchSide side, bool useGamepad) => identity.ConfigureLocalInput(side, useGamepad);
	// 一定時間入力をロックする。
	public void LockInput(float duration) => motor.LockInput(duration);
	// ダッシュ再使用可否を初期化する。
	public void ResetDashAvailability() => dash.ResetAvailability();
	// スタンを適用する。
	public void ApplyStun(float duration) => stun.Apply(duration);

	// 描画フェーズかどうかを返す。
	private static bool IsDrawPhase()
	{
		return GameManager.Instance != null && GameManager.currentState == GameState.Game && GameManager.Instance.CurrentPhase == MatchPhase.Draw;
	}

	// Race 中にブロッカーが非表示扱いかを返す。
	private bool IsRaceBlockerHidden()
	{
		return isRaceBlockerHidden;
	}

	// Race 中のブロッカーだけ表示と当たり判定を切り替える。
	private void RefreshRaceBlockerState()
	{
		bool shouldHide = GameManager.Instance != null
			&& GameManager.currentState == GameState.Game
			&& GameManager.Instance.CurrentPhase == MatchPhase.Race
			&& identity != null
			&& identity.ControlledSide == MatchSide.Blocker;

		if (shouldHide == isRaceBlockerHidden)
		{
			return;
		}

		isRaceBlockerHidden = shouldHide;
		SetRenderersEnabled(!shouldHide);
		SetCollidersEnabled(!shouldHide);

		if (shouldHide)
		{
			moveInput = Vector2.zero;
			motor.SetMoveInput(Vector2.zero);
			dash.SetPreferredDirection(Vector2.zero);
			motor.SetMovementLocked(true);
			motor.Stop();
			dash.ResetAvailability();
			return;
		}

		motor.SetMovementLocked(false);
	}

	// 取得済みのレンダラー群を一括で切り替える。
	private void SetRenderersEnabled(bool enabled)
	{
		if (renderers == null)
		{
			return;
		}

		foreach (Renderer renderer in renderers)
		{
			if (renderer != null)
			{
				renderer.enabled = enabled;
			}
		}
	}

	// 取得済みの当たり判定群を一括で切り替える。
	private void SetCollidersEnabled(bool enabled)
	{
		if (colliders == null)
		{
			return;
		}

		foreach (Collider2D collider in colliders)
		{
			if (collider != null)
			{
				collider.enabled = enabled;
			}
		}
	}
}
