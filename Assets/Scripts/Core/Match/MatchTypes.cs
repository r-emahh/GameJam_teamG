// タイトル・試合中・一時停止など、ゲーム全体の状態を表す。
public enum GameState
{
	Title,
	Game,
	Pause,
	GameOver
}

// 1ラウンド内で進む試合フェーズを表す。
public enum MatchPhase
{
	Idle,
	Draw,
	Place,
	Race,
	Result
}

// どちらの陣営を操作しているかを表す。
public enum MatchSide
{
	GoalRunner,
	Blocker
}

// ラウンドの最終結果を表す。
public enum MatchResult
{
	None,
	GoalRunnerWin,
	BlockerWin,
	TimeUp
}

// 2ラウンド集計後の最終勝者を表す。
public enum MatchWinner
{
	None,
	Player1,
	Player2,
	Draw
}

// HUD や各システムへ通知する試合集計スナップショット。
public readonly struct MatchScoreSummary
{
	public static MatchScoreSummary Empty => new(
		player1RoundWins: 0,
		player2RoundWins: 0,
		player1AttemptRecorded: false,
		player2AttemptRecorded: false,
		player1GoalSucceeded: false,
		player2GoalSucceeded: false,
		player1GoalTimeSeconds: 0f,
		player2GoalTimeSeconds: 0f,
		finalWinner: MatchWinner.None,
		isMatchComplete: false);

	public MatchScoreSummary(
		int player1RoundWins,
		int player2RoundWins,
		bool player1AttemptRecorded,
		bool player2AttemptRecorded,
		bool player1GoalSucceeded,
		bool player2GoalSucceeded,
		float player1GoalTimeSeconds,
		float player2GoalTimeSeconds,
		MatchWinner finalWinner,
		bool isMatchComplete)
	{
		Player1RoundWins = player1RoundWins;
		Player2RoundWins = player2RoundWins;
		Player1AttemptRecorded = player1AttemptRecorded;
		Player2AttemptRecorded = player2AttemptRecorded;
		Player1GoalSucceeded = player1GoalSucceeded;
		Player2GoalSucceeded = player2GoalSucceeded;
		Player1GoalTimeSeconds = player1GoalTimeSeconds;
		Player2GoalTimeSeconds = player2GoalTimeSeconds;
		FinalWinner = finalWinner;
		IsMatchComplete = isMatchComplete;
	}

	public int Player1RoundWins { get; }
	public int Player2RoundWins { get; }
	public bool Player1AttemptRecorded { get; }
	public bool Player2AttemptRecorded { get; }
	public bool Player1GoalSucceeded { get; }
	public bool Player2GoalSucceeded { get; }
	public float Player1GoalTimeSeconds { get; }
	public float Player2GoalTimeSeconds { get; }
	public MatchWinner FinalWinner { get; }
	public bool IsMatchComplete { get; }
}
