using UnityEngine;

// ランタイム生成物で使う汎用スプライトを共有生成する。
public static class RuntimeSpriteFactory
{
	// 単色正方形スプライトをキャッシュする。
	private static Sprite unitSquare;
	// 描画用の円形スプライトをキャッシュする。
	private static Sprite drawingCircle;
	// 描画用の三角形スプライトをキャッシュする。
	private static Sprite drawingTriangle;

	// 1x1 の白い正方形スプライトを返す。
	public static Sprite UnitSquare
	{
		get
		{
			if (unitSquare != null)
			{
				return unitSquare;
			}

			Texture2D texture = new Texture2D(1, 1, TextureFormat.RGBA32, false)
			{
				name = "RuntimeUnitSquare"
			};
			texture.SetPixel(0, 0, Color.white);
			texture.Apply();
			unitSquare = Sprite.Create(texture, new Rect(0f, 0f, 1f, 1f), Vector2.one * 0.5f, 1f);
			unitSquare.name = "RuntimeUnitSquare";
			return unitSquare;
		}
	}

	// 固定描画図形に対応するスプライトを返す。
	public static Sprite GetDrawingStampSprite(DrawingStampShape shape)
	{
		return shape switch
		{
			DrawingStampShape.Circle => GetDrawingCircleSprite(),
			DrawingStampShape.Triangle => GetDrawingTriangleSprite(),
			_ => UnitSquare
		};
	}

	// 円形スタンプのスプライトを返す。
	private static Sprite GetDrawingCircleSprite()
	{
		if (drawingCircle != null)
		{
			return drawingCircle;
		}

		drawingCircle = CreateShapeSprite("RuntimeDrawingCircle", (x, y, size) =>
		{
			float dx = x - size * 0.5f;
			float dy = y - size * 0.5f;
			float radius = size * 0.42f;
			return dx * dx + dy * dy <= radius * radius ? Color.white : Color.clear;
		});
		return drawingCircle;
	}

	// 三角形スタンプのスプライトを返す。
	private static Sprite GetDrawingTriangleSprite()
	{
		if (drawingTriangle != null)
		{
			return drawingTriangle;
		}

		drawingTriangle = CreateShapeSprite("RuntimeDrawingTriangle", (x, y, size) =>
		{
			float fx = (x + 0.5f) / size;
			float fy = (y + 0.5f) / size;
			if (fy < 0.08f || fy > 0.96f)
			{
				return Color.clear;
			}

			float left = 0.5f - fy * 0.46f;
			float right = 0.5f + fy * 0.46f;
			return fx >= left && fx <= right ? Color.white : Color.clear;
		});
		return drawingTriangle;
	}

	// 指定ルールで 32x32 の形状スプライトを作る。
	private static Sprite CreateShapeSprite(string name, System.Func<int, int, int, Color> pixelFn)
	{
		const int size = 32;
		Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
		{
			name = name
		};

		for (int y = 0; y < size; y++)
		{
			for (int x = 0; x < size; x++)
			{
				texture.SetPixel(x, y, pixelFn(x, y, size));
			}
		}

		texture.Apply();
		Sprite sprite = Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), size);
		sprite.name = name;
		return sprite;
	}
}
