using UnityEngine;
using UnityEngine.SceneManagement;

[DisallowMultipleComponent]
// HUD をシーンに応じて生成し、GameManager と接続する。
public sealed class MatchUIBootstrap : MonoBehaviour
{
	// Bootstrap 用のゲームオブジェクト名。
	private const string BootstrapObjectName = "MatchUIBootstrap";

	// 現在の HUD ビューを保持する。
	private MatchHudView view;
	// 購読中の GameManager を保持する。
	private GameManager subscribedManager;

	// シーン開始前に必要なら永続ブートストラップを作る。
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

	// SceneLoaded 購読を開始する。
	private void Awake()
	{
		SceneManager.sceneLoaded += HandleSceneLoaded;
	}

	// 初回表示時に HUD を作成して同期する。
	private void Start()
	{
		HandleSceneLoaded(SceneManager.GetActiveScene(), LoadSceneMode.Single);
	}

	// プレイヤーの大砲選択は入力で変わるため毎フレーム追従する。
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
	}

	// 購読を解除する。
	private void OnDestroy()
	{
		SceneManager.sceneLoaded -= HandleSceneLoaded;
		Unsubscribe();
	}

	// 該当シーンへ入ったら HUD を再同期する。
	private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
	{
		if (!SceneCatalog.IsMatch(scene.name))
		{
			Unsubscribe();
			if (view != null)
			{
				view.gameObject.SetActive(false);
			}

			return;
		}

		EnsureView();
		view.gameObject.SetActive(true);
		Subscribe();
		Refresh();
	}

	// 必要なら HUD ビューを生成する。
	private void EnsureView()
	{
		if (!view)
		{
			view = RuntimeMatchHudFactory.Create(transform);
		}
	}

	// GameManager のイベントへ接続する。
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
		subscribedManager.OnMatchPhaseChanged += view.SetPhase;
		subscribedManager.OnMatchSideChanged += HandleSideChanged;
		subscribedManager.OnRoundChanged += HandleRoundChanged;
		subscribedManager.OnPhaseTimerChanged += view.SetTimer;
		subscribedManager.OnMatchResultChanged += view.SetResult;
		subscribedManager.OnLaunchBudgetChanged += view.SetLaunchBudget;
	}

	// GameManager のイベント接続を解除する。
	private void Unsubscribe()
	{
		if (subscribedManager == null || view == null)
		{
			subscribedManager = null;
			return;
		}

		subscribedManager.OnGameStateChanged -= view.SetGameState;
		subscribedManager.OnMatchPhaseChanged -= view.SetPhase;
		subscribedManager.OnMatchSideChanged -= HandleSideChanged;
		subscribedManager.OnRoundChanged -= HandleRoundChanged;
		subscribedManager.OnPhaseTimerChanged -= view.SetTimer;
		subscribedManager.OnMatchResultChanged -= view.SetResult;
		subscribedManager.OnLaunchBudgetChanged -= view.SetLaunchBudget;
		subscribedManager = null;
	}

	// 現在の状態を HUD に反映する。
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
		RefreshDrawingStatus();
		RefreshCannonSelection();
		RefreshLaunchPower();
		RefreshBlockerRaceAttackCooldown();
	}

	// 陣営変更時のラウンド表示を更新する。
	private void HandleSideChanged(MatchSide side)
	{
		view.SetRound(GameManager.Instance?.CurrentRound ?? 0, side);
	}

	// ラウンド変更時のラウンド表示を更新する。
	private void HandleRoundChanged(int round)
	{
		view.SetRound(round, GameManager.Instance?.CurrentSide ?? MatchSide.GoalRunner);
	}

	// 現在の大砲選択を HUD に反映する。
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

	// Place 中、現在手番プレイヤーの発射パワーだけを HUD に反映する。
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

	// Race 中の Blocker 妨害弾クールダウンを HUD に反映する。
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

	// 2人分の自由描画の使用量と確定状態を HUD に反映する。
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
