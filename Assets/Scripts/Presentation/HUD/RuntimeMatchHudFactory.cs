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

		GameObject launchPowerRoot = FindOrCreatePowerGauge(
			root.transform,
			out TextMeshProUGUI launchPowerText,
			out Image launchPowerFill);

		view.Bind(
			FindOrCreateText(root.transform, "StateText", new Vector2(0f, 0f), 30),
			FindOrCreateText(root.transform, "PhaseText", new Vector2(0f, -42f), 38),
			FindOrCreateText(root.transform, "TimerText", new Vector2(0f, -94f), 28),
			FindOrCreateText(root.transform, "RoundText", new Vector2(0f, -134f), 24),
			FindOrCreateText(root.transform, "LaunchText", new Vector2(0f, -170f), 24),
			FindOrCreateText(root.transform, "DrawingUsageText", new Vector2(0f, -206f), 24),
			FindOrCreateText(root.transform, "DrawingStateText", new Vector2(0f, -242f), 24),
			FindOrCreateText(root.transform, "CannonText", new Vector2(0f, -278f), 24),
			launchPowerRoot,
			launchPowerText,
			launchPowerFill,
			FindOrCreateText(root.transform, "BlockerCooldownText", new Vector2(0f, -386f), 24),
			FindOrCreateText(root.transform, "ResultText", new Vector2(0f, -422f), 24));
		return view;
	}

	// HUD のルート RectTransform を作る。
	private static GameObject CreateRoot(Transform parent)
	{
		GameObject root = new GameObject(RootName, typeof(RectTransform));
		root.transform.SetParent(parent, false);
		RectTransform rect = root.GetComponent<RectTransform>();
		rect.anchorMin = new Vector2(0f, 1f);
		rect.anchorMax = new Vector2(0f, 1f);
		rect.pivot = new Vector2(0f, 1f);
		rect.anchoredPosition = new Vector2(24f, -24f);
		rect.sizeDelta = new Vector2(780f, 500f);
		return root;
	}

	// 発射パワーのラベル、背景、充填部を取得または生成する。
	private static GameObject FindOrCreatePowerGauge(
		Transform parent,
		out TextMeshProUGUI label,
		out Image fill)
	{
		Transform existing = parent.Find("LaunchPowerGauge");
		GameObject root = existing != null ? existing.gameObject : new GameObject("LaunchPowerGauge", typeof(RectTransform));
		root.transform.SetParent(parent, false);

		RectTransform rootRect = root.GetComponent<RectTransform>();
		if (rootRect == null)
		{
			Object.Destroy(root);
			root = new GameObject("LaunchPowerGauge", typeof(RectTransform));
			root.transform.SetParent(parent, false);
			rootRect = root.GetComponent<RectTransform>();
		}
		rootRect.anchorMin = new Vector2(0f, 1f);
		rootRect.anchorMax = new Vector2(0f, 1f);
		rootRect.pivot = new Vector2(0f, 1f);
		rootRect.anchoredPosition = new Vector2(0f, -314f);
		rootRect.sizeDelta = new Vector2(740f, 64f);

		label = FindOrCreateText(root.transform, "Label", Vector2.zero, 24);
		label.rectTransform.sizeDelta = new Vector2(740f, 30f);

		Image background = FindOrCreateImage(root.transform, "Background", new Color(0f, 0f, 0f, 0.65f));
		RectTransform backgroundRect = background.rectTransform;
		backgroundRect.anchorMin = new Vector2(0f, 1f);
		backgroundRect.anchorMax = new Vector2(0f, 1f);
		backgroundRect.pivot = new Vector2(0f, 1f);
		backgroundRect.anchoredPosition = new Vector2(0f, -32f);
		backgroundRect.sizeDelta = new Vector2(520f, 24f);

		fill = FindOrCreateImage(background.transform, "Fill", new Color(1f, 0.65f, 0.12f, 1f));
		RectTransform fillRect = fill.rectTransform;
		fillRect.anchorMin = Vector2.zero;
		fillRect.anchorMax = Vector2.one;
		fillRect.pivot = new Vector2(0f, 0.5f);
		fillRect.anchoredPosition = Vector2.zero;
		fillRect.sizeDelta = Vector2.zero;
		fill.type = Image.Type.Filled;
		fill.fillMethod = Image.FillMethod.Horizontal;
		fill.fillOrigin = (int)Image.OriginHorizontal.Left;
		return root;
	}

	// 指定名の単色 Image を取得または生成する。
	private static Image FindOrCreateImage(Transform parent, string name, Color color)
	{
		Transform existing = parent.Find(name);
		GameObject imageObject = existing != null ? existing.gameObject : new GameObject(name, typeof(RectTransform));
		imageObject.transform.SetParent(parent, false);
		RectTransform rect = imageObject.GetComponent<RectTransform>();
		if (rect == null)
		{
			Object.Destroy(imageObject);
			imageObject = new GameObject(name, typeof(RectTransform));
			imageObject.transform.SetParent(parent, false);
			rect = imageObject.GetComponent<RectTransform>();
		}
		Image image = imageObject.GetComponent<Image>() ?? imageObject.AddComponent<Image>();
		image.color = color;
		return image;
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

		GameObject textObject = new GameObject(name, typeof(RectTransform));
		textObject.transform.SetParent(parent, false);
		RectTransform rect = textObject.GetComponent<RectTransform>();
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
