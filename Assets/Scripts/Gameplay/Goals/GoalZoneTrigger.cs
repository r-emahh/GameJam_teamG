using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Collider2D))]
// ゴール到達を検知して試合ロジックへ通知する。
public sealed class GoalZoneTrigger : MonoBehaviour
{
	// 必ずトリガーとして使う。
	private void Awake()
	{
		GetComponent<Collider2D>().isTrigger = true;
	}

	// プレイヤーが入ったらゴール成立を通知する。
	private void OnTriggerEnter2D(Collider2D other)
	{
		if (GameManager.Instance == null || !TryGetPlayerSide(other, out MatchSide side))
		{
			return;
		}

		GameManager.Instance.TryMarkGoalReached(side);
	}

	// 接触した Collider がプレイヤーに属する場合だけ、その役割を返す。
	public static bool TryGetPlayerSide(Collider2D other, out MatchSide side)
	{
		side = default;
		if (other == null)
		{
			return false;
		}

		PlayerIdentity player = other.GetComponentInParent<PlayerIdentity>();
		if (player == null)
		{
			return false;
		}

		side = player.ControlledSide;
		return true;
	}
}
