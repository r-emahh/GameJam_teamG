using System;
using System.Collections.Generic;

// 描画点列を物理形状向けに間引く。
public static class DrawingPathSimplifier
{
	public static List<DrawingPointData> Simplify(
		IReadOnlyList<DrawingPointData> source,
		float duplicateDistance,
		float tolerance,
		float minimumSegmentLength,
		int maximumSegmentCount)
	{
		List<DrawingPointData> filtered = RemoveInvalidAndDuplicatePoints(source, Math.Max(0f, duplicateDistance));
		if (filtered.Count < 2 || GetPathLength(filtered) < Math.Max(0f, minimumSegmentLength))
		{
			return new List<DrawingPointData>();
		}

		int segmentLimit = Math.Max(1, maximumSegmentCount);
		float currentTolerance = Math.Max(0f, tolerance);
		List<DrawingPointData> simplified = SimplifyRamerDouglasPeucker(filtered, currentTolerance);
		for (int attempt = 0; simplified.Count - 1 > segmentLimit && attempt < 12; attempt++)
		{
			currentTolerance = currentTolerance > 0f ? currentTolerance * 2f : 0.001f;
			simplified = SimplifyRamerDouglasPeucker(filtered, currentTolerance);
		}

		if (simplified.Count - 1 > segmentLimit)
		{
			simplified = ResampleByIndex(simplified, segmentLimit + 1);
		}

		return RemoveShortSegments(simplified, Math.Max(0f, minimumSegmentLength));
	}

	private static List<DrawingPointData> RemoveInvalidAndDuplicatePoints(
		IReadOnlyList<DrawingPointData> source,
		float duplicateDistance)
	{
		List<DrawingPointData> result = new List<DrawingPointData>();
		if (source == null)
		{
			return result;
		}

		for (int index = 0; index < source.Count; index++)
		{
			DrawingPointData point = source[index];
			if (!IsFinite(point))
			{
				continue;
			}

			if (result.Count == 0 || result[result.Count - 1].DistanceTo(point) > duplicateDistance)
			{
				result.Add(point);
			}
		}

		return result;
	}

	private static List<DrawingPointData> SimplifyRamerDouglasPeucker(
		IReadOnlyList<DrawingPointData> points,
		float tolerance)
	{
		if (points.Count <= 2)
		{
			return new List<DrawingPointData>(points);
		}

		bool[] keep = new bool[points.Count];
		keep[0] = true;
		keep[points.Count - 1] = true;
		MarkRequiredPoints(points, 0, points.Count - 1, tolerance * tolerance, keep);

		List<DrawingPointData> result = new List<DrawingPointData>();
		for (int index = 0; index < points.Count; index++)
		{
			if (keep[index])
			{
				result.Add(points[index]);
			}
		}

		return result;
	}

	private static void MarkRequiredPoints(
		IReadOnlyList<DrawingPointData> points,
		int startIndex,
		int endIndex,
		float toleranceSquared,
		bool[] keep)
	{
		if (endIndex <= startIndex + 1)
		{
			return;
		}

		float farthestDistanceSquared = -1f;
		int farthestIndex = -1;
		for (int index = startIndex + 1; index < endIndex; index++)
		{
			float distanceSquared = DistanceToSegmentSquared(points[index], points[startIndex], points[endIndex]);
			if (distanceSquared > farthestDistanceSquared)
			{
				farthestDistanceSquared = distanceSquared;
				farthestIndex = index;
			}
		}

		if (farthestDistanceSquared <= toleranceSquared || farthestIndex < 0)
		{
			return;
		}

		keep[farthestIndex] = true;
		MarkRequiredPoints(points, startIndex, farthestIndex, toleranceSquared, keep);
		MarkRequiredPoints(points, farthestIndex, endIndex, toleranceSquared, keep);
	}

	private static List<DrawingPointData> RemoveShortSegments(
		IReadOnlyList<DrawingPointData> points,
		float minimumSegmentLength)
	{
		List<DrawingPointData> result = new List<DrawingPointData>();
		if (points.Count == 0)
		{
			return result;
		}

		result.Add(points[0]);
		for (int index = 1; index < points.Count; index++)
		{
			DrawingPointData point = points[index];
			if (result[result.Count - 1].DistanceTo(point) >= minimumSegmentLength)
			{
				result.Add(point);
			}
		}

		return result.Count >= 2 ? result : new List<DrawingPointData>();
	}

	private static List<DrawingPointData> ResampleByIndex(IReadOnlyList<DrawingPointData> points, int targetCount)
	{
		List<DrawingPointData> result = new List<DrawingPointData>(targetCount);
		for (int index = 0; index < targetCount; index++)
		{
			int sourceIndex = (int)Math.Round(index * (points.Count - 1d) / (targetCount - 1d));
			DrawingPointData point = points[sourceIndex];
			if (result.Count == 0 || !result[result.Count - 1].Equals(point))
			{
				result.Add(point);
			}
		}

		return result;
	}

	private static float GetPathLength(IReadOnlyList<DrawingPointData> points)
	{
		float length = 0f;
		for (int index = 1; index < points.Count; index++)
		{
			length += points[index - 1].DistanceTo(points[index]);
		}
		return length;
	}

	private static float DistanceToSegmentSquared(
		DrawingPointData point,
		DrawingPointData segmentStart,
		DrawingPointData segmentEnd)
	{
		float segmentX = segmentEnd.X - segmentStart.X;
		float segmentY = segmentEnd.Y - segmentStart.Y;
		float segmentLengthSquared = segmentX * segmentX + segmentY * segmentY;
		if (segmentLengthSquared <= float.Epsilon)
		{
			float pointX = point.X - segmentStart.X;
			float pointY = point.Y - segmentStart.Y;
			return pointX * pointX + pointY * pointY;
		}

		float amount = ((point.X - segmentStart.X) * segmentX + (point.Y - segmentStart.Y) * segmentY)
			/ segmentLengthSquared;
		amount = Math.Max(0f, Math.Min(1f, amount));
		float nearestX = segmentStart.X + segmentX * amount;
		float nearestY = segmentStart.Y + segmentY * amount;
		float deltaX = point.X - nearestX;
		float deltaY = point.Y - nearestY;
		return deltaX * deltaX + deltaY * deltaY;
	}

	private static bool IsFinite(DrawingPointData point)
	{
		return !float.IsNaN(point.X) && !float.IsInfinity(point.X)
			&& !float.IsNaN(point.Y) && !float.IsInfinity(point.Y);
	}
}
