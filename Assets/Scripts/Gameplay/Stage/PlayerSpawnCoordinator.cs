using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
// 試合開始時にプレイヤーをスポーン地点へ配置する。
public sealed class PlayerSpawnCoordinator : MonoBehaviour
{
	// Goal Runner のスポーン名。
	public const string GoalRunnerSpawnName = "GoalRunnerSpawn";
	// Blocker のスポーン名。
	public const string BlockerSpawnName = "BlockerSpawn";

	// 両プレイヤーに試合開始時に割り当てる Animator Controller。
	[SerializeField]
	private RuntimeAnimatorController playerAnimatorController;

	// 登録済みプレイヤーをスポーン地点へ配置する。
	public void PlacePlayers()
	{
		if (InputManager.Instance == null)
		{
			return;
		}

		Transform goalRunnerSpawn = GameObject.Find(GoalRunnerSpawnName)?.transform;
		Transform blockerSpawn = GameObject.Find(BlockerSpawnName)?.transform;
		List<PlayerController> players = new List<PlayerController>(InputManager.Instance.GetRegisteredPlayers());

		if (players.Count == 1)
		{
			Vector3 spawnPosition = blockerSpawn != null
				? blockerSpawn.position
				: players[0].transform.position + new Vector3(1.5f, 0f, 0f);
			PlayerController clone = Instantiate(players[0], spawnPosition, Quaternion.identity);
			clone.name = "Player2";
			players = new List<PlayerController>(InputManager.Instance.GetRegisteredPlayers());
		}

		if (players.Count == 0)
		{
			return;
		}

		PlayerController goalRunnerPlayer = null;
		PlayerController blockerPlayer = null;
		foreach (PlayerController player in players)
		{
			if (player == null)
			{
				continue;
			}

			if (player.ControlledSide == MatchSide.GoalRunner && goalRunnerPlayer == null)
			{
				goalRunnerPlayer = player;
			}
			else if (player.ControlledSide == MatchSide.Blocker && blockerPlayer == null)
			{
				blockerPlayer = player;
			}
		}

		if (goalRunnerPlayer == null && players.Count > 0)
		{
			goalRunnerPlayer = players[0];
		}

		if (blockerPlayer == null && players.Count > 1)
		{
			blockerPlayer = players[1] == goalRunnerPlayer ? players[0] : players[1];
		}

		InputManager.Instance.RefreshPlayerInputAssignments();
		PlacePlayer(goalRunnerPlayer, goalRunnerSpawn);
		AssignAnimatorController(goalRunnerPlayer);
		if (blockerPlayer != null && blockerPlayer != goalRunnerPlayer)
		{
			PlacePlayer(blockerPlayer, blockerSpawn);
			AssignAnimatorController(blockerPlayer);
		}

		InputManager.Instance.ResetDashAvailabilityForAllPlayers();
		SnapCameraToPlacedPlayers();
		Physics2D.SyncTransforms();
	}

	// 1人分の配置と入力設定をまとめる。
	private static void PlacePlayer(PlayerController player, Transform spawn)
	{
		if (player == null)
		{
			return;
		}

		if (spawn != null)
		{
			player.transform.position = spawn.position;
		}
	}

	private static void SnapCameraToPlacedPlayers()
	{
		Camera targetCamera = Camera.main;
		if (targetCamera == null)
		{
			targetCamera = Object.FindFirstObjectByType<Camera>();
		}

		StageCameraScrollController controller = targetCamera != null
			? targetCamera.GetComponent<StageCameraScrollController>()
			: Object.FindFirstObjectByType<StageCameraScrollController>();
		controller?.SnapToTarget();
	}

	private void AssignAnimatorController(PlayerController player)
	{
		if (player == null || playerAnimatorController == null)
		{
			return;
		}

		PlayerAnimationSync animationSync = player.GetComponent<PlayerAnimationSync>();
		if (animationSync == null)
		{
			animationSync = player.gameObject.AddComponent<PlayerAnimationSync>();
		}

		animationSync.AssignRuntimeAnimatorController(playerAnimatorController);
	}
}
