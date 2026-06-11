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
		if (GameManager.Instance == null || GameManager.currentState != GameState.Game)
		{
			return;
		}

		if (other.GetComponentInParent<PlayerIdentity>() != null)
		{
			GameManager.Instance.MarkGoalReached();
		}
	}
}
