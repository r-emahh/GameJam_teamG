using TMPro;
using UnityEngine;
using UnityEngine.UI;

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

	// 描画点数ラベルを表示する。
	[SerializeField]
	private TextMeshProUGUI shapeBudgetText;

	// 描画状態と操作ラベルを表示する。
	[SerializeField]
	private TextMeshProUGUI shapeText;

	// 大砲選択ラベルを表示する。
	[SerializeField]
	private TextMeshProUGUI cannonText;

	// Place 中の発射パワー表示ルートを保持する。
	[SerializeField]
	private GameObject launchPowerRoot;

	// Place 中の発射パワー数値を表示する。
	[SerializeField]
	private TextMeshProUGUI launchPowerText;

	// Place 中の発射パワーゲージ充填部を表示する。
	[SerializeField]
	private Image launchPowerFill;

	// Race 中の Blocker 妨害弾クールダウンを表示する。
	[SerializeField]
	private TextMeshProUGUI blockerCooldownText;

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
		GameObject powerRoot,
		TextMeshProUGUI powerText,
		Image powerFill,
		TextMeshProUGUI blockerCooldown,
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
		launchPowerRoot = powerRoot;
		launchPowerText = powerText;
		launchPowerFill = powerFill;
		blockerCooldownText = blockerCooldown;
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

	// 自由描画の点数と確定状態を更新する。
	public void SetDrawingStatus(
		int goalRunnerPoints,
		int goalRunnerMaxPoints,
		bool goalRunnerConfirmed,
		int blockerPoints,
		int blockerMaxPoints,
		bool blockerConfirmed)
	{
		shapeBudgetText.text = $"DRAW POINTS: G {goalRunnerPoints}/{goalRunnerMaxPoints} / B {blockerPoints}/{blockerMaxPoints}";
		shapeText.text = $"DRAW: G {FormatDrawingState(goalRunnerConfirmed)} / B {FormatDrawingState(blockerConfirmed)}  |  ATTACK draw, X clear, B confirm";
	}

	// 大砲選択表示を更新する。
	public void SetCannonSelection(
		int goalRunnerOrder,
		float goalRunnerAngle,
		int blockerOrder,
		float blockerAngle,
		MatchSide currentSide)
	{
		string goalRunnerTurn = currentSide == MatchSide.GoalRunner ? "*" : string.Empty;
		string blockerTurn = currentSide == MatchSide.Blocker ? "*" : string.Empty;
		cannonText.text = $"CANNON: {goalRunnerTurn}G {FormatSelection(goalRunnerOrder, goalRunnerAngle)} / {blockerTurn}B {FormatSelection(blockerOrder, blockerAngle)}  |  AIM RS/D-PAD or R/F";
	}

	// 現在手番プレイヤーの発射パワーだけを表示する。
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
		launchPowerText.text = $"SHOT POWER ({sideLabel}): {power:0.0}  |  ATTACK fire";
		launchPowerFill.fillAmount = Mathf.Clamp01(normalizedPower);
	}

	// Race 中の Blocker 妨害弾クールダウン表示を更新する。
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
		shapeBudgetText.text = "DRAW POINTS: -";
		shapeText.text = "DRAW: -";
		cannonText.text = "CANNON: -";
		SetLaunchPower(false, MatchSide.GoalRunner, 0f, 0f);
		blockerCooldownText.text = "BLOCKER SHOT: -";
		resultText.text = "RESULT: -";
	}

	// 0-based 順序を見やすい表示へ変換する。
	private static string FormatSelection(int order, float angle)
	{
		return order >= 0 ? $"{order + 1} ({angle:+0;-0;0} deg)" : "-";
	}

	private static string FormatDrawingState(bool confirmed) => confirmed ? "CONFIRMED" : "EDITING";
}
