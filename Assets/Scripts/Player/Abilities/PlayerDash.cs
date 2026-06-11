using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Rigidbody2D))]
// 短時間の高速移動とクールダウンを扱う。
public sealed class PlayerDash : MonoBehaviour
{
	// ダッシュ速度を設定する。
	[SerializeField]
	private float speed = 14f;

	// ダッシュ継続時間を設定する。
	[SerializeField]
	private float duration = 0.15f;

	// ダッシュ後の再使用待ち時間を設定する。
	[SerializeField]
	private float cooldown = 0.5f;

	// Rigidbody を保持する。
	private Rigidbody2D body;
	// 最後に向いた方向を保持する。
	private Vector2 preferredDirection = Vector2.right;
	// 残り継続時間を保持する。
	private float remainingDuration;
	// 残りクールダウンを保持する。
	private float remainingCooldown;
	// 元の重力倍率を保持する。
	private float defaultGravityScale;
	// 使用可能フラグを保持する。
	private bool isAvailable = true;

	// ダッシュ中かを返す。
	public bool IsDashing { get; private set; }

	// Rigidbody を初期化する。
	private void Awake()
	{
		body = GetComponent<Rigidbody2D>();
		defaultGravityScale = body.gravityScale;
	}

	// 入力方向から優先方向を更新する。
	public void SetPreferredDirection(Vector2 input)
	{
		if (Mathf.Abs(input.x) > 0.01f)
		{
			preferredDirection = new Vector2(Mathf.Sign(input.x), 0f);
		}
	}

	// ダッシュ開始を試みる。
	public bool TryDash(Vector2 moveInput)
	{
		if (!isAvailable || remainingCooldown > 0f || IsDashing)
		{
			return false;
		}

		SetPreferredDirection(moveInput);
		IsDashing = true;
		isAvailable = false;
		remainingDuration = duration;
		remainingCooldown = cooldown;
		body.gravityScale = 0f;
		body.linearVelocity = new Vector2(0f, body.linearVelocity.y);
		body.linearVelocity = preferredDirection * speed;
		return true;
	}

	// 固定更新でダッシュとクールダウンを進める。
	public void TickFixed(Vector2 moveInput, bool isGrounded)
	{
		SetPreferredDirection(moveInput);
		remainingCooldown = Mathf.Max(0f, remainingCooldown - Time.fixedDeltaTime);
		if (isGrounded)
		{
			isAvailable = true;
		}

		if (!IsDashing)
		{
			return;
		}

		remainingDuration -= Time.fixedDeltaTime;
		if (remainingDuration <= 0f)
		{
			FinishDash();
			return;
		}

		body.linearVelocity = preferredDirection * speed;
	}

	// 再使用可能状態へ戻す。
	public void ResetAvailability()
	{
		isAvailable = true;
		remainingCooldown = 0f;
		FinishDash();
	}

	// ダッシュ状態を終了し、重力を元に戻す。
	private void FinishDash()
	{
		IsDashing = false;
		remainingDuration = 0f;
		body.linearVelocity = new Vector2(0f, body.linearVelocity.y);
		body.gravityScale = defaultGravityScale;
	}
}
