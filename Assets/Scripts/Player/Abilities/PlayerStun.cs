using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(PlayerMotor2D), typeof(PlayerDash))]
// スタンを独立した状態として管理し、見た目も同期する。
public sealed class PlayerStun : MonoBehaviour
{
	// スタン中の点滅色を設定する。
	[SerializeField]
	private Color stunTint = new Color(1f, 0.3f, 0.3f, 1f);

	// 点滅速度を設定する。
	[SerializeField]
	private float flashSpeed = 10f;

	// 移動コンポーネントを保持する。
	private PlayerMotor2D motor;
	// ダッシュ状態を強制終了するために保持する。
	private PlayerDash dash;
	// 見た目変更対象の SpriteRenderer 群を保持する。
	private SpriteRenderer[] spriteRenderers;
	// 通常時の色を保持する。
	private Color[] defaultColors;

	// スタン中かを返す。
	public bool IsStunned => remainingDuration > 0f;
	// 残りスタン時間を返す。
	public float RemainingDuration => remainingDuration;

	// 残りスタン時間を保持する。
	private float remainingDuration;

	// 必要な参照を初期化する。
	private void Awake()
	{
		EnsureReferences();
	}

	// 毎フレーム、残り時間と見た目を同期する。
	private void Update()
	{
		Tick(Time.deltaTime);
		RefreshVisuals();
	}

	// 無効化時に見た目を元へ戻す。
	private void OnDisable()
	{
		ClearVisuals();
	}

	// 指定時間スタンさせる。既にスタン中なら残り時間と比較して長い方を採用する。
	public void Apply(float duration)
	{
		if (duration <= 0f)
		{
			return;
		}

		remainingDuration = Mathf.Max(remainingDuration, duration);
		EnsureReferences();
		motor.Stop();
		dash.CancelActiveDash();
		RefreshVisuals();
	}

	// ラウンド終了や明示解除時にスタンを解く。
	public void Clear()
	{
		remainingDuration = 0f;
		ClearVisuals();
	}

	// テストや Update からスタン時間を進める。
	public void Tick(float deltaTime)
	{
		if (remainingDuration <= 0f)
		{
			return;
		}

		remainingDuration = Mathf.Max(0f, remainingDuration - deltaTime);
		if (remainingDuration <= 0f)
		{
			ClearVisuals();
		}
	}

	// スタン中の見た目を点滅色へ寄せる。
	private void RefreshVisuals()
	{
		EnsureReferences();
		if (!IsStunned || spriteRenderers == null)
		{
			return;
		}

		float blend = 0.35f + (Mathf.Sin(Time.time * flashSpeed) * 0.5f + 0.5f) * 0.65f;
		for (int i = 0; i < spriteRenderers.Length; i++)
		{
			SpriteRenderer spriteRenderer = spriteRenderers[i];
			if (spriteRenderer != null)
			{
				spriteRenderer.color = Color.Lerp(defaultColors[i], stunTint, blend);
			}
		}
	}

	// 通常時の見た目へ戻す。
	private void ClearVisuals()
	{
		EnsureReferences();
		if (spriteRenderers == null)
		{
			return;
		}

		for (int i = 0; i < spriteRenderers.Length; i++)
		{
			SpriteRenderer spriteRenderer = spriteRenderers[i];
			if (spriteRenderer != null)
			{
				spriteRenderer.color = defaultColors[i];
			}
		}
	}

	private void EnsureReferences()
	{
		if (motor == null)
		{
			motor = GetComponent<PlayerMotor2D>();
		}

		if (dash == null)
		{
			dash = GetComponent<PlayerDash>();
		}

		if (spriteRenderers != null && defaultColors != null)
		{
			return;
		}

		spriteRenderers = GetComponentsInChildren<SpriteRenderer>(true);
		defaultColors = new Color[spriteRenderers.Length];
		for (int i = 0; i < spriteRenderers.Length; i++)
		{
			defaultColors[i] = spriteRenderers[i] != null ? spriteRenderers[i].color : Color.white;
		}
	}
}
