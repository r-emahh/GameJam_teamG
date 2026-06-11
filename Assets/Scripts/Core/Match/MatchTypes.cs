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
