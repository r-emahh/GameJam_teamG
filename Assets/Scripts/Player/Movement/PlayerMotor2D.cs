using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Rigidbody2D))]
// 水平移動、ジャンプ、入力ロックを扱う。
public sealed class PlayerMotor2D : MonoBehaviour
{
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

	// 現在の移動入力を保持する。
	public Vector2 MoveInput { get; private set; }

	// Rigidbody をキャッシュする。
	private void Awake()
	{
		body = GetComponent<Rigidbody2D>();
		defaultConstraints = body.constraints;
	}

	// 移動入力を更新する。
	public void SetMoveInput(Vector2 input)
	{
		MoveInput = input;
	}

	// 固定更新で水平移動を反映する。
	public void TickFixed(PlayerContactSensor2D contacts, bool movementOverridden)
	{
		if (inputLockTimer > 0f)
		{
			inputLockTimer = Mathf.Max(0f, inputLockTimer - Time.fixedDeltaTime);
			return;
		}

		if (movementOverridden)
		{
			return;
		}

		float targetSpeed = MoveInput.x * moveSpeedX;
		float nextSpeed = Mathf.MoveTowards(body.linearVelocity.x, targetSpeed, acceleration * Time.fixedDeltaTime);
		body.linearVelocity = new Vector2(nextSpeed, body.linearVelocity.y);
	}

	// 接地または壁接触時のジャンプを試みる。
	public bool TryJump(PlayerContactSensor2D contacts)
	{
		if (inputLockTimer > 0f)
		{
			return false;
		}

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
		body.linearVelocity = Vector2.zero;
	}

	// 移動を完全にロックまたは解除する。
	public void SetMovementLocked(bool locked)
	{
		if (body == null)
		{
			return;
		}

		if (locked)
		{
			body.linearVelocity = Vector2.zero;
			body.angularVelocity = 0f;
			body.constraints = RigidbodyConstraints2D.FreezeAll;
			return;
		}

		body.constraints = defaultConstraints;
	}
}
