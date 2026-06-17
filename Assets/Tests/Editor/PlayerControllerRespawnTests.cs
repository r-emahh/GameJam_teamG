using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;

public sealed class PlayerControllerRespawnTests
{
	[Test]
	public void RespawnAt_ResetsVelocityDashStunAndAppliesInputLock()
	{
		GameObject playerObject = new GameObject(nameof(RespawnAt_ResetsVelocityDashStunAndAppliesInputLock));
		try
		{
			Rigidbody2D body = playerObject.AddComponent<Rigidbody2D>();
			playerObject.AddComponent<BoxCollider2D>();
			playerObject.AddComponent<PlayerInput>();
			playerObject.AddComponent<PlayerIdentity>();
			playerObject.AddComponent<PlayerContactSensor2D>();
			PlayerMotor2D motor = playerObject.AddComponent<PlayerMotor2D>();
			PlayerDash dash = playerObject.AddComponent<PlayerDash>();
			PlayerStun stun = playerObject.AddComponent<PlayerStun>();
			playerObject.AddComponent<PlayerCannon>();
			playerObject.AddComponent<PlayerDrawing>();
			playerObject.AddComponent<BlockerRaceAttackCooldown>();
			PlayerController controller = playerObject.AddComponent<PlayerController>();

			Assert.That(dash.TryDash(Vector2.right), Is.True);
			stun.Apply(1f);
			body.linearVelocity = new Vector2(5f, -7f);

			Vector3 respawnPosition = new Vector3(3f, 4f, 0f);
			controller.RespawnAt(respawnPosition, 0.75f);

			Assert.That(body.position, Is.EqualTo((Vector2)respawnPosition));
			Assert.That(body.linearVelocity, Is.EqualTo(Vector2.zero));
			Assert.That(dash.IsDashing, Is.False);
			Assert.That(stun.IsStunned, Is.False);

			FieldInfo inputLockTimerField = typeof(PlayerMotor2D).GetField(
				"inputLockTimer",
				BindingFlags.Instance | BindingFlags.NonPublic);
			Assert.That(inputLockTimerField, Is.Not.Null);
			Assert.That((float)inputLockTimerField.GetValue(motor), Is.EqualTo(0.75f).Within(0.001f));
		}
		finally
		{
			Object.DestroyImmediate(playerObject);
		}
	}
}
