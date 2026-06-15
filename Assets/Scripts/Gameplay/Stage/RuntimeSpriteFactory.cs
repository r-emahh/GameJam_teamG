using UnityEngine;

// ランタイム生成物で使う汎用スプライトを共有生成する。
public static class RuntimeSpriteFactory
{
	// 単色正方形スプライトをキャッシュする。
	private static Sprite unitSquare;

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
}
