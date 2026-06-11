using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(BoxCollider2D))]
// 足元と壁側の接触を物理問い合わせで監視する。
public sealed class PlayerContactSensor2D : MonoBehaviour
{
	// 接地判定に使うレイヤーを指定する。
	[SerializeField]
	private LayerMask groundLayer;

	// 接地判定の半径を指定する。
	[SerializeField, Min(0.01f)]
	private float groundProbeRadius = 0.1f;

	// 壁判定のサイズを指定する。
	[SerializeField]
	private Vector2 wallProbeSize = new Vector2(0.3f, 0.2f);

	// 本体コライダーを保持する。
	private BoxCollider2D bodyCollider;

	// 現在接地しているかを返す。
	public bool IsGrounded { get; private set; }
	// 右壁に接しているかを返す。
	public bool IsTouchingWallRight { get; private set; }
	// 左壁に接しているかを返す。
	public bool IsTouchingWallLeft { get; private set; }

	// コライダー参照をキャッシュする。
	private void Awake()
	{
		bodyCollider = GetComponent<BoxCollider2D>();
	}

	// 現在の当たり判定状態を更新する。
	public void Refresh()
	{
		Bounds bounds = bodyCollider.bounds;
		IsGrounded = Physics2D.OverlapCircle(new Vector2(bounds.center.x, bounds.min.y), groundProbeRadius, groundLayer);

		Vector2 probeSize = new Vector2(wallProbeSize.x, Mathf.Max(wallProbeSize.y, bounds.size.y - 0.2f));
		IsTouchingWallRight = Physics2D.OverlapBox(new Vector2(bounds.max.x, bounds.center.y), probeSize, 0f, groundLayer);
		IsTouchingWallLeft = Physics2D.OverlapBox(new Vector2(bounds.min.x, bounds.center.y), probeSize, 0f, groundLayer);
	}
}
