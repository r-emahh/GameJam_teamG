using UnityEngine;
using UnityEngine.InputSystem;


public class PlayerController : MonoBehaviour
{
	// 移動速度の定数
	[SerializeField]
	private float MOVESPEED_X = 3;

	// スティックの入力を保持する変数
	public Vector2 moveInput;
	[SerializeField]
	Rigidbody2D rb2D;

	void Awake()
	{
		if (rb2D == null) 
		rb2D = GetComponent<Rigidbody2D>();
	}

	void FixedUpdate()
	{
		rb2D.linearVelocityX = moveInput.x * MOVESPEED_X;
	}

    void OnMove(InputValue inputValue)
	{
		moveInput = inputValue.Get<Vector2>();
	}

}
