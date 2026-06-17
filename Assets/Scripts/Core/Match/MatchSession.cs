using System;
using UnityEngine;

// 1試合分の状態遷移と通知を担う、Unity 非依存の試合ロジック本体。
public sealed class MatchSession
{
	private const int MaxRounds = 2;
	private const float GoalTimeTieThresholdSeconds = 0.001f;

	// 試合設定からフェーズ時間や弾数を取得する。
	private readonly MatchConfiguration configuration;
	// ラウンドごとの図形数を抽選する関数を保持する。
	private readonly Func<int, int, int> randomRange;
	// 各プレイヤーのラウンド勝利数を保持する。
	private readonly int[] playerRoundWins = new int[2];
	// 各プレイヤーが Goal Runner を担当した結果を保持する。
	private readonly bool[] playerAttemptRecorded = new bool[2];
	private readonly bool[] playerGoalSucceeded = new bool[2];
	private readonly float[] playerGoalTimes = new float[2];
	private bool finalResultExpired;
	private bool phaseTimerPaused;
	private float raceElapsedTime;

	// 現在のフェーズを保持する。
	public MatchPhase Phase { get; private set; } = MatchPhase.Idle;
	// 現在操作している陣営を保持する。
	public MatchSide Side { get; private set; } = MatchSide.GoalRunner;
	// 現在のラウンド結果を保持する。
	public MatchResult Result { get; private set; } = MatchResult.None;
	// 現在のラウンド番号を保持する。
	public int Round { get; private set; }
	// 現在フェーズの残り時間を保持する。
	public float TimeRemaining { get; private set; }
	// 現在フェーズの総時間を保持する。
	public float PhaseDuration { get; private set; }
	// Goal Runner 側の残り発射回数を保持する。
	public int GoalRunnerLaunches { get; private set; }
	// Blocker 側の残り発射回数を保持する。
	public int BlockerLaunches { get; private set; }
	// Goal Runner 側の残り図形数を保持する。
	public int GoalRunnerShapes { get; private set; }
	// Blocker 側の残り図形数を保持する。
	public int BlockerShapes { get; private set; }
	// 現在の試合集計を保持する。
	public MatchScoreSummary ScoreSummary { get; private set; } = MatchScoreSummary.Empty;
	// 2ラウンド集計後の最終勝者を返す。
	public MatchWinner FinalWinner => ScoreSummary.FinalWinner;
	// Idle 以外かつ最終結果表示の消化前なら試合進行中とみなす。
	public bool IsRunning => Phase != MatchPhase.Idle && !finalResultExpired;
	// 2ラウンドの集計が確定済みかを返す。
	public bool IsMatchComplete => ScoreSummary.IsMatchComplete;
	// フェーズタイマー停止中かを返す。
	public bool IsPhaseTimerPaused => phaseTimerPaused;

	// フェーズ変更を外部へ通知する。
	public event Action<MatchPhase> PhaseChanged;
	// 陣営変更を外部へ通知する。
	public event Action<MatchSide> SideChanged;
	// ラウンド変更を外部へ通知する。
	public event Action<int> RoundChanged;
	// 次ラウンド状態の確定後、表示向け通知の前に外部へ通知する。
	public event Action<int> RoundAdvanced;
	// タイマー変更を外部へ通知する。
	public event Action<float, float> TimerChanged;
	// 結果変更を外部へ通知する。
	public event Action<MatchResult> ResultChanged;
	// 発射回数の変更を外部へ通知する。
	public event Action<int, int> LaunchBudgetChanged;
	// 図形数の変更を外部へ通知する。
	public event Action<int, int> ShapeBudgetChanged;
	// 試合集計の変更を外部へ通知する。
	public event Action<MatchScoreSummary> ScoreSummaryChanged;

	// 設定を受け取り、試合進行に必要な値を保持する。
	public MatchSession(MatchConfiguration configuration)
		: this(configuration, UnityEngine.Random.Range)
	{
	}

	// テストから抽選結果を制御できる試合セッションを作る。
	public MatchSession(MatchConfiguration configuration, Func<int, int, int> randomRange)
	{
		this.configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
		this.randomRange = randomRange ?? throw new ArgumentNullException(nameof(randomRange));
	}

	// 新しい試合を開始し、初期ラウンドと初期フェーズへ入る。
	public void Begin()
	{
		phaseTimerPaused = false;
		ResetScoreSummary();
		StartRound(1, false);
	}

	// フレームごとの経過時間を反映し、必要なら次のフェーズへ進める。
	public void Tick(float deltaTime)
	{
		if (!IsRunning)
		{
			return;
		}

		if (phaseTimerPaused)
		{
			return;
		}

		if (TimeRemaining <= 0f)
		{
			AdvancePhase();
			return;
		}

		float consumedTime = Mathf.Min(deltaTime, TimeRemaining);
		TimeRemaining = Mathf.Max(0f, TimeRemaining - consumedTime);
		if (Phase == MatchPhase.Race)
		{
			raceElapsedTime = Mathf.Min(PhaseDuration, raceElapsedTime + consumedTime);
		}

		TimerChanged?.Invoke(TimeRemaining, PhaseDuration);
	}

	// 指定陣営の発射回数を1減らし、必要なら操作権を切り替える。
	public bool TryConsumeLaunch(MatchSide side)
	{
		if (side == MatchSide.GoalRunner)
		{
			if (GoalRunnerLaunches <= 0)
			{
				return false;
			}

			GoalRunnerLaunches--;
			LaunchBudgetChanged?.Invoke(GoalRunnerLaunches, BlockerLaunches);
			if (GoalRunnerLaunches == 0)
			{
				SetSide(MatchSide.Blocker);
			}

			return true;
		}

		if (BlockerLaunches <= 0)
		{
			return false;
		}

		BlockerLaunches--;
		LaunchBudgetChanged?.Invoke(GoalRunnerLaunches, BlockerLaunches);
		if (BlockerLaunches == 0)
		{
			SetSide(MatchSide.GoalRunner);
			SetPhase(MatchPhase.Race);
		}

		return true;
	}

	// Race 中に Goal Runner が到達した場合だけ、結果フェーズへ遷移する。
	public bool TryMarkGoalReached(MatchSide side)
	{
		if (Phase != MatchPhase.Race
			|| side != MatchSide.GoalRunner
			|| Result != MatchResult.None)
		{
			return false;
		}

		RecordRoundOutcome(goalReached: true);
		Result = MatchResult.GoalRunnerWin;
		ResultChanged?.Invoke(Result);
		SetPhase(MatchPhase.Result);
		return true;
	}

	// 制限時間切れとして結果を確定する。
	public void MarkTimeUp()
	{
		if (Result != MatchResult.None)
		{
			return;
		}

		RecordRoundOutcome(goalReached: false);
		Result = MatchResult.TimeUp;
		ResultChanged?.Invoke(Result);
		SetPhase(MatchPhase.Result);
	}

	// 試合状態を初期化し、待機状態へ戻す。
	public void Reset()
	{
		Phase = MatchPhase.Idle;
		Side = MatchSide.GoalRunner;
		Result = MatchResult.None;
		Round = 0;
		TimeRemaining = 0f;
		PhaseDuration = 0f;
		GoalRunnerLaunches = 0;
		BlockerLaunches = 0;
		GoalRunnerShapes = 0;
		BlockerShapes = 0;
		raceElapsedTime = 0f;
		finalResultExpired = false;
		phaseTimerPaused = false;
		ResetScoreSummary();
		PhaseChanged?.Invoke(Phase);
		SideChanged?.Invoke(Side);
		RoundChanged?.Invoke(Round);
		ResultChanged?.Invoke(Result);
		LaunchBudgetChanged?.Invoke(0, 0);
		ShapeBudgetChanged?.Invoke(0, 0);
		TimerChanged?.Invoke(0f, 0f);
		ScoreSummaryChanged?.Invoke(ScoreSummary);
	}

	// 指定陣営の図形を1減らし、必要なら使えなくする。
	public bool TryConsumeShape(MatchSide side)
	{
		if (side == MatchSide.GoalRunner)
		{
			if (GoalRunnerShapes <= 0)
			{
				return false;
			}

			GoalRunnerShapes--;
			ShapeBudgetChanged?.Invoke(GoalRunnerShapes, BlockerShapes);
			return true;
		}

		if (BlockerShapes <= 0)
		{
			return false;
		}

		BlockerShapes--;
		ShapeBudgetChanged?.Invoke(GoalRunnerShapes, BlockerShapes);
		return true;
	}

	// 現在フェーズのタイマー進行を一時停止または再開する。
	public void SetPhaseTimerPaused(bool paused)
	{
		phaseTimerPaused = paused;
	}

	// 現フェーズの終了後に進むべき次のフェーズを決める。
	private void AdvancePhase()
	{
		switch (Phase)
		{
			case MatchPhase.Draw:
				SetPhase(MatchPhase.Place);
				break;
			case MatchPhase.Place:
				SetPhase(MatchPhase.Race);
				break;
			case MatchPhase.Race:
				MarkTimeUp();
				break;
			case MatchPhase.Result:
				if (Round >= MaxRounds)
				{
					finalResultExpired = true;
					TimeRemaining = 0f;
					PhaseDuration = 0f;
					TimerChanged?.Invoke(0f, 0f);
					break;
				}

				AdvanceRound();
				break;
		}
	}

	// 次ラウンドへ進み、ラウンド開始状態を作り直す。
	private void AdvanceRound()
	{
		StartRound(Round + 1, true);
	}

	// フェーズを確定し、タイマーと必要な初期値を更新する。
	private void SetPhase(MatchPhase phase)
	{
		Phase = phase;
		if (Phase == MatchPhase.Race)
		{
			SetSide(MatchSide.GoalRunner);
			raceElapsedTime = 0f;
		}

		PhaseDuration = configuration.GetDuration(Phase);
		TimeRemaining = PhaseDuration;
		PhaseChanged?.Invoke(Phase);
		TimerChanged?.Invoke(TimeRemaining, PhaseDuration);
	}

	// ラウンド開始時に図形配布数をランダム決定する。
	private void AssignShapeBudgets()
	{
		GoalRunnerShapes = randomRange(configuration.MinDrawingStampCount, configuration.MaxDrawingStampCount + 1);
		BlockerShapes = randomRange(configuration.MinBlockerStampCount, configuration.MaxBlockerStampCount + 1);
	}

	// ラウンド状態をすべて確定してから、リセット処理と表示更新を順番に通知する。
	private void StartRound(int round, bool notifyRoundAdvanced)
	{
		finalResultExpired = false;
		phaseTimerPaused = false;
		Round = round;
		Result = MatchResult.None;
		Side = MatchSide.GoalRunner;
		GoalRunnerLaunches = configuration.GoalRunnerLaunchCount;
		BlockerLaunches = configuration.BlockerLaunchCount;
		AssignShapeBudgets();
		Phase = MatchPhase.Draw;
		PhaseDuration = configuration.GetDuration(Phase);
		TimeRemaining = PhaseDuration;

		if (notifyRoundAdvanced)
		{
			RoundAdvanced?.Invoke(Round);
		}

		RoundChanged?.Invoke(Round);
		ResultChanged?.Invoke(Result);
		SideChanged?.Invoke(Side);
		LaunchBudgetChanged?.Invoke(GoalRunnerLaunches, BlockerLaunches);
		ShapeBudgetChanged?.Invoke(GoalRunnerShapes, BlockerShapes);
		PhaseChanged?.Invoke(Phase);
		TimerChanged?.Invoke(TimeRemaining, PhaseDuration);
	}

	private void RecordRoundOutcome(bool goalReached)
	{
		int goalRunnerPlayerNumber = GetGoalRunnerPlayerNumber(Round);
		int blockerPlayerNumber = GetBlockerPlayerNumber(Round);
		int goalRunnerIndex = goalRunnerPlayerNumber - 1;
		int blockerIndex = blockerPlayerNumber - 1;

		playerAttemptRecorded[goalRunnerIndex] = true;
		playerGoalSucceeded[goalRunnerIndex] = goalReached;
		playerGoalTimes[goalRunnerIndex] = goalReached ? raceElapsedTime : 0f;

		if (goalReached)
		{
			playerRoundWins[goalRunnerIndex]++;
		}
		else
		{
			playerRoundWins[blockerIndex]++;
		}

		UpdateScoreSummary();
	}

	private void ResetScoreSummary()
	{
		Array.Clear(playerRoundWins, 0, playerRoundWins.Length);
		Array.Clear(playerAttemptRecorded, 0, playerAttemptRecorded.Length);
		Array.Clear(playerGoalSucceeded, 0, playerGoalSucceeded.Length);
		Array.Clear(playerGoalTimes, 0, playerGoalTimes.Length);
		ScoreSummary = MatchScoreSummary.Empty;
	}

	private void UpdateScoreSummary()
	{
		bool isMatchComplete = playerAttemptRecorded[0] && playerAttemptRecorded[1];
		MatchWinner finalWinner = isMatchComplete ? DetermineFinalWinner() : MatchWinner.None;
		ScoreSummary = new MatchScoreSummary(
			playerRoundWins[0],
			playerRoundWins[1],
			playerAttemptRecorded[0],
			playerAttemptRecorded[1],
			playerGoalSucceeded[0],
			playerGoalSucceeded[1],
			playerGoalTimes[0],
			playerGoalTimes[1],
			finalWinner,
			isMatchComplete);
		ScoreSummaryChanged?.Invoke(ScoreSummary);
	}

	private MatchWinner DetermineFinalWinner()
	{
		if (playerRoundWins[0] > playerRoundWins[1])
		{
			return MatchWinner.Player1;
		}

		if (playerRoundWins[1] > playerRoundWins[0])
		{
			return MatchWinner.Player2;
		}

		if (playerGoalSucceeded[0] && playerGoalSucceeded[1])
		{
			float difference = playerGoalTimes[0] - playerGoalTimes[1];
			if (Mathf.Abs(difference) <= GoalTimeTieThresholdSeconds)
			{
				return MatchWinner.Draw;
			}

			return difference < 0f ? MatchWinner.Player1 : MatchWinner.Player2;
		}

		if (!playerGoalSucceeded[0] && !playerGoalSucceeded[1])
		{
			return MatchWinner.Draw;
		}

		return MatchWinner.Draw;
	}

	private static int GetGoalRunnerPlayerNumber(int round)
	{
		return round % 2 == 1 ? 1 : 2;
	}

	private static int GetBlockerPlayerNumber(int round)
	{
		return GetGoalRunnerPlayerNumber(round) == 1 ? 2 : 1;
	}

	// 現在の操作陣営を変更し、必要なら通知する。
	private void SetSide(MatchSide side)
	{
		if (Side == side)
		{
			return;
		}

		Side = side;
		SideChanged?.Invoke(Side);
	}
}
