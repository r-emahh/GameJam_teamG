using UnityEngine;

[DisallowMultipleComponent]
// 自由描画の入力を Unity 非依存レコーダーへ橋渡しする。
public sealed class PlayerDrawing : MonoBehaviour
{
	[SerializeField, Min(0f)]
	private float cursorSpeed = 3.2f;

	[SerializeField, Min(0)]
	private int maxPointCount = 256;

	[SerializeField, Min(0f)]
	private float maxTotalLineLength = 30f;

	[SerializeField, Min(0f)]
	private float minPointSpacing = 0.08f;

	private const float CursorVisualScale = 0.22f;
	private Vector2 moveInput;
	private bool drawButtonPressed;
	private MatchSide controlledSide = MatchSide.GoalRunner;
	private DrawingSurface drawingSurface;
	private DrawingRecorder recorder;
	private DrawingPlayerSlot playerSlot;
	private GameObject cursorVisual;
	private SpriteRenderer cursorRenderer;

	public DrawingArtifactData Artifact => recorder?.Artifact;
	public int MaxPointCount => maxPointCount;
	public bool IsConfirmed => recorder != null && recorder.Artifact.IsConfirmed;
	public int PointCount => recorder?.Artifact.PointCount ?? 0;

	// このプレイヤーが確定した描画を発射用オブジェクトへ複製する。
	public bool TryConfigureProjectile(CannonProjectile projectile)
	{
		return EnsureRecorder() && drawingSurface.TryConfigureProjectile(playerSlot, projectile);
	}

	private void Awake()
	{
		EnsureRecorder();
		EnsureCursorVisual();
		SetCursorVisible(false);
	}

	private void OnDestroy()
	{
		if (drawingSurface != null)
		{
			drawingSurface.Unregister(this);
		}

		if (cursorVisual != null)
		{
			Destroy(cursorVisual);
		}
	}

	public void SetMoveInput(Vector2 input)
	{
		moveInput = input;
	}

	public void SetDrawButtonPressed(bool pressed)
	{
		drawButtonPressed = pressed;
		if (!IsDrawPhase() || !EnsureRecorder())
		{
			if (!pressed)
			{
				recorder?.SetDrawingActive(false);
			}
			return;
		}

		if (recorder.SetDrawingActive(pressed))
		{
			drawingSurface.RefreshVisual(playerSlot);
		}
	}

	public void ClearDrawing()
	{
		if (!IsDrawPhase() || !EnsureRecorder())
		{
			return;
		}

		drawButtonPressed = false;
		recorder.Clear();
		drawingSurface.RefreshVisual(playerSlot);
	}

	public void ConfirmDrawing()
	{
		if (!IsDrawPhase() || !EnsureRecorder())
		{
			return;
		}

		drawButtonPressed = false;
		recorder.Confirm();
		drawingSurface.RefreshVisual(playerSlot);
	}

	public void ConfigureControlledSide(MatchSide side)
	{
		controlledSide = side;
		UpdateCursorColor();
	}

	public void ResetForNextRound()
	{
		moveInput = Vector2.zero;
		drawButtonPressed = false;
		if (EnsureRecorder())
		{
			recorder.Clear();
			recorder.SetCursorPosition(ToData(transform.position));
			drawingSurface.RefreshVisual(playerSlot);
			UpdateCursorPosition();
		}

		SetCursorVisible(false);
	}

	public void TickFixed()
	{
		if (!IsDrawPhase() || !EnsureRecorder())
		{
			recorder?.SetDrawingActive(false);
			drawButtonPressed = false;
			SetCursorVisible(false);
			return;
		}

		DrawingRectData bounds = drawingSurface.DataBounds;
		recorder.SetBounds(bounds);
		bool changed = recorder.MoveCursor(
			moveInput.x * cursorSpeed * Time.fixedDeltaTime,
			moveInput.y * cursorSpeed * Time.fixedDeltaTime);

		if (drawButtonPressed && !recorder.IsDrawing && !recorder.Artifact.IsConfirmed)
		{
			changed |= recorder.SetDrawingActive(true);
		}

		if (changed)
		{
			drawingSurface.RefreshVisual(playerSlot);
		}

		EnsureCursorVisual();
		UpdateCursorPosition();
		SetCursorVisible(true);
	}

	private bool EnsureRecorder()
	{
		if (recorder != null && drawingSurface != null)
		{
			return true;
		}

		drawingSurface = FindFirstObjectByType<DrawingSurface>();
		if (drawingSurface == null)
		{
			return false;
		}

		playerSlot = drawingSurface.Register(this);
		DrawingLimitsData limits = new DrawingLimitsData(maxPointCount, maxTotalLineLength, minPointSpacing);
		recorder = new DrawingRecorder(
			drawingSurface.GetArtifact(playerSlot),
			drawingSurface.DataBounds,
			limits,
			ToData(transform.position));
		UpdateCursorColor();
		return true;
	}

	private void EnsureCursorVisual()
	{
		if (cursorVisual != null)
		{
			return;
		}

		cursorVisual = new GameObject("DrawingCursor");
		cursorRenderer = cursorVisual.AddComponent<SpriteRenderer>();
		cursorRenderer.sprite = RuntimeSpriteFactory.UnitSquare;
		cursorRenderer.sortingOrder = 4;
		cursorVisual.transform.localScale = Vector3.one * CursorVisualScale;
		UpdateCursorColor();
		UpdateCursorPosition();
	}

	private void UpdateCursorColor()
	{
		if (cursorRenderer == null)
		{
			return;
		}

		Color color = drawingSurface != null
			? drawingSurface.GetPlayerColor(playerSlot)
			: controlledSide == MatchSide.GoalRunner
				? new Color(0.15f, 0.95f, 1f, 0.95f)
				: new Color(1f, 0.35f, 0.25f, 0.95f);
		color.a = 0.9f;
		cursorRenderer.color = color;
	}

	private void UpdateCursorPosition()
	{
		if (cursorVisual == null || recorder == null)
		{
			return;
		}

		DrawingPointData point = recorder.CursorPosition;
		cursorVisual.transform.position = new Vector3(point.X, point.Y, transform.position.z);
	}

	private void SetCursorVisible(bool visible)
	{
		if (cursorVisual != null)
		{
			cursorVisual.SetActive(visible);
		}
	}

	private static DrawingPointData ToData(Vector3 position) => new DrawingPointData(position.x, position.y);

	private static bool IsDrawPhase()
	{
		return GameManager.Instance != null
			&& GameManager.currentState == GameState.Game
			&& GameManager.Instance.CurrentPhase == MatchPhase.Draw;
	}
}
