using NUnit.Framework;
using UnityEngine;

public sealed class PlayerStunTests
{
	[Test]
	public void Apply_UsesLongerRemainingDurationAndDoesNotShortenActiveStun()
	{
		GameObject playerObject = new GameObject(nameof(Apply_UsesLongerRemainingDurationAndDoesNotShortenActiveStun));
		try
		{
			playerObject.AddComponent<Rigidbody2D>();
			playerObject.AddComponent<PlayerMotor2D>();
			playerObject.AddComponent<PlayerDash>();
			PlayerStun stun = playerObject.AddComponent<PlayerStun>();

			stun.Apply(2f);
			stun.Tick(0.75f);
			stun.Apply(0.5f);

			Assert.That(stun.IsStunned, Is.True);
			Assert.That(stun.RemainingDuration, Is.EqualTo(1.25f).Within(0.001f));

			stun.Apply(3f);

			Assert.That(stun.RemainingDuration, Is.EqualTo(3f).Within(0.001f));
		}
		finally
		{
			Object.DestroyImmediate(playerObject);
		}
	}

	[Test]
	public void Apply_StopsVelocityAndCancelsDash()
	{
		GameObject playerObject = new GameObject(nameof(Apply_StopsVelocityAndCancelsDash));
		try
		{
			Rigidbody2D body = playerObject.AddComponent<Rigidbody2D>();
			playerObject.AddComponent<PlayerMotor2D>();
			PlayerDash dash = playerObject.AddComponent<PlayerDash>();
			PlayerStun stun = playerObject.AddComponent<PlayerStun>();

			Assert.That(dash.TryDash(Vector2.right), Is.True);
			Assert.That(dash.IsDashing, Is.True);

			body.linearVelocity = new Vector2(5f, 3f);
			stun.Apply(1f);

			Assert.That(body.linearVelocity, Is.EqualTo(Vector2.zero));
			Assert.That(dash.IsDashing, Is.False);
		}
		finally
		{
			Object.DestroyImmediate(playerObject);
		}
	}

	[Test]
	public void Tick_ExpiresStunAndRestoresState()
	{
		GameObject playerObject = new GameObject(nameof(Tick_ExpiresStunAndRestoresState));
		try
		{
			playerObject.AddComponent<Rigidbody2D>();
			playerObject.AddComponent<PlayerMotor2D>();
			playerObject.AddComponent<PlayerDash>();
			PlayerStun stun = playerObject.AddComponent<PlayerStun>();

			stun.Apply(0.5f);
			stun.Tick(0.6f);

			Assert.That(stun.IsStunned, Is.False);
			Assert.That(stun.RemainingDuration, Is.Zero);
		}
		finally
		{
			Object.DestroyImmediate(playerObject);
		}
	}
}
