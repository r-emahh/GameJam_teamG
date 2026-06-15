using NUnit.Framework;
using UnityEngine;

public sealed class GoalZoneTriggerTests
{
	[Test]
	public void TryGetPlayerSide_RejectsUnrelatedCollider()
	{
		GameObject unrelatedObject = new GameObject("UnrelatedCollider");
		try
		{
			Collider2D collider = unrelatedObject.AddComponent<BoxCollider2D>();

			Assert.That(GoalZoneTrigger.TryGetPlayerSide(collider, out _), Is.False);
		}
		finally
		{
			Object.DestroyImmediate(unrelatedObject);
		}
	}

	[TestCase(MatchSide.GoalRunner)]
	[TestCase(MatchSide.Blocker)]
	public void TryGetPlayerSide_ReturnsParentPlayerRole(MatchSide expectedSide)
	{
		GameObject playerObject = new GameObject("Player");
		try
		{
			PlayerIdentity identity = playerObject.AddComponent<PlayerIdentity>();
			identity.AssignSide(expectedSide);
			GameObject colliderObject = new GameObject("PlayerCollider");
			colliderObject.transform.SetParent(playerObject.transform);
			Collider2D collider = colliderObject.AddComponent<BoxCollider2D>();

			bool found = GoalZoneTrigger.TryGetPlayerSide(collider, out MatchSide actualSide);

			Assert.That(found, Is.True);
			Assert.That(actualSide, Is.EqualTo(expectedSide));
		}
		finally
		{
			Object.DestroyImmediate(playerObject);
		}
	}
}
