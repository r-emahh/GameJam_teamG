using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Rigidbody2D))]
// 水平移動、ジャンプ、入力ロックを扱う。
public sealed class PlayerMotor2D : MonoBehaviour
{
	// 入力停止とは別に、外部要因で物理移動を止める理由を表す。
	[System.Flags]
	public enum MovementLockSource
	{
		None = 0,
		DrawPhase = 1 << 0,
		RaceSuppressed = 1 << 1,
		PlacePhase = 1 << 2
	}

	// 水平移動速度を設定する。
	[SerializeField]
	private float moveSpeedX = 8f;

	// 加速量を設定する。
	[SerializeField]
	private float acceleration = 10f;

	// 通常ジャンプの強さを設定する。
	[SerializeField]
	private float jumpPower = 10f;

	// 壁ジャンプの強さを設定する。
	[SerializeField]
	private float wallJumpPower = 20f;

	// 壁ジャンプ後の入力ロック時間を設定する。
	[SerializeField]
	private float wallJumpInputLockDuration = 0.2f;

	// Rigidbody を保持する。
	private Rigidbody2D body;
	// 解除前の制約を保持する。
	private RigidbodyConstraints2D defaultConstraints;
	// 残り入力ロック時間を保持する。
	private float inputLockTimer;
	// 現在有効な移動ロック理由を保持する。
	private MovementLockSource movementLockSources;

	// 現在の移動入力を保持する。
	public Vector2 MoveInput { get; private set; }

	// Rigidbody をキャッシュする。
	private void Awake()
	{
		EnsureBody();
	}

	// 壁ジャンプ由来の短期入力ロックは他状態と独立して進める。
	private void FixedUpdate()
	{
		if (inputLockTimer > 0f)
		{
			inputLockTimer = Mathf.Max(0f, inputLockTimer - Time.fixedDeltaTime);
		}
	}

	// 移動入力を更新する。
	public void SetMoveInput(Vector2 input)
	{
		MoveInput = input;
	}

	// 固定更新で水平移動を反映する。
	public void TickFixed(PlayerContactSensor2D contacts, bool movementOverridden)
	{
		if (IsMovementLocked)
		{
			return;
		}

		if (inputLockTimer > 0f)
		{
			return;
		}

		if (movementOverridden)
		{
			return;
		}

		EnsureBody();
		float targetSpeed = MoveInput.x * moveSpeedX;
		float nextSpeed = Mathf.MoveTowards(body.linearVelocity.x, targetSpeed, acceleration * Time.fixedDeltaTime);
		body.linearVelocity = new Vector2(nextSpeed, body.linearVelocity.y);
	}

	// 接地または壁接触時のジャンプを試みる。
	public bool TryJump(PlayerContactSensor2D contacts)
	{
		if (IsMovementLocked)
		{
			return false;
		}

		if (inputLockTimer > 0f)
		{
			return false;
		}

		EnsureBody();
		if (contacts.IsGrounded)
		{
			body.linearVelocity = new Vector2(body.linearVelocity.x, jumpPower);
			return true;
		}

		if (contacts.IsTouchingWallRight)
		{
			body.linearVelocity = new Vector2(-wallJumpPower, wallJumpPower);
			LockInput(wallJumpInputLockDuration);
			return true;
		}

		if (contacts.IsTouchingWallLeft)
		{
			body.linearVelocity = new Vector2(wallJumpPower, wallJumpPower);
			LockInput(wallJumpInputLockDuration);
			return true;
		}

		return false;
	}

	// 指定時間だけ入力を止める。
	public void LockInput(float duration)
	{
		inputLockTimer = Mathf.Max(inputLockTimer, duration);
	}

	// 速度を完全に止める。
	public void Stop()
	{
		EnsureBody();
		body.linearVelocity = Vector2.zero;
	}

	// ラウンド切り替えに備えて入力ロックと速度を初期化する。
	public void ResetForNextRound()
	{
		inputLockTimer = 0f;
		movementLockSources = MovementLockSource.None;
		ApplyMovementLockState();
		Stop();
		EnsureBody();
		body.angularVelocity = 0f;
	}

	// 移動を完全にロックまたは解除する。
	public void SetMovementLocked(MovementLockSource source, bool locked)
	{
		EnsureBody();
		movementLockSources = locked
			? movementLockSources | source
			: movementLockSources & ~source;
		ApplyMovementLockState();
	}

	// 完全ロック中かを返す。
	public bool IsMovementLocked => movementLockSources != MovementLockSource.None;

	// 現在のロック理由に合わせて Rigidbody 制約を更新する。
	private void ApplyMovementLockState()
	{
		EnsureBody();
		if (IsMovementLocked)
		{
			body.linearVelocity = Vector2.zero;
			body.angularVelocity = 0f;
			body.constraints = RigidbodyConstraints2D.FreezeAll;
			return;
		}

		body.constraints = defaultConstraints;
	}

	private void EnsureBody()
	{
		if (body != null)
		{
			return;
		}

		body = GetComponent<Rigidbody2D>();
		defaultConstraints = body != null ? body.constraints : RigidbodyConstraints2D.None;
	}
}
