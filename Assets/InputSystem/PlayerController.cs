using UnityEditor.ShaderGraph;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerController : MonoBehaviour
{
	// 移動速度の定数
	[SerializeField]
	private float MOVESPEED_X = 3;

	// スティックの入力を保持する変数
	public Vector2 moveInput;

	// ジャンプ力
	[SerializeField]
	public float jumpPower;
	[SerializeField]
	public float walljumpPower;

	[SerializeField]
	private float disableInputTimer;

	// 慣性
	[SerializeField]
	private float acceleration = 10f;

	// 接地判定
	[SerializeField]
	public bool isGrounded;

	// 壁に触れているかを保持する変数、右と左
	[SerializeField]
	public bool isTouchingWallRight;

	// 壁のレイヤーを指定する変数
	[SerializeField]
	public bool isTouchingWallLeft;

	// 床のレイヤーを指定する変数
	[SerializeField]
	private LayerMask groundLayer;

	// Rigidbody2Dを保持する変数
	[SerializeField]
	Rigidbody2D rb2D;

	// BoxCollider2Dを保持する変数
	[SerializeField]
	BoxCollider2D boxCollider2D;

	void Awake()
	{
		if (rb2D == null)
			rb2D = GetComponent<Rigidbody2D>();

		if (boxCollider2D == null)
			boxCollider2D = GetComponent<BoxCollider2D>();

	}

	void FixedUpdate()
	{
		// 接地判定の更新
		isGrounded = Physics2D.OverlapCircle(new Vector2(boxCollider2D.bounds.center.x, boxCollider2D.bounds.min.y), 0.1f, groundLayer);
		// 右壁接触判定の更新
		isTouchingWallRight = Physics2D.OverlapBox(
			// 中心座標（キャラの中心）
			new Vector2(boxCollider2D.bounds.max.x, boxCollider2D.bounds.center.y),
			// Boxのサイズ
			new Vector2(0.3f, boxCollider2D.bounds.size.y - 0.2f),
			// 角度（回転させないので0f）
			0f,
			// 判定するレイヤー
			groundLayer
		);
		// 左壁接触判定の更新
		isTouchingWallLeft = Physics2D.OverlapBox(
			// 中心座標（キャラの中心）
			new Vector2(boxCollider2D.bounds.min.x, boxCollider2D.bounds.center.y),
			// Boxのサイズ
			new Vector2(0.3f, boxCollider2D.bounds.size.y - 0.2f),
			// 角度（回転させないので0f）
			0f,
			// 判定するレイヤー
			groundLayer
		);

		if (disableInputTimer > 0)
		{
			// 時間を減らす
			disableInputTimer -= Time.fixedDeltaTime;
			// 目標とする速度を計算
			float targetSpeedX = moveInput.x * MOVESPEED_X;
			// 現在の速度から目標速度へ、accelerationのペースで徐々に変化させる
			rb2D.linearVelocityX = Mathf.MoveTowards(rb2D.linearVelocityX, targetSpeedX, acceleration * Time.fixedDeltaTime);
		}
		else
		{
			// 目標とする速度を計算
			float targetSpeedX = moveInput.x * MOVESPEED_X;
			// 現在の速度から目標速度へ、accelerationのペースで徐々に変化させる
			rb2D.linearVelocityX = Mathf.MoveTowards(rb2D.linearVelocityX, targetSpeedX, acceleration * Time.fixedDeltaTime);
		}
	}

	void OnMove(InputValue inputValue)
	{
		moveInput = inputValue.Get<Vector2>();
	}

	void OnJump(InputValue inputValue)
	{
		// 接地していたらジャンプ可能
		if (inputValue.isPressed != false && isGrounded)
		{
			rb2D.linearVelocityY = jumpPower;
		}

		// 壁ジャンプ右
		else if (isTouchingWallRight && !isGrounded)
		{
			rb2D.linearVelocity = new Vector2(-walljumpPower, walljumpPower);
			disableInputTimer += 0.2f;
		}

		// 壁ジャンプ左
		else if (isTouchingWallLeft && !isGrounded)
		{
			rb2D.linearVelocity = new Vector2(walljumpPower, walljumpPower);
			disableInputTimer += 0.2f;
		}
	}
}
