using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

// プレイヤー登録と共通入力操作をまとめる永続マネージャー。
public class InputManager : MonoBehaviour
{
	private const string GamepadControlScheme = "Gamepad";
	private const string KeyboardMouseControlScheme = "Keyboard&Mouse";

	private sealed class LocalInputAssignment
	{
		public LocalInputAssignment(string controlScheme, params InputDevice[] devices)
		{
			ControlScheme = controlScheme;
			Devices = devices;
		}

		public string ControlScheme { get; }
		public InputDevice[] Devices { get; }
	}

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
	// 登録プレイヤーごとの物理入力デバイス割り当てを保持する。
	private readonly Dictionary<PlayerController, LocalInputAssignment> inputAssignments = new();

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
		InputSystem.onDeviceChange += HandleDeviceChange;
	}

	// 入力デバイス監視を解除する。
	private void OnDestroy()
	{
		if (_inputManager != this)
		{
			return;
		}

		InputSystem.onDeviceChange -= HandleDeviceChange;
		_inputManager = null;
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
			player.ClearLocalInput();
			playerInputs.Remove(player);
			inputAssignments.Remove(player);
			OnPlayerUnregistered?.Invoke(player);
		}
	}

	// 接続中デバイスから全プレイヤーの割り当てを作り直す。
	public void RefreshPlayerInputAssignments()
	{
		List<PlayerController> activePlayers = new();
		foreach (PlayerController player in players)
		{
			if (player != null)
			{
				activePlayers.Add(player);
			}
		}

		List<Gamepad> gamepads = new();
		for (int i = 0; i < Gamepad.all.Count; i++)
		{
			Gamepad gamepad = Gamepad.all[i];
			if (gamepad != null && gamepad.added)
			{
				gamepads.Add(gamepad);
			}
		}

		foreach (PlayerController player in activePlayers)
		{
			player.ClearLocalInput();
		}

		inputAssignments.Clear();
		HashSet<int> claimedDeviceIds = new();
		bool useKeyboardFallback = allowKeyboardForFirstPlayer
			&& Keyboard.current != null
			&& Keyboard.current.added
			&& gamepads.Count < activePlayers.Count;
		int gamepadIndex = 0;

		for (int playerIndex = 0; playerIndex < activePlayers.Count; playerIndex++)
		{
			PlayerController player = activePlayers[playerIndex];
			LocalInputAssignment assignment = null;

			if (playerIndex == 0 && useKeyboardFallback)
			{
				assignment = CreateKeyboardAssignment(claimedDeviceIds);
			}
			else if (gamepadIndex < gamepads.Count)
			{
				Gamepad gamepad = gamepads[gamepadIndex++];
				if (claimedDeviceIds.Add(gamepad.deviceId))
				{
					assignment = new LocalInputAssignment(GamepadControlScheme, gamepad);
				}
			}

			if (assignment == null || !player.ConfigureLocalInput(assignment.ControlScheme, assignment.Devices))
			{
				Debug.LogWarning($"Player {playerIndex + 1} has no available local input device.", player);
				continue;
			}

			inputAssignments[player] = assignment;
			Debug.Log($"Player {playerIndex + 1} input: {assignment.ControlScheme} / {FormatDeviceNames(assignment.Devices)}", player);
		}
	}

	// キーボードと任意のマウスを1組の割り当てとして生成する。
	private static LocalInputAssignment CreateKeyboardAssignment(HashSet<int> claimedDeviceIds)
	{
		Keyboard keyboard = Keyboard.current;
		if (keyboard == null || !keyboard.added || !claimedDeviceIds.Add(keyboard.deviceId))
		{
			return null;
		}

		Mouse mouse = Mouse.current;
		if (mouse != null && mouse.added && claimedDeviceIds.Add(mouse.deviceId))
		{
			return new LocalInputAssignment(KeyboardMouseControlScheme, keyboard, mouse);
		}

		return new LocalInputAssignment(KeyboardMouseControlScheme, keyboard);
	}

	// 接続状態が変わった場合だけ割り当てを再評価する。
	private void HandleDeviceChange(InputDevice device, InputDeviceChange change)
	{
		if (device is not Gamepad && device is not Keyboard && device is not Mouse)
		{
			return;
		}

		switch (change)
		{
			case InputDeviceChange.Added:
			case InputDeviceChange.Removed:
			case InputDeviceChange.Disconnected:
			case InputDeviceChange.Reconnected:
			case InputDeviceChange.Enabled:
			case InputDeviceChange.Disabled:
				RefreshPlayerInputAssignments();
				break;
		}
	}

	// ログ表示用にデバイス名を連結する。
	private static string FormatDeviceNames(InputDevice[] devices)
	{
		if (devices == null || devices.Length == 0)
		{
			return "None";
		}

		string result = devices[0] != null ? devices[0].displayName : "Unknown";
		for (int i = 1; i < devices.Length; i++)
		{
			result += $", {(devices[i] != null ? devices[i].displayName : "Unknown")}";
		}

		return result;
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
