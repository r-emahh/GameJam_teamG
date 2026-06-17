using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System;

public readonly struct TutorialCardViewData
{
	public TutorialCardViewData(string playerLabel, string deviceLabel, string roleLabel, string instructionText)
	{
		PlayerLabel = playerLabel ?? string.Empty;
		DeviceLabel = deviceLabel ?? string.Empty;
		RoleLabel = roleLabel ?? string.Empty;
		InstructionText = instructionText ?? string.Empty;
	}

	public string PlayerLabel { get; }
	public string DeviceLabel { get; }
	public string RoleLabel { get; }
	public string InstructionText { get; }
}

public readonly struct PhaseTutorialViewData
{
	public static PhaseTutorialViewData Hidden => new(
		false,
		string.Empty,
		string.Empty,
		string.Empty,
		string.Empty,
		default,
		default);

	public PhaseTutorialViewData(
		bool visible,
		string title,
		string subtitle,
		string timingText,
		string continueText,
		TutorialCardViewData firstCard,
		TutorialCardViewData secondCard)
	{
		Visible = visible;
		Title = title ?? string.Empty;
		Subtitle = subtitle ?? string.Empty;
		TimingText = timingText ?? string.Empty;
		ContinueText = continueText ?? string.Empty;
		FirstCard = firstCard;
		SecondCard = secondCard;
	}

	public bool Visible { get; }
	public string Title { get; }
	public string Subtitle { get; }
	public string TimingText { get; }
	public string ContinueText { get; }
	public TutorialCardViewData FirstCard { get; }
	public TutorialCardViewData SecondCard { get; }
}

[DisallowMultipleComponent]
// 試合情報、最終結果、フェーズチュートリアルを HUD 上へ反映するビュー。
public sealed class MatchHudView : MonoBehaviour
{
	[SerializeField]
	private TextMeshProUGUI stateText;

	[SerializeField]
	private TextMeshProUGUI phaseText;

	[SerializeField]
	private TextMeshProUGUI timerText;

	[SerializeField]
	private TextMeshProUGUI roundText;

	[SerializeField]
	private TextMeshProUGUI launchText;

	[SerializeField]
	private TextMeshProUGUI shapeBudgetText;

	[SerializeField]
	private TextMeshProUGUI shapeText;

	[SerializeField]
	private TextMeshProUGUI cannonText;

	[SerializeField]
	private GameObject launchPowerRoot;

	[SerializeField]
	private TextMeshProUGUI launchPowerText;

	[SerializeField]
	private Image launchPowerFill;

	[SerializeField]
	private TextMeshProUGUI blockerCooldownText;

	[SerializeField]
	private TextMeshProUGUI stunText;

	[SerializeField]
	private TextMeshProUGUI fallCountText;

	[SerializeField]
	private TextMeshProUGUI resultText;

	[SerializeField]
	private TextMeshProUGUI scoreSummaryText;

	[SerializeField]
	private TextMeshProUGUI finalWinnerText;

	[SerializeField]
	private GameObject finalResultRoot;

	[SerializeField]
	private TextMeshProUGUI finalResultHeaderText;

	[SerializeField]
	private TextMeshProUGUI finalResultWinnerText;

	[SerializeField]
	private TextMeshProUGUI finalResultRound1Text;

	[SerializeField]
	private TextMeshProUGUI finalResultRound2Text;

	[SerializeField]
	private TextMeshProUGUI finalResultGoalTimesText;

	[SerializeField]
	private Button retryButton;

	[SerializeField]
	private Button titleButton;

	[SerializeField]
	private GameObject tutorialRoot;

	[SerializeField]
	private TextMeshProUGUI tutorialTitleText;

	[SerializeField]
	private TextMeshProUGUI tutorialSubtitleText;

	[SerializeField]
	private TextMeshProUGUI tutorialTimingText;

	[SerializeField]
	private TextMeshProUGUI tutorialContinueText;

	[SerializeField]
	private TextMeshProUGUI tutorialCard1PlayerText;

	[SerializeField]
	private TextMeshProUGUI tutorialCard1DeviceText;

	[SerializeField]
	private TextMeshProUGUI tutorialCard1RoleText;

	[SerializeField]
	private TextMeshProUGUI tutorialCard1BodyText;

	[SerializeField]
	private TextMeshProUGUI tutorialCard2PlayerText;

	[SerializeField]
	private TextMeshProUGUI tutorialCard2DeviceText;

	[SerializeField]
	private TextMeshProUGUI tutorialCard2RoleText;

	[SerializeField]
	private TextMeshProUGUI tutorialCard2BodyText;

	public void Bind(
		TextMeshProUGUI state,
		TextMeshProUGUI phase,
		TextMeshProUGUI timer,
		TextMeshProUGUI round,
		TextMeshProUGUI launch,
		TextMeshProUGUI shapeBudget,
		TextMeshProUGUI shape,
		TextMeshProUGUI cannon,
		GameObject powerRoot,
		TextMeshProUGUI powerText,
		Image powerFill,
		TextMeshProUGUI blockerCooldown,
		TextMeshProUGUI stun,
		TextMeshProUGUI fallCount,
		TextMeshProUGUI result,
		TextMeshProUGUI scoreSummary,
		TextMeshProUGUI finalWinner,
		GameObject finalResultOverlay,
		TextMeshProUGUI finalResultHeader,
		TextMeshProUGUI finalResultWinner,
		TextMeshProUGUI finalResultRound1,
		TextMeshProUGUI finalResultRound2,
		TextMeshProUGUI finalResultGoalTimes,
		Button retry,
		Button title,
		GameObject tutorialOverlay,
		TextMeshProUGUI tutorialTitle,
		TextMeshProUGUI tutorialSubtitle,
		TextMeshProUGUI tutorialTiming,
		TextMeshProUGUI tutorialContinue,
		TextMeshProUGUI card1Player,
		TextMeshProUGUI card1Device,
		TextMeshProUGUI card1Role,
		TextMeshProUGUI card1Body,
		TextMeshProUGUI card2Player,
		TextMeshProUGUI card2Device,
		TextMeshProUGUI card2Role,
		TextMeshProUGUI card2Body)
	{
		stateText = state;
		phaseText = phase;
		timerText = timer;
		roundText = round;
		launchText = launch;
		shapeBudgetText = shapeBudget;
		shapeText = shape;
		cannonText = cannon;
		launchPowerRoot = powerRoot;
		launchPowerText = powerText;
		launchPowerFill = powerFill;
		blockerCooldownText = blockerCooldown;
		stunText = stun;
		fallCountText = fallCount;
		resultText = result;
		scoreSummaryText = scoreSummary;
		finalWinnerText = finalWinner;
		finalResultRoot = finalResultOverlay;
		finalResultHeaderText = finalResultHeader;
		finalResultWinnerText = finalResultWinner;
		finalResultRound1Text = finalResultRound1;
		finalResultRound2Text = finalResultRound2;
		finalResultGoalTimesText = finalResultGoalTimes;
		retryButton = retry;
		titleButton = title;
		tutorialRoot = tutorialOverlay;
		tutorialTitleText = tutorialTitle;
		tutorialSubtitleText = tutorialSubtitle;
		tutorialTimingText = tutorialTiming;
		tutorialContinueText = tutorialContinue;
		tutorialCard1PlayerText = card1Player;
		tutorialCard1DeviceText = card1Device;
		tutorialCard1RoleText = card1Role;
		tutorialCard1BodyText = card1Body;
		tutorialCard2PlayerText = card2Player;
		tutorialCard2DeviceText = card2Device;
		tutorialCard2RoleText = card2Role;
		tutorialCard2BodyText = card2Body;
	}

	public void BindFinalResultActions(Action retryAction, Action titleAction)
	{
		if (retryButton != null)
		{
			retryButton.onClick.RemoveAllListeners();
			if (retryAction != null)
			{
				retryButton.onClick.AddListener(() => retryAction());
			}
		}

		if (titleButton != null)
		{
			titleButton.onClick.RemoveAllListeners();
			if (titleAction != null)
			{
				titleButton.onClick.AddListener(() => titleAction());
			}
		}
	}

	public void SetGameState(GameState state)
	{
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

	public void SetPhase(MatchPhase phase)
	{
		phaseText.text = $"PHASE: {phase.ToString().ToUpperInvariant()}";
	}

	public void SetTimer(float remaining, float total)
	{
		timerText.text = total > 0f ? $"TIMER: {remaining:0.0}s / {total:0.0}s" : "TIMER: -";
	}

	public void SetRound(int round, MatchSide side)
	{
		string sideLabel = side == MatchSide.GoalRunner ? "GOAL RUNNER" : "BLOCKER";
		roundText.text = round > 0 ? $"ROUND: {round}  /  SIDE: {sideLabel}" : "ROUND: -";
	}

	public void SetLaunchBudget(int goalRunner, int blocker)
	{
		launchText.text = $"LAUNCHES: G {goalRunner} / B {blocker}";
	}

	public void SetDrawingStatus(
		int goalRunnerPoints,
		int goalRunnerMaxPoints,
		bool goalRunnerConfirmed,
		int blockerPoints,
		int blockerMaxPoints,
		bool blockerConfirmed)
	{
		shapeBudgetText.text = $"DRAW POINTS: G {goalRunnerPoints}/{goalRunnerMaxPoints} / B {blockerPoints}/{blockerMaxPoints}";
		shapeText.text = $"DRAW: G {FormatDrawingState(goalRunnerConfirmed)} / B {FormatDrawingState(blockerConfirmed)}";
	}

	public void SetCannonSelection(
		int goalRunnerOrder,
		float goalRunnerAngle,
		int blockerOrder,
		float blockerAngle,
		MatchSide currentSide)
	{
		string goalRunnerTurn = currentSide == MatchSide.GoalRunner ? "*" : string.Empty;
		string blockerTurn = currentSide == MatchSide.Blocker ? "*" : string.Empty;
		cannonText.text = $"CANNON: {goalRunnerTurn}G {FormatSelection(goalRunnerOrder, goalRunnerAngle)} / {blockerTurn}B {FormatSelection(blockerOrder, blockerAngle)}";
	}

	public void SetLaunchPower(bool visible, MatchSide side, float normalizedPower, float power)
	{
		if (launchPowerRoot == null)
		{
			return;
		}

		launchPowerRoot.SetActive(visible);
		if (!visible)
		{
			return;
		}

		string sideLabel = side == MatchSide.GoalRunner ? "GOAL RUNNER" : "BLOCKER";
		launchPowerText.text = $"SHOT POWER ({sideLabel}): {power:0.0}";
		launchPowerFill.fillAmount = Mathf.Clamp01(normalizedPower);
	}

	public void SetBlockerRaceAttackCooldown(bool visible, bool isReady, float remaining, float duration)
	{
		if (!visible)
		{
			blockerCooldownText.text = "BLOCKER SHOT: -";
			return;
		}

		blockerCooldownText.text = isReady
			? "BLOCKER SHOT: READY"
			: $"BLOCKER SHOT: {remaining:0.0}s / {duration:0.0}s";
	}

	public void SetStunStatus(bool goalRunnerStunned, float goalRunnerRemaining, bool blockerStunned, float blockerRemaining)
	{
		stunText.text = $"STUN: G {FormatStunState(goalRunnerStunned, goalRunnerRemaining)} / B {FormatStunState(blockerStunned, blockerRemaining)}";
	}

	public void SetGoalRunnerFallCount(int count)
	{
		fallCountText.text = $"FALLS: G {Mathf.Max(0, count)}";
	}

	public void SetResult(MatchResult result)
	{
		resultText.text = result switch
		{
			MatchResult.None => "RESULT: -",
			MatchResult.GoalRunnerWin => "RESULT: GOAL RUNNER WIN",
			MatchResult.BlockerWin => "RESULT: BLOCKER WIN",
			MatchResult.TimeUp => "RESULT: TIME UP",
			_ => "RESULT: UNKNOWN"
		};
	}

	public void SetScoreSummary(MatchScoreSummary summary)
	{
		scoreSummaryText.text = $"SCORE: P1 {FormatPlayerSummary(summary.Player1RoundWins, summary.Player1AttemptRecorded, summary.Player1GoalSucceeded, summary.Player1GoalTimeSeconds)} / P2 {FormatPlayerSummary(summary.Player2RoundWins, summary.Player2AttemptRecorded, summary.Player2GoalSucceeded, summary.Player2GoalTimeSeconds)}";
		finalWinnerText.text = $"WINNER: {FormatWinner(summary.FinalWinner, summary.IsMatchComplete)}";
	}

	public void SetFinalResultSummary(MatchScoreSummary summary)
	{
		if (finalResultHeaderText == null)
		{
			return;
		}

		finalResultHeaderText.text = "MATCH RESULT";
		finalResultWinnerText.text = $"WINNER: {FormatWinner(summary.FinalWinner, summary.IsMatchComplete)}";
		finalResultRound1Text.text = FormatRoundResult(1, summary.Player1AttemptRecorded, summary.Player1GoalSucceeded, summary.Player1GoalTimeSeconds);
		finalResultRound2Text.text = FormatRoundResult(2, summary.Player2AttemptRecorded, summary.Player2GoalSucceeded, summary.Player2GoalTimeSeconds);
		finalResultGoalTimesText.text = $"GOAL TIME: P1 {FormatGoalTime(summary.Player1AttemptRecorded, summary.Player1GoalSucceeded, summary.Player1GoalTimeSeconds)} / P2 {FormatGoalTime(summary.Player2AttemptRecorded, summary.Player2GoalSucceeded, summary.Player2GoalTimeSeconds)}";
	}

	public void SetFinalResultVisible(bool visible)
	{
		if (finalResultRoot == null)
		{
			return;
		}

		bool wasVisible = finalResultRoot.activeSelf;
		finalResultRoot.SetActive(visible);
		if (!visible || wasVisible)
		{
			return;
		}

		if (EventSystem.current != null && retryButton != null && retryButton.gameObject.activeInHierarchy)
		{
			EventSystem.current.SetSelectedGameObject(retryButton.gameObject);
		}
	}

	public void SetPhaseTutorial(PhaseTutorialViewData tutorial)
	{
		if (tutorialRoot == null)
		{
			return;
		}

		tutorialRoot.SetActive(tutorial.Visible);
		if (!tutorial.Visible)
		{
			return;
		}

		tutorialTitleText.text = tutorial.Title;
		tutorialSubtitleText.text = tutorial.Subtitle;
		tutorialTimingText.text = tutorial.TimingText;
		tutorialContinueText.text = tutorial.ContinueText;
		SetTutorialCard(tutorial.FirstCard, tutorialCard1PlayerText, tutorialCard1DeviceText, tutorialCard1RoleText, tutorialCard1BodyText);
		SetTutorialCard(tutorial.SecondCard, tutorialCard2PlayerText, tutorialCard2DeviceText, tutorialCard2RoleText, tutorialCard2BodyText);
	}

	public void SetIdle()
	{
		stateText.text = "STATE: IDLE";
		phaseText.text = "PHASE: IDLE";
		timerText.text = "TIMER: -";
		roundText.text = "ROUND: -";
		launchText.text = "LAUNCHES: -";
		shapeBudgetText.text = "DRAW POINTS: -";
		shapeText.text = "DRAW: -";
		cannonText.text = "CANNON: -";
		SetLaunchPower(false, MatchSide.GoalRunner, 0f, 0f);
		blockerCooldownText.text = "BLOCKER SHOT: -";
		stunText.text = "STUN: -";
		fallCountText.text = "FALLS: -";
		resultText.text = "RESULT: -";
		scoreSummaryText.text = "SCORE: -";
		finalWinnerText.text = "WINNER: -";
		SetFinalResultSummary(MatchScoreSummary.Empty);
		SetFinalResultVisible(false);
		SetPhaseTutorial(PhaseTutorialViewData.Hidden);
	}

	private static void SetTutorialCard(
		TutorialCardViewData card,
		TextMeshProUGUI playerText,
		TextMeshProUGUI deviceText,
		TextMeshProUGUI roleText,
		TextMeshProUGUI bodyText)
	{
		playerText.text = card.PlayerLabel;
		deviceText.text = card.DeviceLabel;
		roleText.text = card.RoleLabel;
		bodyText.text = card.InstructionText;
	}

	private static string FormatSelection(int order, float angle)
	{
		return order >= 0 ? $"{order + 1} ({angle:+0;-0;0} deg)" : "-";
	}

	private static string FormatPlayerSummary(int roundWins, bool attemptRecorded, bool goalSucceeded, float goalTimeSeconds)
	{
		if (!attemptRecorded)
		{
			return $"{roundWins}W PENDING";
		}

		return goalSucceeded
			? $"{roundWins}W SUCCESS {goalTimeSeconds:0.0}s"
			: $"{roundWins}W FAIL";
	}

	private static string FormatWinner(MatchWinner winner, bool isMatchComplete)
	{
		if (!isMatchComplete)
		{
			return "-";
		}

		return winner switch
		{
			MatchWinner.Player1 => "PLAYER 1",
			MatchWinner.Player2 => "PLAYER 2",
			MatchWinner.Draw => "DRAW",
			_ => "-"
		};
	}

	private static string FormatRoundResult(int round, bool attemptRecorded, bool goalSucceeded, float goalTimeSeconds)
	{
		int goalRunnerPlayer = round == 1 ? 1 : 2;
		int blockerPlayer = goalRunnerPlayer == 1 ? 2 : 1;
		if (!attemptRecorded)
		{
			return $"ROUND {round}: -";
		}

		return goalSucceeded
			? $"ROUND {round}: P{goalRunnerPlayer} GOAL RUNNER WIN ({goalTimeSeconds:0.0}s)"
			: $"ROUND {round}: P{blockerPlayer} BLOCKER WIN (TIME UP)";
	}

	private static string FormatGoalTime(bool attemptRecorded, bool goalSucceeded, float goalTimeSeconds)
	{
		if (!attemptRecorded)
		{
			return "-";
		}

		return goalSucceeded ? $"{goalTimeSeconds:0.0}s" : "FAIL";
	}

	private static string FormatDrawingState(bool confirmed) => confirmed ? "CONFIRMED" : "EDITING";
	private static string FormatStunState(bool stunned, float remaining) => stunned ? $"{remaining:0.0}s" : "-";
}
