using UnityEngine;
using UnityEngine.InputSystem;

[DisallowMultipleComponent]
[RequireComponent(typeof(PlayerInput))]
// プレイヤーの陣営とローカル入力スキームを管理する。
public sealed class PlayerIdentity : MonoBehaviour
{
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

	// ローカルプレイヤーとして入力スキームを設定する。
	public void ConfigureLocalInput(MatchSide side, bool useGamepad)
	{
		controlledSide = side;
		if (!playerInput)
		{
			playerInput = GetComponent<PlayerInput>();
		}

		if (playerInput == null)
		{
			return;
		}

		playerInput.neverAutoSwitchControlSchemes = true;
		if (useGamepad && Gamepad.current != null)
		{
			playerInput.SwitchCurrentControlScheme("Gamepad", Gamepad.current);
			return;
		}

		if (Keyboard.current != null && Mouse.current != null)
		{
			playerInput.SwitchCurrentControlScheme("Keyboard&Mouse", Keyboard.current, Mouse.current);
		}
	}
}
