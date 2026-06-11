using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(PlayerMotor2D))]
// スタン効果を移動ロジックへ反映する。
public sealed class PlayerStun : MonoBehaviour
{
	// 移動コンポーネントを保持する。
	private PlayerMotor2D motor;

	// 移動コンポーネントを取得する。
	private void Awake()
	{
		motor = GetComponent<PlayerMotor2D>();
	}

	// 指定時間入力を止めて速度をリセットする。
	public void Apply(float duration)
	{
		motor.LockInput(duration);
		motor.Stop();
	}
}
