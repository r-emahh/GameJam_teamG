using TMPro;
using UnityEngine;
using UnityEngine.UI;

// ランタイムで HUD の Canvas とテキスト群を組み立てる。
public static class RuntimeMatchHudFactory
{
	// HUD Canvas の名前。
	private const string CanvasName = "MatchHUDCanvas";
	// HUD ルートの名前。
	private const string RootName = "MatchHUDRoot";

	// HUD を生成または再利用して返す。
	public static MatchHudView Create(Transform owner)
	{
		GameObject canvasObject = GameObject.Find(CanvasName);
		if (!canvasObject)
		{
			canvasObject = new GameObject(CanvasName);
		}

		canvasObject.transform.SetParent(owner, false);
		Canvas canvas = canvasObject.GetComponent<Canvas>();
		if (!canvas)
		{
			canvas = canvasObject.AddComponent<Canvas>();
		}

		canvas.renderMode = RenderMode.ScreenSpaceOverlay;
		canvas.sortingOrder = 100;

		CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
		if (!scaler)
		{
			scaler = canvasObject.AddComponent<CanvasScaler>();
		}

		scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
		scaler.referenceResolution = new Vector2(1920f, 1080f);
		if (canvasObject.GetComponent<GraphicRaycaster>() == null)
		{
			canvasObject.AddComponent<GraphicRaycaster>();
		}

		Transform existingRoot = canvas.transform.Find(RootName);
		GameObject root = existingRoot != null ? existingRoot.gameObject : CreateRoot(canvas.transform);
		MatchHudView view = root.GetComponent<MatchHudView>();
		if (!view)
		{
			view = root.AddComponent<MatchHudView>();
		}

		view.Bind(
			FindOrCreateText(root.transform, "StateText", new Vector2(0f, 0f), 30),
			FindOrCreateText(root.transform, "PhaseText", new Vector2(0f, -42f), 38),
			FindOrCreateText(root.transform, "TimerText", new Vector2(0f, -94f), 28),
			FindOrCreateText(root.transform, "RoundText", new Vector2(0f, -134f), 24),
			FindOrCreateText(root.transform, "LaunchText", new Vector2(0f, -170f), 24),
			FindOrCreateText(root.transform, "ShapeBudgetText", new Vector2(0f, -206f), 24),
			FindOrCreateText(root.transform, "ShapeText", new Vector2(0f, -242f), 24),
			FindOrCreateText(root.transform, "CannonText", new Vector2(0f, -278f), 24),
			FindOrCreateText(root.transform, "ResultText", new Vector2(0f, -314f), 24));
		return view;
	}

	// HUD のルート RectTransform を作る。
	private static GameObject CreateRoot(Transform parent)
	{
		GameObject root = new GameObject(RootName);
		root.transform.SetParent(parent, false);
		RectTransform rect = root.AddComponent<RectTransform>();
		rect.anchorMin = new Vector2(0f, 1f);
		rect.anchorMax = new Vector2(0f, 1f);
		rect.pivot = new Vector2(0f, 1f);
		rect.anchoredPosition = new Vector2(24f, -24f);
		rect.sizeDelta = new Vector2(780f, 400f);
		return root;
	}

	// 指定名のテキストを取得または生成する。
	private static TextMeshProUGUI FindOrCreateText(Transform parent, string name, Vector2 position, int fontSize)
	{
		Transform existing = parent.Find(name);
		if (existing != null)
		{
			TextMeshProUGUI existingText = existing.GetComponent<TextMeshProUGUI>();
			if (existingText)
			{
				return existingText;
			}
		}

		GameObject textObject = new GameObject(name);
		textObject.transform.SetParent(parent, false);
		RectTransform rect = textObject.AddComponent<RectTransform>();
		rect.anchorMin = new Vector2(0f, 1f);
		rect.anchorMax = new Vector2(0f, 1f);
		rect.pivot = new Vector2(0f, 1f);
		rect.anchoredPosition = position;
		rect.sizeDelta = new Vector2(740f, 40f);

		TextMeshProUGUI text = textObject.AddComponent<TextMeshProUGUI>();
		text.fontSize = fontSize;
		text.color = Color.white;
		text.alignment = TextAlignmentOptions.Left;
		text.textWrappingMode = TextWrappingModes.NoWrap;
		return text;
	}
}
