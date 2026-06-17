using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

[DisallowMultipleComponent]
// シーン上のステージ・ゴール・大砲・スポーン地点を組み立てる。
public sealed class StageRuntimeBuilder : MonoBehaviour
{
	// 描画用サーフェスの名前。
	private const string StageSurfaceName = "StageSurface";
	// ランタイム生成するステージ用 Grid の名前。
	private const string StageGridName = "StageGrid";
	// ランタイム生成する Tilemap の名前。
	private const string StageTilemapName = "StageTilemap";
	// 床コライダー群のルート名。
	private const string StageGroundCollisionRootName = "StageGroundCollision";
	// ゴールゾーンの名前。
	private const string GoalZoneName = "GoalZone";
	// 画面上中央に置く唯一の大砲名。
	private const string CannonName = "Cannon_TopCenter";
	// ステージ全体の描画可能領域を保持する。
	private static readonly Vector2 StageSurfaceSize = new Vector2(20f, 10.9f);
	// ランタイム Tilemap の幅を保持する。
	private const int StageMapWidth = 52;
	// ランタイム Tilemap の高さを保持する。
	private const int StageMapHeight = 20;
	// Tilemap の基準色を保持する。
	private static readonly Color StageTileColor = new Color(0.18f, 0.2f, 0.22f, 1f);
	// 立ち上がりを少し抑えた明るいブロック色を保持する。
	private static readonly Color StageAccentTileColor = new Color(0.25f, 0.28f, 0.32f, 1f);

	// ランタイム生成した Tile をキャッシュする。
	private static Tile stageSolidTile;
	// ランタイム生成した装飾 Tile をキャッシュする。
	private static Tile stageAccentTile;

	// 簡易矩形。
	private readonly struct TileRect
	{
		public TileRect(int x, int y, int width, int height)
		{
			X = x;
			Y = y;
			Width = width;
			Height = height;
		}

		public int X { get; }
		public int Y { get; }
		public int Width { get; }
		public int Height { get; }
	}

	// 床の左側に確保する安全地帯の幅。
	private const int LeftAnchorWidth = 8;
	// 床の右側に確保する安全地帯の幅。
	private const int RightAnchorWidth = 9;
	// 中央床の最小幅。
	private const int FloorSegmentMinWidth = 3;
	// 中央床の最大幅。
	private const int FloorSegmentMaxWidth = 7;
	// 落とし穴として空ける最小幅。
	private const int GapMinWidth = 2;
	// 落とし穴として空ける最大幅。
	private const int GapMaxWidth = 5;
	// 生成する階段の数。
	private const int StaircaseCount = 3;
	// 階段の段数の最小値。
	private const int StairStepMinCount = 3;
	// 階段の段数の最大値。
	private const int StairStepMaxCount = 5;
	// 階段1段あたりの横幅の最小値。
	private const int StairStepMinWidth = 2;
	// 階段1段あたりの横幅の最大値。
	private const int StairStepMaxWidth = 3;
	// Goal Runner の初期スポーン位置。
	private static readonly Vector3 GoalRunnerSpawnPosition = new Vector3(-23f, -8f, 0f);
	// Blocker の初期スポーン位置。既存と同じ横間隔で Goal Runner の右に置く。
	private static readonly Vector3 BlockerSpawnPosition = new Vector3(-21.2f, -8f, 0f);

	// 必要なステージ要素を生成または再配置する。
	public void Build()
	{
		RemoveLegacyDrawZone();
		TileRect[] solidRectangles = BuildSolidRectangles();
		Rect worldBounds = EnsureTilemapStage(solidRectangles);
		EnsureGroundCollisionStage(solidRectangles);
		EnsureStageSurface(worldBounds);
		EnsureGoalZone(worldBounds);
		CreateBoundaryIfNeeded("StageTop", new Vector3(worldBounds.center.x, worldBounds.yMax + 0.5f, 0f), new Vector2(worldBounds.width, 0.18f));
		CreateBoundaryIfNeeded("StageBottom", new Vector3(worldBounds.center.x, worldBounds.yMin - 0.5f, 0f), new Vector2(worldBounds.width, 0.18f));
		EnsureSingleCannonMount();
		CreateSpawnPointIfNeeded(PlayerSpawnCoordinator.GoalRunnerSpawnName, GoalRunnerSpawnPosition);
		CreateSpawnPointIfNeeded(PlayerSpawnCoordinator.BlockerSpawnName, BlockerSpawnPosition);
		EnsureCameraScroll(worldBounds);
		Physics2D.SyncTransforms();
	}

	// ステージ全体を描画可能領域として用意する。
	private void EnsureStageSurface(Rect worldBounds)
	{
		GameObject stageSurface = FindOrCreate(StageSurfaceName);
		stageSurface.transform.position = new Vector3(worldBounds.center.x, worldBounds.center.y - 0.1f, 0f);
		stageSurface.transform.localScale = Vector3.one;
		ConfigureVisual(stageSurface, new Color(0f, 0f, 0f, 0f), 0);

		DrawingSurface surface = stageSurface.GetComponent<DrawingSurface>() ?? stageSurface.AddComponent<DrawingSurface>();
		surface.Configure(StageSurfaceSize);
	}

	// Tilemap で構成した地形を生成し、ワールド範囲を返す。
	private Rect EnsureTilemapStage(TileRect[] solidRectangles)
	{
		GameObject gridObject = FindOrCreate(StageGridName);
		Grid grid = EnsureComponent<Grid>(gridObject);
		if (!grid)
		{
			UnityEngine.Object.Destroy(gridObject);
			gridObject = new GameObject(StageGridName);
			grid = gridObject.AddComponent<Grid>();
		}

		grid.cellSize = Vector3.one;

		GameObject tilemapObject = FindOrCreate(StageTilemapName);
		Tilemap tilemap = EnsureComponent<Tilemap>(tilemapObject);
		TilemapRenderer renderer = EnsureComponent<TilemapRenderer>(tilemapObject);
		Rigidbody2D body = EnsureComponent<Rigidbody2D>(tilemapObject);

		if (!tilemap || !renderer || !body)
		{
			UnityEngine.Object.Destroy(tilemapObject);
			tilemapObject = new GameObject(StageTilemapName);
			tilemap = tilemapObject.AddComponent<Tilemap>();
			renderer = tilemapObject.AddComponent<TilemapRenderer>();
			body = tilemapObject.AddComponent<Rigidbody2D>();
		}

		tilemapObject.transform.SetParent(gridObject.transform, false);
		tilemapObject.transform.localPosition = new Vector3(-StageMapWidth * 0.5f + 0.5f, -StageMapHeight * 0.5f + 0.5f, 0f);
		tilemapObject.layer = GetGroundLayer();

		renderer.sortingOrder = -5;
		body.bodyType = RigidbodyType2D.Static;
		body.simulated = true;

		tilemap.ClearAllTiles();
		FillSolidTiles(tilemap, solidRectangles);
		tilemap.CompressBounds();

		Rect worldBounds = new Rect(
			tilemapObject.transform.position.x - 0.5f,
			tilemapObject.transform.position.y - 0.5f,
			StageMapWidth,
			StageMapHeight);

		return worldBounds;
	}

	// 床コライダーを生成し、物理衝突を確実にする。
	private void EnsureGroundCollisionStage(TileRect[] solidRectangles)
	{
		GameObject collisionRoot = GameObject.Find(StageGroundCollisionRootName);
		if (collisionRoot != null)
		{
			UnityEngine.Object.Destroy(collisionRoot);
		}

		collisionRoot = new GameObject(StageGroundCollisionRootName);
		collisionRoot.layer = GetGroundLayer();
		collisionRoot.transform.SetParent(GameObject.Find(StageTilemapName)?.transform, false);

		for (int i = 0; i < solidRectangles.Length; i++)
		{
			TileRect rect = solidRectangles[i];
			GameObject colliderObject = new GameObject($"GroundCollider_{i}");
			colliderObject.layer = GetGroundLayer();
			colliderObject.transform.SetParent(collisionRoot.transform, false);
			colliderObject.transform.localPosition = new Vector3(rect.X + rect.Width * 0.5f, rect.Y + rect.Height * 0.5f, 0f);
			BoxCollider2D collider = colliderObject.AddComponent<BoxCollider2D>();
			collider.size = new Vector2(rect.Width, rect.Height);
			collider.isTrigger = false;
		}
	}

	// コンポーネントが存在すれば返し、無ければ追加を試みる。
	private static TComponent EnsureComponent<TComponent>(GameObject target)
		where TComponent : Component
	{
		if (target == null)
		{
			return null;
		}

		TComponent component = target.GetComponent<TComponent>();
		if (component)
		{
			return component;
		}

		component = target.AddComponent<TComponent>();
		return component;
	}

	// 旧描画ゾーンが残っていれば削除する。
	private static void RemoveLegacyDrawZone()
	{
		GameObject legacyDrawZone = GameObject.Find("DrawZone");
		if (legacyDrawZone != null)
		{
			UnityEngine.Object.Destroy(legacyDrawZone);
		}
	}

	// ゴール判定エリアと見た目を用意する。
	private void EnsureGoalZone(Rect worldBounds)
	{
		GameObject goalZone = FindOrCreate(GoalZoneName);
		goalZone.transform.position = new Vector3(worldBounds.xMax - 4.5f, worldBounds.yMin + 2.2f, 0f);
		BoxCollider2D collider = goalZone.GetComponent<BoxCollider2D>();
		if (!collider)
		{
			collider = goalZone.AddComponent<BoxCollider2D>();
		}

		collider.isTrigger = true;
		collider.size = new Vector2(1.1f, 3.8f);
		if (!goalZone.GetComponent<GoalZoneTrigger>())
		{
			goalZone.AddComponent<GoalZoneTrigger>();
		}

		GameObject visual = FindOrCreate($"{GoalZoneName}_Visual");
		visual.transform.position = goalZone.transform.position;
		visual.transform.localScale = new Vector3(1.1f, 3.8f, 1f);
		ConfigureVisual(visual, new Color(0.2f, 1f, 0.35f, 0.2f), 0);
	}

	// ステージ境界用の見た目と当たり判定を用意する。
	private void CreateBoundaryIfNeeded(string objectName, Vector3 position, Vector2 scale)
	{
		GameObject boundary = FindOrCreate(objectName);
		boundary.transform.position = position;
		boundary.transform.localScale = new Vector3(scale.x, scale.y, 1f);
		ConfigureVisual(boundary, new Color(0.1f, 0.1f, 0.1f, 1f), 0);
	}

	// 既存の余分な大砲を除去し、画面上中央の一基だけを用意する。
	private void EnsureSingleCannonMount()
	{
		CannonMount[] existingMounts = FindObjectsByType<CannonMount>(FindObjectsInactive.Include, FindObjectsSortMode.None);
		for (int i = 0; i < existingMounts.Length; i++)
		{
			if (existingMounts[i] != null && existingMounts[i].gameObject.name != CannonName)
			{
				existingMounts[i].gameObject.SetActive(false);
				Destroy(existingMounts[i].gameObject);
			}
		}

		GameObject cannon = FindOrCreate(CannonName);
		cannon.SetActive(true);
		cannon.transform.localScale = new Vector3(0.55f, 0.55f, 1f);
		ConfigureVisual(cannon, new Color(0.8f, 0.65f, 0.2f, 1f), 1);
		CannonMount mount = cannon.GetComponent<CannonMount>() ?? cannon.AddComponent<CannonMount>();
		mount.Configure(0);
	}

	// スポーン地点を用意する。
	private static void CreateSpawnPointIfNeeded(string objectName, Vector3 position)
	{
		GameObject spawn = FindOrCreate(objectName);
		spawn.transform.position = position;
	}

	// 既存オブジェクトを探し、なければ作成する。
	private static GameObject FindOrCreate(string objectName)
	{
		GameObject existing = GameObject.Find(objectName);
		return existing ? existing : new GameObject(objectName);
	}

	// Tilemap に地形を塗る。
	private static void FillSolidTiles(Tilemap tilemap, TileRect[] solidRectangles)
	{
		Tile solidTile = GetOrCreateSolidTile();
		Tile accentTile = GetOrCreateAccentTile();
		for (int i = 0; i < solidRectangles.Length; i++)
		{
			TileRect rect = solidRectangles[i];
			Tile tile = i >= 4 ? accentTile : solidTile;
			FillRect(tilemap, rect, tile);
		}
	}

	// 生成済みの床・階段・落とし穴を返す。
	private static TileRect[] BuildSolidRectangles()
	{
		List<TileRect> rectangles = new List<TileRect>(32);
		System.Random random = CreateStageRandom();

		// 両端は最低限の足場を確保する。
		AddRect(rectangles, 0, 0, 1, StageMapHeight);
		AddRect(rectangles, 1, 0, LeftAnchorWidth, 1);
		AddRect(rectangles, StageMapWidth - 1 - RightAnchorWidth, 0, RightAnchorWidth, 1);
		AddRect(rectangles, StageMapWidth - 1, 0, 1, StageMapHeight);

		AddOpenFloorSegments(rectangles, random);
		AddStaircases(rectangles, random);

		return rectangles.ToArray();
	}

	// ステージに開けた床の島を並べる。
	private static void AddOpenFloorSegments(List<TileRect> rectangles, System.Random random)
	{
		int cursor = 1 + LeftAnchorWidth;
		int limit = StageMapWidth - 1 - RightAnchorWidth;

		while (cursor < limit)
		{
			cursor += NextRange(random, GapMinWidth, GapMaxWidth + 1);
			if (cursor >= limit)
			{
				break;
			}

			int width = NextRange(random, FloorSegmentMinWidth, FloorSegmentMaxWidth + 1);
			width = Mathf.Min(width, limit - cursor);
			AddRect(rectangles, cursor, 0, width, 1);
			cursor += width;
		}
	}

	// 少しだけ階段を混ぜて、上下移動のリズムを作る。
	private static void AddStaircases(List<TileRect> rectangles, System.Random random)
	{
		for (int i = 0; i < StaircaseCount; i++)
		{
			int stepCount = NextRange(random, StairStepMinCount, StairStepMaxCount + 1);
			int stepWidth = NextRange(random, StairStepMinWidth, StairStepMaxWidth + 1);
			int totalWidth = stepCount * stepWidth;
			int maxStartX = Mathf.Max(2, StageMapWidth - 1 - totalWidth);
			int anchor = StageMapWidth / (StaircaseCount + 1) * (i + 1);
			int startX = Mathf.Clamp(anchor + NextRange(random, -3, 4), 2, maxStartX);
			bool ascending = random.Next(0, 2) == 0;
			int startY = ascending ? NextRange(random, 1, 3) : stepCount;
			int direction = ascending ? 1 : -1;

			AddStaircase(rectangles, startX, startY, stepCount, stepWidth, direction);
		}
	}

	// 1本の階段を追加する。
	private static void AddStaircase(List<TileRect> rectangles, int startX, int startY, int stepCount, int stepWidth, int direction)
	{
		int x = startX;
		int y = startY;

		for (int i = 0; i < stepCount; i++)
		{
			if (x >= StageMapWidth - 1 || y <= 0 || y >= StageMapHeight)
			{
				break;
			}

			int width = Mathf.Min(stepWidth, StageMapWidth - 1 - x);
			if (width <= 0)
			{
				break;
			}

			AddRect(rectangles, x, y, width, 1);
			x += width;
			y += direction;
		}
	}

	// 矩形がステージ内に収まるなら追加する。
	private static void AddRect(List<TileRect> rectangles, int x, int y, int width, int height)
	{
		if (width <= 0 || height <= 0 || x >= StageMapWidth || y >= StageMapHeight)
		{
			return;
		}

		int clampedWidth = Mathf.Min(width, StageMapWidth - x);
		int clampedHeight = Mathf.Min(height, StageMapHeight - y);
		if (clampedWidth <= 0 || clampedHeight <= 0)
		{
			return;
		}

		rectangles.Add(new TileRect(x, y, clampedWidth, clampedHeight));
	}

	// 生成用の乱数を返す。
	private static System.Random CreateStageRandom()
	{
		unchecked
		{
			return new System.Random((int)(DateTime.UtcNow.Ticks & 0x7FFFFFFF));
		}
	}

	// 指定範囲の乱数を返す。
	private static int NextRange(System.Random random, int minInclusive, int maxExclusive)
	{
		return random.Next(minInclusive, maxExclusive);
	}

	// 指定矩形を同一 Tile で埋める。
	private static void FillRect(Tilemap tilemap, TileRect rect, TileBase tile)
	{
		for (int x = rect.X; x < rect.X + rect.Width; x++)
		{
			for (int y = rect.Y; y < rect.Y + rect.Height; y++)
			{
				tilemap.SetTile(new Vector3Int(x, y, 0), tile);
			}
		}
	}

	// ランタイム生成の地形用 Tile を返す。
	private static Tile GetOrCreateSolidTile()
	{
		if (stageSolidTile != null)
		{
			return stageSolidTile;
		}

		stageSolidTile = ScriptableObject.CreateInstance<Tile>();
		stageSolidTile.name = "RuntimeStageSolidTile";
		stageSolidTile.sprite = RuntimeSpriteFactory.UnitSquare;
		stageSolidTile.color = StageTileColor;
		stageSolidTile.colliderType = Tile.ColliderType.Sprite;
		return stageSolidTile;
	}

	// アクセント用のランタイム生成 Tile を返す。
	private static Tile GetOrCreateAccentTile()
	{
		if (stageAccentTile != null)
		{
			return stageAccentTile;
		}

		stageAccentTile = ScriptableObject.CreateInstance<Tile>();
		stageAccentTile.name = "RuntimeStageAccentTile";
		stageAccentTile.sprite = RuntimeSpriteFactory.UnitSquare;
		stageAccentTile.color = StageAccentTileColor;
		stageAccentTile.colliderType = Tile.ColliderType.Sprite;
		return stageAccentTile;
	}

	// Tilemap のレイヤー番号を返す。
	private static int GetGroundLayer()
	{
		int layer = LayerMask.NameToLayer("Ground");
		return layer >= 0 ? layer : 0;
	}

	// カメラに Tilemap の範囲を反映する。
	private static void EnsureCameraScroll(Rect worldBounds)
	{
		Camera targetCamera = Camera.main;
		if (targetCamera == null)
		{
			targetCamera = UnityEngine.Object.FindFirstObjectByType<Camera>();
		}

		if (targetCamera == null)
		{
			return;
		}

		StageCameraScrollController controller = targetCamera.GetComponent<StageCameraScrollController>();
		if (controller == null)
		{
			controller = targetCamera.gameObject.AddComponent<StageCameraScrollController>();
		}

		controller.Configure(worldBounds);
	}

	// 単色スプライトで可視化し、描画順を設定する。
	private static void ConfigureVisual(GameObject target, Color color, int sortingOrder)
	{
		SpriteRenderer renderer = target.GetComponent<SpriteRenderer>();
		if (!renderer)
		{
			renderer = target.AddComponent<SpriteRenderer>();
		}

		renderer.sprite = RuntimeSpriteFactory.UnitSquare;
		renderer.color = color;
		renderer.sortingOrder = sortingOrder;
	}
}
