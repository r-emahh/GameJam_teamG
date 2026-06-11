using UnityEngine;
using UnityEngine.SceneManagement;

[DisallowMultipleComponent]
[RequireComponent(typeof(StageRuntimeBuilder), typeof(PlayerSpawnCoordinator))]
// 試合シーン読み込み時にステージとスポーンを再構築する。
public sealed class MatchSceneBootstrap : MonoBehaviour
{
	// Bootstrap 用のゲームオブジェクト名。
	private const string BootstrapObjectName = "MatchSceneBootstrap";
	// 描画領域が見つからない場合の既定値。
	private static readonly Rect DefaultDrawArea = new Rect(-4.5f, 0.7f, 7.5f, 2.6f);

	// ステージ構築コンポーネントを保持する。
	private StageRuntimeBuilder stageBuilder;
	// プレイヤー配置コンポーネントを保持する。
	private PlayerSpawnCoordinator spawnCoordinator;

	// 描画領域を返す。
	public static Rect DrawArea
	{
		get
		{
			DrawingSurface surface = FindFirstObjectByType<DrawingSurface>();
			return surface != null ? surface.WorldBounds : DefaultDrawArea;
		}
	}

	// シーン開始前に必要なら永続ブートストラップを作る。
	[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
	private static void Bootstrap()
	{
		if (FindFirstObjectByType<MatchSceneBootstrap>() != null)
		{
			return;
		}

		GameObject bootstrapObject = new GameObject(BootstrapObjectName);
		bootstrapObject.AddComponent<MatchSceneBootstrap>();
		DontDestroyOnLoad(bootstrapObject);
	}

	// 必要なコンポーネントをキャッシュしてシーン購読する。
	private void Awake()
	{
		stageBuilder = GetComponent<StageRuntimeBuilder>();
		spawnCoordinator = GetComponent<PlayerSpawnCoordinator>();
		SceneManager.sceneLoaded += HandleSceneLoaded;
	}

	// 購読を解除する。
	private void OnDestroy()
	{
		SceneManager.sceneLoaded -= HandleSceneLoaded;
	}

	// 試合シーンを読み込んだらステージを再構築する。
	private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
	{
		if (scene.name != "Rema" && scene.name != "Game")
		{
			return;
		}

		stageBuilder.Build();
		spawnCoordinator.PlacePlayers();
	}

	// ランタイム生成の HUD で使うスプライトを返す。
	public static Sprite GetRuntimeSprite() => RuntimeSpriteFactory.UnitSquare;

	// 描画領域内かどうかを判定する。
	public static bool IsInsideDrawArea(Vector2 worldPosition) => DrawArea.Contains(worldPosition);
}
