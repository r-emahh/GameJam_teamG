using UnityEngine;
using UnityEngine.SceneManagement;

[DisallowMultipleComponent]
// ラウンド切り替え時に盤面を消去し、攻守を入れ替える。
public sealed class MatchRoundResetCoordinator : MonoBehaviour
{
	// Bootstrap 用のゲームオブジェクト名。
	private const string BootstrapObjectName = "MatchRoundResetCoordinator";

	// 購読中の GameManager を保持する。
	private GameManager subscribedManager;
	// シーン開始前に必要なら永続ブートストラップを作る。
	[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
	private static void Bootstrap()
	{
		if (FindFirstObjectByType<MatchRoundResetCoordinator>() != null)
		{
			return;
		}

		GameObject bootstrapObject = new GameObject(BootstrapObjectName);
		bootstrapObject.AddComponent<MatchRoundResetCoordinator>();
		DontDestroyOnLoad(bootstrapObject);
	}

	// シーン読み込みに追従して GameManager と再接続する。
	private void Awake()
	{
		SceneManager.sceneLoaded += HandleSceneLoaded;
		TrySubscribe();
	}

	// 起動順が前後しても確実に購読する。
	private void Start()
	{
		TrySubscribe();
	}

	// 購読を解除する。
	private void OnDestroy()
	{
		SceneManager.sceneLoaded -= HandleSceneLoaded;
		Unsubscribe();
	}

	// シーンが切り替わったら必要な参照を更新する。
	private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
	{
		TrySubscribe();
	}

	// GameManager が生きていればイベント購読する。
	private void TrySubscribe()
	{
		GameManager manager = GameManager.Instance;
		if (manager == null || subscribedManager == manager)
		{
			return;
		}

		Unsubscribe();
		subscribedManager = manager;
		subscribedManager.OnRoundAdvanced += HandleRoundAdvanced;
	}

	// 現在の購読を外す。
	private void Unsubscribe()
	{
		if (subscribedManager != null)
		{
			subscribedManager.OnRoundAdvanced -= HandleRoundAdvanced;
			subscribedManager = null;
		}
	}

	// 次ラウンド開始前に盤面を消去し、役割を反転して再配置する。
	private void HandleRoundAdvanced(int _)
	{
		ClearRuntimeBoardObjects();
		SwapPlayerSides();
		ResetPlayersToSpawnPoints();
	}

	// ラウンド単位で消したいランタイム生成物を破棄する。
	private static void ClearRuntimeBoardObjects()
	{
		RuntimeRoundObject[] runtimeObjects = Object.FindObjectsByType<RuntimeRoundObject>(FindObjectsInactive.Include, FindObjectsSortMode.None);
		foreach (RuntimeRoundObject runtimeObject in runtimeObjects)
		{
			if (runtimeObject != null)
			{
				Object.Destroy(runtimeObject.gameObject);
			}
		}
	}

	// 登録済みプレイヤーの陣営を反転する。
	private static void SwapPlayerSides()
	{
		if (InputManager.Instance == null)
		{
			return;
		}

		foreach (PlayerController player in InputManager.Instance.GetRegisteredPlayers())
		{
			if (player == null)
			{
				continue;
			}

			MatchSide nextSide = player.ControlledSide == MatchSide.GoalRunner
				? MatchSide.Blocker
				: MatchSide.GoalRunner;
			player.AssignControlledSide(nextSide);
		}
	}

	// 反転後の陣営に合わせてプレイヤーをスポーン地点へ戻す。
	private static void ResetPlayersToSpawnPoints()
	{
		if (InputManager.Instance == null)
		{
			return;
		}

		Transform goalRunnerSpawn = GameObject.Find(PlayerSpawnCoordinator.GoalRunnerSpawnName)?.transform;
		Transform blockerSpawn = GameObject.Find(PlayerSpawnCoordinator.BlockerSpawnName)?.transform;

		foreach (PlayerController player in InputManager.Instance.GetRegisteredPlayers())
		{
			if (player == null)
			{
				continue;
			}

			Transform spawn = player.ControlledSide == MatchSide.GoalRunner ? goalRunnerSpawn : blockerSpawn;
			Vector3 spawnPosition = spawn != null ? spawn.position : player.transform.position;
			player.ResetForNextRound(spawnPosition);
		}
	}
}
