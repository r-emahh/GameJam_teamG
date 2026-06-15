using System;
using UnityEngine;

[DisallowMultipleComponent]
// Race 中の Blocker 妨害弾に対する再使用待ち時間を管理する。
public sealed class BlockerRaceAttackCooldown : MonoBehaviour
{
	// 妨害弾を再度発射できるまでの時間を指定する。
	[SerializeField, Min(0f)]
	private float cooldownDuration = 2f;

	// 現在の残り時間を保持する。
	private float remainingTime;
	// フェーズとラウンド変更を購読している GameManager を保持する。
	private GameManager subscribedManager;

	// 妨害弾が現在使用可能かを返す。
	public bool IsReady => remainingTime <= 0f;
	// クールダウンの残り時間を返す。
	public float RemainingTime => remainingTime;
	// Inspector で設定されたクールダウン時間を返す。
	public float Duration => Mathf.Max(0f, cooldownDuration);

	// HUD などへ状態変更を通知する。
	public event Action<float, bool> CooldownChanged;

	// 有効化時に試合イベントへ接続する。
	private void OnEnable()
	{
		TrySubscribe();
	}

	// 起動順が前後しても GameManager へ接続し、Race 中だけ時間を進める。
	private void Update()
	{
		TrySubscribe();
		if (remainingTime <= 0f
			|| GameManager.Instance == null
			|| GameManager.currentState != GameState.Game
			|| GameManager.Instance.CurrentPhase != MatchPhase.Race)
		{
			return;
		}

		SetRemainingTime(remainingTime - Time.unscaledDeltaTime);
	}

	// 無効化時にイベント購読を解除する。
	private void OnDisable()
	{
		Unsubscribe();
	}

	// 使用可能ならクールダウンを開始する。
	public bool TryBeginCooldown()
	{
		if (!IsReady)
		{
			return false;
		}

		SetRemainingTime(Duration);
		return true;
	}

	// Race 開始時やラウンド交替時に即時使用可能へ戻す。
	public void ResetCooldown()
	{
		SetRemainingTime(0f);
	}

	// 現在の GameManager が変わった場合に購読先を更新する。
	private void TrySubscribe()
	{
		GameManager manager = GameManager.Instance;
		if (manager == null || subscribedManager == manager)
		{
			return;
		}

		Unsubscribe();
		subscribedManager = manager;
		subscribedManager.OnMatchPhaseChanged += HandlePhaseChanged;
		subscribedManager.OnRoundAdvanced += HandleRoundAdvanced;
	}

	// 現在のイベント購読を解除する。
	private void Unsubscribe()
	{
		if (subscribedManager == null)
		{
			return;
		}

		subscribedManager.OnMatchPhaseChanged -= HandlePhaseChanged;
		subscribedManager.OnRoundAdvanced -= HandleRoundAdvanced;
		subscribedManager = null;
	}

	// Race に入った時点で前フェーズの状態を持ち越さない。
	private void HandlePhaseChanged(MatchPhase phase)
	{
		if (phase == MatchPhase.Race)
		{
			ResetCooldown();
		}
	}

	// ラウンド交替処理の開始時にも状態を初期化する。
	private void HandleRoundAdvanced(int _)
	{
		ResetCooldown();
	}

	// 残り時間を範囲内へ収め、状態を通知する。
	private void SetRemainingTime(float value)
	{
		float nextValue = Mathf.Clamp(value, 0f, Duration);
		if (Mathf.Approximately(remainingTime, nextValue))
		{
			return;
		}

		remainingTime = nextValue;
		CooldownChanged?.Invoke(remainingTime, IsReady);
	}
}
