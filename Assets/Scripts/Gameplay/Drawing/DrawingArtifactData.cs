using System;
using System.Collections.Generic;

[Serializable]
// Unity 型に依存しない描画点を表す。
public readonly struct DrawingPointData : IEquatable<DrawingPointData>
{
	public DrawingPointData(float x, float y)
	{
		X = x;
		Y = y;
	}

	public float X { get; }
	public float Y { get; }

	public float DistanceTo(DrawingPointData other)
	{
		double deltaX = other.X - X;
		double deltaY = other.Y - Y;
		return (float)Math.Sqrt(deltaX * deltaX + deltaY * deltaY);
	}

	public DrawingPointData Move(float deltaX, float deltaY) => new DrawingPointData(X + deltaX, Y + deltaY);

	public static DrawingPointData Lerp(DrawingPointData from, DrawingPointData to, float amount)
	{
		return new DrawingPointData(
			from.X + (to.X - from.X) * amount,
			from.Y + (to.Y - from.Y) * amount);
	}

	public bool Equals(DrawingPointData other) => X.Equals(other.X) && Y.Equals(other.Y);
	public override bool Equals(object obj) => obj is DrawingPointData other && Equals(other);
	public override int GetHashCode() => HashCode.Combine(X, Y);
}

[Serializable]
// Unity 型に依存しない描画可能矩形を表す。
public readonly struct DrawingRectData
{
	public DrawingRectData(float xMin, float yMin, float xMax, float yMax)
	{
		XMin = Math.Min(xMin, xMax);
		YMin = Math.Min(yMin, yMax);
		XMax = Math.Max(xMin, xMax);
		YMax = Math.Max(yMin, yMax);
	}

	public float XMin { get; }
	public float YMin { get; }
	public float XMax { get; }
	public float YMax { get; }

	public DrawingPointData Clamp(DrawingPointData point)
	{
		return new DrawingPointData(
			Math.Max(XMin, Math.Min(XMax, point.X)),
			Math.Max(YMin, Math.Min(YMax, point.Y)));
	}

	public bool Contains(DrawingPointData point)
	{
		return point.X >= XMin && point.X <= XMax && point.Y >= YMin && point.Y <= YMax;
	}
}

[Serializable]
// 1つの描画データに適用する上限値をまとめる。
public readonly struct DrawingLimitsData
{
	public DrawingLimitsData(int maxPointCount, float maxTotalLineLength, float minPointSpacing)
	{
		MaxPointCount = Math.Max(0, maxPointCount);
		MaxTotalLineLength = Math.Max(0f, maxTotalLineLength);
		MinPointSpacing = Math.Max(0f, minPointSpacing);
	}

	public int MaxPointCount { get; }
	public float MaxTotalLineLength { get; }
	public float MinPointSpacing { get; }
}

[Serializable]
// 描画ボタンを1回押してから離すまでの点列を保持する。
public sealed class DrawingStrokeData
{
	private readonly List<DrawingPointData> points = new List<DrawingPointData>();

	public IReadOnlyList<DrawingPointData> Points => points;
	public int PointCount => points.Count;

	internal DrawingPointData LastPoint => points[points.Count - 1];
	internal void Add(DrawingPointData point) => points.Add(point);
}

[Serializable]
// 1人分の自由描画を複数ストロークとして保持する。
public sealed class DrawingArtifactData
{
	private readonly List<DrawingStrokeData> strokes = new List<DrawingStrokeData>();
	private int activeStrokeIndex = -1;

	public IReadOnlyList<DrawingStrokeData> Strokes => strokes;
	public int PointCount { get; private set; }
	public float TotalLineLength { get; private set; }
	public bool IsConfirmed { get; private set; }
	public bool IsStrokeActive => activeStrokeIndex >= 0;

	public bool BeginStroke(DrawingPointData point, DrawingLimitsData limits)
	{
		EndStroke();
		if (IsConfirmed || PointCount >= limits.MaxPointCount)
		{
			return false;
		}

		DrawingStrokeData stroke = new DrawingStrokeData();
		stroke.Add(point);
		strokes.Add(stroke);
		activeStrokeIndex = strokes.Count - 1;
		PointCount++;
		return true;
	}

	public bool TryAppendPoint(DrawingPointData point, DrawingLimitsData limits)
	{
		if (IsConfirmed || activeStrokeIndex < 0 || PointCount >= limits.MaxPointCount)
		{
			return false;
		}

		DrawingStrokeData stroke = strokes[activeStrokeIndex];
		DrawingPointData previous = stroke.LastPoint;
		float segmentLength = previous.DistanceTo(point);
		if (segmentLength < limits.MinPointSpacing || segmentLength <= 0f)
		{
			return false;
		}

		float remainingLength = limits.MaxTotalLineLength - TotalLineLength;
		if (remainingLength <= 0f)
		{
			return false;
		}

		if (segmentLength > remainingLength)
		{
			if (remainingLength < limits.MinPointSpacing)
			{
				return false;
			}

			point = DrawingPointData.Lerp(previous, point, remainingLength / segmentLength);
			segmentLength = remainingLength;
		}

		stroke.Add(point);
		PointCount++;
		TotalLineLength += segmentLength;
		return true;
	}

	public void EndStroke()
	{
		activeStrokeIndex = -1;
	}

	public void Clear()
	{
		strokes.Clear();
		activeStrokeIndex = -1;
		PointCount = 0;
		TotalLineLength = 0f;
		IsConfirmed = false;
	}

	public void Confirm()
	{
		EndStroke();
		IsConfirmed = true;
	}
}

// 物理プレイヤーを区別するための固定スロット。
public enum DrawingPlayerSlot
{
	PlayerOne,
	PlayerTwo
}

[Serializable]
// 2人分の描画データを互いに独立して保持する。
public sealed class DrawingArtifactSetData
{
	private readonly DrawingArtifactData playerOne = new DrawingArtifactData();
	private readonly DrawingArtifactData playerTwo = new DrawingArtifactData();

	public DrawingArtifactData Get(DrawingPlayerSlot slot)
	{
		return slot == DrawingPlayerSlot.PlayerOne ? playerOne : playerTwo;
	}

	public void ClearAll()
	{
		playerOne.Clear();
		playerTwo.Clear();
	}
}

// カーソル移動と描画点の記録を Unity から分離して扱う。
public sealed class DrawingRecorder
{
	private DrawingRectData bounds;
	private readonly DrawingLimitsData limits;

	public DrawingRecorder(
		DrawingArtifactData artifact,
		DrawingRectData bounds,
		DrawingLimitsData limits,
		DrawingPointData initialCursorPosition)
	{
		Artifact = artifact ?? throw new ArgumentNullException(nameof(artifact));
		this.bounds = bounds;
		this.limits = limits;
		CursorPosition = bounds.Clamp(initialCursorPosition);
	}

	public DrawingArtifactData Artifact { get; }
	public DrawingPointData CursorPosition { get; private set; }
	public bool IsDrawing { get; private set; }

	public void SetBounds(DrawingRectData newBounds)
	{
		bounds = newBounds;
		CursorPosition = bounds.Clamp(CursorPosition);
	}

	public bool MoveCursor(float deltaX, float deltaY)
	{
		CursorPosition = bounds.Clamp(CursorPosition.Move(deltaX, deltaY));
		return IsDrawing && Artifact.TryAppendPoint(CursorPosition, limits);
	}

	public bool SetCursorPosition(DrawingPointData position)
	{
		CursorPosition = bounds.Clamp(position);
		return IsDrawing && Artifact.TryAppendPoint(CursorPosition, limits);
	}

	public bool SetDrawingActive(bool active)
	{
		if (active == IsDrawing)
		{
			return false;
		}

		IsDrawing = active;
		if (active)
		{
			bool began = Artifact.BeginStroke(CursorPosition, limits);
			IsDrawing = began;
			return began;
		}

		Artifact.EndStroke();
		return true;
	}

	public void Clear()
	{
		IsDrawing = false;
		Artifact.Clear();
	}

	public void Confirm()
	{
		IsDrawing = false;
		Artifact.Confirm();
	}
}
