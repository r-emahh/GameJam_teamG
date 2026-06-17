using System.Reflection;
using NUnit.Framework;
using UnityEngine;

public sealed class CannonProjectileStopTests
{
	[Test]
	public void TryStopProjectile_OnlyOwnerCanFreezeAtCurrentPoseAsGround()
	{
		GameObject ownerObject = new GameObject("Owner");
		GameObject otherObject = new GameObject("Other");
		GameObject projectileObject = new GameObject("Projectile");
		try
		{
			PlayerCannon owner = ownerObject.AddComponent<PlayerCannon>();
			PlayerCannon other = otherObject.AddComponent<PlayerCannon>();
			Rigidbody2D body = projectileObject.AddComponent<Rigidbody2D>();
			CannonProjectile projectile = projectileObject.AddComponent<CannonProjectile>();
			GameObject colliderChild = new GameObject("Collider");
			colliderChild.transform.SetParent(projectileObject.transform, false);
			colliderChild.AddComponent<BoxCollider2D>();
			SetPrivateField(projectile, "hasDrawingArtifact", true);

			projectile.Initialize(owner, false, 8f);
			body.position = new Vector2(3.25f, -1.5f);
			body.rotation = 37f;

			Assert.That(projectile.TryStopProjectile(other), Is.False);
			Assert.That(body.bodyType, Is.EqualTo(RigidbodyType2D.Dynamic));

			Assert.That(projectile.TryStopProjectile(owner), Is.True);
			Assert.That(body.bodyType, Is.EqualTo(RigidbodyType2D.Static));
			Assert.That(body.position, Is.EqualTo(new Vector2(3.25f, -1.5f)));
			Assert.That(body.rotation, Is.EqualTo(37f).Within(0.001f));
			Assert.That(body.linearVelocity, Is.EqualTo(Vector2.zero));
			Assert.That(body.angularVelocity, Is.Zero);
			Assert.That(projectileObject.layer, Is.EqualTo(LayerMask.NameToLayer("Ground")));
			Assert.That(colliderChild.layer, Is.EqualTo(LayerMask.NameToLayer("Ground")));
		}
		finally
		{
			Object.DestroyImmediate(projectileObject);
			Object.DestroyImmediate(otherObject);
			Object.DestroyImmediate(ownerObject);
		}
	}

	[Test]
	public void Initialize_AfterStopDoesNotMoveProjectileAgain()
	{
		GameObject projectileObject = new GameObject("Projectile");
		try
		{
			Rigidbody2D body = projectileObject.AddComponent<Rigidbody2D>();
			CannonProjectile projectile = projectileObject.AddComponent<CannonProjectile>();
			SetPrivateField(projectile, "hasDrawingArtifact", true);

			projectile.Initialize(null, false, 8f);
			projectile.StopProjectile();
			projectile.Initialize(null, false, 20f);

			Assert.That(body.bodyType, Is.EqualTo(RigidbodyType2D.Static));
			Assert.That(body.linearVelocity, Is.EqualTo(Vector2.zero));
			Assert.That(body.angularVelocity, Is.Zero);
		}
		finally
		{
			Object.DestroyImmediate(projectileObject);
		}
	}

	private static void SetPrivateField(object target, string fieldName, object value)
	{
		FieldInfo field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
		Assert.That(field, Is.Not.Null, $"Missing field: {fieldName}");
		field.SetValue(target, value);
	}
}
