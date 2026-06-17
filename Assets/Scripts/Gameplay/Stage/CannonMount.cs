using UnityEngine;

[DisallowMultipleComponent]
// 大砲の設置位置と選択順を表す。
public sealed class CannonMount : MonoBehaviour
{
	// カメラ上端から少し内側に寄せる量を保持する。
	[SerializeField]
	private Vector2 edgeInset = new Vector2(0.7f, 0.7f);

	// マウント順を保持する。
	[SerializeField]
	private int order;

	// ステージ中央として参照する Transform。未指定時は DrawingSurface またはカメラ中央を使う。
	[SerializeField]
	private Transform stageCenter;

	// ステージ中央方向から許可する最小相対角度。
	[SerializeField]
	private float minimumAngle = -60f;

	// ステージ中央方向から許可する最大相対角度。
	[SerializeField]
	private float maximumAngle = 60f;

	// ラウンド開始時の相対角度。
	[SerializeField]
	private float initialAngle;

	// 参照するカメラを保持する。
	private Camera targetCamera;
	// 自動検出したステージ中央を保持する。
	private DrawingSurface drawingSurface;
	// 現在のステージ中央方向からの相対角度を保持する。
	private float currentAngle;

	// 外部から参照する順序を返す。
	public int Order => order;
	// 現在のステージ中央方向からの相対角度を返す。
	public float CurrentAngle => currentAngle;

	// 起動時に Inspector の初期角度を適用する。
	private void Awake()
	{
		ResetAngle();
	}

	// Inspector 変更時にも角度範囲を正規化する。
	private void OnValidate()
	{
		NormalizeAngleSettings();
		currentAngle = Mathf.Clamp(currentAngle, minimumAngle, maximumAngle);
		ApplyRotation();
	}

	// シーン構築時に順序を設定する。
	public void Configure(int mountOrder)
	{
		order = mountOrder;
		ResetAngle();
		ApplyPlacementAndRotation();
	}

	// テストやランタイム構築から角度範囲と初期角度を設定する。
	public void ConfigureAim(float minAngle, float maxAngle, float startAngle)
	{
		minimumAngle = minAngle;
		maximumAngle = maxAngle;
		initialAngle = startAngle;
		NormalizeAngleSettings();
		ResetAngle();
	}

	// 明示した Transform をステージ中央として使う。
	public void ConfigureStageCenter(Transform center)
	{
		stageCenter = center;
		ApplyRotation();
	}

	// 指定量だけ相対角度を変更し、Inspector の範囲内へ制限する。
	public void AdjustAngle(float deltaDegrees)
	{
		SetAngle(currentAngle + deltaDegrees);
	}

	// 相対角度を直接設定する。
	public void SetAngle(float angle)
	{
		NormalizeAngleSettings();
		currentAngle = Mathf.Clamp(angle, minimumAngle, maximumAngle);
		ApplyRotation();
	}

	// ワールド方向を指定し、ステージ中央基準の相対角度へ変換して適用する。
	public void SetWorldDirection(Vector2 worldDirection)
	{
		if (worldDirection.sqrMagnitude <= Mathf.Epsilon)
		{
			return;
		}

		Vector3 center = ResolveStageCenter();
		Vector2 centerDirection = center - transform.position;
		if (centerDirection.sqrMagnitude <= Mathf.Epsilon)
		{
			centerDirection = Vector2.down;
		}

		float centerAngle = Mathf.Atan2(centerDirection.y, centerDirection.x) * Mathf.Rad2Deg;
		float targetAngle = Mathf.Atan2(worldDirection.y, worldDirection.x) * Mathf.Rad2Deg;
		SetAngle(Mathf.DeltaAngle(centerAngle, targetAngle));
	}

	// ラウンド開始時の角度へ戻す。
	public void ResetAngle()
	{
		NormalizeAngleSettings();
		currentAngle = Mathf.Clamp(initialAngle, minimumAngle, maximumAngle);
		ApplyRotation();
	}

	// 毎フレームカメラ上中央へ追従する。
	private void LateUpdate()
	{
		ApplyPlacementAndRotation();
	}

	// 画面上中央の位置とステージ中央基準の角度を適用する。
	private void ApplyPlacementAndRotation()
	{
		if (targetCamera == null)
		{
			targetCamera = Camera.main;
		}

		if (targetCamera != null && targetCamera.orthographic)
		{
			float halfHeight = targetCamera.orthographicSize;
			Vector3 center = targetCamera.transform.position;
			transform.position = new Vector3(
				center.x,
				center.y + Mathf.Max(0f, halfHeight - edgeInset.y),
				transform.position.z);
		}

		ApplyRotation();
	}

	// 各大砲の位置からステージ中央へ向く角度へ相対角度を加える。
	private void ApplyRotation()
	{
		Vector3 center = ResolveStageCenter();
		Vector2 direction = center - transform.position;
		if (direction.sqrMagnitude <= Mathf.Epsilon)
		{
			direction = Vector2.down;
		}

		float centerAngle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
		transform.rotation = Quaternion.Euler(0f, 0f, centerAngle + currentAngle);
	}

	// ステージ中央を Inspector 指定、DrawingSurface、カメラの順で解決する。
	private Vector3 ResolveStageCenter()
	{
		if (stageCenter != null)
		{
			return stageCenter.position;
		}

		if (drawingSurface == null)
		{
			drawingSurface = FindFirstObjectByType<DrawingSurface>();
		}

		if (drawingSurface != null)
		{
			return drawingSurface.transform.position;
		}

		return targetCamera != null ? targetCamera.transform.position : Vector3.zero;
	}

	// 最小・最大角度の逆転を補正し、初期角度を範囲内へ収める。
	private void NormalizeAngleSettings()
	{
		if (minimumAngle > maximumAngle)
		{
			(minimumAngle, maximumAngle) = (maximumAngle, minimumAngle);
		}

		initialAngle = Mathf.Clamp(initialAngle, minimumAngle, maximumAngle);
	}
}
