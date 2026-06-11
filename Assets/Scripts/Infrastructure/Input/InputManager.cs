using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

// プレイヤー登録と共通入力操作をまとめる永続マネージャー。
public class InputManager : MonoBehaviour
{
	// シングルトン参照を保持する。
	public static InputManager _inputManager { get; private set; }
	// 外部参照用のアクセサ。
	public static InputManager Instance => _inputManager;

	// 先頭プレイヤーにキーボードを許可するかを制御する。
	[SerializeField]
	private bool allowKeyboardForFirstPlayer = true;

	// 登録済みプレイヤーを順序付きで保持する。
	private readonly List<PlayerController> players = new();
	// 各プレイヤーに紐づく PlayerInput を保持する。
	private readonly Dictionary<PlayerController, PlayerInput> playerInputs = new();

	// プレイヤー登録時に通知する。
	public event Action<PlayerController, int> OnPlayerRegistered;
	// プレイヤー解除時に通知する。
	public event Action<PlayerController> OnPlayerUnregistered;

	// シーン読み込み前に必要なら自動生成する。
	[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
	private static void Bootstrap()
	{
		if (_inputManager != null)
		{
			return;
		}

		new GameObject(nameof(InputManager)).AddComponent<InputManager>();
	}

	// シングルトンを確立し、永続化する。
	private void Awake()
	{
		if (_inputManager != null && _inputManager != this)
		{
			Destroy(gameObject);
			return;
		}

		_inputManager = this;
		DontDestroyOnLoad(gameObject);
	}

	// 新しいプレイヤーを登録し、必要なら陣営と入力を記録する。
	public void RegisterPlayer(PlayerController player)
	{
		if (player == null || players.Contains(player))
		{
			return;
		}

		players.Add(player);
		player.AssignControlledSide(players.Count == 1 ? MatchSide.GoalRunner : MatchSide.Blocker);
		PlayerInput playerInput = player.GetComponent<PlayerInput>();
		if (playerInput != null)
		{
			playerInputs[player] = playerInput;
		}

		OnPlayerRegistered?.Invoke(player, players.Count - 1);
	}

	// 登録済みプレイヤーを外す。
	public void UnregisterPlayer(PlayerController player)
	{
		if (player == null)
		{
			return;
		}

		if (players.Remove(player))
		{
			playerInputs.Remove(player);
			OnPlayerUnregistered?.Invoke(player);
		}
	}

	// 登録順におけるプレイヤーのインデックスを返す。
	public int GetPlayerIndex(PlayerController player)
	{
		return player == null ? -1 : players.IndexOf(player);
	}

	// 先頭プレイヤーにキーボードを許可する設定を返す。
	public bool IsKeyboardAllowedForFirstPlayer()
	{
		return allowKeyboardForFirstPlayer;
	}

	// 登録済みプレイヤー一覧を返す。
	public IReadOnlyList<PlayerController> GetRegisteredPlayers()
	{
		return players;
	}

	// 全プレイヤーの入力を一定時間止める。
	public void FreezeAllPlayers(float duration)
	{
		foreach (PlayerController player in players)
		{
			if (player == null)
			{
				continue;
			}

			player.LockInput(duration);
		}
	}

	// 全プレイヤーのダッシュ可否を初期状態へ戻す。
	public void ResetDashAvailabilityForAllPlayers()
	{
		foreach (PlayerController player in players)
		{
			if (player == null)
			{
				continue;
			}

			player.ResetDashAvailability();
		}
	}

	// 指定プレイヤーに紐づく PlayerInput を返す。
	public PlayerInput GetPlayerInput(PlayerController player)
	{
		if (player == null)
		{
			return null;
		}

		playerInputs.TryGetValue(player, out PlayerInput input);
		return input;
	}
}
