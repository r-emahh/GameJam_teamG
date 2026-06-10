using UnityEngine;

public class CannonProjectile : MonoBehaviour
{
	private PlayerController owner;
	private Rigidbody2D rb2D;
	private bool isStunProjectile;
	private bool isStopped;
	private float lifeTimer = 12f;

	public void Initialize(PlayerController owner, Rigidbody2D rigidbody2D, bool stunProjectile)
	{
		this.owner = owner;
		rb2D = rigidbody2D;
		isStunProjectile = stunProjectile;

		rb2D.linearVelocity = transform.right * (isStunProjectile ? 14f : 10f);
		rb2D.angularVelocity = isStunProjectile ? 300f : 180f;
	}

	private void Update()
	{
		if (isStopped)
		{
			return;
		}

		lifeTimer -= Time.deltaTime;
		if (lifeTimer <= 0f)
		{
			Destroy(gameObject);
		}
	}

	private void OnCollisionEnter2D(Collision2D collision)
	{
		if (isStopped)
		{
			return;
		}

		PlayerController hitPlayer = collision.collider.GetComponentInParent<PlayerController>();
		if (isStunProjectile && hitPlayer != null && hitPlayer != owner)
		{
			hitPlayer.ApplyStun(2f);
			Destroy(gameObject);
			return;
		}

		StopProjectile();
	}

	public void StopProjectile()
	{
		if (isStopped)
		{
			return;
		}

		isStopped = true;
		if (rb2D != null)
		{
			rb2D.linearVelocity = Vector2.zero;
			rb2D.angularVelocity = 0f;
			rb2D.bodyType = RigidbodyType2D.Static;
		}

		if (!isStunProjectile)
		{
			gameObject.name = "PlacedDrawing";
		}

		if (owner != null)
		{
			owner.ClearProjectileReference(this);
		}
	}

	private void OnDestroy()
	{
		if (owner != null)
		{
			owner.ClearProjectileReference(this);
		}
	}
}
