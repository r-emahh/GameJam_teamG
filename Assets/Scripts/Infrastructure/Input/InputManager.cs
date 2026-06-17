using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Layouts;
using UnityEngine.InputSystem.LowLevel;

// プレイヤー登録と共通入力操作をまとめる永続マネージャー。
public class InputManager : MonoBehaviour
{
	private const string GamepadControlScheme = "Gamepad";
	private const string JoystickControlScheme = "Joystick";
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
	// Steam Input や DS4Windows などで1台の物理パッドが複数 Gamepad として見える場合に重複を除外する。
	[SerializeField]
	private bool ignoreMirroredVirtualGamepads = true;

	// 登録済みプレイヤーを順序付きで保持する。
	private readonly List<PlayerController> players = new();
	// 各プレイヤーに紐づく PlayerInput を保持する。
	private readonly Dictionary<PlayerController, PlayerInput> playerInputs = new();
	// 登録プレイヤーごとの物理入力デバイス割り当てを保持する。
	private readonly Dictionary<PlayerController, LocalInputAssignment> inputAssignments = new();
	private readonly Dictionary<int, float> inputEventLogTimes = new();
	private readonly List<InputDevice> recentlyActiveGameDevices = new();

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
		InputSystem.onEvent += HandleInputEvent;
	}

	// 入力デバイス監視を解除する。
	private void OnDestroy()
	{
		if (_inputManager != this)
		{
			return;
		}

		InputSystem.onDeviceChange -= HandleDeviceChange;
		InputSystem.onEvent -= HandleInputEvent;
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

		List<Gamepad> gamepads = OrderAssignableDevicesByRecent(GetAssignableGamepads(activePlayers.Count));
		List<Joystick> joysticks = OrderAssignableDevicesByRecent(GetAssignableJoysticks(gamepads, activePlayers.Count));

		foreach (PlayerController player in activePlayers)
		{
			player.ClearLocalInput();
		}

		inputAssignments.Clear();
		HashSet<int> claimedDeviceIds = new();
		int assignableGameDeviceCount = gamepads.Count + joysticks.Count;
		bool useKeyboardFallback = allowKeyboardForFirstPlayer
			&& Keyboard.current != null
			&& Keyboard.current.added
			&& assignableGameDeviceCount < activePlayers.Count;
		int gamepadIndex = 0;
		int joystickIndex = 0;

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
				InputDevice gamepad = gamepads[gamepadIndex++];
				if (claimedDeviceIds.Add(gamepad.deviceId))
				{
					assignment = new LocalInputAssignment(GamepadControlScheme, gamepad);
				}
			}
			else if (joystickIndex < joysticks.Count)
			{
				InputDevice joystick = joysticks[joystickIndex++];
				if (claimedDeviceIds.Add(joystick.deviceId))
				{
					assignment = new LocalInputAssignment(JoystickControlScheme, joystick);
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

	// 物理的に別の入力として扱うべきゲームパッドだけを返す。
	private List<Gamepad> GetAssignableGamepads(int requiredPlayerCount)
	{
		List<Gamepad> rawGamepads = new();
		for (int i = 0; i < Gamepad.all.Count; i++)
		{
			Gamepad gamepad = Gamepad.all[i];
			if (gamepad != null && gamepad.added)
			{
				rawGamepads.Add(gamepad);
			}
		}

		if (!ignoreMirroredVirtualGamepads || rawGamepads.Count <= 1)
		{
			return rawGamepads;
		}

		List<Gamepad> assignable = new();
		HashSet<string> physicalKeys = new();
		bool shouldFilterLikelyVirtualMirrors = rawGamepads.Count > requiredPlayerCount
			&& rawGamepads.Exists(IsXInputLikeGamepad);
		foreach (Gamepad gamepad in rawGamepads)
		{
			string key = BuildPhysicalDeviceKey(gamepad);
			if (!string.IsNullOrEmpty(key) && !physicalKeys.Add(key))
			{
				Debug.LogWarning($"Ignoring mirrored gamepad input: {FormatDeviceDescription(gamepad)}");
				continue;
			}

			if (shouldFilterLikelyVirtualMirrors && IsNonXInputVirtualMirrorCandidate(gamepad))
			{
				Debug.LogWarning($"Ignoring likely mirrored gamepad input: {FormatDeviceDescription(gamepad)}");
				continue;
			}

			assignable.Add(gamepad);
		}

		return assignable;
	}

	// Gamepad のミラーとして同時検出された Joystick を除いた候補を返す。
	private List<Joystick> GetAssignableJoysticks(IReadOnlyList<Gamepad> assignableGamepads, int requiredPlayerCount)
	{
		List<Joystick> rawJoysticks = new();
		for (int i = 0; i < Joystick.all.Count; i++)
		{
			Joystick joystick = Joystick.all[i];
			if (joystick != null && joystick.added)
			{
				rawJoysticks.Add(joystick);
			}
		}

		if (!ignoreMirroredVirtualGamepads || rawJoysticks.Count == 0)
		{
			return rawJoysticks;
		}

		List<Joystick> assignable = new();
		HashSet<string> physicalKeys = new();
		if (assignableGamepads != null)
		{
			foreach (Gamepad gamepad in assignableGamepads)
			{
				string gamepadKey = BuildPhysicalDeviceKey(gamepad);
				if (!string.IsNullOrEmpty(gamepadKey))
				{
					physicalKeys.Add(gamepadKey);
				}
			}
		}

		bool hasEnoughGamepads = assignableGamepads != null && assignableGamepads.Count >= requiredPlayerCount;
		foreach (Joystick joystick in rawJoysticks)
		{
			string key = BuildPhysicalDeviceKey(joystick);
			if (!string.IsNullOrEmpty(key) && !physicalKeys.Add(key))
			{
				Debug.LogWarning($"Ignoring mirrored joystick input: {FormatDeviceDescription(joystick)}");
				continue;
			}

			if ((hasEnoughGamepads || IsLikelyMirroredJoystick(joystick, assignableGamepads))
				&& IsGameControllerLikeJoystick(joystick))
			{
				Debug.LogWarning($"Ignoring likely mirrored joystick input: {FormatDeviceDescription(joystick)}");
				continue;
			}

			assignable.Add(joystick);
		}

		return assignable;
	}

	private List<InputDevice> GetAssignableGameDevices(int requiredPlayerCount)
	{
		List<Gamepad> gamepads = OrderAssignableDevicesByRecent(GetAssignableGamepads(requiredPlayerCount));
		List<Joystick> joysticks = OrderAssignableDevicesByRecent(GetAssignableJoysticks(gamepads, requiredPlayerCount));
		List<InputDevice> assignable = new();

		foreach (Gamepad gamepad in gamepads)
		{
			assignable.Add(gamepad);
		}

		foreach (Joystick joystick in joysticks)
		{
			assignable.Add(joystick);
		}

		return assignable;
	}

	private List<TDevice> OrderAssignableDevicesByRecent<TDevice>(IReadOnlyList<TDevice> devices) where TDevice : InputDevice
	{
		List<TDevice> ordered = new();
		if (devices == null || devices.Count == 0)
		{
			return ordered;
		}

		HashSet<int> remainingDeviceIds = new();
		for (int i = 0; i < devices.Count; i++)
		{
			TDevice device = devices[i];
			if (device != null && device.added)
			{
				remainingDeviceIds.Add(device.deviceId);
			}
		}

		foreach (InputDevice recentDevice in recentlyActiveGameDevices)
		{
			if (recentDevice is TDevice typedDevice
				&& typedDevice.added
				&& remainingDeviceIds.Remove(typedDevice.deviceId))
			{
				ordered.Add(typedDevice);
			}
		}

		for (int i = 0; i < devices.Count; i++)
		{
			TDevice device = devices[i];
			if (device != null && device.added && remainingDeviceIds.Remove(device.deviceId))
			{
				ordered.Add(device);
			}
		}

		return ordered;
	}

	// Steam Input / DS4Windows では実機 HID と仮想 XInput が同時に見えることがある。
	private static bool IsNonXInputVirtualMirrorCandidate(Gamepad gamepad)
	{
		if (gamepad == null || IsXInputLikeGamepad(gamepad))
		{
			return false;
		}

		InputDeviceDescription description = gamepad.description;
		if (!string.IsNullOrWhiteSpace(description.serial))
		{
			return false;
		}

		string text = $"{description.interfaceName} {description.manufacturer} {description.product} {gamepad.displayName} {gamepad.layout}".ToLowerInvariant();
		return text.Contains("dualshock")
			|| text.Contains("dualsense")
			|| text.Contains("wireless controller")
			|| text.Contains("playstation")
			|| text.Contains("steam");
	}

	private static bool IsXInputLikeGamepad(Gamepad gamepad)
	{
		if (gamepad == null)
		{
			return false;
		}

		InputDeviceDescription description = gamepad.description;
		string text = $"{description.interfaceName} {description.product} {gamepad.displayName} {gamepad.layout}".ToLowerInvariant();
		return text.Contains("xinput") || text.Contains("xbox");
	}

	private static bool IsLikelyMirroredJoystick(Joystick joystick, IReadOnlyList<Gamepad> assignableGamepads)
	{
		if (joystick == null || assignableGamepads == null || assignableGamepads.Count == 0)
		{
			return false;
		}

		InputDeviceDescription description = joystick.description;
		if (!string.IsNullOrWhiteSpace(description.serial))
		{
			return false;
		}

		if (!IsGameControllerLikeJoystick(joystick))
		{
			return false;
		}

		for (int i = 0; i < assignableGamepads.Count; i++)
		{
			Gamepad gamepad = assignableGamepads[i];
			if (gamepad != null && IsXInputLikeGamepad(gamepad))
			{
				return true;
			}
		}

		return false;
	}

	private static bool IsGameControllerLikeJoystick(Joystick joystick)
	{
		if (joystick == null)
		{
			return false;
		}

		InputDeviceDescription description = joystick.description;
		string text = $"{description.interfaceName} {description.manufacturer} {description.product} {joystick.displayName} {joystick.layout}".ToLowerInvariant();
		return text.Contains("xinput")
			|| text.Contains("xbox")
			|| text.Contains("dualshock")
			|| text.Contains("dualsense")
			|| text.Contains("wireless controller")
			|| text.Contains("playstation")
			|| text.Contains("steam");
	}

	// 同一シリアルが取れる場合だけ、1台の物理入力として扱う。
	private static string BuildPhysicalDeviceKey(InputDevice device)
	{
		if (device == null)
		{
			return string.Empty;
		}

		InputDeviceDescription description = device.description;
		if (!string.IsNullOrWhiteSpace(description.serial))
		{
			return $"serial:{NormalizeDeviceDescriptionPart(description.serial)}";
		}

		return string.Empty;
	}

	private static string NormalizeDeviceDescriptionPart(string value)
	{
		return value.Trim().ToLowerInvariant();
	}

	private static string FormatDeviceDescription(InputDevice device)
	{
		if (device == null)
		{
			return "Unknown";
		}

		InputDeviceDescription description = device.description;
		return $"{device.displayName} [{description.interfaceName} / {description.manufacturer} / {description.product} / {description.serial}]";
	}

	// 接続状態が変わった場合だけ割り当てを再評価する。
	private void HandleDeviceChange(InputDevice device, InputDeviceChange change)
	{
		if (device is not Gamepad && device is not Joystick && device is not Keyboard && device is not Mouse)
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

	private void HandleInputEvent(InputEventPtr eventPtr, InputDevice device)
	{
		if (device is not Gamepad && device is not Joystick)
		{
			return;
		}

		RememberRecentlyActiveGameDevice(device);
		float now = Time.realtimeSinceStartup;
		if (inputEventLogTimes.TryGetValue(device.deviceId, out float lastLogTime) && now - lastLogTime < 0.5f)
		{
			return;
		}

		inputEventLogTimes[device.deviceId] = now;
		Debug.Log($"Input event: {FormatDeviceDescription(device)} type={eventPtr.type}");
	}

	private void RememberRecentlyActiveGameDevice(InputDevice device)
	{
		if (device == null)
		{
			return;
		}

		for (int i = recentlyActiveGameDevices.Count - 1; i >= 0; i--)
		{
			if (recentlyActiveGameDevices[i] == null
				|| !recentlyActiveGameDevices[i].added
				|| recentlyActiveGameDevices[i].deviceId == device.deviceId)
			{
				recentlyActiveGameDevices.RemoveAt(i);
			}
		}

		recentlyActiveGameDevices.Insert(0, device);
		const int maxRecentDeviceCount = 8;
		while (recentlyActiveGameDevices.Count > maxRecentDeviceCount)
		{
			recentlyActiveGameDevices.RemoveAt(recentlyActiveGameDevices.Count - 1);
		}
	}

	// ログ表示用にデバイス名を連結する。
	private static string FormatDeviceNames(InputDevice[] devices)
	{
		if (devices == null || devices.Length == 0)
		{
			return "None";
		}

		string result = FormatDeviceName(devices[0]);
		for (int i = 1; i < devices.Length; i++)
		{
			result += $", {FormatDeviceName(devices[i])}";
		}

		return result;
	}

	private static string FormatDeviceName(InputDevice device)
	{
		if (device == null)
		{
			return "Unknown";
		}

		InputDeviceDescription description = device.description;
		return $"{device.displayName}#{device.deviceId} [{description.interfaceName} / {description.product} / {description.serial}]";
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

	// 全プレイヤーのゲームプレイ入力受付を一括で切り替える。
	public void SetGameplayInputEnabledForAllPlayers(bool enabled)
	{
		foreach (PlayerController player in players)
		{
			if (player == null)
			{
				continue;
			}

			player.SetGameplayInputEnabled(enabled);
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
