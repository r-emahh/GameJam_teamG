using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

// ランタイムで HUD の Canvas と各表示領域を組み立てる。
public static class RuntimeMatchHudFactory
{
	private const string CanvasName = "MatchHUDCanvas";
	private const string RootName = "MatchHUDRoot";
	private const string HudPanelName = "HudPanel";
	private const string TutorialPanelName = "TutorialPanel";
	private const string ResultOverlayName = "ResultUI";

	public static MatchHudView Create(Transform owner)
	{
		GameObject canvasObject = GameObject.Find(CanvasName);
		if (!canvasObject)
		{
			canvasObject = new GameObject(CanvasName);
		}

		canvasObject.transform.SetParent(owner, false);
		Canvas canvas = canvasObject.GetComponent<Canvas>();
		if (canvas == null)
		{
			canvas = canvasObject.AddComponent<Canvas>();
		}
		canvas.renderMode = RenderMode.ScreenSpaceOverlay;
		canvas.sortingOrder = 100;

		CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
		if (scaler == null)
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
		Transform hudPanel = FindOrCreatePanel(
			root.transform,
			HudPanelName,
			new Vector2(0f, 1f),
			new Vector2(0f, 1f),
			new Vector2(0f, 1f),
			new Vector2(24f, -24f),
			new Vector2(580f, 660f),
			new Color(0.05f, 0.07f, 0.11f, 0.82f)).transform;
		Transform tutorialPanel = FindOrCreatePanel(
			root.transform,
			TutorialPanelName,
			new Vector2(1f, 1f),
			new Vector2(1f, 1f),
			new Vector2(1f, 1f),
			new Vector2(-24f, -24f),
			new Vector2(820f, 620f),
			new Color(0.04f, 0.05f, 0.09f, 0.94f)).transform;

		MatchHudView view = root.GetComponent<MatchHudView>() ?? root.AddComponent<MatchHudView>();

		GameObject launchPowerRoot = FindOrCreatePowerGauge(
			hudPanel,
			out TextMeshProUGUI launchPowerText,
			out Image launchPowerFill);
		GameObject resultOverlay = FindOrCreateResultOverlay(root.transform);
		TextMeshProUGUI resultHeader = FindOrCreateOverlayText(resultOverlay.transform, "ResultHeaderText", new Vector2(0f, -56f), new Vector2(720f, 64f), 42, FontStyles.Bold);
		TextMeshProUGUI resultWinner = FindOrCreateOverlayText(resultOverlay.transform, "ResultWinnerText", new Vector2(0f, -138f), new Vector2(720f, 48f), 32, FontStyles.Bold);
		TextMeshProUGUI resultRound1 = FindOrCreateOverlayText(resultOverlay.transform, "Round1ResultText", new Vector2(0f, -220f), new Vector2(720f, 44f), 26, FontStyles.Normal);
		TextMeshProUGUI resultRound2 = FindOrCreateOverlayText(resultOverlay.transform, "Round2ResultText", new Vector2(0f, -272f), new Vector2(720f, 44f), 26, FontStyles.Normal);
		TextMeshProUGUI resultGoalTimes = FindOrCreateOverlayText(resultOverlay.transform, "GoalTimeText", new Vector2(0f, -336f), new Vector2(720f, 44f), 24, FontStyles.Normal);
		TextMeshProUGUI resultHint = FindOrCreateOverlayText(resultOverlay.transform, "ResultHintText", new Vector2(0f, -396f), new Vector2(720f, 36f), 22, FontStyles.Italic);
		resultHint.text = "D-PAD / STICK: SELECT   SOUTH / ENTER: SUBMIT";

		Button retryButton = FindOrCreateButton(resultOverlay.transform, "ButtonRetry", "ButtonRetryText", "RETRY", new Vector2(0f, -486f));
		Button titleButton = FindOrCreateButton(resultOverlay.transform, "ButtonTitle", "ButtonTitleText", "TITLE", new Vector2(0f, -564f));
		ConfigureVerticalNavigation(retryButton, titleButton);

		TextMeshProUGUI tutorialTitle = FindOrCreateText(tutorialPanel, "TutorialTitleText", new Vector2(28f, -24f), 36);
		tutorialTitle.fontStyle = FontStyles.Bold;
		tutorialTitle.alignment = TextAlignmentOptions.Left;
		TextMeshProUGUI tutorialSubtitle = FindOrCreateText(tutorialPanel, "TutorialSubtitleText", new Vector2(28f, -72f), 22);
		tutorialSubtitle.color = new Color(0.82f, 0.88f, 0.96f, 1f);
		TextMeshProUGUI tutorialTiming = FindOrCreateText(tutorialPanel, "TutorialTimingText", new Vector2(28f, -112f), 22);
		tutorialTiming.color = new Color(1f, 0.85f, 0.48f, 1f);
		TextMeshProUGUI tutorialContinue = FindOrCreateText(tutorialPanel, "TutorialContinueText", new Vector2(28f, -548f), 22);
		tutorialContinue.fontStyle = FontStyles.Bold;
		tutorialContinue.color = new Color(0.94f, 0.96f, 1f, 1f);

		Transform tutorialCard1 = FindOrCreatePanel(
			tutorialPanel,
			"TutorialCard1",
			new Vector2(0f, 1f),
			new Vector2(0f, 1f),
			new Vector2(0f, 1f),
			new Vector2(28f, -164f),
			new Vector2(360f, 352f),
			new Color(0.09f, 0.12f, 0.18f, 0.92f)).transform;
		Transform tutorialCard2 = FindOrCreatePanel(
			tutorialPanel,
			"TutorialCard2",
			new Vector2(1f, 1f),
			new Vector2(1f, 1f),
			new Vector2(1f, 1f),
			new Vector2(-28f, -164f),
			new Vector2(360f, 352f),
			new Color(0.09f, 0.12f, 0.18f, 0.92f)).transform;

		view.Bind(
			FindOrCreateText(hudPanel, "StateText", new Vector2(20f, -20f), 26),
			FindOrCreateText(hudPanel, "PhaseText", new Vector2(20f, -58f), 34),
			FindOrCreateText(hudPanel, "TimerText", new Vector2(20f, -104f), 24),
			FindOrCreateText(hudPanel, "RoundText", new Vector2(20f, -140f), 22),
			FindOrCreateText(hudPanel, "LaunchText", new Vector2(20f, -176f), 22),
			FindOrCreateText(hudPanel, "DrawingUsageText", new Vector2(20f, -212f), 22),
			FindOrCreateText(hudPanel, "DrawingStateText", new Vector2(20f, -248f), 22),
			FindOrCreateText(hudPanel, "CannonText", new Vector2(20f, -284f), 22),
			launchPowerRoot,
			launchPowerText,
			launchPowerFill,
			FindOrCreateText(hudPanel, "BlockerCooldownText", new Vector2(20f, -392f), 22),
			FindOrCreateText(hudPanel, "StunText", new Vector2(20f, -428f), 22),
			FindOrCreateText(hudPanel, "FallCountText", new Vector2(20f, -464f), 22),
			FindOrCreateText(hudPanel, "ResultText", new Vector2(20f, -500f), 22),
			FindOrCreateText(hudPanel, "ScoreSummaryText", new Vector2(20f, -536f), 20),
			FindOrCreateText(hudPanel, "FinalWinnerText", new Vector2(20f, -572f), 22),
			resultOverlay,
			resultHeader,
			resultWinner,
			resultRound1,
			resultRound2,
			resultGoalTimes,
			retryButton,
			titleButton,
			tutorialPanel.gameObject,
			tutorialTitle,
			tutorialSubtitle,
			tutorialTiming,
			tutorialContinue,
			FindOrCreateCardTitle(tutorialCard1, "TutorialCard1PlayerText", new Vector2(16f, -16f), 22),
			FindOrCreateCardMeta(tutorialCard1, "TutorialCard1DeviceText", new Vector2(16f, -48f), 18),
			FindOrCreateCardRole(tutorialCard1, "TutorialCard1RoleText", new Vector2(16f, -82f), 20),
			FindOrCreateCardBody(tutorialCard1, "TutorialCard1BodyText", new Vector2(16f, -120f), new Vector2(328f, 212f), 18),
			FindOrCreateCardTitle(tutorialCard2, "TutorialCard2PlayerText", new Vector2(16f, -16f), 22),
			FindOrCreateCardMeta(tutorialCard2, "TutorialCard2DeviceText", new Vector2(16f, -48f), 18),
			FindOrCreateCardRole(tutorialCard2, "TutorialCard2RoleText", new Vector2(16f, -82f), 20),
			FindOrCreateCardBody(tutorialCard2, "TutorialCard2BodyText", new Vector2(16f, -120f), new Vector2(328f, 212f), 18));
		view.SetFinalResultVisible(false);
		view.SetPhaseTutorial(PhaseTutorialViewData.Hidden);
		return view;
	}

	private static GameObject CreateRoot(Transform parent)
	{
		GameObject root = new GameObject(RootName, typeof(RectTransform));
		root.transform.SetParent(parent, false);
		RectTransform rect = root.GetComponent<RectTransform>();
		rect.anchorMin = Vector2.zero;
		rect.anchorMax = Vector2.one;
		rect.pivot = new Vector2(0.5f, 0.5f);
		rect.anchoredPosition = Vector2.zero;
		rect.sizeDelta = Vector2.zero;
		return root;
	}

	private static GameObject FindOrCreatePanel(
		Transform parent,
		string name,
		Vector2 anchorMin,
		Vector2 anchorMax,
		Vector2 pivot,
		Vector2 anchoredPosition,
		Vector2 size,
		Color color)
	{
		Transform existing = parent.Find(name);
		GameObject panelObject = existing != null ? existing.gameObject : new GameObject(name, typeof(RectTransform));
		panelObject.transform.SetParent(parent, false);

		RectTransform rect = panelObject.GetComponent<RectTransform>();
		if (rect == null)
		{
			Object.Destroy(panelObject);
			panelObject = new GameObject(name, typeof(RectTransform));
			panelObject.transform.SetParent(parent, false);
			rect = panelObject.GetComponent<RectTransform>();
		}

		rect.anchorMin = anchorMin;
		rect.anchorMax = anchorMax;
		rect.pivot = pivot;
		rect.anchoredPosition = anchoredPosition;
		rect.sizeDelta = size;

		Image image = panelObject.GetComponent<Image>() ?? panelObject.AddComponent<Image>();
		image.color = color;
		return panelObject;
	}

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
		rootRect.anchoredPosition = new Vector2(20f, -320f);
		rootRect.sizeDelta = new Vector2(520f, 64f);

		label = FindOrCreateText(root.transform, "Label", Vector2.zero, 22);
		label.rectTransform.anchoredPosition = Vector2.zero;
		label.rectTransform.sizeDelta = new Vector2(520f, 30f);

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

	private static GameObject FindOrCreateResultOverlay(Transform parent)
	{
		GameObject overlay = FindOrCreatePanel(
			parent,
			ResultOverlayName,
			new Vector2(0.5f, 0.5f),
			new Vector2(0.5f, 0.5f),
			new Vector2(0.5f, 0.5f),
			Vector2.zero,
			new Vector2(820f, 700f),
			new Color(0.05f, 0.07f, 0.11f, 0.94f));
		if (overlay.GetComponent<CanvasGroup>() == null)
		{
			overlay.AddComponent<CanvasGroup>();
		}

		return overlay;
	}

	private static TextMeshProUGUI FindOrCreateOverlayText(Transform parent, string name, Vector2 position, Vector2 size, int fontSize, FontStyles style)
	{
		TextMeshProUGUI text = FindOrCreateText(parent, name, Vector2.zero, fontSize);
		RectTransform rect = text.rectTransform;
		rect.anchorMin = new Vector2(0.5f, 1f);
		rect.anchorMax = new Vector2(0.5f, 1f);
		rect.pivot = new Vector2(0.5f, 1f);
		rect.anchoredPosition = position;
		rect.sizeDelta = size;
		text.alignment = TextAlignmentOptions.Center;
		text.fontStyle = style;
		text.textWrappingMode = TextWrappingModes.NoWrap;
		return text;
	}

	private static Button FindOrCreateButton(Transform parent, string buttonName, string textName, string label, Vector2 anchoredPosition)
	{
		Transform existing = parent.Find(buttonName);
		GameObject buttonObject = existing != null ? existing.gameObject : new GameObject(buttonName, typeof(RectTransform));
		buttonObject.transform.SetParent(parent, false);

		RectTransform rect = buttonObject.GetComponent<RectTransform>();
		if (rect == null)
		{
			Object.Destroy(buttonObject);
			buttonObject = new GameObject(buttonName, typeof(RectTransform));
			buttonObject.transform.SetParent(parent, false);
			rect = buttonObject.GetComponent<RectTransform>();
		}

		rect.anchorMin = new Vector2(0.5f, 1f);
		rect.anchorMax = new Vector2(0.5f, 1f);
		rect.pivot = new Vector2(0.5f, 1f);
		rect.anchoredPosition = anchoredPosition;
		rect.sizeDelta = new Vector2(280f, 56f);

		Image image = buttonObject.GetComponent<Image>() ?? buttonObject.AddComponent<Image>();
		image.color = new Color(0.92f, 0.92f, 0.92f, 1f);

		Button button = buttonObject.GetComponent<Button>() ?? buttonObject.AddComponent<Button>();
		ColorBlock colors = button.colors;
		colors.normalColor = new Color(0.92f, 0.92f, 0.92f, 1f);
		colors.highlightedColor = new Color(1f, 1f, 1f, 1f);
		colors.pressedColor = new Color(0.75f, 0.75f, 0.75f, 1f);
		colors.selectedColor = new Color(1f, 0.82f, 0.32f, 1f);
		colors.disabledColor = new Color(0.5f, 0.5f, 0.5f, 0.6f);
		button.colors = colors;
		button.targetGraphic = image;

		TextMeshProUGUI text = FindOrCreateText(buttonObject.transform, textName, Vector2.zero, 28);
		RectTransform textRect = text.rectTransform;
		textRect.anchorMin = Vector2.zero;
		textRect.anchorMax = Vector2.one;
		textRect.pivot = new Vector2(0.5f, 0.5f);
		textRect.anchoredPosition = Vector2.zero;
		textRect.sizeDelta = Vector2.zero;
		text.alignment = TextAlignmentOptions.Center;
		text.color = new Color(0.08f, 0.08f, 0.08f, 1f);
		text.text = label;
		return button;
	}

	private static void ConfigureVerticalNavigation(Button upper, Button lower)
	{
		if (upper == null || lower == null)
		{
			return;
		}

		Navigation upperNav = upper.navigation;
		upperNav.mode = Navigation.Mode.Explicit;
		upperNav.selectOnDown = lower;
		upperNav.selectOnUp = lower;
		upper.navigation = upperNav;

		Navigation lowerNav = lower.navigation;
		lowerNav.mode = Navigation.Mode.Explicit;
		lowerNav.selectOnDown = upper;
		lowerNav.selectOnUp = upper;
		lower.navigation = lowerNav;
	}

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
		rect.sizeDelta = new Vector2(520f, 40f);

		TextMeshProUGUI text = textObject.AddComponent<TextMeshProUGUI>();
		text.fontSize = fontSize;
		text.color = Color.white;
		text.alignment = TextAlignmentOptions.Left;
		text.textWrappingMode = TextWrappingModes.NoWrap;
		return text;
	}

	private static TextMeshProUGUI FindOrCreateCardTitle(Transform parent, string name, Vector2 position, int fontSize)
	{
		TextMeshProUGUI text = FindOrCreateText(parent, name, position, fontSize);
		text.fontStyle = FontStyles.Bold;
		text.rectTransform.sizeDelta = new Vector2(328f, 28f);
		return text;
	}

	private static TextMeshProUGUI FindOrCreateCardMeta(Transform parent, string name, Vector2 position, int fontSize)
	{
		TextMeshProUGUI text = FindOrCreateText(parent, name, position, fontSize);
		text.color = new Color(0.75f, 0.82f, 0.92f, 1f);
		text.rectTransform.sizeDelta = new Vector2(328f, 24f);
		return text;
	}

	private static TextMeshProUGUI FindOrCreateCardRole(Transform parent, string name, Vector2 position, int fontSize)
	{
		TextMeshProUGUI text = FindOrCreateText(parent, name, position, fontSize);
		text.color = new Color(1f, 0.84f, 0.42f, 1f);
		text.fontStyle = FontStyles.Bold;
		text.rectTransform.sizeDelta = new Vector2(328f, 28f);
		return text;
	}

	private static TextMeshProUGUI FindOrCreateCardBody(Transform parent, string name, Vector2 position, Vector2 size, int fontSize)
	{
		TextMeshProUGUI text = FindOrCreateText(parent, name, position, fontSize);
		RectTransform rect = text.rectTransform;
		rect.sizeDelta = size;
		text.textWrappingMode = TextWrappingModes.Normal;
		text.overflowMode = TextOverflowModes.Overflow;
		return text;
	}
}
