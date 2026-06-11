using UnityEngine;

[DisallowMultipleComponent]
// シーン上のステージ・ゴール・大砲・スポーン地点を組み立てる。
public sealed class StageRuntimeBuilder : MonoBehaviour
{
	// 描画用サーフェスの名前。
	private const string StageSurfaceName = "StageSurface";
	// ゴールゾーンの名前。
	private const string GoalZoneName = "GoalZone";
	// ステージ全体の描画可能領域を保持する。
	private static readonly Vector2 StageSurfaceSize = new Vector2(20f, 10.9f);
	// ステージ全体の描画可能領域の中心を保持する。
	private static readonly Vector3 StageSurfaceCenter = new Vector3(0f, -0.1f, 0f);

	// 必要なステージ要素を生成または再配置する。
	public void Build()
	{
		RemoveLegacyDrawZone();
		EnsureStageSurface();
		EnsureGoalZone();
		CreateBoundaryIfNeeded("StageTop", new Vector3(0f, 5.35f, 0f), new Vector2(20f, 0.18f));
		CreateBoundaryIfNeeded("StageBottom", new Vector3(0f, -5.55f, 0f), new Vector2(20f, 0.18f));
		CreateCannonMount("TopLeft", 0);
		CreateCannonMount("TopRight", 1);
		CreateCannonMount("BottomLeft", 2);
		CreateCannonMount("BottomRight", 3);
		CreateSpawnPointIfNeeded(PlayerSpawnCoordinator.GoalRunnerSpawnName, new Vector3(-7.4f, -3.1f, 0f));
		CreateSpawnPointIfNeeded(PlayerSpawnCoordinator.BlockerSpawnName, new Vector3(-5.6f, -3.1f, 0f));
	}

	// ステージ全体を描画可能領域として用意する。
	private void EnsureStageSurface()
	{
		GameObject stageSurface = FindOrCreate(StageSurfaceName);
		stageSurface.transform.position = StageSurfaceCenter;
		stageSurface.transform.localScale = Vector3.one;
		ConfigureVisual(stageSurface, new Color(0f, 0f, 0f, 0f), 0);

		DrawingSurface surface = stageSurface.GetComponent<DrawingSurface>() ?? stageSurface.AddComponent<DrawingSurface>();
		surface.Configure(StageSurfaceSize);
	}

	// 旧描画ゾーンが残っていれば削除する。
	private static void RemoveLegacyDrawZone()
	{
		GameObject legacyDrawZone = GameObject.Find("DrawZone");
		if (legacyDrawZone != null)
		{
			Object.Destroy(legacyDrawZone);
		}
	}

	// ゴール判定エリアと見た目を用意する。
	private void EnsureGoalZone()
	{
		GameObject goalZone = FindOrCreate(GoalZoneName);
		goalZone.transform.position = new Vector3(8.25f, -2.35f, 0f);
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

	// 大砲マウントを生成し、順序を設定する。
	private void CreateCannonMount(string suffix, int order)
	{
		GameObject cannon = FindOrCreate($"Cannon_{suffix}");
		cannon.transform.localScale = new Vector3(0.55f, 0.55f, 1f);
		ConfigureVisual(cannon, new Color(0.8f, 0.65f, 0.2f, 1f), 1);
		CannonMount mount = cannon.GetComponent<CannonMount>() ?? cannon.AddComponent<CannonMount>();
		mount.Configure(order);
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
