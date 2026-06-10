using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MatchUIBootstrap : MonoBehaviour
{
	private const string BootstrapObjectName = "MatchUIBootstrap";
	private const string CanvasName = "MatchHUDCanvas";
	private const string RootName = "MatchHUDRoot";
	private const string StateTextName = "StateText";
	private const string PhaseTextName = "PhaseText";
	private const string TimerTextName = "TimerText";
	private const string RoundTextName = "RoundText";
	private const string LaunchTextName = "LaunchText";
	private const string ResultTextName = "ResultText";

	private Canvas canvas;
	private TextMeshProUGUI stateText;
	private TextMeshProUGUI phaseText;
	private TextMeshProUGUI timerText;
	private TextMeshProUGUI roundText;
	private TextMeshProUGUI launchText;
	private TextMeshProUGUI resultText;

	[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
	private static void Bootstrap()
	{
		if (FindFirstObjectByType<MatchUIBootstrap>() != null)
		{
			return;
		}

		GameObject bootstrapObject = new GameObject(BootstrapObjectName);
		bootstrapObject.AddComponent<MatchUIBootstrap>();
		Object.DontDestroyOnLoad(bootstrapObject);
	}

	private void Awake()
	{
		SceneManager.sceneLoaded += HandleSceneLoaded;
	}

	private void OnDestroy()
	{
		SceneManager.sceneLoaded -= HandleSceneLoaded;
		UnsubscribeGameManager();
	}

	private void Start()
	{
		EnsureUi();
		SubscribeGameManager();
		RefreshFromGameManager();
	}

	private void Update()
	{
		UpdateLaunchText();
	}

	private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
	{
		if (scene.name != "Game" && scene.name != "Rema" && scene.name != "Title")
		{
			return;
		}

		EnsureUi();
		RefreshFromGameManager();
	}

	private void EnsureUi()
	{
		if (canvas != null)
		{
			return;
		}

		GameObject canvasObject = GameObject.Find(CanvasName);
		if (canvasObject == null)
		{
			canvasObject = new GameObject(CanvasName);
			canvasObject.transform.SetParent(transform);
			canvas = canvasObject.AddComponent<Canvas>();
			canvas.renderMode = RenderMode.ScreenSpaceOverlay;
			canvas.sortingOrder = 100;
			canvasObject.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
			canvasObject.GetComponent<CanvasScaler>().referenceResolution = new Vector2(1920f, 1080f);
			canvasObject.AddComponent<GraphicRaycaster>();
		}
		else
		{
			canvas = canvasObject.GetComponent<Canvas>();
		}

		Transform root = canvas.transform.Find(RootName);
		if (root == null)
		{
			GameObject rootObject = new GameObject(RootName);
			rootObject.transform.SetParent(canvas.transform, false);

			RectTransform rootRect = rootObject.AddComponent<RectTransform>();
			rootRect.anchorMin = new Vector2(0f, 1f);
			rootRect.anchorMax = new Vector2(0f, 1f);
			rootRect.pivot = new Vector2(0f, 1f);
			rootRect.anchoredPosition = new Vector2(24f, -24f);
			rootRect.sizeDelta = new Vector2(780f, 260f);

			stateText = CreateText(rootObject.transform, StateTextName, new Vector2(0f, 0f), 30);
			phaseText = CreateText(rootObject.transform, PhaseTextName, new Vector2(0f, -42f), 38);
			timerText = CreateText(rootObject.transform, TimerTextName, new Vector2(0f, -94f), 28);
			roundText = CreateText(rootObject.transform, RoundTextName, new Vector2(0f, -134f), 24);
			launchText = CreateText(rootObject.transform, LaunchTextName, new Vector2(0f, -170f), 24);
			resultText = CreateText(rootObject.transform, ResultTextName, new Vector2(0f, -206f), 24);
		}
		else
		{
			stateText = root.Find(StateTextName)?.GetComponent<TextMeshProUGUI>();
			phaseText = root.Find(PhaseTextName)?.GetComponent<TextMeshProUGUI>();
			timerText = root.Find(TimerTextName)?.GetComponent<TextMeshProUGUI>();
			roundText = root.Find(RoundTextName)?.GetComponent<TextMeshProUGUI>();
			launchText = root.Find(LaunchTextName)?.GetComponent<TextMeshProUGUI>();
			resultText = root.Find(ResultTextName)?.GetComponent<TextMeshProUGUI>();
		}
	}

	private TextMeshProUGUI CreateText(Transform parent, string objectName, Vector2 anchoredPosition, int fontSize)
	{
		GameObject textObject = new GameObject(objectName);
		textObject.transform.SetParent(parent, false);

		RectTransform rectTransform = textObject.AddComponent<RectTransform>();
		rectTransform.anchorMin = new Vector2(0f, 1f);
		rectTransform.anchorMax = new Vector2(0f, 1f);
		rectTransform.pivot = new Vector2(0f, 1f);
		rectTransform.anchoredPosition = anchoredPosition;
		rectTransform.sizeDelta = new Vector2(740f, 40f);

		TextMeshProUGUI text = textObject.AddComponent<TextMeshProUGUI>();
		text.fontSize = fontSize;
		text.color = Color.white;
		text.text = "";
		text.alignment = TextAlignmentOptions.Left;
		text.textWrappingMode = TextWrappingModes.NoWrap;

		return text;
	}

	private void SubscribeGameManager()
	{
		if (GameManager.Instance == null)
		{
			return;
		}

		GameManager.Instance.OnGameStateChanged += HandleGameStateChanged;
		GameManager.Instance.OnMatchPhaseChanged += HandlePhaseChanged;
		GameManager.Instance.OnMatchSideChanged += HandleSideChanged;
		GameManager.Instance.OnRoundChanged += HandleRoundChanged;
		GameManager.Instance.OnPhaseTimerChanged += HandleTimerChanged;
		GameManager.Instance.OnMatchResultChanged += HandleResultChanged;
	}

	private void UnsubscribeGameManager()
	{
		if (GameManager.Instance == null)
		{
			return;
		}

		GameManager.Instance.OnGameStateChanged -= HandleGameStateChanged;
		GameManager.Instance.OnMatchPhaseChanged -= HandlePhaseChanged;
		GameManager.Instance.OnMatchSideChanged -= HandleSideChanged;
		GameManager.Instance.OnRoundChanged -= HandleRoundChanged;
		GameManager.Instance.OnPhaseTimerChanged -= HandleTimerChanged;
		GameManager.Instance.OnMatchResultChanged -= HandleResultChanged;
	}

	private void RefreshFromGameManager()
	{
		if (GameManager.Instance == null)
		{
			SetIdleText();
			return;
		}

		HandleGameStateChanged(GameManager.currentState);
		HandlePhaseChanged(GameManager.Instance.CurrentPhase);
		HandleSideChanged(GameManager.Instance.CurrentSide);
		HandleRoundChanged(GameManager.Instance.CurrentRound);
		HandleTimerChanged(GameManager.Instance.CurrentPhaseTimeRemaining, GameManager.Instance.CurrentPhaseDuration);
		HandleResultChanged(GameManager.Instance.CurrentResult);
	}

	private void HandleGameStateChanged(GameState state)
	{
		if (stateText == null)
		{
			return;
		}

		string label = state switch
		{
			GameState.Title => "TITLE",
			GameState.Game => "GAME",
			GameState.Pause => "PAUSE",
			GameState.GameOver => "GAME OVER",
			_ => "UNKNOWN"
		};

		stateText.text = $"STATE: {label}";
	}

	private void HandlePhaseChanged(MatchPhase phase)
	{
		if (phaseText == null)
		{
			return;
		}

		string label = phase switch
		{
			MatchPhase.Idle => "IDLE",
			MatchPhase.Draw => "DRAW",
			MatchPhase.Place => "PLACE",
			MatchPhase.Race => "RACE",
			MatchPhase.Result => "RESULT",
			_ => "UNKNOWN"
		};

		phaseText.text = $"PHASE: {label}";
		UpdateLaunchText();
	}

	private void HandleSideChanged(MatchSide side)
	{
		if (roundText == null)
		{
			return;
		}

		string label = side == MatchSide.GoalRunner ? "GOAL RUNNER" : "BLOCKER";
		roundText.text = $"ROUND SIDE: {label}";
	}

	private void HandleRoundChanged(int round)
	{
		if (roundText == null)
		{
			return;
		}

		string sideLabel = GameManager.Instance == null || GameManager.Instance.CurrentSide == MatchSide.GoalRunner ? "GOAL RUNNER" : "BLOCKER";
		roundText.text = $"ROUND: {round}  /  SIDE: {sideLabel}";
	}

	private void HandleTimerChanged(float remaining, float total)
	{
		if (timerText == null)
		{
			return;
		}

		if (total <= 0f)
		{
			timerText.text = "TIMER: -";
			return;
		}

		timerText.text = $"TIMER: {remaining:0.0}s / {total:0.0}s";
	}

	private void HandleResultChanged(MatchResult result)
	{
		if (resultText == null)
		{
			return;
		}

		string label = result switch
		{
			MatchResult.None => "RESULT: -",
			MatchResult.GoalRunnerWin => "RESULT: GOAL RUNNER WIN",
			MatchResult.BlockerWin => "RESULT: BLOCKER WIN",
			MatchResult.TimeUp => "RESULT: TIME UP",
			_ => "RESULT: UNKNOWN"
		};

		resultText.text = label;
		UpdateLaunchText();
	}

	private void UpdateLaunchText()
	{
		if (launchText == null || GameManager.Instance == null)
		{
			return;
		}

		launchText.text = $"LAUNCHES: G {GameManager.Instance.GoalRunnerLaunchesRemaining} / B {GameManager.Instance.BlockerLaunchesRemaining}";
	}

	private void SetIdleText()
	{
		if (stateText != null)
		{
			stateText.text = "STATE: IDLE";
		}

		if (phaseText != null)
		{
			phaseText.text = "PHASE: IDLE";
		}

		if (timerText != null)
		{
			timerText.text = "TIMER: -";
		}

		if (roundText != null)
		{
			roundText.text = "ROUND: -";
		}

		if (launchText != null)
		{
			launchText.text = "LAUNCHES: -";
		}

		if (resultText != null)
		{
			resultText.text = "RESULT: -";
		}
	}
}
