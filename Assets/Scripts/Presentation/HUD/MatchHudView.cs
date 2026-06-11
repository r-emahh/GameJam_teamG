using TMPro;
using UnityEngine;

[DisallowMultipleComponent]
// 試合情報をテキストへ反映する HUD ビュー。
public sealed class MatchHudView : MonoBehaviour
{
	// 現在状態ラベルを表示する。
	[SerializeField]
	private TextMeshProUGUI stateText;

	// フェーズラベルを表示する。
	[SerializeField]
	private TextMeshProUGUI phaseText;

	// タイマーラベルを表示する。
	[SerializeField]
	private TextMeshProUGUI timerText;

	// ラウンドラベルを表示する。
	[SerializeField]
	private TextMeshProUGUI roundText;

	// 発射回数ラベルを表示する。
	[SerializeField]
	private TextMeshProUGUI launchText;

	// 図形残数ラベルを表示する。
	[SerializeField]
	private TextMeshProUGUI shapeBudgetText;

	// 図形ラベルを表示する。
	[SerializeField]
	private TextMeshProUGUI shapeText;

	// 大砲選択ラベルを表示する。
	[SerializeField]
	private TextMeshProUGUI cannonText;

	// 結果ラベルを表示する。
	[SerializeField]
	private TextMeshProUGUI resultText;

	// 生成済みのテキスト参照を結び直す。
	public void Bind(
		TextMeshProUGUI state,
		TextMeshProUGUI phase,
		TextMeshProUGUI timer,
		TextMeshProUGUI round,
		TextMeshProUGUI launch,
		TextMeshProUGUI shapeBudget,
		TextMeshProUGUI shape,
		TextMeshProUGUI cannon,
		TextMeshProUGUI result)
	{
		stateText = state;
		phaseText = phase;
		timerText = timer;
		roundText = round;
		launchText = launch;
		shapeBudgetText = shapeBudget;
		shapeText = shape;
		cannonText = cannon;
		resultText = result;
	}

	// ゲーム状態表示を更新する。
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

	// フェーズ表示を更新する。
	public void SetPhase(MatchPhase phase)
	{
		phaseText.text = $"PHASE: {phase.ToString().ToUpperInvariant()}";
	}

	// タイマー表示を更新する。
	public void SetTimer(float remaining, float total)
	{
		timerText.text = total > 0f ? $"TIMER: {remaining:0.0}s / {total:0.0}s" : "TIMER: -";
	}

	// ラウンド表示を更新する。
	public void SetRound(int round, MatchSide side)
	{
		string sideLabel = side == MatchSide.GoalRunner ? "GOAL RUNNER" : "BLOCKER";
		roundText.text = round > 0 ? $"ROUND: {round}  /  SIDE: {sideLabel}" : "ROUND: -";
	}

	// 発射回数表示を更新する。
	public void SetLaunchBudget(int goalRunner, int blocker)
	{
		launchText.text = $"LAUNCHES: G {goalRunner} / B {blocker}";
	}

	// 図形残数を更新する。
	public void SetShapeBudget(int goalRunner, int blocker)
	{
		shapeBudgetText.text = $"STAMPS: G {goalRunner} / B {blocker}";
	}

	// 図形表示を更新する。
	public void SetDrawingShape(DrawingStampShape goalRunnerShape, DrawingStampShape blockerShape)
	{
		shapeText.text = $"SHAPE: G {FormatShape(goalRunnerShape)} / B {FormatShape(blockerShape)}";
	}

	// 大砲選択表示を更新する。
	public void SetCannonSelection(int goalRunnerOrder, int blockerOrder)
	{
		cannonText.text = $"CANNON: G {FormatSelection(goalRunnerOrder)} / B {FormatSelection(blockerOrder)}";
	}

	// 結果表示を更新する。
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

	// 初期状態表示へ戻す。
	public void SetIdle()
	{
		stateText.text = "STATE: IDLE";
		phaseText.text = "PHASE: IDLE";
		timerText.text = "TIMER: -";
		roundText.text = "ROUND: -";
		launchText.text = "LAUNCHES: -";
		shapeBudgetText.text = "STAMPS: -";
		shapeText.text = "SHAPE: -";
		cannonText.text = "CANNON: -";
		resultText.text = "RESULT: -";
	}

	// 0-based 順序を見やすい表示へ変換する。
	private static string FormatSelection(int order)
	{
		return order >= 0 ? (order + 1).ToString() : "-";
	}

	// 図形名を短く表示する。
	private static string FormatShape(DrawingStampShape shape)
	{
		return shape switch
		{
			DrawingStampShape.Square => "SQUARE",
			DrawingStampShape.Circle => "CIRCLE",
			DrawingStampShape.Triangle => "TRIANGLE",
			_ => "UNKNOWN"
		};
	}
}
