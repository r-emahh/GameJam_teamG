using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;

public sealed class PlayerControllerAimTests
{
	[Test]
	public void ProcessAim_KeepsVerticalOnlyStickInputAsWorldDirection()
	{
		GameObject playerObject = CreatePlayerObject(nameof(ProcessAim_KeepsVerticalOnlyStickInputAsWorldDirection));
		try
		{
			PlayerController controller = playerObject.GetComponent<PlayerController>();
			PlayerCannon cannon = playerObject.GetComponent<PlayerCannon>();

			InvokeProcessAim(controller, Vector2.up);

			Assert.That(GetPrivateField<float>(cannon, "aimInput"), Is.EqualTo(0f));
			Assert.That(GetPrivateField<Vector2>(cannon, "aimDirectionInput"), Is.EqualTo(Vector2.up));
		}
		finally
		{
			Object.DestroyImmediate(playerObject);
		}
	}

	[Test]
	public void ProcessAim_KeepsDirectionalStickInputAsWorldDirection()
	{
		GameObject playerObject = CreatePlayerObject(nameof(ProcessAim_KeepsDirectionalStickInputAsWorldDirection));
		try
		{
			PlayerController controller = playerObject.GetComponent<PlayerController>();
			PlayerCannon cannon = playerObject.GetComponent<PlayerCannon>();
			Vector2 aimDirection = new Vector2(-0.5f, -0.5f);

			InvokeProcessAim(controller, aimDirection);

			Assert.That(GetPrivateField<float>(cannon, "aimInput"), Is.EqualTo(0f));
			Assert.That(GetPrivateField<Vector2>(cannon, "aimDirectionInput"), Is.EqualTo(aimDirection));
		}
		finally
		{
			Object.DestroyImmediate(playerObject);
		}
	}

	private static GameObject CreatePlayerObject(string objectName)
	{
		GameObject playerObject = new GameObject(objectName);
		playerObject.AddComponent<Rigidbody2D>();
		playerObject.AddComponent<BoxCollider2D>();
		playerObject.AddComponent<PlayerInput>();
		playerObject.AddComponent<PlayerIdentity>();
		playerObject.AddComponent<PlayerContactSensor2D>();
		playerObject.AddComponent<PlayerMotor2D>();
		playerObject.AddComponent<PlayerDash>();
		playerObject.AddComponent<PlayerStun>();
		playerObject.AddComponent<PlayerCannon>();
		playerObject.AddComponent<PlayerDrawing>();
		playerObject.AddComponent<BlockerRaceAttackCooldown>();
		playerObject.AddComponent<PlayerController>();
		return playerObject;
	}

	private static void InvokeProcessAim(PlayerController controller, Vector2 value)
	{
		MethodInfo method = typeof(PlayerController).GetMethod(
			"ProcessAim",
			BindingFlags.Instance | BindingFlags.NonPublic);
		Assert.That(method, Is.Not.Null);
		method.Invoke(controller, new object[] { value });
	}

	private static T GetPrivateField<T>(object target, string fieldName)
	{
		FieldInfo field = target.GetType().GetField(
			fieldName,
			BindingFlags.Instance | BindingFlags.NonPublic);
		Assert.That(field, Is.Not.Null);
		return (T)field.GetValue(target);
	}
}
