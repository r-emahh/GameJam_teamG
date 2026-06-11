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
		EnsureView();
		Subscribe();
		Refresh();
	}

	// プレイヤーの大砲選択は入力で変わるため毎フレーム追従する。
	private void Update()
	{
		RefreshDrawingShape();
		RefreshCannonSelection();
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
		if (scene.name != "Game" && scene.name != "Rema" && scene.name != "Title")
		{
			return;
		}

		EnsureView();
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
		subscribedManager.OnShapeBudgetChanged += view.SetShapeBudget;
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
		subscribedManager.OnShapeBudgetChanged -= view.SetShapeBudget;
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
		view.SetShapeBudget(manager.GoalRunnerShapesRemaining, manager.BlockerShapesRemaining);
		RefreshDrawingShape();
		RefreshCannonSelection();
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
			view.SetCannonSelection(-1, -1);
			return;
		}

		int goalRunnerOrder = -1;
		int blockerOrder = -1;
		foreach (PlayerController player in InputManager.Instance.GetRegisteredPlayers())
		{
			if (player == null)
			{
				continue;
			}

			if (player.ControlledSide == MatchSide.GoalRunner)
			{
				goalRunnerOrder = player.SelectedCannonOrder;
			}
			else if (player.ControlledSide == MatchSide.Blocker)
			{
				blockerOrder = player.SelectedCannonOrder;
			}
		}

		view.SetCannonSelection(goalRunnerOrder, blockerOrder);
	}

	// 現在の図形を HUD に反映する。
	private void RefreshDrawingShape()
	{
		if (view == null)
		{
			return;
		}

		if (GameManager.Instance == null || GameManager.currentState != GameState.Game || GameManager.Instance.CurrentPhase != MatchPhase.Draw || InputManager.Instance == null)
		{
			view.SetDrawingShape(DrawingStampShape.Square, DrawingStampShape.Square);
			return;
		}

		DrawingStampShape goalRunnerShape = DrawingStampShape.Square;
		DrawingStampShape blockerShape = DrawingStampShape.Square;
		foreach (PlayerController player in InputManager.Instance.GetRegisteredPlayers())
		{
			if (player == null)
			{
				continue;
			}

			if (player.ControlledSide == MatchSide.GoalRunner)
			{
				goalRunnerShape = player.CurrentDrawingShape;
			}
			else if (player.ControlledSide == MatchSide.Blocker)
			{
				blockerShape = player.CurrentDrawingShape;
			}
		}

		view.SetDrawingShape(goalRunnerShape, blockerShape);
	}
}
