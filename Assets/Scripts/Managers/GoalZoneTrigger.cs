using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class GoalZoneTrigger : MonoBehaviour
{
	private void Awake()
	{
		Collider2D collider2D = GetComponent<Collider2D>();
		collider2D.isTrigger = true;
	}

	private void OnTriggerEnter2D(Collider2D other)
	{
		if (GameManager.Instance == null || GameManager.currentState != GameState.Game)
		{
			return;
		}

		if (other.GetComponentInParent<PlayerController>() == null)
		{
			return;
		}

		GameManager.Instance.MarkGoalReached();
	}
}
