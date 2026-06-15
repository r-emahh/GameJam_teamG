using System.Reflection;
using NUnit.Framework;
using UnityEngine;

public sealed class PlayerCannonPowerTests
{
	[Test]
	public void LaunchPower_OscillatesBetweenConfiguredLimits()
	{
		GameObject playerObject = new GameObject(nameof(LaunchPower_OscillatesBetweenConfiguredLimits));
		try
		{
			PlayerCannon cannon = playerObject.AddComponent<PlayerCannon>();
			SetPrivateField(cannon, "minimumLaunchPower", 5f);
			SetPrivateField(cannon, "maximumLaunchPower", 15f);
			SetPrivateField(cannon, "launchPowerSweepSpeed", 10f);
			InvokePrivate(cannon, "ResetLaunchPower");

			InvokePrivate(cannon, "TickLaunchPower", 1f);
			Assert.That(cannon.CurrentLaunchPower, Is.EqualTo(15f).Within(0.001f));

			InvokePrivate(cannon, "TickLaunchPower", 0.5f);
			Assert.That(cannon.CurrentLaunchPower, Is.EqualTo(10f).Within(0.001f));

			InvokePrivate(cannon, "TickLaunchPower", 0.5f);
			Assert.That(cannon.CurrentLaunchPower, Is.EqualTo(5f).Within(0.001f));
		}
		finally
		{
			Object.DestroyImmediate(playerObject);
		}
	}

	[Test]
	public void InitializeDrawingProjectile_UsesAdoptedLaunchPowerAsInitialSpeed()
	{
		GameObject projectileObject = new GameObject(nameof(InitializeDrawingProjectile_UsesAdoptedLaunchPowerAsInitialSpeed));
		try
		{
			Rigidbody2D body = projectileObject.AddComponent<Rigidbody2D>();
			CannonProjectile projectile = projectileObject.AddComponent<CannonProjectile>();
			SetPrivateField(projectile, "hasDrawingArtifact", true);
			projectileObject.transform.rotation = Quaternion.Euler(0f, 0f, 90f);

			projectile.Initialize(null, false, 12.5f);

			Assert.That(body.linearVelocity.x, Is.EqualTo(0f).Within(0.001f));
			Assert.That(body.linearVelocity.y, Is.EqualTo(12.5f).Within(0.001f));
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

	private static void InvokePrivate(object target, string methodName, params object[] arguments)
	{
		MethodInfo method = target.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic);
		Assert.That(method, Is.Not.Null, $"Missing method: {methodName}");
		method.Invoke(target, arguments);
	}
}
