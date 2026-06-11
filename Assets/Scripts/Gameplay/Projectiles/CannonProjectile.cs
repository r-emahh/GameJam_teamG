using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Rigidbody2D), typeof(CircleCollider2D))]
// 描画用とスタン用の弾の挙動をまとめる。
public sealed class CannonProjectile : MonoBehaviour
{
	// 描画弾の速度を設定する。
	[SerializeField]
	private float drawingSpeed = 10f;

	// スタン弾の速度を設定する。
	[SerializeField]
	private float stunSpeed = 14f;

	// スタン時間を設定する。
	[SerializeField]
	private float stunDuration = 2f;

	// 弾の寿命を設定する。
	[SerializeField]
	private float lifetime = 12f;

	// 設置用の描画弾の見た目スケールを設定する。
	[SerializeField]
	private float drawingProjectileScale = 1f;

	// 妨害用の小さい弾の見た目スケールを設定する。
	[SerializeField]
	private float stunProjectileScale = 0.65f;

	// 設置用の当たり判定半径を設定する。
	[SerializeField]
	private float drawingProjectileRadius = 0.18f;

	// 妨害用の当たり判定半径を設定する。
	[SerializeField]
	private float stunProjectileRadius = 0.12f;

	// 発射元の大砲を保持する。
	private PlayerCannon owner;
	// 物理挙動を制御する。
	private Rigidbody2D body;
	// スタン用弾かどうかを記録する。
	private bool isStunProjectile;
	// 停止済みかどうかを記録する。
	private bool isStopped;

	// 実行時生成の弾オブジェクトを作る。
	public static CannonProjectile CreateRuntime(Vector3 position, Quaternion rotation, bool stunProjectile)
	{
		GameObject projectileObject = new GameObject(stunProjectile ? "StunProjectile" : "DrawingProjectile");
		projectileObject.transform.SetPositionAndRotation(position, rotation);

		SpriteRenderer renderer = projectileObject.AddComponent<SpriteRenderer>();
		renderer.sprite = RuntimeSpriteFactory.UnitSquare;
		renderer.color = stunProjectile ? new Color(1f, 0.35f, 0.35f, 1f) : new Color(0.95f, 0.95f, 0.95f, 1f);
		renderer.sortingOrder = 2;

		Rigidbody2D rigidbody = projectileObject.AddComponent<Rigidbody2D>();
		rigidbody.gravityScale = 1.4f;
		rigidbody.angularDamping = 0.05f;
		rigidbody.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
		projectileObject.AddComponent<CircleCollider2D>().radius = stunProjectile ? 0.12f : 0.18f;
		projectileObject.transform.localScale = Vector3.one * (stunProjectile ? 0.65f : 1f);
		return projectileObject.AddComponent<CannonProjectile>();
	}

	// Rigidbody を取得する。
	private void Awake()
	{
		body = GetComponent<Rigidbody2D>();
	}

	// 発射元と弾種を設定し、初速を与える。
	public void Initialize(PlayerCannon projectileOwner, bool stunProjectile)
	{
		owner = projectileOwner;
		isStunProjectile = stunProjectile;
		if (!body)
		{
			body = GetComponent<Rigidbody2D>();
		}

		float scale = isStunProjectile ? stunProjectileScale : drawingProjectileScale;
		transform.localScale = Vector3.one * scale;
		CircleCollider2D collider = GetComponent<CircleCollider2D>();
		if (collider != null)
		{
			collider.radius = isStunProjectile ? stunProjectileRadius : drawingProjectileRadius;
		}

		body.linearVelocity = transform.right * (isStunProjectile ? stunSpeed : drawingSpeed);
		body.angularVelocity = isStunProjectile ? 300f : 180f;
	}

	// 寿命切れで自動破棄する。
	private void Update()
	{
		if (isStopped)
		{
			return;
		}

		lifetime -= Time.deltaTime;
		if (lifetime <= 0f)
		{
			Destroy(gameObject);
		}
	}

	// 衝突先に応じて停止またはスタンを行う。
	private void OnCollisionEnter2D(Collision2D collision)
	{
		if (isStopped)
		{
			return;
		}

		PlayerStun target = collision.collider.GetComponentInParent<PlayerStun>();
		if (isStunProjectile && target != null && (owner == null || target.gameObject != owner.gameObject))
		{
			target.Apply(stunDuration);
			Destroy(gameObject);
			return;
		}

		StopProjectile();
	}

	// 弾を静止状態にして設置物として扱う。
	public void StopProjectile()
	{
		if (isStopped)
		{
			return;
		}

		isStopped = true;
		body.linearVelocity = Vector2.zero;
		body.angularVelocity = 0f;
		body.bodyType = RigidbodyType2D.Static;
		if (!isStunProjectile)
		{
			gameObject.name = "PlacedDrawing";
		}

		owner?.ClearProjectileReference(this);
	}

	// 破棄時に所有者参照を外す。
	private void OnDestroy()
	{
		owner?.ClearProjectileReference(this);
	}
}
