using NUnit.Framework;
using UnityEngine;

public sealed class BlockerRaceAttackCooldownTests
{
	[Test]
	public void TryBeginCooldown_BlocksRepeatedUseUntilReset()
	{
		GameObject gameObject = new GameObject(nameof(BlockerRaceAttackCooldownTests));
		try
		{
			BlockerRaceAttackCooldown cooldown = gameObject.AddComponent<BlockerRaceAttackCooldown>();

			Assert.That(cooldown.IsReady, Is.True);
			Assert.That(cooldown.TryBeginCooldown(), Is.True);
			Assert.That(cooldown.IsReady, Is.False);
			Assert.That(cooldown.RemainingTime, Is.EqualTo(cooldown.Duration));
			Assert.That(cooldown.TryBeginCooldown(), Is.False);

			cooldown.ResetCooldown();

			Assert.That(cooldown.IsReady, Is.True);
			Assert.That(cooldown.RemainingTime, Is.Zero);
		}
		finally
		{
			Object.DestroyImmediate(gameObject);
		}
	}
}
