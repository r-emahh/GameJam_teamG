using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;

public static class PlayerInputDisplayFormatter
{
	public static string DescribeAction(PlayerInput playerInput, string actionName)
	{
		if (playerInput == null || playerInput.actions == null || string.IsNullOrEmpty(actionName))
		{
			return "?";
		}

		InputAction action = playerInput.actions[actionName];
		if (action == null)
		{
			return "?";
		}

		string controlScheme = playerInput.currentControlScheme;
		List<string> displays = new();
		for (int i = 0; i < action.bindings.Count; i++)
		{
			InputBinding binding = action.bindings[i];
			if (binding.isPartOfComposite)
			{
				continue;
			}

			if (!string.IsNullOrEmpty(controlScheme)
				&& !string.IsNullOrEmpty(binding.groups)
				&& !binding.groups.Contains(controlScheme))
			{
				continue;
			}

			string display = binding.isComposite && !string.IsNullOrEmpty(binding.name)
				? binding.name
				: action.GetBindingDisplayString(i);
			if (string.IsNullOrEmpty(display))
			{
				display = FallbackDisplay(binding.effectivePath);
			}

			if (!string.IsNullOrEmpty(display) && !displays.Contains(display))
			{
				displays.Add(display.ToUpperInvariant());
			}
		}

		if (displays.Count == 0)
		{
			return "?";
		}

		return string.Join(" / ", displays);
	}

	public static string DescribeDevices(PlayerInput playerInput)
	{
		if (playerInput == null)
		{
			return "NO DEVICE";
		}

		if (playerInput.devices.Count == 0)
		{
			return string.IsNullOrEmpty(playerInput.currentControlScheme)
				? "UNASSIGNED"
				: playerInput.currentControlScheme.ToUpperInvariant();
		}

		List<string> deviceNames = new();
		foreach (InputDevice device in playerInput.devices)
		{
			if (device == null)
			{
				continue;
			}

			string name = string.IsNullOrEmpty(device.displayName) ? device.layout : device.displayName;
			if (!string.IsNullOrEmpty(name) && !deviceNames.Contains(name))
			{
				deviceNames.Add(name.ToUpperInvariant());
			}
		}

		if (deviceNames.Count == 0)
		{
			return string.IsNullOrEmpty(playerInput.currentControlScheme)
				? "UNASSIGNED"
				: playerInput.currentControlScheme.ToUpperInvariant();
		}

		return string.Join(" + ", deviceNames);
	}

	private static string FallbackDisplay(string path)
	{
		return path switch
		{
			"<Gamepad>/buttonSouth" => "A",
			"<Gamepad>/buttonEast" => "B",
			"<Gamepad>/buttonWest" => "X",
			"<Gamepad>/buttonNorth" => "Y",
			"<Gamepad>/leftShoulder" => "LB",
			"<Gamepad>/rightShoulder" => "RB",
			"<Gamepad>/leftStick" => "LEFT STICK",
			"<Gamepad>/rightStick" => "RIGHT STICK",
			"<Gamepad>/dpad/left" => "DPAD LEFT",
			"<Gamepad>/dpad/right" => "DPAD RIGHT",
			"<Gamepad>/dpad/up" => "DPAD UP",
			"<Gamepad>/dpad/down" => "DPAD DOWN",
			"<Keyboard>/space" => "SPACE",
			"<Keyboard>/enter" => "ENTER",
			"<Keyboard>/leftShift" => "LEFT SHIFT",
			"<Keyboard>/x" => "X",
			"<Keyboard>/c" => "C",
			"<Keyboard>/1" => "1",
			"<Keyboard>/2" => "2",
			_ => string.Empty
		};
	}
}

[DisallowMultipleComponent]
// HUD をシーンに応じて生成し、GameManager と接続し、フェーズ開始チュートリアルも制御する。
public sealed class MatchUIBootstrap : MonoBehaviour
{
	private const string BootstrapObjectName = "MatchUIBootstrap";

	private MatchHudView view;
	private GameManager subscribedManager;
	private bool tutorialVisible;
	private MatchPhase tutorialPhase = MatchPhase.Idle;
	private int tutorialRound;

	[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
	private static void Bootstrap()
	{
		if (FindFirstObjectByType<MatchUIBootstrap>() != null)
		{
			return;
		}

		GameObject bootstrapObject = new GameObject(BootstrapObjectName);
		bootstrapObject.AddComponent<MatchUIBootstrap>();
		DontDestroyOnLoad(bootstrapObject);
	}

	private void Awake()
	{
		SceneManager.sceneLoaded += HandleSceneLoaded;
	}

	private void Start()
	{
		HandleSceneLoaded(SceneManager.GetActiveScene(), LoadSceneMode.Single);
	}

	private void Update()
	{
		if (view == null || !view.gameObject.activeSelf)
		{
			return;
		}

		RefreshDrawingStatus();
		RefreshCannonSelection();
		RefreshLaunchPower();
		RefreshBlockerRaceAttackCooldown();
		RefreshStunStatus();
		if (tutorialVisible)
		{
			RefreshPhaseTutorial(force: true);
			TryDismissPhaseTutorial();
		}
	}

	private void OnDestroy()
	{
		SceneManager.sceneLoaded -= HandleSceneLoaded;
		HidePhaseTutorial();
		Unsubscribe();
	}

	private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
	{
		if (!SceneCatalog.IsMatch(scene.name))
		{
			HidePhaseTutorial();
			Unsubscribe();
			if (view != null)
			{
				view.gameObject.SetActive(false);
			}

			return;
		}

		EnsureView();
		ConfigureEventSystem();
		view.gameObject.SetActive(true);
		Subscribe();
		Refresh();
	}

	private void EnsureView()
	{
		if (!view)
		{
			view = RuntimeMatchHudFactory.Create(transform);
			view.BindFinalResultActions(
				() => GameManager.Instance?.RetryMatch(),
				() => GameManager.Instance?.ReturnToTitle());
		}
	}

	private void Subscribe()
	{
		if (subscribedManager == GameManager.Instance)
		{
			return;
		}

		Unsubscribe();
		subscribedManager = GameManager.Instance;
		if (subscribedManager == null)
		{
			return;
		}

		subscribedManager.OnGameStateChanged += view.SetGameState;
		subscribedManager.OnGameStateChanged += HandleGameStateChanged;
		subscribedManager.OnMatchPhaseChanged += view.SetPhase;
		subscribedManager.OnMatchPhaseChanged += HandlePhaseChanged;
		subscribedManager.OnMatchSideChanged += HandleSideChanged;
		subscribedManager.OnRoundChanged += HandleRoundChanged;
		subscribedManager.OnPhaseTimerChanged += view.SetTimer;
		subscribedManager.OnMatchResultChanged += view.SetResult;
		subscribedManager.OnLaunchBudgetChanged += view.SetLaunchBudget;
		subscribedManager.OnGoalRunnerFallCountChanged += view.SetGoalRunnerFallCount;
		subscribedManager.OnMatchScoreSummaryChanged += view.SetScoreSummary;
		subscribedManager.OnMatchScoreSummaryChanged += HandleScoreSummaryChanged;
	}

	private void Unsubscribe()
	{
		if (subscribedManager == null || view == null)
		{
			subscribedManager = null;
			return;
		}

		subscribedManager.OnGameStateChanged -= view.SetGameState;
		subscribedManager.OnGameStateChanged -= HandleGameStateChanged;
		subscribedManager.OnMatchPhaseChanged -= view.SetPhase;
		subscribedManager.OnMatchPhaseChanged -= HandlePhaseChanged;
		subscribedManager.OnMatchSideChanged -= HandleSideChanged;
		subscribedManager.OnRoundChanged -= HandleRoundChanged;
		subscribedManager.OnPhaseTimerChanged -= view.SetTimer;
		subscribedManager.OnMatchResultChanged -= view.SetResult;
		subscribedManager.OnLaunchBudgetChanged -= view.SetLaunchBudget;
		subscribedManager.OnGoalRunnerFallCountChanged -= view.SetGoalRunnerFallCount;
		subscribedManager.OnMatchScoreSummaryChanged -= view.SetScoreSummary;
		subscribedManager.OnMatchScoreSummaryChanged -= HandleScoreSummaryChanged;
		subscribedManager = null;
	}

	private void Refresh()
	{
		if (view == null || GameManager.Instance == null)
		{
			view?.SetIdle();
			return;
		}

		GameManager manager = GameManager.Instance;
		view.SetGameState(GameManager.currentState);
		view.SetPhase(manager.CurrentPhase);
		view.SetRound(manager.CurrentRound, manager.CurrentSide);
		view.SetTimer(manager.CurrentPhaseTimeRemaining, manager.CurrentPhaseDuration);
		view.SetResult(manager.CurrentResult);
		view.SetLaunchBudget(manager.GoalRunnerLaunchesRemaining, manager.BlockerLaunchesRemaining);
		view.SetGoalRunnerFallCount(manager.GoalRunnerFallCount);
		view.SetScoreSummary(manager.CurrentScoreSummary);
		RefreshFinalResult();
		RefreshDrawingStatus();
		RefreshCannonSelection();
		RefreshLaunchPower();
		RefreshBlockerRaceAttackCooldown();
		RefreshStunStatus();
		RefreshPhaseTutorial();
	}

	private void HandleSideChanged(MatchSide side)
	{
		view.SetRound(GameManager.Instance?.CurrentRound ?? 0, side);
	}

	private void HandleRoundChanged(int round)
	{
		view.SetRound(round, GameManager.Instance?.CurrentSide ?? MatchSide.GoalRunner);
		RefreshFinalResult();
		RefreshPhaseTutorial();
	}

	private void HandlePhaseChanged(MatchPhase _)
	{
		RefreshFinalResult();
		RefreshPhaseTutorial();
	}

	private void HandleGameStateChanged(GameState _)
	{
		RefreshFinalResult();
		RefreshPhaseTutorial();
	}

	private void HandleScoreSummaryChanged(MatchScoreSummary _)
	{
		RefreshFinalResult();
	}

	private void RefreshFinalResult()
	{
		if (view == null)
		{
			return;
		}

		GameManager manager = GameManager.Instance;
		if (manager == null)
		{
			view.SetFinalResultSummary(MatchScoreSummary.Empty);
			view.SetFinalResultVisible(false);
			return;
		}

		view.SetFinalResultSummary(manager.CurrentScoreSummary);
		view.SetFinalResultVisible(manager.IsFinalResultVisible);
	}

	private static void ConfigureEventSystem()
	{
		EventSystem current = EventSystem.current;
		if (current == null)
		{
			return;
		}

		InputSystemUIInputModule module = current.GetComponent<InputSystemUIInputModule>();
		if (module == null)
		{
			return;
		}

		if (module.move == null || module.submit == null || module.cancel == null)
		{
			module.AssignDefaultActions();
		}
	}

	private void RefreshPhaseTutorial(bool force = false)
	{
		if (view == null)
		{
			return;
		}

		GameManager manager = GameManager.Instance;
		if (manager == null || GameManager.currentState != GameState.Game || !ShouldShowPhaseTutorial(manager.CurrentPhase))
		{
			if (tutorialVisible)
			{
				HidePhaseTutorial();
			}
			else
			{
				view.SetPhaseTutorial(PhaseTutorialViewData.Hidden);
			}

			return;
		}

		bool phaseChanged = !tutorialVisible
			|| tutorialPhase != manager.CurrentPhase
			|| tutorialRound != manager.CurrentRound;
		if (!phaseChanged && !force)
		{
			return;
		}

		PhaseTutorialViewData tutorial = BuildPhaseTutorial(manager);
		tutorialVisible = true;
		tutorialPhase = manager.CurrentPhase;
		tutorialRound = manager.CurrentRound;
		manager.SetPhaseTimerPaused(manager.ShouldPausePhaseTimerDuringTutorial);
		InputManager.Instance?.SetGameplayInputEnabledForAllPlayers(false);
		view.SetPhaseTutorial(tutorial);
	}

	private void TryDismissPhaseTutorial()
	{
		if (!tutorialVisible)
		{
			return;
		}

		foreach (PlayerController player in GetOrderedPlayers())
		{
			PlayerInput playerInput = InputManager.Instance?.GetPlayerInput(player);
			InputAction jumpAction = playerInput?.actions?["Jump"];
			if (jumpAction != null && jumpAction.WasPressedThisFrame())
			{
				HidePhaseTutorial();
				return;
			}
		}
	}

	private void HidePhaseTutorial()
	{
		tutorialVisible = false;
		tutorialPhase = MatchPhase.Idle;
		tutorialRound = 0;
		GameManager.Instance?.SetPhaseTimerPaused(false);
		InputManager.Instance?.SetGameplayInputEnabledForAllPlayers(true);
		view?.SetPhaseTutorial(PhaseTutorialViewData.Hidden);
	}

	private PhaseTutorialViewData BuildPhaseTutorial(GameManager manager)
	{
		List<PlayerController> players = GetOrderedPlayers();
		bool detailed = manager.CurrentRound <= 1;
		TutorialCardViewData firstCard = players.Count > 0
			? BuildTutorialCard(players[0], manager.CurrentPhase, detailed)
			: new TutorialCardViewData("PLAYER 1", "UNASSIGNED", "ROLE: -", "NO PLAYER REGISTERED.");
		TutorialCardViewData secondCard = players.Count > 1
			? BuildTutorialCard(players[1], manager.CurrentPhase, detailed)
			: new TutorialCardViewData("PLAYER 2", "UNASSIGNED", "ROLE: -", "WAITING FOR PLAYER 2.");

		string phaseLabel = manager.CurrentPhase.ToString().ToUpperInvariant();
		string subtitle = detailed
			? $"ROUND {manager.CurrentRound} START  |  FULL TUTORIAL"
			: $"ROUND {manager.CurrentRound} START  |  QUICK REMINDER";
		string timingText = manager.ShouldPausePhaseTimerDuringTutorial
			? "PHASE TIMER: PAUSED UNTIL THIS PANEL IS CLOSED"
			: "PHASE TIMER: CONTINUES WHILE THIS PANEL IS OPEN";
		string continueText = BuildContinueText(players);
		return new PhaseTutorialViewData(
			true,
			$"{phaseLabel} PHASE",
			subtitle,
			timingText,
			continueText,
			firstCard,
			secondCard);
	}

	private TutorialCardViewData BuildTutorialCard(PlayerController player, MatchPhase phase, bool detailed)
	{
		int playerIndex = InputManager.Instance?.GetPlayerIndex(player) ?? -1;
		PlayerInput playerInput = InputManager.Instance?.GetPlayerInput(player);
		string playerLabel = playerIndex >= 0 ? $"PLAYER {playerIndex + 1}" : player.name.ToUpperInvariant();
		string deviceLabel = PlayerInputDisplayFormatter.DescribeDevices(playerInput);
		string roleLabel = player.ControlledSide == MatchSide.GoalRunner ? "ROLE: GOAL RUNNER" : "ROLE: BLOCKER";
		string body = detailed
			? BuildDetailedTutorialText(player.ControlledSide, phase, playerInput)
			: BuildShortTutorialText(player.ControlledSide, phase, playerInput);
		return new TutorialCardViewData(playerLabel, deviceLabel, roleLabel, body);
	}

	private static string BuildDetailedTutorialText(MatchSide side, MatchPhase phase, PlayerInput playerInput)
	{
		string move = PlayerInputDisplayFormatter.DescribeAction(playerInput, "Move");
		string jump = PlayerInputDisplayFormatter.DescribeAction(playerInput, "Jump");
		string dash = PlayerInputDisplayFormatter.DescribeAction(playerInput, "Sprint");
		string draw = PlayerInputDisplayFormatter.DescribeAction(playerInput, "Attack");
		string clear = PlayerInputDisplayFormatter.DescribeAction(playerInput, "CycleShape");
		string confirm = PlayerInputDisplayFormatter.DescribeAction(playerInput, "Crouch");
		string previous = PlayerInputDisplayFormatter.DescribeAction(playerInput, "Previous");
		string next = PlayerInputDisplayFormatter.DescribeAction(playerInput, "Next");
		string aim = PlayerInputDisplayFormatter.DescribeAction(playerInput, "Aim");
		string fire = PlayerInputDisplayFormatter.DescribeAction(playerInput, "Attack");
		string stop = PlayerInputDisplayFormatter.DescribeAction(playerInput, "Crouch");

		return phase switch
		{
			MatchPhase.Draw => side == MatchSide.GoalRunner
				? $"Move the cursor with {move}. Hold {draw} to draw. Use {clear} to clear the current shape and {confirm} to lock it in. Your confirmed shape becomes your projectile for Place."
				: $"Move the cursor with {move}. Hold {draw} to sketch obstacles or traps. Use {clear} to reset the current shape and {confirm} to confirm it before Place begins.",
			MatchPhase.Place => side == MatchSide.GoalRunner
				? $"When your side is active, choose a cannon with {previous} and {next}, aim with {aim}, and fire with {fire}. Use {stop} to stop your current projectile early."
				: $"Wait for the turn marker to reach Blocker. Then choose a cannon with {previous} and {next}, aim with {aim}, and fire with {fire}. Use {stop} to stop your current projectile early.",
			MatchPhase.Race => side == MatchSide.GoalRunner
				? $"Move with {move}, jump with {jump}, and dash with {dash}. Reach the goal before time runs out and avoid blocker attacks."
				: $"Blocker cannot move during Race. Fire stun shots with {fire} from the selected cannon whenever the cooldown is READY to slow the Goal Runner.",
			_ => string.Empty
		};
	}

	private static string BuildShortTutorialText(MatchSide side, MatchPhase phase, PlayerInput playerInput)
	{
		string move = PlayerInputDisplayFormatter.DescribeAction(playerInput, "Move");
		string jump = PlayerInputDisplayFormatter.DescribeAction(playerInput, "Jump");
		string dash = PlayerInputDisplayFormatter.DescribeAction(playerInput, "Sprint");
		string draw = PlayerInputDisplayFormatter.DescribeAction(playerInput, "Attack");
		string confirm = PlayerInputDisplayFormatter.DescribeAction(playerInput, "Crouch");
		string previous = PlayerInputDisplayFormatter.DescribeAction(playerInput, "Previous");
		string next = PlayerInputDisplayFormatter.DescribeAction(playerInput, "Next");
		string aim = PlayerInputDisplayFormatter.DescribeAction(playerInput, "Aim");
		string fire = PlayerInputDisplayFormatter.DescribeAction(playerInput, "Attack");

		return phase switch
		{
			MatchPhase.Draw => side == MatchSide.GoalRunner
				? $"Draw with {draw}. Confirm with {confirm}."
				: $"Make obstacles with {draw}. Confirm with {confirm}.",
			MatchPhase.Place => $"Select with {previous} / {next}, aim with {aim}, fire with {fire}.",
			MatchPhase.Race => side == MatchSide.GoalRunner
				? $"Run with {move}, jump with {jump}, dash with {dash}."
				: $"Stay on cannons and stun with {fire} when READY.",
			_ => string.Empty
		};
	}

	private static bool ShouldShowPhaseTutorial(MatchPhase phase)
	{
		return phase == MatchPhase.Draw || phase == MatchPhase.Place || phase == MatchPhase.Race;
	}

	private static string BuildContinueText(IReadOnlyList<PlayerController> players)
	{
		if (players == null || players.Count == 0)
		{
			return "PRESS JUMP TO START";
		}

		List<string> prompts = new();
		for (int i = 0; i < players.Count; i++)
		{
			PlayerInput playerInput = InputManager.Instance?.GetPlayerInput(players[i]);
			string jump = PlayerInputDisplayFormatter.DescribeAction(playerInput, "Jump");
			prompts.Add($"P{i + 1} {jump}");
		}

		return $"PRESS {string.Join("  /  ", prompts)} TO START";
	}

	private static List<PlayerController> GetOrderedPlayers()
	{
		List<PlayerController> orderedPlayers = new();
		if (InputManager.Instance == null)
		{
			return orderedPlayers;
		}

		foreach (PlayerController player in InputManager.Instance.GetRegisteredPlayers())
		{
			if (player != null)
			{
				orderedPlayers.Add(player);
			}
		}

		orderedPlayers.Sort((left, right) => InputManager.Instance.GetPlayerIndex(left).CompareTo(InputManager.Instance.GetPlayerIndex(right)));
		return orderedPlayers;
	}

	private void RefreshCannonSelection()
	{
		if (view == null)
		{
			return;
		}

		if (GameManager.Instance == null || GameManager.currentState != GameState.Game || GameManager.Instance.CurrentPhase != MatchPhase.Place || InputManager.Instance == null)
		{
			view.SetCannonSelection(-1, 0f, -1, 0f, MatchSide.GoalRunner);
			return;
		}

		int goalRunnerOrder = -1;
		float goalRunnerAngle = 0f;
		int blockerOrder = -1;
		float blockerAngle = 0f;
		foreach (PlayerController player in InputManager.Instance.GetRegisteredPlayers())
		{
			if (player == null)
			{
				continue;
			}

			if (player.ControlledSide == MatchSide.GoalRunner)
			{
				goalRunnerOrder = player.SelectedCannonOrder;
				goalRunnerAngle = player.SelectedCannonAngle;
			}
			else if (player.ControlledSide == MatchSide.Blocker)
			{
				blockerOrder = player.SelectedCannonOrder;
				blockerAngle = player.SelectedCannonAngle;
			}
		}

		view.SetCannonSelection(
			goalRunnerOrder,
			goalRunnerAngle,
			blockerOrder,
			blockerAngle,
			GameManager.Instance.CurrentSide);
	}

	private void RefreshLaunchPower()
	{
		if (view == null)
		{
			return;
		}

		if (GameManager.Instance == null
			|| GameManager.currentState != GameState.Game
			|| GameManager.Instance.CurrentPhase != MatchPhase.Place
			|| InputManager.Instance == null)
		{
			view.SetLaunchPower(false, MatchSide.GoalRunner, 0f, 0f);
			return;
		}

		MatchSide currentSide = GameManager.Instance.CurrentSide;
		foreach (PlayerController player in InputManager.Instance.GetRegisteredPlayers())
		{
			if (player == null || player.ControlledSide != currentSide || !player.IsPreparingCannonLaunch)
			{
				continue;
			}

			view.SetLaunchPower(
				true,
				currentSide,
				player.NormalizedCannonLaunchPower,
				player.CannonLaunchPower);
			return;
		}

		view.SetLaunchPower(false, currentSide, 0f, 0f);
	}

	private void RefreshBlockerRaceAttackCooldown()
	{
		if (view == null)
		{
			return;
		}

		bool isRace = GameManager.Instance != null
			&& GameManager.currentState == GameState.Game
			&& GameManager.Instance.CurrentPhase == MatchPhase.Race;
		if (!isRace || InputManager.Instance == null)
		{
			view.SetBlockerRaceAttackCooldown(false, true, 0f, 0f);
			return;
		}

		foreach (PlayerController player in InputManager.Instance.GetRegisteredPlayers())
		{
			if (player == null || player.ControlledSide != MatchSide.Blocker)
			{
				continue;
			}

			view.SetBlockerRaceAttackCooldown(
				true,
				player.IsBlockerRaceAttackReady,
				player.BlockerRaceAttackCooldownRemaining,
				player.BlockerRaceAttackCooldownDuration);
			return;
		}

		view.SetBlockerRaceAttackCooldown(false, true, 0f, 0f);
	}

	private void RefreshStunStatus()
	{
		if (view == null || InputManager.Instance == null)
		{
			view?.SetStunStatus(false, 0f, false, 0f);
			return;
		}

		bool goalRunnerStunned = false;
		float goalRunnerRemaining = 0f;
		bool blockerStunned = false;
		float blockerRemaining = 0f;
		foreach (PlayerController player in InputManager.Instance.GetRegisteredPlayers())
		{
			if (player == null)
			{
				continue;
			}

			if (player.ControlledSide == MatchSide.GoalRunner)
			{
				goalRunnerStunned = player.IsStunned;
				goalRunnerRemaining = player.StunTimeRemaining;
			}
			else if (player.ControlledSide == MatchSide.Blocker)
			{
				blockerStunned = player.IsStunned;
				blockerRemaining = player.StunTimeRemaining;
			}
		}

		view.SetStunStatus(goalRunnerStunned, goalRunnerRemaining, blockerStunned, blockerRemaining);
	}

	private void RefreshDrawingStatus()
	{
		if (view == null)
		{
			return;
		}

		if (GameManager.Instance == null || GameManager.currentState != GameState.Game || GameManager.Instance.CurrentPhase != MatchPhase.Draw || InputManager.Instance == null)
		{
			view.SetDrawingStatus(0, 0, false, 0, 0, false);
			return;
		}

		int goalRunnerPoints = 0;
		int goalRunnerMaxPoints = 0;
		bool goalRunnerConfirmed = false;
		int blockerPoints = 0;
		int blockerMaxPoints = 0;
		bool blockerConfirmed = false;
		foreach (PlayerController player in InputManager.Instance.GetRegisteredPlayers())
		{
			if (player == null)
			{
				continue;
			}

			if (player.ControlledSide == MatchSide.GoalRunner)
			{
				goalRunnerPoints = player.DrawingPointCount;
				goalRunnerMaxPoints = player.DrawingMaxPointCount;
				goalRunnerConfirmed = player.IsDrawingConfirmed;
			}
			else if (player.ControlledSide == MatchSide.Blocker)
			{
				blockerPoints = player.DrawingPointCount;
				blockerMaxPoints = player.DrawingMaxPointCount;
				blockerConfirmed = player.IsDrawingConfirmed;
			}
		}

		view.SetDrawingStatus(
			goalRunnerPoints,
			goalRunnerMaxPoints,
			goalRunnerConfirmed,
			blockerPoints,
			blockerMaxPoints,
			blockerConfirmed);
	}
}
