using System;
using UnityEngine;

// 1試合分の状態遷移と通知を担う、Unity 非依存の試合ロジック本体。
public sealed class MatchSession
{
	// 試合設定からフェーズ時間や弾数を取得する。
	private readonly MatchConfiguration configuration;

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
	// Idle 以外なら試合進行中とみなす。
	public bool IsRunning => Phase != MatchPhase.Idle;

	// フェーズ変更を外部へ通知する。
	public event Action<MatchPhase> PhaseChanged;
	// 陣営変更を外部へ通知する。
	public event Action<MatchSide> SideChanged;
	// ラウンド変更を外部へ通知する。
	public event Action<int> RoundChanged;
	// 次ラウンドへ移る直前に外部へ通知する。
	public event Action<int> RoundAdvanced;
	// タイマー変更を外部へ通知する。
	public event Action<float, float> TimerChanged;
	// 結果変更を外部へ通知する。
	public event Action<MatchResult> ResultChanged;
	// 発射回数の変更を外部へ通知する。
	public event Action<int, int> LaunchBudgetChanged;
	// 図形数の変更を外部へ通知する。
	public event Action<int, int> ShapeBudgetChanged;

	// 設定を受け取り、試合進行に必要な値を保持する。
	public MatchSession(MatchConfiguration configuration)
	{
		this.configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
	}

	// 新しい試合を開始し、初期ラウンドと初期フェーズへ入る。
	public void Begin()
	{
		Round = 1;
		Result = MatchResult.None;
		SetSide(MatchSide.GoalRunner, true);
		SetPhase(MatchPhase.Draw);
		AssignShapeBudgets();
		RoundChanged?.Invoke(Round);
	}

	// フレームごとの経過時間を反映し、必要なら次のフェーズへ進める。
	public void Tick(float deltaTime)
	{
		if (!IsRunning)
		{
			return;
		}

		if (TimeRemaining <= 0f)
		{
			AdvancePhase();
			return;
		}

		TimeRemaining = Mathf.Max(0f, TimeRemaining - deltaTime);
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

	// ゴール到達を記録し、結果フェーズへ遷移する。
	public void MarkGoalReached()
	{
		Result = MatchResult.GoalRunnerWin;
		ResultChanged?.Invoke(Result);
		SetPhase(MatchPhase.Result);
	}

	// 制限時間切れとして結果を確定する。
	public void MarkTimeUp()
	{
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
		LaunchBudgetChanged?.Invoke(0, 0);
		ShapeBudgetChanged?.Invoke(0, 0);
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
				AdvanceRound();
				break;
		}
	}

	// 次ラウンドへ進み、役割を初期状態へ戻す。
	private void AdvanceRound()
	{
		Round++;
		Result = MatchResult.None;
		RoundChanged?.Invoke(Round);
		ResultChanged?.Invoke(Result);
		SetSide(MatchSide.GoalRunner, true);
		RoundAdvanced?.Invoke(Round);
		SetPhase(MatchPhase.Draw);
	}

	// フェーズを確定し、タイマーと必要な初期値を更新する。
	private void SetPhase(MatchPhase phase)
	{
		Phase = phase;
		if (Phase == MatchPhase.Place)
		{
			GoalRunnerLaunches = configuration.GoalRunnerLaunchCount;
			BlockerLaunches = configuration.BlockerLaunchCount;
			LaunchBudgetChanged?.Invoke(GoalRunnerLaunches, BlockerLaunches);
		}
		else if (Phase == MatchPhase.Race)
		{
			SetSide(MatchSide.GoalRunner);
		}

		PhaseDuration = configuration.GetDuration(Phase);
		TimeRemaining = PhaseDuration;
		PhaseChanged?.Invoke(Phase);
		TimerChanged?.Invoke(TimeRemaining, PhaseDuration);
	}

	// ラウンド開始時に図形配布数をランダム決定する。
	private void AssignShapeBudgets()
	{
		GoalRunnerShapes = UnityEngine.Random.Range(configuration.MinDrawingStampCount, configuration.MaxDrawingStampCount + 1);
		BlockerShapes = UnityEngine.Random.Range(configuration.MinBlockerStampCount, configuration.MaxBlockerStampCount + 1);
		ShapeBudgetChanged?.Invoke(GoalRunnerShapes, BlockerShapes);
	}

	// 現在の操作陣営を変更し、必要なら通知する。
	private void SetSide(MatchSide side, bool forceNotify = false)
	{
		if (Side == side && !forceNotify)
		{
			return;
		}

		Side = side;
		SideChanged?.Invoke(Side);
	}
}
