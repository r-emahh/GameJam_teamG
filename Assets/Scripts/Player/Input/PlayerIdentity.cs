using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Users;

[DisallowMultipleComponent]
[RequireComponent(typeof(PlayerInput))]
// プレイヤーの陣営とローカル入力スキームを管理する。
public sealed class PlayerIdentity : MonoBehaviour
{
	private const string GameplayActionMap = "Player";

	// Inspector から初期陣営を設定できる。
	[SerializeField]
	private MatchSide controlledSide = MatchSide.GoalRunner;

	// このプレイヤーに紐づく入力コンポーネントを保持する。
	private PlayerInput playerInput;

	// 現在の操作陣営を返す。
	public MatchSide ControlledSide => controlledSide;

	// PlayerInput をキャッシュする。
	private void Awake()
	{
		playerInput = GetComponent<PlayerInput>();
	}

	// 陣営だけを差し替える。
	public void AssignSide(MatchSide side)
	{
		controlledSide = side;
	}

	// 指定された物理デバイスだけをローカル入力として設定する。
	public bool ConfigureLocalInput(string controlScheme, params InputDevice[] devices)
	{
		if (!playerInput)
		{
			playerInput = GetComponent<PlayerInput>();
		}

		if (playerInput == null
			|| !playerInput.isActiveAndEnabled
			|| playerInput.actions == null
			|| string.IsNullOrEmpty(controlScheme)
			|| devices == null
			|| devices.Length == 0)
		{
			ClearLocalInput();
			return false;
		}

		playerInput.neverAutoSwitchControlSchemes = true;
		for (int i = 0; i < devices.Length; i++)
		{
			if (devices[i] == null || !devices[i].added)
			{
				ClearLocalInput();
				return false;
			}
		}

		try
		{
			playerInput.SwitchCurrentControlScheme(controlScheme, devices);
			playerInput.notificationBehavior = PlayerNotifications.InvokeCSharpEvents;
			playerInput.ActivateInput();
			string actionMapName = !string.IsNullOrEmpty(playerInput.defaultActionMap)
				? playerInput.defaultActionMap
				: GameplayActionMap;
			playerInput.SwitchCurrentActionMap(actionMapName);
			playerInput.currentActionMap?.Enable();
			return true;
		}
		catch (System.ArgumentException exception)
		{
			Debug.LogWarning($"Failed to configure local input: {exception.Message}", this);
		}
		catch (System.InvalidOperationException exception)
		{
			Debug.LogWarning($"Failed to configure local input: {exception.Message}", this);
		}

		ClearLocalInput();
		return false;
	}

	// 入力を停止し、現在のペアリングを解除する。
	public void ClearLocalInput()
	{
		if (!playerInput)
		{
			playerInput = GetComponent<PlayerInput>();
		}

		if (playerInput == null)
		{
			return;
		}

		playerInput.neverAutoSwitchControlSchemes = true;
		playerInput.DeactivateInput();
		InputUser user = playerInput.user;
		if (user.valid)
		{
			user.UnpairDevices();
		}
	}
}
