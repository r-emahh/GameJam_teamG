using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Rigidbody2D))]
// 描画用とスタン用の弾の挙動をまとめる。
public sealed class CannonProjectile : MonoBehaviour
{
	// 描画弾の速度を設定する。
	[SerializeField]
	private float drawingSpeed = 10f;

	// 描画弾に適用する重力倍率を設定する。
	[SerializeField, Min(0f)]
	private float drawingGravityScale = 1.4f;

	// スタン弾の速度を設定する。
	[SerializeField]
	private float stunSpeed = 14f;

	// スタン時間を設定する。
	[SerializeField]
	private float stunDuration = 2f;

	// 弾の寿命を設定する。
	[SerializeField]
	private float lifetime = 12f;

	// 設置用の描画弾の見た目スケールを設定する。
	[SerializeField]
	private float drawingProjectileScale = 1f;

	// 妨害用の小さい弾の見た目スケールを設定する。
	[SerializeField]
	private float stunProjectileScale = 0.65f;

	// 妨害用の当たり判定半径を設定する。
	[SerializeField]
	private float stunProjectileRadius = 0.12f;

	// 発射元の大砲を保持する。
	private PlayerCannon owner;
	// 物理挙動を制御する。
	private Rigidbody2D body;
	// スタン用弾かどうかを記録する。
	private bool isStunProjectile;
	// 停止済みかどうかを記録する。
	private bool isStopped;
	// 衝突や寿命切れによる解決済み状態を記録する。
	private bool isResolved;
	// 生成後の残り寿命を保持する。
	private float remainingLifetime;
	// 通常弾へ発射者の確定描画を適用済みかを記録する。
	private bool hasDrawingArtifact;

	// 実行時生成の弾オブジェクトを作る。
	public static CannonProjectile CreateRuntime(Vector3 position, Quaternion rotation, bool stunProjectile)
	{
		GameObject projectileObject = new GameObject(stunProjectile ? "StunProjectile" : "DrawingProjectile");
		projectileObject.AddComponent<RuntimeRoundObject>();
		projectileObject.transform.SetPositionAndRotation(position, rotation);

		SpriteRenderer renderer = projectileObject.AddComponent<SpriteRenderer>();
		renderer.sprite = RuntimeSpriteFactory.UnitSquare;
		renderer.color = stunProjectile ? new Color(1f, 0.35f, 0.35f, 1f) : new Color(0.95f, 0.95f, 0.95f, 1f);
		renderer.sortingOrder = 2;

		Rigidbody2D rigidbody = projectileObject.AddComponent<Rigidbody2D>();
		rigidbody.gravityScale = 1.4f;
		rigidbody.angularDamping = 0.05f;
		rigidbody.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
		projectileObject.AddComponent<CircleCollider2D>().radius = stunProjectile ? 0.12f : 0.18f;
		projectileObject.transform.localScale = Vector3.one * (stunProjectile ? 0.65f : 1f);
		return projectileObject.AddComponent<CannonProjectile>();
	}

	// Rigidbody を取得する。
	private void Awake()
	{
		body = GetComponent<Rigidbody2D>();
	}

	// 確定描画を中心基準のローカル形状へ変換し、表示と複合Colliderを構築する。
	public bool ConfigureDrawingArtifact(
		DrawingArtifactData artifact,
		Color color,
		Material lineMaterial,
		float lineWidth,
		float duplicatePointDistance,
		float simplificationTolerance,
		float minimumSegmentLength,
		int maximumSegmentsPerStroke)
	{
		if (artifact == null || !artifact.IsConfirmed || !TryGetDrawingCenter(artifact, out Vector2 center))
		{
			return false;
		}

		RemovePreviousDrawingGeometry();
		DisableFixedProjectileVisual();
		gameObject.layer = GetGroundLayer();

		int colliderIndex = 0;
		for (int strokeIndex = 0; strokeIndex < artifact.Strokes.Count; strokeIndex++)
		{
			DrawingStrokeData stroke = artifact.Strokes[strokeIndex];
			if (stroke.PointCount < 2)
			{
				continue;
			}

			CreateStrokeVisual(stroke, strokeIndex, center, color, lineMaterial, lineWidth);
			List<DrawingPointData> colliderPoints = DrawingPathSimplifier.Simplify(
				stroke.Points,
				duplicatePointDistance,
				simplificationTolerance,
				minimumSegmentLength,
				maximumSegmentsPerStroke);

			for (int pointIndex = 1; pointIndex < colliderPoints.Count; pointIndex++)
			{
				CreateSegmentCollider(
					colliderPoints[pointIndex - 1],
					colliderPoints[pointIndex],
					center,
					lineWidth,
					minimumSegmentLength,
					colliderIndex++);
			}
		}

		hasDrawingArtifact = colliderIndex > 0;
		return hasDrawingArtifact;
	}

	// 発射元と弾種を設定し、初速を与える。
	public void Initialize(PlayerCannon projectileOwner, bool stunProjectile, float launchPower = -1f)
	{
		owner = projectileOwner;
		isStunProjectile = stunProjectile;
		remainingLifetime = lifetime;
		if (!body)
		{
			body = GetComponent<Rigidbody2D>();
		}

		if (isStunProjectile)
		{
			InitializeStunProjectile();
			return;
		}

		InitializeDrawingProjectile(launchPower);
	}

	// 飛翔中の弾を寿命切れで破棄し、妨害弾は画面外でも破棄する。
	private void Update()
	{
		if (isStopped || isResolved)
		{
			return;
		}

		remainingLifetime -= Time.deltaTime;
		if (remainingLifetime <= 0f || (isStunProjectile && IsOutsideScreen()))
		{
			ResolveAndDestroy();
		}
	}

	// 弾種ごとの衝突処理へ振り分ける。
	private void OnCollisionEnter2D(Collision2D collision)
	{
		if (isStopped || isResolved)
		{
			return;
		}

		if (isStunProjectile)
		{
			HandleStunProjectileCollision(collision.collider);
			return;
		}

		StopProjectile();
	}

	// 通常の設置用砲弾を初期化する。
	private void InitializeDrawingProjectile(float launchPower)
	{
		if (!hasDrawingArtifact)
		{
			ResolveAndDestroy();
			return;
		}

		transform.localScale = Vector3.one * drawingProjectileScale;
		body.gravityScale = drawingGravityScale;
		float initialSpeed = launchPower >= 0f ? launchPower : drawingSpeed;
		body.linearVelocity = transform.right * initialSpeed;
		body.angularVelocity = 180f;
	}

	// 妨害弾を初期化する。
	private void InitializeStunProjectile()
	{
		ConfigureCollider(stunProjectileScale, stunProjectileRadius);
		body.linearVelocity = transform.right * stunSpeed;
		body.angularVelocity = 300f;
	}

	// 弾種に応じた大きさと当たり判定を設定する。
	private void ConfigureCollider(float scale, float radius)
	{
		transform.localScale = Vector3.one * scale;
		CircleCollider2D projectileCollider = GetComponent<CircleCollider2D>();
		if (projectileCollider == null)
		{
			projectileCollider = gameObject.AddComponent<CircleCollider2D>();
		}

		projectileCollider.enabled = true;
		projectileCollider.radius = radius;
	}

	// 妨害弾は GoalRunner にだけ一度スタンを与え、衝突後は必ず破棄する。
	private void HandleStunProjectileCollision(Collider2D hitCollider)
	{
		isResolved = true;
		PlayerIdentity identity = hitCollider.GetComponentInParent<PlayerIdentity>();
		PlayerStun target = hitCollider.GetComponentInParent<PlayerStun>();
		if (identity != null && identity.ControlledSide == MatchSide.GoalRunner && target != null)
		{
			target.Apply(stunDuration);
		}

		DisableCollisionAndDestroy();
	}

	// 現在位置がメインカメラの表示範囲外かを判定する。
	private bool IsOutsideScreen()
	{
		Camera targetCamera = Camera.main;
		if (targetCamera == null)
		{
			return false;
		}

		Vector3 viewportPosition = targetCamera.WorldToViewportPoint(transform.position);
		return viewportPosition.z < 0f
			|| viewportPosition.x < 0f
			|| viewportPosition.x > 1f
			|| viewportPosition.y < 0f
			|| viewportPosition.y > 1f;
	}

	// 未解決の飛翔中弾を破棄状態へ移す。
	private void ResolveAndDestroy()
	{
		if (isResolved)
		{
			return;
		}

		isResolved = true;
		DisableCollisionAndDestroy();
	}

	// 同一フレーム中の再衝突を防いでから破棄する。
	private void DisableCollisionAndDestroy()
	{
		foreach (Collider2D projectileCollider in GetComponentsInChildren<Collider2D>())
		{
			projectileCollider.enabled = false;
		}

		owner?.ClearProjectileReference(this);
		Destroy(gameObject);
	}

	// 弾を静止状態にして設置物として扱う。
	public void StopProjectile()
	{
		if (isStopped || isResolved)
		{
			return;
		}

		if (isStunProjectile)
		{
			ResolveAndDestroy();
			return;
		}

		isStopped = true;
		body.linearVelocity = Vector2.zero;
		body.angularVelocity = 0f;
		body.bodyType = RigidbodyType2D.Static;
		gameObject.name = "PlacedDrawing";

		owner?.ClearProjectileReference(this);
	}

	// 破棄時に所有者参照を外す。
	private void OnDestroy()
	{
		owner?.ClearProjectileReference(this);
	}

	private static bool TryGetDrawingCenter(DrawingArtifactData artifact, out Vector2 center)
	{
		float minX = float.PositiveInfinity;
		float minY = float.PositiveInfinity;
		float maxX = float.NegativeInfinity;
		float maxY = float.NegativeInfinity;
		bool foundSegment = false;

		foreach (DrawingStrokeData stroke in artifact.Strokes)
		{
			if (stroke.PointCount < 2)
			{
				continue;
			}

			foundSegment = true;
			foreach (DrawingPointData point in stroke.Points)
			{
				minX = Mathf.Min(minX, point.X);
				minY = Mathf.Min(minY, point.Y);
				maxX = Mathf.Max(maxX, point.X);
				maxY = Mathf.Max(maxY, point.Y);
			}
		}

		center = foundSegment
			? new Vector2((minX + maxX) * 0.5f, (minY + maxY) * 0.5f)
			: Vector2.zero;
		return foundSegment;
	}

	private void RemovePreviousDrawingGeometry()
	{
		for (int childIndex = transform.childCount - 1; childIndex >= 0; childIndex--)
		{
			Transform child = transform.GetChild(childIndex);
			if (child.name.StartsWith("DrawingStroke_") || child.name.StartsWith("DrawingCollider_"))
			{
				DestroyRuntimeObject(child.gameObject);
			}
		}
	}

	private void DisableFixedProjectileVisual()
	{
		SpriteRenderer fixedRenderer = GetComponent<SpriteRenderer>();
		if (fixedRenderer != null)
		{
			fixedRenderer.enabled = false;
		}

		CircleCollider2D fixedCollider = GetComponent<CircleCollider2D>();
		if (fixedCollider != null)
		{
			fixedCollider.enabled = false;
			DestroyRuntimeObject(fixedCollider);
		}
	}

	private void CreateStrokeVisual(
		DrawingStrokeData stroke,
		int strokeIndex,
		Vector2 center,
		Color color,
		Material lineMaterial,
		float lineWidth)
	{
		GameObject strokeObject = new GameObject($"DrawingStroke_{strokeIndex}");
		strokeObject.layer = GetGroundLayer();
		strokeObject.transform.SetParent(transform, false);

		LineRenderer line = strokeObject.AddComponent<LineRenderer>();
		line.useWorldSpace = false;
		line.loop = false;
		line.numCapVertices = 2;
		line.numCornerVertices = 2;
		line.sortingOrder = 2;
		line.sharedMaterial = lineMaterial;
		line.widthMultiplier = lineWidth;
		line.startColor = color;
		line.endColor = color;
		line.positionCount = stroke.PointCount;

		for (int pointIndex = 0; pointIndex < stroke.PointCount; pointIndex++)
		{
			DrawingPointData point = stroke.Points[pointIndex];
			line.SetPosition(pointIndex, new Vector3(point.X - center.x, point.Y - center.y, 0f));
		}
	}

	private void CreateSegmentCollider(
		DrawingPointData start,
		DrawingPointData end,
		Vector2 center,
		float lineWidth,
		float minimumSegmentLength,
		int colliderIndex)
	{
		Vector2 localStart = new Vector2(start.X, start.Y) - center;
		Vector2 localEnd = new Vector2(end.X, end.Y) - center;
		Vector2 delta = localEnd - localStart;
		float length = delta.magnitude;
		if (length < minimumSegmentLength || length <= 0f)
		{
			return;
		}

		GameObject segment = new GameObject($"DrawingCollider_{colliderIndex}");
		segment.layer = GetGroundLayer();
		segment.transform.SetParent(transform, false);
		segment.transform.localPosition = (localStart + localEnd) * 0.5f;
		segment.transform.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg);

		CapsuleCollider2D capsule = segment.AddComponent<CapsuleCollider2D>();
		capsule.direction = CapsuleDirection2D.Horizontal;
		capsule.size = new Vector2(length + lineWidth, lineWidth);
		capsule.isTrigger = false;
	}

	private static int GetGroundLayer()
	{
		int layer = LayerMask.NameToLayer("Ground");
		return layer >= 0 ? layer : 0;
	}

	private static void DestroyRuntimeObject(Object target)
	{
		if (Application.isPlaying)
		{
			Destroy(target);
		}
		else
		{
			DestroyImmediate(target);
		}
	}
}
