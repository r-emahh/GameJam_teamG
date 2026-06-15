using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

public sealed class DrawingArtifactDataTests
{
	[Test]
	public void Recorder_ClampsCursorAndRecordsOnlyWhileDrawing()
	{
		DrawingArtifactData artifact = new DrawingArtifactData();
		DrawingRecorder recorder = CreateRecorder(artifact, minPointSpacing: 0.1f);

		recorder.MoveCursor(2f, 2f);
		AssertPoint(recorder.CursorPosition, 1f, 1f);
		Assert.That(artifact.PointCount, Is.Zero);

		Assert.That(recorder.SetDrawingActive(true), Is.True);
		recorder.MoveCursor(-0.5f, -0.5f);
		recorder.SetDrawingActive(false);

		Assert.That(artifact.Strokes.Count, Is.EqualTo(1));
		Assert.That(artifact.PointCount, Is.EqualTo(2));
		AssertPoint(artifact.Strokes[0].Points[0], 1f, 1f);
		AssertPoint(artifact.Strokes[0].Points[1], 0.5f, 0.5f);
	}

	[Test]
	public void Recorder_EnforcesMinimumPointSpacing()
	{
		DrawingArtifactData artifact = new DrawingArtifactData();
		DrawingRecorder recorder = CreateRecorder(artifact, minPointSpacing: 0.5f);
		recorder.SetDrawingActive(true);

		Assert.That(recorder.MoveCursor(0.2f, 0f), Is.False);
		Assert.That(recorder.MoveCursor(0.3f, 0f), Is.True);

		Assert.That(artifact.PointCount, Is.EqualTo(2));
		Assert.That(artifact.TotalLineLength, Is.EqualTo(0.5f).Within(0.0001f));
	}

	[Test]
	public void ReleasingAndPressingDraw_CreatesSeparateStrokes()
	{
		DrawingArtifactData artifact = new DrawingArtifactData();
		DrawingRecorder recorder = CreateRecorder(artifact);

		recorder.SetDrawingActive(true);
		recorder.MoveCursor(0.5f, 0f);
		recorder.SetDrawingActive(false);
		recorder.MoveCursor(0f, 0.5f);
		recorder.SetDrawingActive(true);
		recorder.MoveCursor(-0.5f, 0f);
		recorder.SetDrawingActive(false);

		Assert.That(artifact.Strokes.Count, Is.EqualTo(2));
		Assert.That(artifact.Strokes[0].PointCount, Is.EqualTo(2));
		Assert.That(artifact.Strokes[1].PointCount, Is.EqualTo(2));
	}

	[Test]
	public void Artifact_EnforcesPointAndTotalLineLengthLimits()
	{
		DrawingArtifactData artifact = new DrawingArtifactData();
		DrawingRecorder recorder = CreateRecorder(
			artifact,
			maxPointCount: 3,
			maxTotalLineLength: 1.5f,
			minPointSpacing: 0.1f,
			bounds: new DrawingRectData(-10f, -10f, 10f, 10f));
		recorder.SetDrawingActive(true);

		Assert.That(recorder.MoveCursor(1f, 0f), Is.True);
		Assert.That(recorder.MoveCursor(1f, 0f), Is.True);
		Assert.That(recorder.MoveCursor(1f, 0f), Is.False);

		Assert.That(artifact.PointCount, Is.EqualTo(3));
		Assert.That(artifact.TotalLineLength, Is.EqualTo(1.5f).Within(0.0001f));
		AssertPoint(artifact.Strokes[0].Points[2], 1.5f, 0f);
	}

	[Test]
	public void Confirm_BlocksFurtherDrawingUntilClear()
	{
		DrawingArtifactData artifact = new DrawingArtifactData();
		DrawingRecorder recorder = CreateRecorder(artifact);
		recorder.SetDrawingActive(true);
		recorder.MoveCursor(0.5f, 0f);

		recorder.Confirm();

		Assert.That(artifact.IsConfirmed, Is.True);
		Assert.That(recorder.SetDrawingActive(true), Is.False);
		Assert.That(artifact.PointCount, Is.EqualTo(2));

		recorder.Clear();
		Assert.That(artifact.IsConfirmed, Is.False);
		Assert.That(artifact.PointCount, Is.Zero);
		Assert.That(recorder.SetDrawingActive(true), Is.True);
	}

	[Test]
	public void ArtifactSet_KeepsTwoPlayersSeparate()
	{
		DrawingArtifactSetData artifacts = new DrawingArtifactSetData();
		DrawingLimitsData limits = new DrawingLimitsData(10, 10f, 0.1f);

		artifacts.Get(DrawingPlayerSlot.PlayerOne).BeginStroke(new DrawingPointData(0f, 0f), limits);
		artifacts.Get(DrawingPlayerSlot.PlayerTwo).BeginStroke(new DrawingPointData(1f, 1f), limits);
		artifacts.Get(DrawingPlayerSlot.PlayerTwo).TryAppendPoint(new DrawingPointData(2f, 1f), limits);

		Assert.That(artifacts.Get(DrawingPlayerSlot.PlayerOne).PointCount, Is.EqualTo(1));
		Assert.That(artifacts.Get(DrawingPlayerSlot.PlayerTwo).PointCount, Is.EqualTo(2));
		Assert.That(artifacts.Get(DrawingPlayerSlot.PlayerOne), Is.Not.SameAs(artifacts.Get(DrawingPlayerSlot.PlayerTwo)));
	}

	[Test]
	public void DrawingSurface_ReusesLineRendererWhenPointsAreAppended()
	{
		GameObject surfaceObject = new GameObject("DrawingSurfaceTest");
		try
		{
			DrawingSurface surface = surfaceObject.AddComponent<DrawingSurface>();
			DrawingArtifactData artifact = surface.GetArtifact(DrawingPlayerSlot.PlayerOne);
			DrawingLimitsData limits = new DrawingLimitsData(10, 10f, 0.01f);
			artifact.BeginStroke(new DrawingPointData(0f, 0f), limits);
			artifact.TryAppendPoint(new DrawingPointData(1f, 0f), limits);

			surface.RefreshVisual(DrawingPlayerSlot.PlayerOne);
			LineRenderer firstRenderer = surfaceObject.GetComponentInChildren<LineRenderer>();
			Assert.That(firstRenderer, Is.Not.Null);
			Assert.That(firstRenderer.positionCount, Is.EqualTo(2));

			artifact.TryAppendPoint(new DrawingPointData(2f, 0f), limits);
			surface.RefreshVisual(DrawingPlayerSlot.PlayerOne);
			LineRenderer reusedRenderer = surfaceObject.GetComponentInChildren<LineRenderer>();

			Assert.That(reusedRenderer, Is.SameAs(firstRenderer));
			Assert.That(reusedRenderer.positionCount, Is.EqualTo(3));
		}
		finally
		{
			Object.DestroyImmediate(surfaceObject);
		}
	}

	[Test]
	public void DrawingSurface_ChangesStyleOnConfirmAndClearsAllVisuals()
	{
		GameObject surfaceObject = new GameObject("DrawingSurfaceTest");
		try
		{
			DrawingSurface surface = surfaceObject.AddComponent<DrawingSurface>();
			DrawingArtifactData artifact = surface.GetArtifact(DrawingPlayerSlot.PlayerTwo);
			DrawingLimitsData limits = new DrawingLimitsData(10, 10f, 0.01f);
			artifact.BeginStroke(new DrawingPointData(0f, 0f), limits);
			artifact.TryAppendPoint(new DrawingPointData(1f, 0f), limits);

			surface.RefreshVisual(DrawingPlayerSlot.PlayerTwo);
			LineRenderer line = surfaceObject.GetComponentInChildren<LineRenderer>();
			float drawingAlpha = line.startColor.a;
			float drawingWidth = line.widthMultiplier;

			artifact.Confirm();
			surface.RefreshVisual(DrawingPlayerSlot.PlayerTwo);

			Assert.That(line.startColor.a, Is.GreaterThan(drawingAlpha));
			Assert.That(line.widthMultiplier, Is.GreaterThan(drawingWidth));

			surface.ClearAllDrawings();
			Assert.That(surfaceObject.GetComponentInChildren<LineRenderer>(), Is.Null);
			Assert.That(artifact.PointCount, Is.Zero);
		}
		finally
		{
			Object.DestroyImmediate(surfaceObject);
		}
	}

	[Test]
	public void PathSimplifier_RemovesDuplicatesShortLinesAndCollinearPoints()
	{
		List<DrawingPointData> points = new List<DrawingPointData>
		{
			new DrawingPointData(0f, 0f),
			new DrawingPointData(0f, 0f),
			new DrawingPointData(0.5f, 0.001f),
			new DrawingPointData(1f, 0f)
		};

		List<DrawingPointData> simplified = DrawingPathSimplifier.Simplify(points, 0.01f, 0.01f, 0.05f, 64);
		Assert.That(simplified.Count, Is.EqualTo(2));

		List<DrawingPointData> shortLine = DrawingPathSimplifier.Simplify(
			new[] { new DrawingPointData(0f, 0f), new DrawingPointData(0.01f, 0f) },
			0.001f,
			0.001f,
			0.05f,
			64);
		Assert.That(shortLine, Is.Empty);
	}

	[Test]
	public void DrawingSurface_ConfirmedDrawingCreatesGroundCapsulesAndClearsThem()
	{
		GameObject surfaceObject = new GameObject("DrawingSurfaceTest");
		try
		{
			DrawingSurface surface = surfaceObject.AddComponent<DrawingSurface>();
			DrawingArtifactData artifact = surface.GetArtifact(DrawingPlayerSlot.PlayerOne);
			DrawingLimitsData limits = new DrawingLimitsData(10, 10f, 0.01f);
			artifact.BeginStroke(new DrawingPointData(0f, 0f), limits);
			artifact.TryAppendPoint(new DrawingPointData(0.5f, 0f), limits);
			artifact.TryAppendPoint(new DrawingPointData(1f, 0f), limits);
			artifact.Confirm();

			surface.RefreshVisual(DrawingPlayerSlot.PlayerOne);

			CapsuleCollider2D[] capsules = surfaceObject.GetComponentsInChildren<CapsuleCollider2D>();
			LineRenderer line = surfaceObject.GetComponentInChildren<LineRenderer>();
			RuntimeRoundObject roundObject = surfaceObject.GetComponentInChildren<RuntimeRoundObject>();
			Assert.That(capsules.Length, Is.EqualTo(1));
			Assert.That(capsules[0].size.y, Is.EqualTo(line.widthMultiplier).Within(0.0001f));
			Assert.That(capsules[0].gameObject.layer, Is.EqualTo(LayerMask.NameToLayer("Ground")));
			Assert.That(roundObject, Is.Not.Null);

			surface.ClearAllDrawings();
			Assert.That(surfaceObject.GetComponentInChildren<CapsuleCollider2D>(), Is.Null);
		}
		finally
		{
			Object.DestroyImmediate(surfaceObject);
		}
	}

	[Test]
	public void DrawingSurface_LimitsColliderCountForNoisyStroke()
	{
		GameObject surfaceObject = new GameObject("DrawingSurfaceTest");
		try
		{
			DrawingSurface surface = surfaceObject.AddComponent<DrawingSurface>();
			DrawingArtifactData artifact = surface.GetArtifact(DrawingPlayerSlot.PlayerOne);
			DrawingLimitsData limits = new DrawingLimitsData(200, 100f, 0.001f);
			artifact.BeginStroke(new DrawingPointData(0f, 0f), limits);
			for (int index = 1; index <= 100; index++)
			{
				artifact.TryAppendPoint(new DrawingPointData(index * 0.06f, index % 2 == 0 ? 0f : 0.1f), limits);
			}
			artifact.Confirm();

			surface.RefreshVisual(DrawingPlayerSlot.PlayerOne);

			Assert.That(surfaceObject.GetComponentsInChildren<CapsuleCollider2D>().Length, Is.LessThanOrEqualTo(64));
		}
		finally
		{
			Object.DestroyImmediate(surfaceObject);
		}
	}

	[Test]
	public void DrawingSurface_ConfiguresReusableProjectileWithoutMutatingSource()
	{
		GameObject surfaceObject = new GameObject("DrawingSurfaceTest");
		CannonProjectile projectile = null;
		try
		{
			DrawingSurface surface = surfaceObject.AddComponent<DrawingSurface>();
			DrawingArtifactData artifact = surface.GetArtifact(DrawingPlayerSlot.PlayerOne);
			DrawingLimitsData limits = new DrawingLimitsData(10, 10f, 0.01f);
			artifact.BeginStroke(new DrawingPointData(2f, 3f), limits);
			artifact.TryAppendPoint(new DrawingPointData(3f, 3f), limits);
			artifact.TryAppendPoint(new DrawingPointData(3f, 4f), limits);
			artifact.Confirm();
			int originalPointCount = artifact.PointCount;

			projectile = CannonProjectile.CreateRuntime(Vector3.zero, Quaternion.identity, false);
			Assert.That(surface.TryConfigureProjectile(DrawingPlayerSlot.PlayerOne, projectile), Is.True);

			LineRenderer line = projectile.GetComponentInChildren<LineRenderer>();
			CapsuleCollider2D[] capsules = projectile.GetComponentsInChildren<CapsuleCollider2D>();
			CircleCollider2D fixedCollider = projectile.GetComponent<CircleCollider2D>();
			Rigidbody2D body = projectile.GetComponent<Rigidbody2D>();
			Assert.That(line, Is.Not.Null);
			Assert.That(line.useWorldSpace, Is.False);
			Assert.That(line.positionCount, Is.EqualTo(3));
			Assert.That(capsules.Length, Is.EqualTo(2));
			Assert.That(fixedCollider, Is.Null);
			Assert.That(artifact.PointCount, Is.EqualTo(originalPointCount));
			Assert.That(artifact.IsConfirmed, Is.True);

			projectile.Initialize(null, false);
			Assert.That(body.gravityScale, Is.GreaterThan(0f));
			Assert.That(Mathf.Abs(body.angularVelocity), Is.GreaterThan(0f));

			projectile.StopProjectile();
			Assert.That(body.bodyType, Is.EqualTo(RigidbodyType2D.Static));
			Assert.That(capsules[0].enabled, Is.True);
		}
		finally
		{
			if (projectile != null)
			{
				Object.DestroyImmediate(projectile.gameObject);
			}
			Object.DestroyImmediate(surfaceObject);
		}
	}

	private static DrawingRecorder CreateRecorder(
		DrawingArtifactData artifact,
		int maxPointCount = 10,
		float maxTotalLineLength = 10f,
		float minPointSpacing = 0.1f,
		DrawingRectData? bounds = null)
	{
		return new DrawingRecorder(
			artifact,
			bounds ?? new DrawingRectData(-1f, -1f, 1f, 1f),
			new DrawingLimitsData(maxPointCount, maxTotalLineLength, minPointSpacing),
			new DrawingPointData(0f, 0f));
	}

	private static void AssertPoint(DrawingPointData point, float expectedX, float expectedY)
	{
		Assert.That(point.X, Is.EqualTo(expectedX).Within(0.0001f));
		Assert.That(point.Y, Is.EqualTo(expectedY).Within(0.0001f));
	}
}
