using UnityEngine;

[DisallowMultipleComponent]
// 描画可能領域とスタンプ生成を扱う。
public sealed class DrawingSurface : MonoBehaviour
{
	// 描画領域の大きさを保持する。
	[SerializeField]
	private Vector2 size = new Vector2(7.5f, 2.6f);

	// スタンプの大きさを保持する。
	[SerializeField]
	private Vector2 stampSize = new Vector2(0.5f, 0.5f);

	// スタンプの色を保持する。
	[SerializeField]
	private Color stampColor = new Color(0.93f, 0.93f, 0.93f, 1f);

	// スタンプを配置するレイヤー名。
	[SerializeField]
	private string stampLayerName = "Ground";

	// ワールド座標上の描画可能範囲を返す。
	public Rect WorldBounds => new Rect((Vector2)transform.position - size * 0.5f, size);

	// 描画領域サイズを更新する。
	public void Configure(Vector2 newSize)
	{
		size = newSize;
	}

	// 指定座標を描画領域内に収める。
	public Vector3 Clamp(Vector3 worldPosition)
	{
		Rect bounds = WorldBounds;
		return new Vector3(
			Mathf.Clamp(worldPosition.x, bounds.xMin, bounds.xMax),
			Mathf.Clamp(worldPosition.y, bounds.yMin, bounds.yMax),
			worldPosition.z);
	}

	// スタンプ用オブジェクトを生成する。
	public void CreateStamp(Vector3 position, DrawingStampShape shape)
	{
		GameObject stamp = new GameObject($"{shape}Stamp");
		stamp.AddComponent<RuntimeRoundObject>();
		stamp.transform.SetParent(transform, true);
		stamp.transform.position = position;
		stamp.transform.localScale = new Vector3(stampSize.x, stampSize.y, 1f);

		// レイヤーを設定する
		int layer = LayerMask.NameToLayer(stampLayerName);
		if (layer != -1) { // レイヤーが見つかった場合のみ設定する
			stamp.layer = layer;
		}

		SpriteRenderer renderer = stamp.AddComponent<SpriteRenderer>();
		renderer.sprite = RuntimeSpriteFactory.GetDrawingStampSprite(shape);
		renderer.color = stampColor;
		renderer.sortingOrder = 1;

		switch (shape)
		{
			case DrawingStampShape.Circle:
				stamp.AddComponent<CircleCollider2D>().radius = 0.5f;
				break;
			case DrawingStampShape.Triangle:
				stamp.AddComponent<PolygonCollider2D>().points = new[]
				{
					new Vector2(0f, 0.5f),
					new Vector2(-0.5f, -0.45f),
					new Vector2(0.5f, -0.45f)
				};
				break;
			default:
				stamp.AddComponent<BoxCollider2D>().size = Vector2.one;
				break;
		}
	}
}

// 描画スタンプの固定図形を表す。
public enum DrawingStampShape
{
	Square,
	Circle,
	Triangle
}
