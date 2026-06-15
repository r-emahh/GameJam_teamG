using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
// 描画可能領域、2人分のデータ、自由線の表示を管理する。
public sealed class DrawingSurface : MonoBehaviour
{
	private sealed class PlayerVisualState
	{
		public GameObject Root;
		public readonly List<LineRenderer> Strokes = new List<LineRenderer>();
		public GameObject ColliderRoot;
		public bool CollidersBuilt;
	}

	private const string GroundLayerName = "Ground";

	[SerializeField]
	private Vector2 size = new Vector2(7.5f, 2.6f);

	[SerializeField, Min(0.01f)]
	private float lineWidth = 0.12f;

	[SerializeField, Range(0f, 1f)]
	private float drawingAlpha = 0.55f;

	[SerializeField, Range(0.1f, 1f)]
	private float drawingWidthScale = 0.8f;

	[SerializeField]
	private Color playerOneColor = new Color(0.15f, 0.95f, 1f, 0.95f);

	[SerializeField]
	private Color playerTwoColor = new Color(1f, 0.35f, 0.25f, 0.95f);

	[SerializeField, Min(0f)]
	private float colliderSimplificationTolerance = 0.04f;

	[SerializeField, Min(0f)]
	private float duplicatePointDistance = 0.01f;

	[SerializeField, Min(0f)]
	private float minimumColliderSegmentLength = 0.05f;

	[SerializeField, Min(1)]
	private int maximumColliderSegmentsPerStroke = 64;

	[SerializeField]
	private bool showColliderDebug = true;

	[SerializeField]
	private Color colliderDebugColor = new Color(1f, 0.15f, 0.85f, 1f);

	private readonly DrawingArtifactSetData artifacts = new DrawingArtifactSetData();
	private readonly Dictionary<object, DrawingPlayerSlot> registrations = new Dictionary<object, DrawingPlayerSlot>();
	private readonly Dictionary<DrawingPlayerSlot, PlayerVisualState> visualStates = new Dictionary<DrawingPlayerSlot, PlayerVisualState>();
	private Material lineMaterial;
	private GameManager subscribedManager;

	public Rect WorldBounds => new Rect((Vector2)transform.position - size * 0.5f, size);
	public DrawingRectData DataBounds
	{
		get
		{
			Rect bounds = WorldBounds;
			return new DrawingRectData(bounds.xMin, bounds.yMin, bounds.xMax, bounds.yMax);
		}
	}

	private void OnEnable()
	{
		TrySubscribeToMatch();
	}

	private void Start()
	{
		TrySubscribeToMatch();
	}

	private void OnDisable()
	{
		UnsubscribeFromMatch();
	}

	public void Configure(Vector2 newSize)
	{
		size = new Vector2(Mathf.Max(0f, newSize.x), Mathf.Max(0f, newSize.y));
	}

	public DrawingPlayerSlot Register(object owner)
	{
		if (owner == null)
		{
			throw new System.ArgumentNullException(nameof(owner));
		}

		if (registrations.TryGetValue(owner, out DrawingPlayerSlot existing))
		{
			return existing;
		}

		bool playerOneUsed = registrations.ContainsValue(DrawingPlayerSlot.PlayerOne);
		DrawingPlayerSlot slot = playerOneUsed ? DrawingPlayerSlot.PlayerTwo : DrawingPlayerSlot.PlayerOne;
		registrations[owner] = slot;
		return slot;
	}

	public void Unregister(object owner)
	{
		if (owner != null)
		{
			registrations.Remove(owner);
		}
	}

	public DrawingArtifactData GetArtifact(DrawingPlayerSlot slot) => artifacts.Get(slot);

	public Color GetPlayerColor(DrawingPlayerSlot slot)
	{
		return slot == DrawingPlayerSlot.PlayerOne ? playerOneColor : playerTwoColor;
	}

	// 確定済みの原本データから、発射用オブジェクトへ同じ形状とCollider構成を複製する。
	public bool TryConfigureProjectile(DrawingPlayerSlot slot, CannonProjectile projectile)
	{
		DrawingArtifactData artifact = artifacts.Get(slot);
		if (projectile == null || !artifact.IsConfirmed)
		{
			return false;
		}

		return projectile.ConfigureDrawingArtifact(
			artifact,
			GetPlayerColor(slot),
			GetLineMaterial(),
			lineWidth,
			duplicatePointDistance,
			colliderSimplificationTolerance,
			minimumColliderSegmentLength,
			maximumColliderSegmentsPerStroke);
	}

	public Vector3 Clamp(Vector3 worldPosition)
	{
		DrawingPointData point = DataBounds.Clamp(new DrawingPointData(worldPosition.x, worldPosition.y));
		return new Vector3(point.X, point.Y, worldPosition.z);
	}

	// 既存の LineRenderer を再利用し、増えた点だけを反映する。
	public void RefreshVisual(DrawingPlayerSlot slot)
	{
		DrawingArtifactData artifact = artifacts.Get(slot);
		if (artifact.Strokes.Count == 0)
		{
			DestroyVisualState(slot);
			return;
		}

		PlayerVisualState state = GetOrCreateVisualState(slot);
		EnsureStrokeSlots(state, artifact.Strokes.Count);

		for (int strokeIndex = 0; strokeIndex < artifact.Strokes.Count; strokeIndex++)
		{
			DrawingStrokeData stroke = artifact.Strokes[strokeIndex];
			if (stroke.PointCount < 2)
			{
				if (state.Strokes[strokeIndex] != null)
				{
					state.Strokes[strokeIndex].enabled = false;
				}
				continue;
			}

			LineRenderer line = state.Strokes[strokeIndex];
			if (line == null)
			{
				line = CreateLineRenderer(state.Root.transform, strokeIndex);
				state.Strokes[strokeIndex] = line;
			}

			line.enabled = true;
			ApplyLineStyle(line, slot, artifact.IsConfirmed);
			AppendNewPositions(line, stroke);
		}

		RemoveUnusedStrokeVisuals(state, artifact.Strokes.Count);
		if (artifact.IsConfirmed && !state.CollidersBuilt)
		{
			BuildColliders(state, artifact);
		}
	}

	// ラウンド終了時や手動クリア時に、データと表示をまとめて破棄する。
	public void ClearAllDrawings()
	{
		artifacts.ClearAll();
		DestroyAllVisualStates();
	}

	private void OnDestroy()
	{
		DestroyAllVisualStates();

		if (lineMaterial != null)
		{
			DestroyRuntimeObject(lineMaterial);
			lineMaterial = null;
		}
	}

	private void TrySubscribeToMatch()
	{
		GameManager manager = GameManager.Instance;
		if (manager == null || subscribedManager == manager)
		{
			return;
		}

		UnsubscribeFromMatch();
		subscribedManager = manager;
		subscribedManager.OnMatchPhaseChanged += HandleMatchPhaseChanged;
	}

	private void UnsubscribeFromMatch()
	{
		if (subscribedManager == null)
		{
			return;
		}

		subscribedManager.OnMatchPhaseChanged -= HandleMatchPhaseChanged;
		subscribedManager = null;
	}

	private void HandleMatchPhaseChanged(MatchPhase phase)
	{
		if (phase == MatchPhase.Result)
		{
			ClearAllDrawings();
			return;
		}

		SetSourceVisualsVisible(phase == MatchPhase.Draw);
	}

	// Place以降は発射前の原本を盤面から隠し、データだけを再利用可能な状態で保持する。
	private void SetSourceVisualsVisible(bool visible)
	{
		foreach (PlayerVisualState state in visualStates.Values)
		{
			if (state.Root != null)
			{
				state.Root.SetActive(visible);
			}
		}
	}

	private PlayerVisualState GetOrCreateVisualState(DrawingPlayerSlot slot)
	{
		if (visualStates.TryGetValue(slot, out PlayerVisualState state) && state.Root != null)
		{
			return state;
		}

		state = new PlayerVisualState
		{
			Root = new GameObject($"{slot}Drawing")
		};
		state.Root.transform.SetParent(transform, false);
		state.Root.layer = GetGroundLayer();
		state.Root.AddComponent<RuntimeRoundObject>();
		state.Root.AddComponent<DrawingColliderDebugView>().Configure(showColliderDebug, colliderDebugColor);
		visualStates[slot] = state;
		return state;
	}

	private void BuildColliders(PlayerVisualState state, DrawingArtifactData artifact)
	{
		if (state.ColliderRoot != null)
		{
			DestroyRuntimeObject(state.ColliderRoot);
		}

		state.ColliderRoot = new GameObject("Colliders");
		state.ColliderRoot.transform.SetParent(state.Root.transform, false);
		state.ColliderRoot.layer = GetGroundLayer();
		int colliderIndex = 0;

		foreach (DrawingStrokeData stroke in artifact.Strokes)
		{
			List<DrawingPointData> points = DrawingPathSimplifier.Simplify(
				stroke.Points,
				duplicatePointDistance,
				colliderSimplificationTolerance,
				minimumColliderSegmentLength,
				maximumColliderSegmentsPerStroke);

			for (int pointIndex = 1; pointIndex < points.Count; pointIndex++)
			{
				CreateSegmentCollider(state.ColliderRoot.transform, points[pointIndex - 1], points[pointIndex], colliderIndex++);
			}
		}

		state.CollidersBuilt = true;
	}

	private void CreateSegmentCollider(
		Transform parent,
		DrawingPointData start,
		DrawingPointData end,
		int colliderIndex)
	{
		Vector2 startPosition = new Vector2(start.X, start.Y);
		Vector2 endPosition = new Vector2(end.X, end.Y);
		Vector2 delta = endPosition - startPosition;
		float length = delta.magnitude;
		if (length < minimumColliderSegmentLength || length <= 0f)
		{
			return;
		}

		GameObject segment = new GameObject($"Segment_{colliderIndex}");
		segment.layer = GetGroundLayer();
		segment.transform.SetParent(parent, false);
		segment.transform.position = (startPosition + endPosition) * 0.5f;
		segment.transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg);

		CapsuleCollider2D capsule = segment.AddComponent<CapsuleCollider2D>();
		capsule.direction = CapsuleDirection2D.Horizontal;
		capsule.size = new Vector2(length + lineWidth, lineWidth);
		capsule.isTrigger = false;
	}

	private static int GetGroundLayer()
	{
		int layer = LayerMask.NameToLayer(GroundLayerName);
		return layer >= 0 ? layer : 0;
	}

	private static void EnsureStrokeSlots(PlayerVisualState state, int count)
	{
		while (state.Strokes.Count < count)
		{
			state.Strokes.Add(null);
		}
	}

	private LineRenderer CreateLineRenderer(Transform parent, int strokeIndex)
	{
		GameObject lineObject = new GameObject($"Stroke_{strokeIndex}");
		lineObject.transform.SetParent(parent, false);
		LineRenderer line = lineObject.AddComponent<LineRenderer>();
		line.useWorldSpace = true;
		line.loop = false;
		line.numCapVertices = 2;
		line.numCornerVertices = 2;
		line.sortingOrder = 2;
		line.sharedMaterial = GetLineMaterial();
		line.positionCount = 0;
		return line;
	}

	private void ApplyLineStyle(LineRenderer line, DrawingPlayerSlot slot, bool confirmed)
	{
		Color color = GetPlayerColor(slot);
		if (!confirmed)
		{
			color.a *= drawingAlpha;
		}

		line.widthMultiplier = lineWidth * (confirmed ? 1f : drawingWidthScale);
		line.startColor = color;
		line.endColor = color;
	}

	private void AppendNewPositions(LineRenderer line, DrawingStrokeData stroke)
	{
		int previousCount = line.positionCount;
		if (previousCount > stroke.PointCount)
		{
			previousCount = 0;
			line.positionCount = 0;
		}

		line.positionCount = stroke.PointCount;
		for (int pointIndex = previousCount; pointIndex < stroke.PointCount; pointIndex++)
		{
			DrawingPointData point = stroke.Points[pointIndex];
			line.SetPosition(pointIndex, new Vector3(point.X, point.Y, transform.position.z));
		}
	}

	private static void RemoveUnusedStrokeVisuals(PlayerVisualState state, int activeStrokeCount)
	{
		for (int index = state.Strokes.Count - 1; index >= activeStrokeCount; index--)
		{
			LineRenderer line = state.Strokes[index];
			if (line != null)
			{
				DestroyRuntimeObject(line.gameObject);
			}
			state.Strokes.RemoveAt(index);
		}
	}

	private Material GetLineMaterial()
	{
		if (lineMaterial == null)
		{
			lineMaterial = new Material(Shader.Find("Sprites/Default"))
			{
				name = "RuntimeDrawingLineMaterial"
			};
		}

		return lineMaterial;
	}

	private void DestroyVisualState(DrawingPlayerSlot slot)
	{
		if (!visualStates.TryGetValue(slot, out PlayerVisualState state))
		{
			return;
		}

		visualStates.Remove(slot);
		if (state.Root != null)
		{
			state.Root.SetActive(false);
			DestroyRuntimeObject(state.Root);
		}
	}

	private void DestroyAllVisualStates()
	{
		foreach (PlayerVisualState state in visualStates.Values)
		{
			if (state.Root != null)
			{
				state.Root.SetActive(false);
				DestroyRuntimeObject(state.Root);
			}
		}
		visualStates.Clear();
	}

	private static void DestroyRuntimeObject(Object target)
	{
		if (target == null)
		{
			return;
		}

		if (Application.isPlaying)
		{
			Destroy(target);
		}
		else
		{
			DestroyImmediate(target);
		}
	}
}
