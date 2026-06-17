using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

[DisallowMultipleComponent]
[RequireComponent(typeof(Rigidbody2D), typeof(BoxCollider2D), typeof(PlayerInput))]
[RequireComponent(typeof(PlayerIdentity), typeof(PlayerContactSensor2D), typeof(PlayerMotor2D))]
[RequireComponent(typeof(PlayerDash), typeof(PlayerStun), typeof(PlayerCannon))]
[RequireComponent(typeof(PlayerDrawing), typeof(BlockerRaceAttackCooldown))]
// 入力メッセージを各プレイヤー能力へ振り分ける互換コンポーネント。
public sealed class PlayerController : MonoBehaviour
{
	private const string GamepadControlScheme = "Gamepad";
	private const string JoystickControlScheme = "Joystick";
	private const string KeyboardMouseControlScheme = "Keyboard&Mouse";
	private const string MoveActionName = "Move";
	private const string AimActionName = "Aim";
	private const string JumpActionName = "Jump";
	private const string SprintActionName = "Sprint";
	private const string PreviousActionName = "Previous";
	private const string NextActionName = "Next";
	private const string AttackActionName = "Attack";
	private const string CycleShapeActionName = "CycleShape";
	private const string CrouchActionName = "Crouch";

	// 各機能コンポーネントをキャッシュする。
	private PlayerIdentity identity;
	// 入力イベントを受け取る PlayerInput を保持する。
	private PlayerInput playerInput;
	// InputSystem の PlayerInput 配送が複製プレイヤーで詰まる場合に備え、割り当て済み物理デバイスを直接読む。
	private InputDevice[] assignedInputDevices;
	private string assignedControlScheme;
	// 速度リセットと瞬間移動に使う Rigidbody を保持する。
	private Rigidbody2D body;
	// 接地と壁接触を監視する。
	private PlayerContactSensor2D contactSensor;
	// 移動とジャンプを制御する。
	private PlayerMotor2D motor;
	// ダッシュ状態を管理する。
	private PlayerDash dash;
	// アニメーター同期を管理する。
	private PlayerAnimationSync animationSync;
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
	// HUD チュートリアルなど外部要因でゲームプレイ入力を止めているかを保持する。
	private bool gameplayInputEnabled = true;
	private bool previousJumpPressed;
	private bool previousSprintPressed;
	private bool previousPreviousPressed;
	private bool previousNextPressed;
	private bool previousAttackPressed;
	private bool previousCycleShapePressed;
	private bool previousCrouchPressed;
	private float lastPolledInputLogTime;

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
	// スタン中かを返す。
	public bool IsStunned => stun != null && stun.IsStunned;
	// 残りスタン時間を返す。
	public float StunTimeRemaining => stun != null ? stun.RemainingDuration : 0f;
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
	// 現在ゲームプレイ入力を受け付ける状態かを返す。
	public bool IsGameplayInputEnabled => gameplayInputEnabled;

	// 必要なコンポーネントをキャッシュし、入力登録を試みる。
	private void Awake()
	{
		EnsureComponentReferences();
		drawing.ConfigureControlledSide(identity.ControlledSide);
		RefreshBlockerPresentationState();
		RegisterWithInputManager();
	}

	private void EnsureComponentReferences()
	{
		identity = GetComponent<PlayerIdentity>();
		playerInput = GetComponent<PlayerInput>();
		body = GetComponent<Rigidbody2D>();
		contactSensor = GetComponent<PlayerContactSensor2D>();
		motor = GetComponent<PlayerMotor2D>();
		dash = GetComponent<PlayerDash>();
		animationSync = GetComponent<PlayerAnimationSync>();
		if (animationSync == null)
		{
			animationSync = gameObject.AddComponent<PlayerAnimationSync>();
		}
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
		PollAssignedInputDevices();
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
			motor.SetMovementLocked(PlayerMotor2D.MovementLockSource.RaceSuppressed, true);
			motor.Stop();
			dash.ResetAvailability();
			return;
		}

		if (!gameplayInputEnabled)
		{
			motor.SetMoveInput(Vector2.zero);
			dash.SetPreferredDirection(Vector2.zero);
			motor.Stop();
			return;
		}

		motor.SetMovementLocked(PlayerMotor2D.MovementLockSource.RaceSuppressed, false);
		bool isPlacePhase = IsPlacePhase();

		bool isResultPhase = IsResultPhase();
		bool isDrawPhase = IsDrawPhase();
		contactSensor.Refresh();
		if (isDrawPhase || isResultPhase || isPlacePhase)
		{
			motor.SetMovementLocked(PlayerMotor2D.MovementLockSource.DrawPhase, true);
			motor.SetMovementLocked(PlayerMotor2D.MovementLockSource.PlacePhase, isPlacePhase);
			motor.Stop();
			dash.ResetAvailability();
			return;
		}

		motor.SetMovementLocked(PlayerMotor2D.MovementLockSource.DrawPhase, false);
		motor.SetMovementLocked(PlayerMotor2D.MovementLockSource.PlacePhase, false);
		if (IsStunned)
		{
			motor.SetMoveInput(Vector2.zero);
			dash.TickFixed(moveInput, contactSensor.IsGrounded);
			motor.Stop();
			dash.CancelActiveDash();
			return;
		}

		motor.SetMoveInput(moveInput);
		dash.SetPreferredDirection(moveInput);
		dash.TickFixed(moveInput, contactSensor.IsGrounded);
		motor.TickFixed(contactSensor, dash.IsDashing);
	}

	// 破棄時に入力管理から外す。
	private void OnDestroy()
	{
		UnbindPlayerInputActions();
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
		ProcessMove(inputValue.Get<Vector2>());
	}

	private void ProcessMove(Vector2 value)
	{
		if (!gameplayInputEnabled)
		{
			moveInput = Vector2.zero;
			drawing.SetMoveInput(Vector2.zero);
			return;
		}

		if (IsResultPhase() || IsPlacePhase())
		{
			moveInput = Vector2.zero;
			drawing.SetMoveInput(Vector2.zero);
			return;
		}

		moveInput = value;
		drawing.SetMoveInput(value);
		if (IsRaceBlockerSuppressed())
		{
			return;
		}

		if (!IsDrawPhase())
		{
			if (!IsStunned)
			{
				motor.SetMoveInput(value);
				dash.SetPreferredDirection(value);
			}
		}
	}

	// Place 中の大砲角度調整入力を渡す。
	public void OnAim(InputValue inputValue)
	{
		ProcessAim(inputValue.Get<Vector2>());
	}

	private void ProcessAim(Vector2 value)
	{
		if (!gameplayInputEnabled)
		{
			cannon.SetAimDirection(Vector2.zero);
			cannon.SetAimInput(0f);
			return;
		}

		if (IsResultPhase())
		{
			cannon.SetAimDirection(Vector2.zero);
			cannon.SetAimInput(0f);
			return;
		}

		cannon.SetAimDirection(value);
		if (value == Vector2.zero)
		{
			cannon.SetAimInput(0f);
		}
	}

	private void ProcessAimAxis(float value)
	{
		if (!gameplayInputEnabled || IsResultPhase())
		{
			cannon.SetAimDirection(Vector2.zero);
			cannon.SetAimInput(0f);
			return;
		}

		cannon.SetAimDirection(Vector2.zero);
		cannon.SetAimInput(value);
	}

	// ジャンプ入力を移動コンポーネントへ渡す。
	public void OnJump(InputValue inputValue)
	{
		ProcessJump(inputValue.isPressed);
	}

	private void ProcessJump(bool isPressed)
	{
		if (!gameplayInputEnabled)
		{
			return;
		}

		if (IsResultPhase() || IsPlacePhase())
		{
			return;
		}

		if (isPressed && !IsDrawPhase() && !IsRaceBlockerSuppressed())
		{
			if (IsStunned)
			{
				return;
			}

			if (motor.TryJump(contactSensor))
			{
				animationSync?.TriggerJump();
			}
		}
	}

	// ダッシュ入力を処理する。
	public void OnSprint(InputValue inputValue)
	{
		ProcessSprint(inputValue.isPressed);
	}

	private void ProcessSprint(bool isPressed)
	{
		if (!gameplayInputEnabled)
		{
			return;
		}

		if (IsResultPhase() || IsPlacePhase())
		{
			return;
		}

		if (isPressed && !IsDrawPhase() && !IsRaceBlockerSuppressed())
		{
			if (IsStunned)
			{
				return;
			}

			dash.TryDash(moveInput);
		}
	}

	// 前の大砲へ切り替える。
	public void OnPrevious(InputValue inputValue)
	{
		ProcessPrevious(inputValue.isPressed);
	}

	private void ProcessPrevious(bool isPressed)
	{
		if (!gameplayInputEnabled)
		{
			return;
		}

		if (IsResultPhase())
		{
			return;
		}

		if (isPressed)
		{
			cannon.SelectPrevious();
		}
	}

	// 次の大砲へ切り替える。
	public void OnNext(InputValue inputValue)
	{
		ProcessNext(inputValue.isPressed);
	}

	private void ProcessNext(bool isPressed)
	{
		if (!gameplayInputEnabled)
		{
			return;
		}

		if (IsResultPhase())
		{
			return;
		}

		if (isPressed)
		{
			cannon.SelectNext();
		}
	}

	// 攻撃入力を自由描画と大砲へ振り分ける。
	public void OnAttack(InputValue inputValue)
	{
		ProcessAttack(inputValue.isPressed);
	}

	private void ProcessAttack(bool isPressed)
	{
		if (!gameplayInputEnabled)
		{
			drawing.SetDrawButtonPressed(false);
			return;
		}

		if (IsResultPhase())
		{
			return;
		}

		if (IsDrawPhase())
		{
			drawing.SetDrawButtonPressed(isPressed);
			return;
		}

		if (isPressed)
		{
			cannon.TryAttack(identity.ControlledSide);
		}
	}

	// Draw 中は現在の自由描画をクリアする。
	public void OnCycleShape(InputValue inputValue)
	{
		ProcessCycleShape(inputValue.isPressed);
	}

	private void ProcessCycleShape(bool isPressed)
	{
		if (!gameplayInputEnabled)
		{
			return;
		}

		if (IsResultPhase())
		{
			return;
		}

		if (isPressed && IsDrawPhase())
		{
			drawing.ClearDrawing();
		}
	}

	// Draw 中は描画を確定し、それ以外では発射中の弾を止める。
	public void OnCrouch(InputValue inputValue)
	{
		ProcessCrouch(inputValue.isPressed);
	}

	private void ProcessCrouch(bool isPressed)
	{
		if (!gameplayInputEnabled)
		{
			return;
		}

		if (IsResultPhase())
		{
			return;
		}

		if (!isPressed)
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
	public bool TryDash() => !IsStunned && !IsPlacePhase() && dash.TryDash(moveInput);
	// 陣営を設定する。
	public void AssignControlledSide(MatchSide side)
	{
		identity.AssignSide(side);
		drawing.ConfigureControlledSide(side);
		RefreshBlockerPresentationState();
	}
	// 指定されたローカル入力デバイスを設定する。
	public bool ConfigureLocalInput(string controlScheme, params InputDevice[] devices)
	{
		bool configured = identity.ConfigureLocalInput(controlScheme, devices);
		bool canPollDirectly = CanPollDirectly(devices);
		if (configured || canPollDirectly)
		{
			assignedControlScheme = controlScheme;
			assignedInputDevices = devices;
			ResetPolledButtonState();
		}

		return configured || canPollDirectly;
	}
	// 現在のローカル入力ペアリングを解除する。
	public void ClearLocalInput()
	{
		UnbindPlayerInputActions();
		assignedControlScheme = null;
		assignedInputDevices = null;
		ResetPolledButtonState();
		identity.ClearLocalInput();
	}
	// 一定時間入力をロックする。
	public void LockInput(float duration) => motor.LockInput(duration);
	// ゲームプレイ入力受付を切り替える。
	public void SetGameplayInputEnabled(bool enabled)
	{
		gameplayInputEnabled = enabled;
		if (enabled)
		{
			return;
		}

		moveInput = Vector2.zero;
		drawing.SetMoveInput(Vector2.zero);
		drawing.SetDrawButtonPressed(false);
		cannon.SetAimDirection(Vector2.zero);
		cannon.SetAimInput(0f);
		motor.SetMoveInput(Vector2.zero);
		dash.SetPreferredDirection(Vector2.zero);
		motor.Stop();
	}
	// ダッシュ再使用可否を初期化する。
	public void ResetDashAvailability() => dash.ResetAvailability();
	// スタンを適用する。
	public void ApplyStun(float duration) => stun.Apply(duration);
	// ラウンド切り替え時の状態をまとめて初期化する。
	public void ResetForNextRound(Vector3 spawnPosition)
	{
		ResetForRespawnInternal(spawnPosition, 0f, true);
	}

	// Race 中の落下リスポーンに必要な状態だけを初期化する。
	public void RespawnAt(Vector3 spawnPosition, float inputLockDuration)
	{
		ResetForRespawnInternal(spawnPosition, inputLockDuration, false);
	}

	// 描画フェーズかどうかを返す。
	private static bool IsDrawPhase()
	{
		return GameManager.Instance != null && GameManager.currentState == GameState.Game && GameManager.Instance.CurrentPhase == MatchPhase.Draw;
	}

	// Result フェーズかどうかを返す。
	private static bool IsResultPhase()
	{
		return GameManager.Instance != null && GameManager.currentState == GameState.Game && GameManager.Instance.CurrentPhase == MatchPhase.Result;
	}

	// Place フェーズかどうかを返す。
	private static bool IsPlacePhase()
	{
		return GameManager.Instance != null && GameManager.currentState == GameState.Game && GameManager.Instance.CurrentPhase == MatchPhase.Place;
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
			motor.SetMovementLocked(PlayerMotor2D.MovementLockSource.RaceSuppressed, true);
			motor.Stop();
			dash.ResetAvailability();
			return;
		}

		motor.SetMovementLocked(PlayerMotor2D.MovementLockSource.RaceSuppressed, false);
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

	// 位置、速度、ダッシュ、スタン、入力状態をリセットする。
	private void ResetForRespawnInternal(Vector3 spawnPosition, float inputLockDuration, bool resetRoundArtifacts)
	{
		EnsureComponentReferences();
		if (body != null)
		{
			body.position = spawnPosition;
			body.linearVelocity = Vector2.zero;
			body.angularVelocity = 0f;
		}
		else
		{
			transform.position = spawnPosition;
		}

		moveInput = Vector2.zero;
		gameplayInputEnabled = true;
		stun.Clear();
		cannon.SetAimInput(0f);
		drawing.SetMoveInput(Vector2.zero);
		drawing.SetDrawButtonPressed(false);
		motor.SetMoveInput(Vector2.zero);
		dash.SetPreferredDirection(Vector2.zero);
		motor.ResetForNextRound();
		dash.ResetAvailability();
		cannon.ResetForNextRound();
		blockerRaceAttackCooldown?.ResetCooldown();
		if (resetRoundArtifacts)
		{
			drawing.ResetForNextRound();
		}

		if (inputLockDuration > 0f)
		{
			motor.LockInput(inputLockDuration);
		}

		contactSensor.Refresh();
		animationSync?.ResetAnimatorState();
		RefreshBlockerPresentationState();
	}

	private void BindPlayerInputActions()
	{
		if (playerInput == null)
		{
			playerInput = GetComponent<PlayerInput>();
		}

		if (playerInput == null)
		{
			return;
		}

		playerInput.onActionTriggered -= HandlePlayerInputAction;
		playerInput.onActionTriggered += HandlePlayerInputAction;
	}

	private void UnbindPlayerInputActions()
	{
		if (playerInput == null)
		{
			return;
		}

		playerInput.onActionTriggered -= HandlePlayerInputAction;
	}

	private void HandlePlayerInputAction(InputAction.CallbackContext context)
	{
		if (assignedInputDevices != null && assignedInputDevices.Length > 0)
		{
			return;
		}

		if (!context.performed && !context.canceled)
		{
			return;
		}

		switch (context.action.name)
		{
			case MoveActionName:
				ProcessMove(context.canceled ? Vector2.zero : context.ReadValue<Vector2>());
				break;
			case AimActionName:
				ProcessAimAction(context);
				break;
			case JumpActionName:
				ProcessJump(context.ReadValueAsButton());
				break;
			case SprintActionName:
				ProcessSprint(context.ReadValueAsButton());
				break;
			case PreviousActionName:
				ProcessPrevious(context.ReadValueAsButton());
				break;
			case NextActionName:
				ProcessNext(context.ReadValueAsButton());
				break;
			case AttackActionName:
				ProcessAttack(context.ReadValueAsButton());
				break;
			case CycleShapeActionName:
				ProcessCycleShape(context.ReadValueAsButton());
				break;
			case CrouchActionName:
				ProcessCrouch(context.ReadValueAsButton());
				break;
		}
	}

	private void ProcessAimAction(InputAction.CallbackContext context)
	{
		Vector2 value = context.canceled ? Vector2.zero : context.ReadValue<Vector2>();
		if (IsAimAxisControl(context.control))
		{
			ProcessAimAxis(value.y);
			return;
		}

		ProcessAim(value);
	}

	private static bool IsAimAxisControl(InputControl control)
	{
		string path = control?.path ?? string.Empty;
		return path.Contains("/dpad/") || path.EndsWith("/r") || path.EndsWith("/f");
	}

	private void PollAssignedInputDevices()
	{
		if (assignedInputDevices == null || assignedInputDevices.Length == 0)
		{
			return;
		}

		if (TryGetAssignedDevice(out Gamepad gamepad))
		{
			PollGamepad(gamepad);
			return;
		}

		if ((assignedControlScheme == GamepadControlScheme || assignedControlScheme == JoystickControlScheme)
			&& TryGetAssignedDevice(out Joystick joystick))
		{
			PollJoystick(joystick);
			return;
		}

		if (assignedControlScheme == KeyboardMouseControlScheme && TryGetAssignedDevice(out Keyboard keyboard))
		{
			PollKeyboard(keyboard);
		}
	}

	private void PollGamepad(Gamepad gamepad)
	{
		if (gamepad == null || !gamepad.added)
		{
			ProcessMove(Vector2.zero);
			ProcessAim(Vector2.zero);
			return;
		}

		Vector2 move = ApplyStickDeadzone(gamepad.leftStick.ReadValue());
		if (IsDrawPhase() && move == Vector2.zero)
		{
			move = GetGamepadDpadVector(gamepad);
			if (move == Vector2.zero)
			{
				move = ApplyStickDeadzone(gamepad.rightStick.ReadValue());
			}
		}

		ProcessMove(move);
		Vector2 aim = ApplyStickDeadzone(gamepad.rightStick.ReadValue());
		float aimAxis = 0f;
		if (aim == Vector2.zero)
		{
			aimAxis = GetDirectionalAxis(gamepad.dpad.up.isPressed, gamepad.dpad.down.isPressed);
			ProcessAimAxis(aimAxis);
		}
		else
		{
			ProcessAim(aim);
		}

		LogPolledInputIfActive(gamepad, move, aim, gamepad.rightShoulder.isPressed
			|| gamepad.buttonSouth.isPressed
			|| gamepad.buttonEast.isPressed
			|| gamepad.buttonWest.isPressed
			|| gamepad.leftShoulder.isPressed
			|| gamepad.dpad.left.isPressed
			|| gamepad.dpad.right.isPressed);
		PollButton(gamepad.buttonSouth.isPressed, ref previousJumpPressed, ProcessJump);
		PollButton(gamepad.leftShoulder.isPressed, ref previousSprintPressed, ProcessSprint);
		PollButton(gamepad.dpad.left.isPressed, ref previousPreviousPressed, ProcessPrevious);
		PollButton(gamepad.dpad.right.isPressed, ref previousNextPressed, ProcessNext);
		ProcessAttack(gamepad.rightShoulder.isPressed);
		previousAttackPressed = gamepad.rightShoulder.isPressed;
		PollButton(gamepad.buttonWest.isPressed, ref previousCycleShapePressed, ProcessCycleShape);
		PollButton(gamepad.buttonEast.isPressed, ref previousCrouchPressed, ProcessCrouch);
	}

	private void PollKeyboard(Keyboard keyboard)
	{
		if (keyboard == null || !keyboard.added)
		{
			ProcessMove(Vector2.zero);
			ProcessAim(Vector2.zero);
			return;
		}

		Vector2 move = new Vector2(
			GetDirectionalAxis(keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed, keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed),
			GetDirectionalAxis(keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed, keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed));
		ProcessMove(move.sqrMagnitude > 1f ? move.normalized : move);
		float aim = GetDirectionalAxis(keyboard.rKey.isPressed, keyboard.fKey.isPressed);
		ProcessAimAxis(aim);
		LogPolledInputIfActive(keyboard, move, new Vector2(0f, aim), keyboard.spaceKey.isPressed
			|| keyboard.leftShiftKey.isPressed
			|| keyboard.digit1Key.isPressed
			|| keyboard.digit2Key.isPressed
			|| keyboard.enterKey.isPressed
			|| keyboard.xKey.isPressed
			|| keyboard.cKey.isPressed);
		PollButton(keyboard.spaceKey.isPressed, ref previousJumpPressed, ProcessJump);
		PollButton(keyboard.leftShiftKey.isPressed, ref previousSprintPressed, ProcessSprint);
		PollButton(keyboard.digit1Key.isPressed, ref previousPreviousPressed, ProcessPrevious);
		PollButton(keyboard.digit2Key.isPressed, ref previousNextPressed, ProcessNext);
		ProcessAttack(keyboard.enterKey.isPressed);
		previousAttackPressed = keyboard.enterKey.isPressed;
		PollButton(keyboard.xKey.isPressed, ref previousCycleShapePressed, ProcessCycleShape);
		PollButton(keyboard.cKey.isPressed, ref previousCrouchPressed, ProcessCrouch);
	}

	private void PollJoystick(Joystick joystick)
	{
		if (joystick == null || !joystick.added)
		{
			ProcessMove(Vector2.zero);
			ProcessAim(Vector2.zero);
			return;
		}

		Vector2 move = ApplyStickDeadzone(joystick.stick.ReadValue());
		if (IsDrawPhase() && move == Vector2.zero)
		{
			move = GetJoystickHatswitchVector(joystick);
		}

		ProcessMove(move);
		Vector2 hatswitch = GetJoystickHatswitchVector(joystick);
		float aim = GetDirectionalAxis(hatswitch.y > 0.5f, hatswitch.y < -0.5f);
		ProcessAimAxis(aim);
		bool jumpPressed = IsJoystickButtonPressed(joystick, "button0", "buttonSouth", "primaryButton");
		bool sprintPressed = IsJoystickButtonPressed(joystick, "button4", "leftShoulder", "leftTriggerButton");
		bool previousPressed = hatswitch.x < -0.5f || IsJoystickButtonPressed(joystick, "button6", "select");
		bool nextPressed = hatswitch.x > 0.5f || IsJoystickButtonPressed(joystick, "button7", "start");
		bool attackPressed = (joystick.trigger != null && joystick.trigger.isPressed)
			|| IsJoystickButtonPressed(joystick, "button5", "rightShoulder", "rightTriggerButton");
		bool cycleShapePressed = IsJoystickButtonPressed(joystick, "button2", "buttonWest", "secondaryButton");
		bool crouchPressed = IsJoystickButtonPressed(joystick, "button1", "buttonEast");
		LogPolledInputIfActive(joystick, move, new Vector2(0f, aim), jumpPressed
			|| sprintPressed
			|| previousPressed
			|| nextPressed
			|| attackPressed
			|| cycleShapePressed
			|| crouchPressed);
		PollButton(jumpPressed, ref previousJumpPressed, ProcessJump);
		PollButton(sprintPressed, ref previousSprintPressed, ProcessSprint);
		PollButton(previousPressed, ref previousPreviousPressed, ProcessPrevious);
		PollButton(nextPressed, ref previousNextPressed, ProcessNext);
		ProcessAttack(attackPressed);
		previousAttackPressed = attackPressed;
		PollButton(cycleShapePressed, ref previousCycleShapePressed, ProcessCycleShape);
		PollButton(crouchPressed, ref previousCrouchPressed, ProcessCrouch);
	}

	private bool TryGetAssignedDevice<TDevice>(out TDevice device) where TDevice : InputDevice
	{
		for (int i = 0; i < assignedInputDevices.Length; i++)
		{
			if (assignedInputDevices[i] is TDevice typedDevice)
			{
				device = typedDevice;
				return true;
			}
		}

		device = null;
		return false;
	}

	private static bool CanPollDirectly(InputDevice[] devices)
	{
		if (devices == null)
		{
			return false;
		}

		for (int i = 0; i < devices.Length; i++)
		{
			if (devices[i] is Gamepad || devices[i] is Joystick || devices[i] is Keyboard)
			{
				return true;
			}
		}

		return false;
	}

	private static void PollButton(bool isPressed, ref bool previousPressed, System.Action<bool> handler)
	{
		if (isPressed && !previousPressed)
		{
			handler(true);
		}
		else if (!isPressed && previousPressed)
		{
			handler(false);
		}

		previousPressed = isPressed;
	}

	private static Vector2 ApplyStickDeadzone(Vector2 value)
	{
		return value.magnitude < 0.2f ? Vector2.zero : value;
	}

	private static float ApplyAxisDeadzone(float value)
	{
		return Mathf.Abs(value) < 0.2f ? 0f : value;
	}

	private static Vector2 GetGamepadDpadVector(Gamepad gamepad)
	{
		Vector2 value = new Vector2(
			GetDirectionalAxis(gamepad.dpad.right.isPressed, gamepad.dpad.left.isPressed),
			GetDirectionalAxis(gamepad.dpad.up.isPressed, gamepad.dpad.down.isPressed));
		return value.sqrMagnitude > 1f ? value.normalized : value;
	}

	private static Vector2 GetJoystickHatswitchVector(Joystick joystick)
	{
		if (joystick == null || joystick.hatswitch == null)
		{
			return Vector2.zero;
		}

		Vector2 value = joystick.hatswitch.ReadValue();
		return value.sqrMagnitude > 1f ? value.normalized : value;
	}

	private static bool IsJoystickButtonPressed(Joystick joystick, params string[] controlPaths)
	{
		if (joystick == null || controlPaths == null)
		{
			return false;
		}

		for (int i = 0; i < controlPaths.Length; i++)
		{
			ButtonControl button = joystick.TryGetChildControl<ButtonControl>(controlPaths[i]);
			if (button != null && button.isPressed)
			{
				return true;
			}
		}

		return false;
	}

	private static float GetDirectionalAxis(bool positive, bool negative)
	{
		if (positive == negative)
		{
			return 0f;
		}

		return positive ? 1f : -1f;
	}

	private void ResetPolledButtonState()
	{
		previousJumpPressed = false;
		previousSprintPressed = false;
		previousPreviousPressed = false;
		previousNextPressed = false;
		previousAttackPressed = false;
		previousCycleShapePressed = false;
		previousCrouchPressed = false;
	}

	private void LogPolledInputIfActive(InputDevice device, Vector2 move, Vector2 aim, bool anyButtonPressed)
	{
		if (move.sqrMagnitude < 0.04f && aim.sqrMagnitude < 0.04f && !anyButtonPressed)
		{
			return;
		}

		float now = Time.realtimeSinceStartup;
		if (now - lastPolledInputLogTime < 0.5f)
		{
			return;
		}

		lastPolledInputLogTime = now;
		Debug.Log($"{name} polled input: {device.displayName}#{device.deviceId} move={move} aim={aim} button={anyButtonPressed}", this);
	}
}
