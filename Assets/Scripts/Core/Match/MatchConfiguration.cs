using System;
using UnityEngine;

[Serializable]
// 試合時間や弾数など、ルール値をまとめる設定オブジェクト。
public sealed class MatchConfiguration
{
	// Draw フェーズの長さを定義する。
	[SerializeField, Min(0f)]
	private float drawPhaseDuration = 20f;

	// Place フェーズの長さを定義する。
	[SerializeField, Min(0f)]
	private float placePhaseDuration = 20f;

	// Race フェーズの長さを定義する。
	[SerializeField, Min(0f)]
	private float racePhaseDuration = 60f;

	// Result フェーズの長さを定義する。
	[SerializeField, Min(0f)]
	private float resultPhaseDuration = 6f;

	// Goal Runner 側が使える発射回数を定義する。
	[SerializeField, Min(0)]
	private int goalRunnerLaunchCount = 3;

	// Blocker 側が使える発射回数を定義する。
	[SerializeField, Min(0)]
	private int blockerLaunchCount = 2;

	// Goal Runner に配る図形数の最小値を定義する。
	[SerializeField, Min(0)]
	private int minDrawingStampCount = 3;

	// Goal Runner に配る図形数の最大値を定義する。
	[SerializeField, Min(0)]
	private int maxDrawingStampCount = 6;

	// Blocker に配る図形数の最小値を定義する。
	[SerializeField, Min(0)]
	private int minBlockerStampCount = 2;

	// Blocker に配る図形数の最大値を定義する。
	[SerializeField, Min(0)]
	private int maxBlockerStampCount = 5;

	// Goal Runner の初期発射回数を返す。
	public int GoalRunnerLaunchCount => goalRunnerLaunchCount;
	// Blocker の初期発射回数を返す。
	public int BlockerLaunchCount => blockerLaunchCount;

	// Goal Runner に配る図形数の下限を返す。
	public int MinDrawingStampCount => minDrawingStampCount;
	// Goal Runner に配る図形数の上限を返す。
	public int MaxDrawingStampCount => maxDrawingStampCount;
	// Blocker に配る図形数の下限を返す。
	public int MinBlockerStampCount => minBlockerStampCount;
	// Blocker に配る図形数の上限を返す。
	public int MaxBlockerStampCount => maxBlockerStampCount;

	// 指定フェーズに対応する継続時間を返す。
	public float GetDuration(MatchPhase phase)
	{
		return phase switch
		{
			MatchPhase.Draw => drawPhaseDuration,
			MatchPhase.Place => placePhaseDuration,
			MatchPhase.Race => racePhaseDuration,
			MatchPhase.Result => resultPhaseDuration,
			_ => 0f
		};
	}
}
