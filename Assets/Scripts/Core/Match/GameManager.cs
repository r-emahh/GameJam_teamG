using System;
using UnityEngine;
using UnityEngine.SceneManagement;

[DisallowMultipleComponent]
// Unity シーンをまたいで試合状態を保持する、ゲーム全体の窓口。
public sealed class GameManager : MonoBehaviour
{
	// シングルトン参照を保持する。
	public static GameManager _gameManager { get; private set; }
	// 外部から参照するための公開アクセサ。
	public static GameManager Instance => _gameManager;
	// 現在のゲーム状態を保持する。
	public static GameState currentState { get; private set; } = GameState.Title;

	// インスペクタで調整できる試合設定を持つ。
	[SerializeField]
	private MatchConfiguration configuration = new();

	// Unity 非依存の試合ロジック本体を保持する。
	private MatchSession session;

	// 現在のフェーズを公開する。
	public MatchPhase CurrentPhase => session.Phase;
	// 現在の陣営を公開する。
	public MatchSide CurrentSide => session.Side;
	// 現在の結果を公開する。
	public MatchResult CurrentResult => session.Result;
	// 現在のラウンドを公開する。
	public int CurrentRound => session.Round;
	// 現在の残り時間を公開する。
	public float CurrentPhaseTimeRemaining => session.TimeRemaining;
	// 現在フェーズの総時間を公開する。
	public float CurrentPhaseDuration => session.PhaseDuration;
	// Goal Runner の残り発射回数を公開する。
	public int GoalRunnerLaunchesRemaining => session.GoalRunnerLaunches;
	// Blocker の残り発射回数を公開する。
	public int BlockerLaunchesRemaining => session.BlockerLaunches;
	// Goal Runner の残り図形数を公開する。
	public int GoalRunnerShapesRemaining => session.GoalRunnerShapes;
	// Blocker の残り図形数を公開する。
	public int BlockerShapesRemaining => session.BlockerShapes;
	// 現在ラウンドで Goal Runner が落下した回数を公開する。
	public int GoalRunnerFallCount { get; private set; }
	// 現在の試合集計を公開する。
	public MatchScoreSummary CurrentScoreSummary => session.ScoreSummary;
	// 最終勝者を公開する。
	public MatchWinner CurrentFinalWinner => session.FinalWinner;
	// 試合が進行中かを公開する。
	public bool IsMatchRunning => session.IsRunning;
	// 最終結果オーバーレイを表示すべき状態かを公開する。
	public bool IsFinalResultVisible => session.Phase == MatchPhase.Result && session.IsMatchComplete;
	// フェーズ開始チュートリアル表示中にタイマーを止める設定を公開する。
	public bool ShouldPausePhaseTimerDuringTutorial => configuration != null && configuration.PausePhaseTimerDuringTutorial;
	// 現在フェーズのタイマー停止状態を公開する。
	public bool IsPhaseTimerPaused => session.IsPhaseTimerPaused;

	// ゲーム状態変更を購読側へ通知する。
	public event Action<GameState> OnGameStateChanged;
	// フェーズ変更を購読側へ通知する。
	public event Action<MatchPhase> OnMatchPhaseChanged;
	// 陣営変更を購読側へ通知する。
	public event Action<MatchSide> OnMatchSideChanged;
	// ラウンド変更を購読側へ通知する。
	public event Action<int> OnRoundChanged;
	// 次ラウンド状態の確定後、表示向け通知の前に購読側へ通知する。
	public event Action<int> OnRoundAdvanced;
	// タイマー更新を購読側へ通知する。
	public event Action<float, float> OnPhaseTimerChanged;
	// 結果変更を購読側へ通知する。
	public event Action<MatchResult> OnMatchResultChanged;
	// 発射回数の更新を購読側へ通知する。
	public event Action<int, int> OnLaunchBudgetChanged;
	// 図形数の更新を購読側へ通知する。
	public event Action<int, int> OnShapeBudgetChanged;
	// Goal Runner の落下回数更新を購読側へ通知する。
	public event Action<int> OnGoalRunnerFallCountChanged;
	// HUD 向けの試合集計更新を通知する。
	public event Action<MatchScoreSummary> OnMatchScoreSummaryChanged;

	// シーンロード前に必要なら自動生成する。
	[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
	private static void Bootstrap()
	{
		if (_gameManager == null)
		{
			new GameObject(nameof(GameManager)).AddComponent<GameManager>();
		}
	}

	private void Awake()
	{
		if (_gameManager != null && _gameManager != this)
		{
			Destroy(gameObject);
			return;
		}

		_gameManager = this;
		DontDestroyOnLoad(gameObject);
		session = new MatchSession(configuration);
		SubscribeSession();
		SceneManager.sceneLoaded += HandleSceneLoaded;
	}

	// シングルトン解放時に購読も解除する。
	private void OnDestroy()
	{
		if (_gameManager != this)
		{
			return;
		}

		SceneManager.sceneLoaded -= HandleSceneLoaded;
		UnsubscribeSession();
		_gameManager = null;
	}

	// ゲーム中のみ試合のタイマーを進める。
	private void Update()
	{
		if (currentState == GameState.Game)
		{
			session.Tick(Time.unscaledDeltaTime);
		}
	}

	// ゲーム状態を切り替え、必要なら試合を初期化する。
	public void ChangeState(GameState nextState)
	{
		if (currentState == nextState)
		{
			return;
		}

		currentState = nextState;
		OnGameStateChanged?.Invoke(currentState);
		if (currentState == GameState.Title)
		{
			ResetGoalRunnerFallCount();
			session.Reset();
		}
	}

	// 試合を開始状態へ移行する。
	public void BeginMatch()
	{
		ChangeState(GameState.Game);
		ResetGoalRunnerFallCount();
		session.Begin();
	}

	// ゲーム中なら一時停止へ切り替える。
	public void PauseMatch()
	{
		if (currentState == GameState.Game)
		{
			ChangeState(GameState.Pause);
		}
	}

	// 一時停止中ならゲームへ戻す。
	public void ResumeMatch()
	{
		if (currentState == GameState.Pause)
		{
			ChangeState(GameState.Game);
		}
	}

	// ゲーム進行中のゴール到達を試合ロジックへ通知する。
	public bool TryMarkGoalReached(MatchSide side)
	{
		return currentState == GameState.Game && session.TryMarkGoalReached(side);
	}
	// 時間切れを試合ロジックへ通知する。
	public void MarkTimeUp() => session.MarkTimeUp();
	// 指定陣営の発射回数を消費する。
	public bool TryConsumeLaunch(MatchSide side) => session.TryConsumeLaunch(side);
	// 指定陣営の図形数を消費する。
	public bool TryConsumeShape(MatchSide side) => session.TryConsumeShape(side);
	// フェーズタイマーの進行を一時停止または再開する。
	public void SetPhaseTimerPaused(bool paused) => session.SetPhaseTimerPaused(paused);
	// 試合を完全にリセットする。
	public void ResetMatch()
	{
		ResetGoalRunnerFallCount();
		session.Reset();
	}

	// 試合シーンを再読み込みして最初から再戦する。
	public void RetryMatch()
	{
		BeginMatch();
		SceneManager.LoadScene(SceneCatalog.Match);
	}

	// タイトルへ戻り、試合状態を待機へ戻す。
	public void ReturnToTitle()
	{
		ChangeState(GameState.Title);
		SceneManager.LoadScene(SceneCatalog.Title);
	}

	// Race 中の Goal Runner 落下リスポーンを記録する。
	public void RegisterGoalRunnerFall()
	{
		if (currentState != GameState.Game || session.Phase != MatchPhase.Race)
		{
			return;
		}

		GoalRunnerFallCount++;
		OnGoalRunnerFallCountChanged?.Invoke(GoalRunnerFallCount);
	}

	// 該当シーンに入ったとき、必要なら試合を再開する。
	private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
	{
		if (SceneCatalog.IsMatch(scene.name) && currentState == GameState.Game && !session.IsRunning && !session.IsMatchComplete)
		{
			session.Begin();
		}
	}

	// Session 側イベントを Unity 側イベントへ中継する。
	private void SubscribeSession()
	{
		session.PhaseChanged += HandlePhaseChanged;
		session.SideChanged += HandleSideChanged;
		session.RoundChanged += HandleRoundChanged;
		session.RoundAdvanced += HandleRoundAdvanced;
		session.TimerChanged += HandleTimerChanged;
		session.ResultChanged += HandleResultChanged;
		session.LaunchBudgetChanged += HandleLaunchBudgetChanged;
		session.ShapeBudgetChanged += HandleShapeBudgetChanged;
		session.ScoreSummaryChanged += HandleScoreSummaryChanged;
	}

	// Session への購読を外す。
	private void UnsubscribeSession()
	{
		session.PhaseChanged -= HandlePhaseChanged;
		session.SideChanged -= HandleSideChanged;
		session.RoundChanged -= HandleRoundChanged;
		session.RoundAdvanced -= HandleRoundAdvanced;
		session.TimerChanged -= HandleTimerChanged;
		session.ResultChanged -= HandleResultChanged;
		session.LaunchBudgetChanged -= HandleLaunchBudgetChanged;
		session.ShapeBudgetChanged -= HandleShapeBudgetChanged;
		session.ScoreSummaryChanged -= HandleScoreSummaryChanged;
	}

	// 各イベントをそのまま公開イベントへ流す。
	private void HandlePhaseChanged(MatchPhase value) => OnMatchPhaseChanged?.Invoke(value);
	// 各イベントをそのまま公開イベントへ流す。
	private void HandleSideChanged(MatchSide value) => OnMatchSideChanged?.Invoke(value);
	// 各イベントをそのまま公開イベントへ流す。
	private void HandleRoundChanged(int value) => OnRoundChanged?.Invoke(value);
	// 各イベントをそのまま公開イベントへ流す。
	private void HandleRoundAdvanced(int value)
	{
		ResetGoalRunnerFallCount();
		OnRoundAdvanced?.Invoke(value);
	}
	// 各イベントをそのまま公開イベントへ流す。
	private void HandleTimerChanged(float remaining, float total) => OnPhaseTimerChanged?.Invoke(remaining, total);
	// 各イベントをそのまま公開イベントへ流す。
	private void HandleResultChanged(MatchResult value) => OnMatchResultChanged?.Invoke(value);
	// 各イベントをそのまま公開イベントへ流す。
	private void HandleLaunchBudgetChanged(int goalRunner, int blocker) => OnLaunchBudgetChanged?.Invoke(goalRunner, blocker);
	// 各イベントをそのまま公開イベントへ流す。
	private void HandleShapeBudgetChanged(int goalRunner, int blocker) => OnShapeBudgetChanged?.Invoke(goalRunner, blocker);
	// 各イベントをそのまま公開イベントへ流す。
	private void HandleScoreSummaryChanged(MatchScoreSummary value) => OnMatchScoreSummaryChanged?.Invoke(value);

	// Goal Runner の落下回数を 0 に戻し、必要なら HUD に通知する。
	private void ResetGoalRunnerFallCount()
	{
		GoalRunnerFallCount = 0;
		OnGoalRunnerFallCountChanged?.Invoke(GoalRunnerFallCount);
	}
}
