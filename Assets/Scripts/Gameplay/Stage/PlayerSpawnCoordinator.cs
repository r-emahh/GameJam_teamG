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

		PlacePlayer(players[0], goalRunnerSpawn, MatchSide.GoalRunner, false);
		if (players.Count > 1)
		{
			PlacePlayer(players[1], blockerSpawn, MatchSide.Blocker, true);
		}

		InputManager.Instance.ResetDashAvailabilityForAllPlayers();
	}

	// 1人分の配置と入力設定をまとめる。
	private static void PlacePlayer(PlayerController player, Transform spawn, MatchSide side, bool useGamepad)
	{
		if (spawn != null)
		{
			player.transform.position = spawn.position;
		}

		player.ConfigureLocalInput(side, useGamepad);
	}
}
