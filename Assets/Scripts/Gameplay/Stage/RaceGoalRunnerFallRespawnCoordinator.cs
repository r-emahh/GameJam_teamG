using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

[DisallowMultipleComponent]
// Race 中に落下した Goal Runner を監視し、スポーン地点へ戻す。
public sealed class RaceGoalRunnerFallRespawnCoordinator : MonoBehaviour
{
	private const string BootstrapObjectName = "RaceGoalRunnerFallRespawnCoordinator";
	private const string StageBottomObjectName = "StageBottom";

	[SerializeField]
	private float fallThresholdOffset = 0.75f;

	[SerializeField]
	private float respawnInputLockDuration = 0.6f;

	[SerializeField]
	private float respawnGraceDuration = 0.4f;

	[SerializeField]
	private float minimumRespawnInterval = 0.75f;

	[SerializeField]
	private float minimumSpawnClearanceAboveThreshold = 0.8f;

	private readonly Dictionary<PlayerController, float> respawnGraceUntil = new();
	private readonly Dictionary<PlayerController, float> respawnCooldownUntil = new();

	[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
	private static void Bootstrap()
	{
		if (FindFirstObjectByType<RaceGoalRunnerFallRespawnCoordinator>() != null)
		{
			return;
		}

		GameObject bootstrapObject = new GameObject(BootstrapObjectName);
		bootstrapObject.AddComponent<RaceGoalRunnerFallRespawnCoordinator>();
		DontDestroyOnLoad(bootstrapObject);
	}

	private void Awake()
	{
		SceneManager.sceneLoaded += HandleSceneLoaded;
	}

	private void OnDestroy()
	{
		SceneManager.sceneLoaded -= HandleSceneLoaded;
	}

	private void Update()
	{
		if (!ShouldMonitorFalls() || InputManager.Instance == null)
		{
			return;
		}

		float now = Time.unscaledTime;
		float threshold = GetFallThreshold();
		foreach (PlayerController player in InputManager.Instance.GetRegisteredPlayers())
		{
			if (!ShouldRespawn(player, threshold, now))
			{
				continue;
			}

			Vector3 spawnPosition = GetSafeRespawnPosition(player.transform.position, threshold);
			player.RespawnAt(spawnPosition, respawnInputLockDuration);
			respawnGraceUntil[player] = now + respawnGraceDuration;
			respawnCooldownUntil[player] = now + minimumRespawnInterval;
			GameManager.Instance?.RegisterGoalRunnerFall();
		}
	}

	private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
	{
		if (SceneCatalog.IsMatch(scene.name))
		{
			return;
		}

		respawnGraceUntil.Clear();
		respawnCooldownUntil.Clear();
	}

	private static bool ShouldMonitorFalls()
	{
		return GameManager.Instance != null
			&& GameManager.currentState == GameState.Game
			&& GameManager.Instance.CurrentPhase == MatchPhase.Race
			&& SceneCatalog.IsMatch(SceneManager.GetActiveScene().name);
	}

	private bool ShouldRespawn(PlayerController player, float threshold, float now)
	{
		if (player == null || player.ControlledSide != MatchSide.GoalRunner)
		{
			return false;
		}

		if (player.transform.position.y >= threshold)
		{
			return false;
		}

		if (respawnGraceUntil.TryGetValue(player, out float graceUntil) && now < graceUntil)
		{
			return false;
		}

		if (respawnCooldownUntil.TryGetValue(player, out float cooldownUntil) && now < cooldownUntil)
		{
			return false;
		}

		return true;
	}

	private float GetFallThreshold()
	{
		GameObject stageBottom = GameObject.Find(StageBottomObjectName);
		if (stageBottom != null)
		{
			return stageBottom.transform.position.y - Mathf.Abs(fallThresholdOffset);
		}

		return MatchSceneBootstrap.DrawArea.yMin - Mathf.Abs(fallThresholdOffset);
	}

	private Vector3 GetSafeRespawnPosition(Vector3 fallbackPosition, float threshold)
	{
		Transform spawn = GameObject.Find(PlayerSpawnCoordinator.GoalRunnerSpawnName)?.transform;
		Vector3 spawnPosition = spawn != null ? spawn.position : fallbackPosition;
		float minimumSafeY = threshold + Mathf.Abs(minimumSpawnClearanceAboveThreshold);
		if (spawnPosition.y < minimumSafeY)
		{
			spawnPosition.y = minimumSafeY;
		}

		return spawnPosition;
	}
}
