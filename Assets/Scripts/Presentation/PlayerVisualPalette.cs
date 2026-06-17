using UnityEngine;

// プレイヤーごとの表示色をまとめる共通パレット。
public static class PlayerVisualPalette
{
	private static readonly Color PlayerOneColor = new Color(0.25f, 0.65f, 1f, 1f);
	private static readonly Color PlayerTwoColor = new Color(1f, 0.35f, 0.35f, 1f);

	public static Color GetPlayerColor(int playerIndex)
	{
		return playerIndex == 0 ? PlayerOneColor : playerIndex == 1 ? PlayerTwoColor : Color.white;
	}

	public static Color GetPlayerColor(DrawingPlayerSlot slot)
	{
		return slot == DrawingPlayerSlot.PlayerOne ? PlayerOneColor : PlayerTwoColor;
	}
}
