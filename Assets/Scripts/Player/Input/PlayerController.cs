using UnityEngine;
using UnityEngine.InputSystem;

[DisallowMultipleComponent]
[RequireComponent(typeof(Rigidbody2D), typeof(BoxCollider2D), typeof(PlayerInput))]
[RequireComponent(typeof(PlayerIdentity), typeof(PlayerContactSensor2D), typeof(PlayerMotor2D))]
[RequireComponent(typeof(PlayerDash), typeof(PlayerStun), typeof(PlayerCannon))]
[RequireComponent(typeof(PlayerDrawing), typeof(BlockerRaceAttackCooldown))]
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
	// Race 中の Blocker 妨害弾クールダウンを管理する。
	private BlockerRaceAttackCooldown blockerRaceAttackCooldown;
	// 自由描画入力とデータ記録を管理する。
	private PlayerDrawing drawing;
	// 頭上ラベルの表示を管理する。
	private PlayerNameplate nameplate;
	// プレイヤー本体の表示切り替えに使う描画コンポーネント群を保持する。
	private Renderer[] renderers;
	// Race 中のブロッカー制限に使う当たり判定群を保持する。
	private Collider2D[] colliders;
	// 現在ブロッカー本体の見た目を隠しているかを保持する。
	private bool isBlockerAvatarHidden;
	// 現在ブロッカーが Race 中の制限対象かを保持する。
	private bool isRaceBlockerSuppressed;
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
	// 現在選択中の大砲のステージ中央基準角度を返す。
	public float SelectedCannonAngle => cannon != null ? cannon.SelectedMountAngle : 0f;
	// 現在の発射パワーを返す。
	public float CannonLaunchPower => cannon != null ? cannon.CurrentLaunchPower : 0f;
	// HUD 用に正規化した発射パワーを返す。
	public float NormalizedCannonLaunchPower => cannon != null ? cannon.NormalizedLaunchPower : 0f;
	// 現在発射準備中かを返す。
	public bool IsPreparingCannonLaunch => cannon != null && cannon.IsPreparingLaunch;
	// Race 中の Blocker 妨害弾が使用可能かを返す。
	public bool IsBlockerRaceAttackReady => blockerRaceAttackCooldown == null || blockerRaceAttackCooldown.IsReady;
	// Race 中の Blocker 妨害弾クールダウン残り時間を返す。
	public float BlockerRaceAttackCooldownRemaining => blockerRaceAttackCooldown != null ? blockerRaceAttackCooldown.RemainingTime : 0f;
	// Race 中の Blocker 妨害弾クールダウン総時間を返す。
	public float BlockerRaceAttackCooldownDuration => blockerRaceAttackCooldown != null ? blockerRaceAttackCooldown.Duration : 0f;
	// 現在記録済みの描画点数を返す。
	public int DrawingPointCount => drawing?.PointCount ?? 0;
	// 描画点数の上限を返す。
	public int DrawingMaxPointCount => drawing?.MaxPointCount ?? 0;
	// 描画が確定済みかを返す。
	public bool IsDrawingConfirmed => drawing != null && drawing.IsConfirmed;
	// Unity 非依存の描画データを返す。
	public DrawingArtifactData DrawingArtifact => drawing?.Artifact;

	// 必要なコンポーネントをキャッシュし、入力登録を試みる。
	private void Awake()
	{
		identity = GetComponent<PlayerIdentity>();
		contactSensor = GetComponent<PlayerContactSensor2D>();
		motor = GetComponent<PlayerMotor2D>();
		dash = GetComponent<PlayerDash>();
		stun = GetComponent<PlayerStun>();
		cannon = GetComponent<PlayerCannon>();
		blockerRaceAttackCooldown = GetComponent<BlockerRaceAttackCooldown>();
		drawing = GetComponent<PlayerDrawing>();
		nameplate = GetComponent<PlayerNameplate>();
		if (nameplate == null)
		{
			nameplate = gameObject.AddComponent<PlayerNameplate>();
		}

		renderers = GetComponentsInChildren<Renderer>(true);
		colliders = GetComponentsInChildren<Collider2D>(true);
		drawing.ConfigureControlledSide(identity.ControlledSide);
		RefreshBlockerPresentationState();
		RegisterWithInputManager();
	}

	// 起動直後にも再登録を試みる。
	private void Start()
	{
		RefreshBlockerPresentationState();
		RegisterWithInputManager();
		InputManager.Instance?.RefreshPlayerInputAssignments();
		nameplate?.RefreshDisplay();
	}

	// 毎フレーム、ブロッカー本体の見た目と制限状態を同期する。
	private void Update()
	{
		RefreshBlockerPresentationState();
	}

	// 固定更新で移動系の処理をまとめて進める。
	private void FixedUpdate()
	{
		drawing.TickFixed();
		RefreshBlockerPresentationState();
		if (isRaceBlockerSuppressed)
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
		if (IsRaceBlockerSuppressed())
		{
			return;
		}

		if (!IsDrawPhase())
		{
			motor.SetMoveInput(value);
			dash.SetPreferredDirection(value);
		}
	}

	// Place 中の大砲角度調整入力を渡す。
	public void OnAim(InputValue inputValue)
	{
		cannon.SetAimInput(inputValue.Get<float>());
	}

	// ジャンプ入力を移動コンポーネントへ渡す。
	public void OnJump(InputValue inputValue)
	{
		if (inputValue.isPressed && !IsDrawPhase() && !IsRaceBlockerSuppressed())
		{
			motor.TryJump(contactSensor);
		}
	}

	// ダッシュ入力を処理する。
	public void OnSprint(InputValue inputValue)
	{
		if (inputValue.isPressed && !IsDrawPhase() && !IsRaceBlockerSuppressed())
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

	// 攻撃入力を自由描画と大砲へ振り分ける。
	public void OnAttack(InputValue inputValue)
	{
		if (IsDrawPhase())
		{
			drawing.SetDrawButtonPressed(inputValue.isPressed);
			return;
		}

		if (inputValue.isPressed)
		{
			cannon.TryAttack(identity.ControlledSide);
		}
	}

	// Draw 中は現在の自由描画をクリアする。
	public void OnCycleShape(InputValue inputValue)
	{
		if (inputValue.isPressed && IsDrawPhase())
		{
			drawing.ClearDrawing();
		}
	}

	// Draw 中は描画を確定し、それ以外では発射中の弾を止める。
	public void OnCrouch(InputValue inputValue)
	{
		if (!inputValue.isPressed)
		{
			return;
		}

		if (IsDrawPhase())
		{
			drawing.ConfirmDrawing();
		}
		else
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
		RefreshBlockerPresentationState();
	}
	// 指定されたローカル入力デバイスを設定する。
	public bool ConfigureLocalInput(string controlScheme, params InputDevice[] devices) => identity.ConfigureLocalInput(controlScheme, devices);
	// 現在のローカル入力ペアリングを解除する。
	public void ClearLocalInput() => identity.ClearLocalInput();
	// 一定時間入力をロックする。
	public void LockInput(float duration) => motor.LockInput(duration);
	// ダッシュ再使用可否を初期化する。
	public void ResetDashAvailability() => dash.ResetAvailability();
	// スタンを適用する。
	public void ApplyStun(float duration) => stun.Apply(duration);
	// ラウンド切り替え時の状態をまとめて初期化する。
	public void ResetForNextRound(Vector3 spawnPosition)
	{
		transform.position = spawnPosition;
		moveInput = Vector2.zero;
		cannon.SetAimInput(0f);
		drawing.SetMoveInput(Vector2.zero);
		drawing.SetDrawButtonPressed(false);
		motor.SetMoveInput(Vector2.zero);
		dash.SetPreferredDirection(Vector2.zero);
		motor.ResetForNextRound();
		dash.ResetAvailability();
		cannon.ResetForNextRound();
		blockerRaceAttackCooldown?.ResetCooldown();
		drawing.ResetForNextRound();
		RefreshBlockerPresentationState();
	}

	// 描画フェーズかどうかを返す。
	private static bool IsDrawPhase()
	{
		return GameManager.Instance != null && GameManager.currentState == GameState.Game && GameManager.Instance.CurrentPhase == MatchPhase.Draw;
	}

	// Race 中にブロッカーが移動停止と当たり判定オフ対象かを返す。
	private bool IsRaceBlockerSuppressed()
	{
		return isRaceBlockerSuppressed;
	}

	// ブロッカー本体の見た目と Race 中の制限状態を同期する。
	private void RefreshBlockerPresentationState()
	{
		bool shouldHideAvatar = identity != null
			&& identity.ControlledSide == MatchSide.Blocker;
		if (shouldHideAvatar != isBlockerAvatarHidden)
		{
			isBlockerAvatarHidden = shouldHideAvatar;
			SetRenderersEnabled(!shouldHideAvatar);
			nameplate?.SetVisible(!shouldHideAvatar);
		}

		bool shouldSuppressInRace = GameManager.Instance != null
			&& GameManager.currentState == GameState.Game
			&& GameManager.Instance.CurrentPhase == MatchPhase.Race
			&& identity != null
			&& identity.ControlledSide == MatchSide.Blocker;

		if (shouldSuppressInRace == isRaceBlockerSuppressed)
		{
			return;
		}

		isRaceBlockerSuppressed = shouldSuppressInRace;
		SetCollidersEnabled(!shouldSuppressInRace);

		if (shouldSuppressInRace)
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
