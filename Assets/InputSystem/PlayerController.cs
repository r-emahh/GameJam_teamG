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
	public float JumpPower;

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

	[SerializeField]
	private LayerMask WallLayer;

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
			new Vector2(boxCollider2D.bounds.max.x, boxCollider2D.bounds.center.y),           // 1. 中心座標（キャラの中心）
			new Vector2(0.3f, boxCollider2D.bounds.size.y - 0.2f),  // 2. Boxのサイズ
			0f,                                    // 3. 角度（回転させないので0f）
			WallLayer                            // 4. 判定するレイヤー
		);
		// 左壁接触判定の更新
		isTouchingWallLeft = Physics2D.OverlapBox(
			new Vector2(boxCollider2D.bounds.min.x, boxCollider2D.bounds.center.y),  // 1. 中心座標（キャラの中心）
			new Vector2(0.3f, boxCollider2D.bounds.size.y - 0.2f),  // 2. Boxのサイズ
			0f,                                    // 3. 角度（回転させないので0f）
			WallLayer                            // 4. 判定するレイヤー
		);

		rb2D.linearVelocityX = moveInput.x * MOVESPEED_X;
	}

	void OnMove(InputValue inputValue)
	{
		moveInput = inputValue.Get<Vector2>();
	}

	void OnJump(InputValue inputValue)
	{
		// 接地していたらジャンプ可能
		if (inputValue.isPressed != false && isGrounded != false)
		{
			rb2D.linearVelocityY = JumpPower;
		}

		else if (isTouchingWallRight)
		{
			rb2D.linearVelocityY = JumpPower;
		}

		else if (isTouchingWallLeft)
		{
			rb2D.linearVelocityY = JumpPower;
		}
	}
}
