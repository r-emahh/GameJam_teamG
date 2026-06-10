using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class InputManager : MonoBehaviour
{
	public static InputManager _inputManager { get; private set; }
	public static InputManager Instance => _inputManager;

	[SerializeField]
	private bool allowKeyboardForFirstPlayer = true;

	private readonly List<PlayerController> players = new();
	private readonly Dictionary<PlayerController, PlayerInput> playerInputs = new();

	public event Action<PlayerController, int> OnPlayerRegistered;
	public event Action<PlayerController> OnPlayerUnregistered;

	[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
	private static void Bootstrap()
	{
		if (_inputManager != null)
		{
			return;
		}

		new GameObject(nameof(InputManager)).AddComponent<InputManager>();
	}

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

	public int GetPlayerIndex(PlayerController player)
	{
		return player == null ? -1 : players.IndexOf(player);
	}

	public bool IsKeyboardAllowedForFirstPlayer()
	{
		return allowKeyboardForFirstPlayer;
	}

	public IReadOnlyList<PlayerController> GetRegisteredPlayers()
	{
		return players;
	}

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
