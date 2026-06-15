using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(BlockerRaceAttackCooldown))]
// 大砲の選択と発射を担当する。
public sealed class PlayerCannon : MonoBehaviour
{
	// 発射に使う弾のプレハブを指定する。
	[SerializeField]
	private CannonProjectile projectilePrefab;

	// Goal Runner 用の選択中カラーを指定する。
	[SerializeField]
	private Color goalRunnerHighlightColor = new Color(0.2f, 0.85f, 1f, 0.65f);

	// Blocker 用の選択中カラーを指定する。
	[SerializeField]
	private Color blockerHighlightColor = new Color(1f, 0.35f, 0.35f, 0.65f);

	// シーン内の大砲マウント候補を保持する。
	[SerializeField]
	private List<CannonMount> cannonMounts = new();

	// スティックまたはボタンを押し続けたときの角度変更速度。
	[SerializeField]
	private float aimSpeed = 60f;

	// スティックの微小入力を無視する閾値。
	[SerializeField]
	[Range(0f, 1f)]
	private float aimDeadZone = 0.2f;

	// Place 中の発射パワー最小値を設定する。
	[SerializeField, Min(0f)]
	private float minimumLaunchPower = 6f;

	// Place 中の発射パワー最大値を設定する。
	[SerializeField, Min(0f)]
	private float maximumLaunchPower = 16f;

	// 発射パワーが1秒あたりに増減する量を設定する。
	[SerializeField, Min(0f)]
	private float launchPowerSweepSpeed = 10f;

	// プレイヤーの陣営を取得する。
	private PlayerIdentity playerIdentity;
	// この発射者がDrawフェーズで確定した描画を保持する。
	private PlayerDrawing playerDrawing;
	// 選択中を示すマーカーを保持する。
	private GameObject selectionMarker;
	// マーカーの描画コンポーネントを保持する。
	private SpriteRenderer selectionMarkerRenderer;
	// 現在選択中の大砲インデックスを保持する。
	private int selectedIndex;
	// 発射中の弾を保持する。
	private CannonProjectile activeProjectile;
	// Race 中の Blocker 妨害弾クールダウンを管理する。
	private BlockerRaceAttackCooldown blockerRaceAttackCooldown;
	// 現在の角度調整入力を保持する。
	private float aimInput;
	// 発射パワー往復開始からの経過量を保持する。
	private float launchPowerTravel;
	// 現在の発射パワーを保持する。
	private float currentLaunchPower;
	// 前フレームまで発射準備中だったかを保持する。
	private bool wasPreparingLaunch;

	// 現在選択している大砲の順番を返す。
	public int SelectedMountOrder => GetSelectedMount()?.Order ?? -1;
	// 現在選択している大砲の相対角度を返す。
	public float SelectedMountAngle => GetSelectedMount()?.CurrentAngle ?? 0f;
	// 現在の発射パワーを返す。
	public float CurrentLaunchPower => currentLaunchPower;
	// HUD 表示用に正規化した発射パワーを返す。
	public float NormalizedLaunchPower => Mathf.InverseLerp(minimumLaunchPower, maximumLaunchPower, currentLaunchPower);
	// 現在このプレイヤーが発射準備中かを返す。
	public bool IsPreparingLaunch => CanControlCannon() && activeProjectile == null;

	// 同期用のプレイヤー情報とマーカーを初期化する。
	private void Awake()
	{
		playerIdentity = GetComponent<PlayerIdentity>();
		playerDrawing = GetComponent<PlayerDrawing>();
		blockerRaceAttackCooldown = GetComponent<BlockerRaceAttackCooldown>();
		NormalizeLaunchPowerSettings();
		ResetLaunchPower();
		EnsureSelectionMarker();
	}

	// Inspector 変更時に発射パワー設定を正規化する。
	private void OnValidate()
	{
		NormalizeLaunchPowerSettings();
		currentLaunchPower = Mathf.Clamp(currentLaunchPower, minimumLaunchPower, maximumLaunchPower);
	}

	// Place 中、現在手番のプレイヤーだけ選択中の大砲角度を変更する。
	private void Update()
	{
		bool isPreparingLaunch = IsPreparingLaunch;
		if (isPreparingLaunch && !wasPreparingLaunch)
		{
			ResetLaunchPower();
		}

		wasPreparingLaunch = isPreparingLaunch;
		if (isPreparingLaunch)
		{
			TickLaunchPower(Time.deltaTime);
		}

		if (!CanControlCannon() || Mathf.Abs(aimInput) < aimDeadZone)
		{
			return;
		}

		CannonMount selectedMount = GetSelectedMount();
		selectedMount?.AdjustAngle(aimInput * aimSpeed * Time.deltaTime);
	}

	// 選択中表示を毎フレーム同期する。
	private void LateUpdate()
	{
		SyncSelectionVisual();
	}

	// 破棄時にランタイム生成したマーカーを掃除する。
	private void OnDestroy()
	{
		if (selectionMarker != null)
		{
			Destroy(selectionMarker);
		}
	}

	// 前の大砲へ切り替える。
	public void SelectPrevious()
	{
		if (!CanSelectCannon())
		{
			return;
		}

		RefreshMounts();
		selectedIndex = WrapIndex(selectedIndex - 1, cannonMounts.Count);
		SyncSelectionVisual();
	}

	// 次の大砲へ切り替える。
	public void SelectNext()
	{
		if (!CanSelectCannon())
		{
			return;
		}

		RefreshMounts();
		selectedIndex = WrapIndex(selectedIndex + 1, cannonMounts.Count);
		SyncSelectionVisual();
	}

	// 角度調整用の連続入力を受け取る。
	public void SetAimInput(float value)
	{
		aimInput = Mathf.Clamp(value, -1f, 1f);
	}

	// 現在の試合状態に応じて攻撃を試みる。
	public void TryAttack(MatchSide controlledSide)
	{
		if (GameManager.Instance == null || GameManager.currentState != GameState.Game)
		{
			return;
		}

		if (GameManager.Instance.CurrentPhase == MatchPhase.Place)
		{
			if (CanControlCannon() && playerIdentity.ControlledSide == controlledSide)
			{
				TryFire(controlledSide, false);
			}

			return;
		}

		if (GameManager.Instance.CurrentPhase == MatchPhase.Race && controlledSide == MatchSide.Blocker)
		{
			TryFire(controlledSide, true);
		}
	}

	// 発射中の弾を停止させる。
	public void StopActiveProjectile()
	{
		if (GameManager.Instance != null
			&& GameManager.Instance.CurrentPhase == MatchPhase.Place
			&& !CanControlCannon())
		{
			return;
		}

		activeProjectile?.StopProjectile();
	}

	// 次ラウンドに向けて選択状態と発射中オブジェクト参照を初期化する。
	public void ResetForNextRound()
	{
		StopActiveProjectile();
		aimInput = 0f;
		selectedIndex = 0;
		wasPreparingLaunch = false;
		ResetLaunchPower();
	}

	// 弾が破棄されたら参照を外す。
	public void ClearProjectileReference(CannonProjectile projectile)
	{
		if (activeProjectile == projectile)
		{
			activeProjectile = null;
		}
	}

	// 大砲を選択できる局面かを判定する。
	private bool CanSelectCannon()
	{
		return CanControlCannon();
	}

	// Place 中かつ現在手番のプレイヤーかを判定する。
	private bool CanControlCannon()
	{
		return GameManager.Instance != null
			&& GameManager.currentState == GameState.Game
			&& GameManager.Instance.CurrentPhase == MatchPhase.Place
			&& playerIdentity != null
			&& GameManager.Instance.CurrentSide == playerIdentity.ControlledSide;
	}

	// 現在選択中の大砲から弾を生成する。
	private void TryFire(MatchSide side, bool isStunProjectile)
	{
		if (activeProjectile != null)
		{
			return;
		}

		RefreshMounts();
		if (cannonMounts.Count == 0)
		{
			SyncSelectionVisual();
			return;
		}

		if (isStunProjectile
			&& (blockerRaceAttackCooldown == null || !blockerRaceAttackCooldown.TryBeginCooldown()))
		{
			return;
		}

		selectedIndex = WrapIndex(selectedIndex, cannonMounts.Count);
		CannonMount mount = cannonMounts[selectedIndex];
		float adoptedLaunchPower = currentLaunchPower;
		CannonProjectile projectile = projectilePrefab != null
			? Instantiate(projectilePrefab, mount.transform.position, mount.transform.rotation)
			: CannonProjectile.CreateRuntime(mount.transform.position, mount.transform.rotation, isStunProjectile);

		if (!isStunProjectile && (playerDrawing == null || !playerDrawing.TryConfigureProjectile(projectile)))
		{
			Destroy(projectile.gameObject);
			return;
		}

		if (GameManager.Instance.CurrentPhase == MatchPhase.Place && !GameManager.Instance.TryConsumeLaunch(side))
		{
			Destroy(projectile.gameObject);
			return;
		}

		activeProjectile = projectile;
		if (activeProjectile.GetComponent<RuntimeRoundObject>() == null)
		{
			activeProjectile.gameObject.AddComponent<RuntimeRoundObject>();
		}

		activeProjectile.Initialize(this, isStunProjectile, adoptedLaunchPower);
		ResetLaunchPower();
		SyncSelectionVisual();
	}

	// 発射パワーを最小値と最大値の間で往復させる。
	private void TickLaunchPower(float deltaTime)
	{
		NormalizeLaunchPowerSettings();
		float range = maximumLaunchPower - minimumLaunchPower;
		if (range <= 0f || launchPowerSweepSpeed <= 0f)
		{
			currentLaunchPower = minimumLaunchPower;
			return;
		}

		launchPowerTravel += Mathf.Max(0f, deltaTime) * launchPowerSweepSpeed;
		currentLaunchPower = minimumLaunchPower + Mathf.PingPong(launchPowerTravel, range);
	}

	// 次の発射準備を最小パワーから開始する。
	private void ResetLaunchPower()
	{
		NormalizeLaunchPowerSettings();
		launchPowerTravel = 0f;
		currentLaunchPower = minimumLaunchPower;
	}

	// Inspector 設定を有効な範囲へ揃える。
	private void NormalizeLaunchPowerSettings()
	{
		minimumLaunchPower = Mathf.Max(0f, minimumLaunchPower);
		maximumLaunchPower = Mathf.Max(0f, maximumLaunchPower);
		if (minimumLaunchPower > maximumLaunchPower)
		{
			(minimumLaunchPower, maximumLaunchPower) = (maximumLaunchPower, minimumLaunchPower);
		}

		launchPowerSweepSpeed = Mathf.Max(0f, launchPowerSweepSpeed);
	}

	// シーン内の大砲を再収集し、順序を整える。
	private void RefreshMounts()
	{
		cannonMounts.RemoveAll(mount => mount == null);
		if (cannonMounts.Count > 0)
		{
			return;
		}

		cannonMounts.AddRange(FindObjectsByType<CannonMount>(FindObjectsInactive.Exclude, FindObjectsSortMode.None));
		cannonMounts.Sort((left, right) => left.Order.CompareTo(right.Order));
	}

	// 選択中の大砲マウントを返す。
	private CannonMount GetSelectedMount()
	{
		RefreshMounts();
		if (cannonMounts.Count == 0)
		{
			return null;
		}

		selectedIndex = WrapIndex(selectedIndex, cannonMounts.Count);
		return cannonMounts[selectedIndex];
	}

	// 選択中表示を必要に応じて更新する。
	private void SyncSelectionVisual()
	{
		EnsureSelectionMarker();

		if (selectionMarker == null)
		{
			return;
		}

		if (!CanControlCannon())
		{
			selectionMarker.SetActive(false);
			return;
		}

		RefreshMounts();
		CannonMount selectedMount = GetSelectedMount();
		if (selectedMount == null)
		{
			selectionMarker.SetActive(false);
			return;
		}

		selectionMarker.transform.SetPositionAndRotation(selectedMount.transform.position, selectedMount.transform.rotation);
		selectionMarker.transform.localScale = selectedMount.transform.localScale * 1.35f;
		selectionMarkerRenderer.color = playerIdentity != null && playerIdentity.ControlledSide == MatchSide.Blocker
			? blockerHighlightColor
			: goalRunnerHighlightColor;
		selectionMarker.SetActive(true);
	}

	// 選択中マーカーを必要なら生成する。
	private void EnsureSelectionMarker()
	{
		if (selectionMarker != null)
		{
			return;
		}

		selectionMarker = new GameObject($"{name}_SelectionMarker");
		selectionMarker.AddComponent<RuntimeRoundObject>();
		selectionMarker.transform.SetParent(null, true);
		selectionMarkerRenderer = selectionMarker.AddComponent<SpriteRenderer>();
		selectionMarkerRenderer.sprite = RuntimeSpriteFactory.UnitSquare;
		selectionMarkerRenderer.sortingOrder = 2;
		selectionMarker.transform.localScale = Vector3.one * 0.75f;
		selectionMarker.SetActive(false);
	}

	// インデックスを範囲内へ折り返す。
	private static int WrapIndex(int index, int count)
	{
		return count <= 0 ? 0 : (index % count + count) % count;
	}
}
