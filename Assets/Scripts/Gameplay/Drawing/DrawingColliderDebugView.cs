using UnityEngine;

[DisallowMultipleComponent]
// 生成済みカプセルの形状を Scene ビューへ表示する。
public sealed class DrawingColliderDebugView : MonoBehaviour
{
	private bool visible;
	private Color color = Color.magenta;

	public void Configure(bool show, Color debugColor)
	{
		visible = show;
		color = debugColor;
	}

	private void OnDrawGizmos()
	{
		if (!visible)
		{
			return;
		}

		Gizmos.color = color;
		CapsuleCollider2D[] colliders = GetComponentsInChildren<CapsuleCollider2D>(true);
		foreach (CapsuleCollider2D capsule in colliders)
		{
			DrawHorizontalCapsule(capsule);
		}
	}

	private static void DrawHorizontalCapsule(CapsuleCollider2D capsule)
	{
		Transform capsuleTransform = capsule.transform;
		Vector3 scale = capsuleTransform.lossyScale;
		float radius = capsule.size.y * Mathf.Abs(scale.y) * 0.5f;
		float totalLength = capsule.size.x * Mathf.Abs(scale.x);
		float halfCenterLine = Mathf.Max(0f, totalLength * 0.5f - radius);
		Vector3 center = capsuleTransform.TransformPoint(capsule.offset);
		Vector3 direction = capsuleTransform.right.normalized;
		Vector3 normal = capsuleTransform.up.normalized;
		Vector3 start = center - direction * halfCenterLine;
		Vector3 end = center + direction * halfCenterLine;

		Gizmos.DrawWireSphere(start, radius);
		Gizmos.DrawWireSphere(end, radius);
		Gizmos.DrawLine(start + normal * radius, end + normal * radius);
		Gizmos.DrawLine(start - normal * radius, end - normal * radius);
	}
}
