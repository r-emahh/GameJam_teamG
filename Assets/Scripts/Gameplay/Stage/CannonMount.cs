using UnityEngine;

[DisallowMultipleComponent]
// 大砲の設置位置と選択順を表す。
public sealed class CannonMount : MonoBehaviour
{
	// カメラ端から少し内側に寄せる量を保持する。
	[SerializeField]
	private Vector2 edgeInset = new Vector2(0.7f, 0.7f);

	// マウント順を保持する。
	[SerializeField]
	private int order;

	// 参照するカメラを保持する。
	private Camera targetCamera;

	// 外部から参照する順序を返す。
	public int Order => order;

	// シーン構築時に順序を設定する。
	public void Configure(int mountOrder)
	{
		order = mountOrder;
		ApplyPlacement();
	}

	// 毎フレームカメラ角へ追従する。
	private void LateUpdate()
	{
		ApplyPlacement();
	}

	// カメラの四隅に対応した位置と向きを適用する。
	private void ApplyPlacement()
	{
		if (targetCamera == null)
		{
			targetCamera = Camera.main;
		}

		if (targetCamera == null || !targetCamera.orthographic)
		{
			return;
		}

		float halfHeight = targetCamera.orthographicSize;
		float halfWidth = halfHeight * targetCamera.aspect;
		Vector3 center = targetCamera.transform.position;
		Vector2 corner = GetCornerOffset(order);
		transform.position = new Vector3(
			center.x + corner.x * Mathf.Max(0f, halfWidth - edgeInset.x),
			center.y + corner.y * Mathf.Max(0f, halfHeight - edgeInset.y),
			transform.position.z);
		transform.rotation = Quaternion.Euler(0f, 0f, GetCornerRotation(order));
	}

	// 順番に応じた画面角の方向を返す。
	private static Vector2 GetCornerOffset(int mountOrder)
	{
		switch (mountOrder)
		{
			case 1:
				return new Vector2(1f, 1f);
			case 2:
				return new Vector2(-1f, -1f);
			case 3:
				return new Vector2(1f, -1f);
			default:
				return new Vector2(-1f, 1f);
		}
	}

	// 角に応じた回転を返す。
	private static float GetCornerRotation(int mountOrder)
	{
		switch (mountOrder)
		{
			case 1:
				return 225f;
			case 2:
				return 45f;
			case 3:
				return 135f;
			default:
				return 315f;
		}
	}
}
