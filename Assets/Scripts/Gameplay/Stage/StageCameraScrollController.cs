using UnityEngine;

[DisallowMultipleComponent]
// タイルマップの範囲内で、進行役のプレイヤーへカメラを追従させる。
public sealed class StageCameraScrollController : MonoBehaviour
{
	[SerializeField, Min(0f)]
	private float followSmoothTime = 0.18f;

	[SerializeField]
	private Vector2 focusOffset = new Vector2(0f, 0.35f);

	[SerializeField, Min(0f)]
	private float edgePadding = 0.15f;

	private Camera targetCamera;
	private Rect worldBounds;
	private bool hasWorldBounds;
	private Vector3 followVelocity;

	// ステージ範囲を設定する。
	public void Configure(Rect bounds)
	{
		worldBounds = bounds;
		hasWorldBounds = true;
		followVelocity = Vector3.zero;
		ClampCameraInstantly();
	}

	// 現在の追従対象へ補間なしで移動する。
	public void SnapToTarget()
	{
		EnsureCameraReference();
		if (targetCamera == null)
		{
			return;
		}

		Vector3 targetPosition = ResolveTargetPosition();
		targetPosition.z = transform.position.z;
		transform.position = ClampToWorldBounds(targetPosition);
		followVelocity = Vector3.zero;
	}

	private void Awake()
	{
		EnsureCameraReference();
	}

	private void LateUpdate()
	{
		EnsureCameraReference();
		if (targetCamera == null)
		{
			return;
		}

		Vector3 targetPosition = ResolveTargetPosition();
		targetPosition.z = transform.position.z;
		Vector3 clampedTarget = ClampToWorldBounds(targetPosition);

		if (followSmoothTime <= 0f)
		{
			transform.position = clampedTarget;
			return;
		}

		transform.position = Vector3.SmoothDamp(
			transform.position,
			clampedTarget,
			ref followVelocity,
			followSmoothTime,
			Mathf.Infinity,
			Time.deltaTime);
		transform.position = ClampToWorldBounds(transform.position);
	}

	private void EnsureCameraReference()
	{
		if (targetCamera == null)
		{
			targetCamera = GetComponent<Camera>();
		}

		if (targetCamera == null)
		{
			targetCamera = Camera.main;
		}
	}

	private Vector3 ResolveTargetPosition()
	{
		if (InputManager.Instance == null)
		{
			return hasWorldBounds
				? new Vector3(worldBounds.center.x, worldBounds.center.y, transform.position.z)
				: transform.position;
		}

		PlayerController focusPlayer = null;
		foreach (PlayerController player in InputManager.Instance.GetRegisteredPlayers())
		{
			if (player == null)
			{
				continue;
			}

			if (focusPlayer == null)
			{
				focusPlayer = player;
			}

			if (player.ControlledSide == MatchSide.GoalRunner)
			{
				focusPlayer = player;
				break;
			}
		}

		if (focusPlayer == null)
		{
			return hasWorldBounds
				? new Vector3(worldBounds.center.x, worldBounds.center.y, transform.position.z)
				: transform.position;
		}

		Vector3 targetPosition = focusPlayer.transform.position + (Vector3)focusOffset;
		targetPosition.z = transform.position.z;
		return targetPosition;
	}

	private Vector3 ClampToWorldBounds(Vector3 desiredPosition)
	{
		if (!hasWorldBounds || targetCamera == null || !targetCamera.orthographic)
		{
			return desiredPosition;
		}

		float halfHeight = targetCamera.orthographicSize;
		float halfWidth = halfHeight * targetCamera.aspect;
		float minX = worldBounds.xMin + halfWidth + edgePadding;
		float maxX = worldBounds.xMax - halfWidth - edgePadding;
		float minY = worldBounds.yMin + halfHeight + edgePadding;
		float maxY = worldBounds.yMax - halfHeight - edgePadding;

		if (minX > maxX)
		{
			desiredPosition.x = worldBounds.center.x;
		}
		else
		{
			desiredPosition.x = Mathf.Clamp(desiredPosition.x, minX, maxX);
		}

		if (minY > maxY)
		{
			desiredPosition.y = worldBounds.center.y;
		}
		else
		{
			desiredPosition.y = Mathf.Clamp(desiredPosition.y, minY, maxY);
		}

		return desiredPosition;
	}

	private void ClampCameraInstantly()
	{
		EnsureCameraReference();
		if (targetCamera == null)
		{
			return;
		}

		Vector3 clamped = ClampToWorldBounds(transform.position);
		clamped.z = transform.position.z;
		transform.position = clamped;
	}
}
