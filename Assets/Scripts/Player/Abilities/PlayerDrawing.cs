using TMPro;
using UnityEngine;

[DisallowMultipleComponent]
// 描画フェーズ中のカーソル移動とスタンプ生成を担当する。
public sealed class PlayerDrawing : MonoBehaviour
{
	// 利用する固定図形を管理する。
	private static readonly DrawingStampShape[] Shapes =
	{
		DrawingStampShape.Square,
		DrawingStampShape.Circle,
		DrawingStampShape.Triangle
	};

	// カーソルの移動速度を設定する。
	[SerializeField]
	private float cursorSpeed = 3.2f;

	// カメラ端からカーソルを止める余白を保持する。
	[SerializeField]
	private Vector2 cameraEdgePadding = new Vector2(0.7f, 0.7f);

	// スタンプの表示倍率を保持する。
	private const float StampScale = 0.5f;
	// カーソル表示の倍率を保持する。
	private const float CursorVisualScale = 0.5f;
	// カーソル上に表示する残数ラベルの高さを保持する。
	private const float CursorLabelOffsetY = 0.78f;

	// カーソル表示を保持する。
	private GameObject cursorVisual;
	// カーソル表示の描画コンポーネントを保持する。
	private SpriteRenderer cursorRenderer;
	// カーソル上の残数表示を保持する。
	private TextMeshPro cursorCountText;
	// 入力方向を保持する。
	private Vector2 moveInput;
	// 現在のカーソル位置を保持する。
	private Vector3 cursorPosition;
	// 現在選択中の図形を保持する。
	private int currentShapeIndex;
	// 現在の陣営を保持する。
	private MatchSide controlledSide = MatchSide.GoalRunner;
	// 表示用カメラを保持する。
	private Camera targetCamera;

	// 現在選択中の図形を返す。
	public DrawingStampShape CurrentShape => Shapes[currentShapeIndex];

	// 開始位置を現在座標に合わせる。
	private void Awake()
	{
		cursorPosition = transform.position;
		targetCamera = Camera.main;
		EnsureCursorVisual();
		UpdateCursorVisual();
		UpdateCursorBudgetLabel();
		SetCursorVisible(false);
	}

	// 破棄時にランタイム生成したカーソルを掃除する。
	private void OnDestroy()
	{
		if (cursorVisual != null)
		{
			Destroy(cursorVisual);
		}
	}

	// 移動入力を保持する。
	public void SetMoveInput(Vector2 input)
	{
		moveInput = input;
	}

	// 図形を次の候補へ切り替える。
	public void CycleShape()
	{
		if (!IsDrawPhase())
		{
			return;
		}

		currentShapeIndex = (currentShapeIndex + 1) % Shapes.Length;
		UpdateCursorVisual();
		UpdateCursorBudgetLabel();
	}

	// このカーソルが担当する陣営を設定する。
	public void ConfigureControlledSide(MatchSide side)
	{
		controlledSide = side;
		UpdateCursorBudgetLabel();
	}

	// 現在の図形を1枚だけ貼り付ける。
	public void PlaceStamp()
	{
		if (!IsDrawPhase())
		{
			return;
		}

		if (!IsCursorInsideCameraView())
		{
			return;
		}

		GameObject stamp = new GameObject($"{CurrentShape}Stamp");
		stamp.transform.position = cursorPosition;
		stamp.transform.localScale = new Vector3(StampScale, StampScale, 1f);

		SpriteRenderer renderer = stamp.AddComponent<SpriteRenderer>();
		renderer.sprite = RuntimeSpriteFactory.GetDrawingStampSprite(CurrentShape);
		renderer.color = new Color(0.93f, 0.93f, 0.93f, 1f);
		renderer.sortingOrder = 1;

		switch (CurrentShape)
		{
			case DrawingStampShape.Circle:
				stamp.AddComponent<CircleCollider2D>().radius = 0.5f;
				break;
			case DrawingStampShape.Triangle:
				stamp.AddComponent<PolygonCollider2D>().points = new[]
				{
					new Vector2(0f, 0.5f),
					new Vector2(-0.5f, -0.45f),
					new Vector2(0.5f, -0.45f)
				};
				break;
			default:
				stamp.AddComponent<BoxCollider2D>().size = Vector2.one;
				break;
		}
	}

	// 描画フェーズ中だけカーソルを進める。
	public void TickFixed()
	{
		if (!IsDrawPhase())
		{
			SetCursorVisible(false);
			return;
		}

		if (targetCamera == null)
		{
			targetCamera = Camera.main;
		}

		if (targetCamera == null)
		{
			SetCursorVisible(false);
			return;
		}

		EnsureCursorVisual();
		UpdateCursorVisual();
		cursorPosition += (Vector3)(moveInput * cursorSpeed * Time.fixedDeltaTime);
		cursorPosition = ClampToCameraView(cursorPosition);
		cursorVisual.transform.position = cursorPosition;
		UpdateCursorBudgetLabel();
		SetCursorVisible(true);
	}

	// カーソル用の表示オブジェクトを必要なら生成する。
	private void EnsureCursorVisual()
	{
		if (cursorVisual != null)
		{
			return;
		}

		cursorVisual = new GameObject("DrawingCursor");
		cursorVisual.transform.SetParent(null, true);
		cursorRenderer = cursorVisual.AddComponent<SpriteRenderer>();
		cursorRenderer.color = new Color(0.15f, 0.95f, 1f, 0.7f);
		cursorRenderer.sortingOrder = 3;
		cursorVisual.transform.localScale = new Vector3(CursorVisualScale, CursorVisualScale, 1f);

		GameObject cursorCountObject = new GameObject("DrawingCursorCount");
		cursorCountObject.transform.SetParent(cursorVisual.transform, false);
		cursorCountObject.transform.localPosition = new Vector3(0f, CursorLabelOffsetY, 0f);
		cursorCountObject.transform.localScale = Vector3.one * 0.1f;
		cursorCountText = cursorCountObject.AddComponent<TextMeshPro>();
		cursorCountText.alignment = TextAlignmentOptions.Center;
		cursorCountText.fontSize = 4f;
		cursorCountText.color = Color.white;
		cursorCountText.enableAutoSizing = false;
		cursorCountText.text = "x0";

		MeshRenderer cursorCountRenderer = cursorCountObject.GetComponent<MeshRenderer>();
		if (cursorCountRenderer != null)
		{
			cursorCountRenderer.sortingOrder = 4;
		}
	}

	// カーソルの見た目を現在の図形に合わせる。
	private void UpdateCursorVisual()
	{
		if (cursorRenderer == null)
		{
			return;
		}

		cursorRenderer.sprite = RuntimeSpriteFactory.GetDrawingStampSprite(CurrentShape);
	}

	// カーソル上の残数表示を更新する。
	private void UpdateCursorBudgetLabel()
	{
		if (cursorCountText == null)
		{
			return;
		}

		cursorCountText.text = $"x{GetRemainingStampCount()}";
	}

	// カーソルの表示を切り替える。
	private void SetCursorVisible(bool visible)
	{
		if (cursorVisual == null)
		{
			return;
		}

		cursorVisual.SetActive(visible);
	}

	// カメラ表示範囲内にカーソルを収める。
	private Vector3 ClampToCameraView(Vector3 worldPosition)
	{
		Rect viewBounds = GetPaddedCameraViewBounds();
		return new Vector3(
			Mathf.Clamp(worldPosition.x, viewBounds.xMin, viewBounds.xMax),
			Mathf.Clamp(worldPosition.y, viewBounds.yMin, viewBounds.yMax),
			worldPosition.z);
	}

	// カーソルがカメラ表示範囲内かを返す。
	private bool IsCursorInsideCameraView()
	{
		return GetPaddedCameraViewBounds().Contains(cursorPosition);
	}

	// カメラのワールド表示範囲を返す。
	private Rect GetCameraViewBounds()
	{
		if (targetCamera == null)
		{
			targetCamera = Camera.main;
		}

		if (targetCamera == null || !targetCamera.orthographic)
		{
			return new Rect(-10f, -5f, 20f, 10f);
		}

		float height = targetCamera.orthographicSize * 2f;
		float width = height * targetCamera.aspect;
		Vector3 center = targetCamera.transform.position;
		return new Rect(center.x - width * 0.5f, center.y - height * 0.5f, width, height);
	}

	// カーソル用に少し内側へ縮めた表示範囲を返す。
	private Rect GetPaddedCameraViewBounds()
	{
		Rect bounds = GetCameraViewBounds();
		float horizontalPadding = Mathf.Min(cameraEdgePadding.x, Mathf.Max(0f, bounds.width * 0.5f - 0.01f));
		float verticalPadding = Mathf.Min(cameraEdgePadding.y, Mathf.Max(0f, bounds.height * 0.5f - 0.01f));
		bounds.xMin += horizontalPadding;
		bounds.xMax -= horizontalPadding;
		bounds.yMin += verticalPadding;
		bounds.yMax -= verticalPadding;
		return bounds;
	}

	// 現在の陣営に残っているスタンプ数を返す。
	private int GetRemainingStampCount()
	{
		if (GameManager.Instance == null)
		{
			return 0;
		}

		return controlledSide == MatchSide.Blocker
			? GameManager.Instance.BlockerShapesRemaining
			: GameManager.Instance.GoalRunnerShapesRemaining;
	}

	// 描画フェーズかどうかを返す。
	private static bool IsDrawPhase()
	{
		return GameManager.Instance != null && GameManager.currentState == GameState.Game && GameManager.Instance.CurrentPhase == MatchPhase.Draw;
	}
}
