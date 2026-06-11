using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
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

	// プレイヤーの陣営を取得する。
	private PlayerIdentity playerIdentity;
	// 選択中を示すマーカーを保持する。
	private GameObject selectionMarker;
	// マーカーの描画コンポーネントを保持する。
	private SpriteRenderer selectionMarkerRenderer;
	// 現在選択中の大砲インデックスを保持する。
	private int selectedIndex;
	// 発射中の弾を保持する。
	private CannonProjectile activeProjectile;

	// 現在選択している大砲の順番を返す。
	public int SelectedMountOrder => GetSelectedMount()?.Order ?? -1;

	// 同期用のプレイヤー情報とマーカーを初期化する。
	private void Awake()
	{
		playerIdentity = GetComponent<PlayerIdentity>();
		EnsureSelectionMarker();
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

	// 現在の試合状態に応じて攻撃を試みる。
	public void TryAttack(MatchSide controlledSide)
	{
		if (GameManager.Instance == null || GameManager.currentState != GameState.Game)
		{
			return;
		}

		if (GameManager.Instance.CurrentPhase == MatchPhase.Place)
		{
			if (GameManager.Instance.CurrentSide == controlledSide)
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
		activeProjectile?.StopProjectile();
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
		return GameManager.Instance != null && GameManager.Instance.CurrentPhase == MatchPhase.Place;
	}

	// 現在選択中の大砲から弾を生成する。
	private void TryFire(MatchSide side, bool isStunProjectile)
	{
		if (GameManager.Instance.CurrentPhase == MatchPhase.Place && !GameManager.Instance.TryConsumeLaunch(side))
		{
			return;
		}

		RefreshMounts();
		if (cannonMounts.Count == 0)
		{
			SyncSelectionVisual();
			return;
		}

		selectedIndex = WrapIndex(selectedIndex, cannonMounts.Count);
		CannonMount mount = cannonMounts[selectedIndex];
		activeProjectile = projectilePrefab != null
			? Instantiate(projectilePrefab, mount.transform.position, mount.transform.rotation)
			: CannonProjectile.CreateRuntime(mount.transform.position, mount.transform.rotation, isStunProjectile);
		activeProjectile.Initialize(this, isStunProjectile);
		SyncSelectionVisual();
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

		if (GameManager.Instance == null || GameManager.currentState != GameState.Game || GameManager.Instance.CurrentPhase != MatchPhase.Place)
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
