using TMPro;
using UnityEngine;

[DisallowMultipleComponent]
// プレイヤー頭上に番号ラベルを表示する。
public sealed class PlayerNameplate : MonoBehaviour
{
	private const string LabelObjectName = "PlayerNameplate";

	// 頭上へ持ち上げる量を微調整する。
	[SerializeField]
	private float verticalOffset = 0.45f;

	// ワールド空間テキストの見た目サイズを指定する。
	[SerializeField]
	private float labelScale = 0.18f;

	// 対象プレイヤーをキャッシュする。
	private PlayerController playerController;
	// ラベルの Transform を保持する。
	private Transform labelTransform;
	// ラベル描画本体を保持する。
	private TextMeshPro textMesh;
	// 表示切り替え用の Renderer を保持する。
	private Renderer textRenderer;
	// 現在の表示可否を保持する。
	private bool isVisible = true;
	// 前回反映したプレイヤー番号を保持する。
	private int lastPlayerIndex = int.MinValue;
	// 前回反映したスタン状態を保持する。
	private bool lastStunnedState;
	// 頭上位置計算に使う半分の高さを保持する。
	private float cachedHalfHeight = 0.5f;
	// 必要な参照とラベルを初期化する。
	private void Awake()
	{
		playerController = GetComponent<PlayerController>();
	}

	// 初期化が一通り終わってからラベルを組み立てる。
	private void Start()
	{
		RefreshDisplay();
	}

	// プレイヤー番号変化と位置を追従する。
	private void LateUpdate()
	{
		EnsureLabel();

		int playerIndex = GetPlayerIndex();
		bool isStunned = playerController != null && playerController.IsStunned;
		if (playerIndex != lastPlayerIndex || isStunned != lastStunnedState)
		{
			ApplyPlayerIndex(playerIndex, isStunned);
		}

		UpdatePosition();
	}

	// 外部から見た目を再同期する。
	public void RefreshDisplay()
	{
		EnsureLabel();
		RefreshHeight();
		ApplyPlayerIndex(GetPlayerIndex(), playerController != null && playerController.IsStunned);
		UpdatePosition();
	}

	// 外部から表示可否を切り替える。
	public void SetVisible(bool visible)
	{
		isVisible = visible;
		UpdateVisibility();
	}

	// ランタイム生成のラベルを取得または作成する。
	private void EnsureLabel()
	{
		if (textMesh != null && labelTransform == null)
		{
			labelTransform = textMesh.transform;
		}

		if (labelTransform == null)
		{
			Transform existing = transform.Find(LabelObjectName);
			if (existing != null)
			{
				labelTransform = existing;
			}
			else
			{
				GameObject labelObject = new GameObject(LabelObjectName, typeof(TextMeshPro));
				labelTransform = labelObject.transform;
				labelTransform.SetParent(transform, false);
				textMesh = labelObject.GetComponent<TextMeshPro>();
			}
		}

		if (textMesh == null)
		{
			textMesh = labelTransform.GetComponent<TextMeshPro>();
			if (textMesh == null)
			{
				textMesh = labelTransform.gameObject.AddComponent<TextMeshPro>();
			}

			textMesh.alignment = TextAlignmentOptions.Center;
			textMesh.fontSize = 4f;
			textMesh.textWrappingMode = TextWrappingModes.NoWrap;
			textMesh.text = string.Empty;
		}

		if (labelTransform == null && textMesh != null)
		{
			labelTransform = textMesh.transform;
		}

		if (labelTransform != null)
		{
			labelTransform.localScale = Vector3.one * labelScale;
		}

		if (textRenderer == null)
		{
			textRenderer = textMesh.GetComponent<Renderer>();
			if (textRenderer != null)
			{
				textRenderer.sortingOrder = 20;
			}
		}
	}

	// 登録順に応じてラベル文字と色を更新する。
	private void ApplyPlayerIndex(int playerIndex, bool isStunned)
	{
		lastPlayerIndex = playerIndex;
		lastStunnedState = isStunned;
		if (textMesh == null)
		{
			return;
		}

		if (playerIndex < 0)
		{
			textMesh.text = string.Empty;
			UpdateVisibility();
			return;
		}

		string baseLabel = $"P{playerIndex + 1}";
		textMesh.text = isStunned ? $"{baseLabel} STUN" : baseLabel;
		Color baseColor = PlayerVisualPalette.GetPlayerColor(playerIndex);
		textMesh.color = isStunned ? Color.Lerp(baseColor, Color.red, 0.6f) : baseColor;
		UpdateVisibility();
	}

	// ラベル位置をプレイヤーの上へ寄せる。
	private void UpdatePosition()
	{
		if (labelTransform == null)
		{
			return;
		}

		labelTransform.position = transform.position + new Vector3(0f, cachedHalfHeight + verticalOffset, 0f);
		labelTransform.rotation = Quaternion.identity;
	}

	// プレイヤー描画物の高さから頭上オフセットを求める。
	private void RefreshHeight()
	{
		float fullHeight = 1f;
		Collider2D[] colliders = GetComponentsInChildren<Collider2D>(true);
		foreach (Collider2D collider in colliders)
		{
			if (collider != null)
			{
				fullHeight = Mathf.Max(fullHeight, collider.bounds.size.y);
			}
		}

		Renderer[] renderers = GetComponentsInChildren<Renderer>(true);
		foreach (Renderer renderer in renderers)
		{
			if (renderer != null && renderer != textRenderer)
			{
				fullHeight = Mathf.Max(fullHeight, renderer.bounds.size.y);
			}
		}

		cachedHalfHeight = fullHeight * 0.5f;
	}

	// 現在の登録順を返す。
	private int GetPlayerIndex()
	{
		return InputManager.Instance == null || playerController == null
			? -1
			: InputManager.Instance.GetPlayerIndex(playerController);
	}

	// ラベル描画可否を現在状態へ合わせる。
	private void UpdateVisibility()
	{
		if (textRenderer != null)
		{
			textRenderer.enabled = isVisible && lastPlayerIndex >= 0 && textMesh != null && !string.IsNullOrEmpty(textMesh.text);
		}
	}
}
