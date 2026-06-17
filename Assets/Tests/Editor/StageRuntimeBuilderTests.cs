using NUnit.Framework;
using UnityEngine;
using UnityEngine.Tilemaps;

public sealed class StageRuntimeBuilderTests
{
	[Test]
	public void Build_CreatesTilemapStageWithoutPreexistingComponents()
	{
		GameObject cameraObject = new GameObject("TestCamera");
		GameObject builderObject = new GameObject("StageRuntimeBuilderTest");
		try
		{
			cameraObject.AddComponent<Camera>();
			StageRuntimeBuilder builder = builderObject.AddComponent<StageRuntimeBuilder>();

			Assert.DoesNotThrow(() => builder.Build());

			GameObject gridObject = GameObject.Find("StageGrid");
			GameObject tilemapObject = GameObject.Find("StageTilemap");
			Assert.That(gridObject, Is.Not.Null);
			Assert.That(gridObject.GetComponent<Grid>(), Is.Not.Null);
			Assert.That(tilemapObject, Is.Not.Null);
			Assert.That(tilemapObject.GetComponent<Tilemap>(), Is.Not.Null);
			Assert.That(tilemapObject.GetComponent<TilemapRenderer>(), Is.Not.Null);
			Assert.That(tilemapObject.GetComponent<TilemapCollider2D>(), Is.Not.Null);
			Assert.That(tilemapObject.GetComponent<CompositeCollider2D>(), Is.Not.Null);
			Assert.That(tilemapObject.GetComponent<Rigidbody2D>(), Is.Not.Null);

			GameObject goalRunnerSpawn = GameObject.Find(PlayerSpawnCoordinator.GoalRunnerSpawnName);
			GameObject blockerSpawn = GameObject.Find(PlayerSpawnCoordinator.BlockerSpawnName);
			float floorTopY = tilemapObject.transform.position.y + 0.5f;
			Assert.That(goalRunnerSpawn, Is.Not.Null);
			Assert.That(blockerSpawn, Is.Not.Null);
			Assert.That(goalRunnerSpawn.transform.position.y, Is.GreaterThanOrEqualTo(floorTopY + 0.55f));
			Assert.That(blockerSpawn.transform.position.y, Is.GreaterThanOrEqualTo(floorTopY + 0.55f));
		}
		finally
		{
			Object.DestroyImmediate(builderObject);
			Object.DestroyImmediate(cameraObject);

			GameObject gridObject = GameObject.Find("StageGrid");
			if (gridObject != null)
			{
				Object.DestroyImmediate(gridObject);
			}

			GameObject tilemapObject = GameObject.Find("StageTilemap");
			if (tilemapObject != null)
			{
				Object.DestroyImmediate(tilemapObject);
			}

			GameObject stageSurfaceObject = GameObject.Find("StageSurface");
			if (stageSurfaceObject != null)
			{
				Object.DestroyImmediate(stageSurfaceObject);
			}

			GameObject goalZoneObject = GameObject.Find("GoalZone");
			if (goalZoneObject != null)
			{
				Object.DestroyImmediate(goalZoneObject);
			}

			GameObject goalZoneVisualObject = GameObject.Find("GoalZone_Visual");
			if (goalZoneVisualObject != null)
			{
				Object.DestroyImmediate(goalZoneVisualObject);
			}

			GameObject stageTopObject = GameObject.Find("StageTop");
			if (stageTopObject != null)
			{
				Object.DestroyImmediate(stageTopObject);
			}

			GameObject stageBottomObject = GameObject.Find("StageBottom");
			if (stageBottomObject != null)
			{
				Object.DestroyImmediate(stageBottomObject);
			}

			GameObject cannonObject = GameObject.Find("Cannon_TopCenter");
			if (cannonObject != null)
			{
				Object.DestroyImmediate(cannonObject);
			}

			GameObject goalRunnerSpawnObject = GameObject.Find(PlayerSpawnCoordinator.GoalRunnerSpawnName);
			if (goalRunnerSpawnObject != null)
			{
				Object.DestroyImmediate(goalRunnerSpawnObject);
			}

			GameObject blockerSpawnObject = GameObject.Find(PlayerSpawnCoordinator.BlockerSpawnName);
			if (blockerSpawnObject != null)
			{
				Object.DestroyImmediate(blockerSpawnObject);
			}
		}
	}
}
