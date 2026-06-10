using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MatchSceneBootstrap : MonoBehaviour
{
	private const string BootstrapObjectName = "MatchSceneBootstrap";
	private const string DrawZoneName = "DrawZone";
	private const string GoalZoneName = "GoalZone";
	private const string GoalRunnerSpawnName = "GoalRunnerSpawn";
	private const string BlockerSpawnName = "BlockerSpawn";
	private const string CannonPrefix = "Cannon_";
	private static Sprite unitSquareSprite;
	public static Rect DrawArea { get; private set; } = new Rect(-4.5f, 0.7f, 7.5f, 2.6f);

	[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
	private static void Bootstrap()
	{
		if (FindFirstObjectByType<MatchSceneBootstrap>() != null)
		{
			return;
		}

		GameObject bootstrapObject = new GameObject(BootstrapObjectName);
		bootstrapObject.AddComponent<MatchSceneBootstrap>();
		Object.DontDestroyOnLoad(bootstrapObject);
	}

	private void Awake()
	{
		SceneManager.sceneLoaded += HandleSceneLoaded;
	}

	private void OnDestroy()
	{
		SceneManager.sceneLoaded -= HandleSceneLoaded;
	}

	private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
	{
		if (scene.name != "Rema" && scene.name != "Game")
		{
			return;
		}

		EnsureGoalZone();
		EnsureStageVisuals();
		EnsureSpawnPoints();
		PlacePlayersAtSpawns();
	}

	private void EnsureStageVisuals()
	{
		CreateVisualIfNeeded(DrawZoneName, new Vector3(DrawArea.center.x, DrawArea.center.y, 0f), DrawArea.size, new Color(0.25f, 0.75f, 1f, 0.18f));
		CreateVisualIfNeeded($"{GoalZoneName}_Visual", new Vector3(8.25f, -2.35f, 0f), new Vector2(1.1f, 3.8f), new Color(0.2f, 1f, 0.35f, 0.2f));
		CreateBoundaryIfNeeded("StageTop", new Vector3(0f, 5.35f, 0f), new Vector2(20f, 0.18f));
		CreateBoundaryIfNeeded("StageBottom", new Vector3(0f, -5.55f, 0f), new Vector2(20f, 0.18f));
		CreateCannonMarker("TopLeft", new Vector3(-8.75f, 4.4f, 0f), 315f);
		CreateCannonMarker("TopRight", new Vector3(8.75f, 4.4f, 0f), 225f);
		CreateCannonMarker("BottomLeft", new Vector3(-8.75f, -4.4f, 0f), 45f);
		CreateCannonMarker("BottomRight", new Vector3(8.75f, -4.4f, 0f), 135f);
	}

	private void EnsureGoalZone()
	{
		if (GameObject.Find(GoalZoneName) != null)
		{
			return;
		}

		GameObject goalZone = new GameObject(GoalZoneName);
		goalZone.transform.position = new Vector3(8.25f, -2.35f, 0f);

		BoxCollider2D collider2D = goalZone.AddComponent<BoxCollider2D>();
		collider2D.isTrigger = true;
		collider2D.size = new Vector2(1.1f, 3.8f);

		goalZone.AddComponent<GoalZoneTrigger>();
	}

	private void EnsureSpawnPoints()
	{
		CreateSpawnPointIfNeeded(GoalRunnerSpawnName, new Vector3(-7.4f, -3.1f, 0f));
		CreateSpawnPointIfNeeded(BlockerSpawnName, new Vector3(-5.6f, -3.1f, 0f));
	}

	private void CreateSpawnPointIfNeeded(string objectName, Vector3 position)
	{
		if (GameObject.Find(objectName) != null)
		{
			return;
		}

		GameObject spawnPoint = new GameObject(objectName);
		spawnPoint.transform.position = position;
	}

	private void CreateVisualIfNeeded(string objectName, Vector3 position, Vector2 scale, Color color)
	{
		if (GameObject.Find(objectName) != null)
		{
			return;
		}

		GameObject visual = new GameObject(objectName);
		visual.transform.position = position;
		visual.transform.localScale = new Vector3(scale.x, scale.y, 1f);

		SpriteRenderer renderer = visual.AddComponent<SpriteRenderer>();
		renderer.sprite = GetRuntimeSprite();
		renderer.color = color;
		renderer.sortingOrder = 0;
	}

	private void CreateBoundaryIfNeeded(string objectName, Vector3 position, Vector2 scale)
	{
		CreateVisualIfNeeded(objectName, position, scale, new Color(0.1f, 0.1f, 0.1f, 1f));
	}

	private void CreateCannonMarker(string suffix, Vector3 position, float rotationZ)
	{
		string objectName = $"{CannonPrefix}{suffix}";
		if (GameObject.Find(objectName) != null)
		{
			return;
		}

		GameObject cannon = new GameObject(objectName);
		cannon.transform.position = position;
		cannon.transform.rotation = Quaternion.Euler(0f, 0f, rotationZ);
		cannon.transform.localScale = new Vector3(0.55f, 0.55f, 1f);

		SpriteRenderer renderer = cannon.AddComponent<SpriteRenderer>();
		renderer.sprite = GetRuntimeSprite();
		renderer.color = new Color(0.8f, 0.65f, 0.2f, 1f);
		renderer.sortingOrder = 1;
	}

	public static Sprite GetRuntimeSprite()
	{
		if (unitSquareSprite != null)
		{
			return unitSquareSprite;
		}

		Texture2D texture = new Texture2D(1, 1, TextureFormat.RGBA32, false);
		texture.SetPixel(0, 0, Color.white);
		texture.Apply();
		unitSquareSprite = Sprite.Create(texture, new Rect(0f, 0f, 1f, 1f), new Vector2(0.5f, 0.5f), 1f);
		return unitSquareSprite;
	}

	public static bool IsInsideDrawArea(Vector2 worldPosition)
	{
		return DrawArea.Contains(worldPosition);
	}

	private void PlacePlayersAtSpawns()
	{
		if (InputManager.Instance == null)
		{
			return;
		}

		Transform goalRunnerSpawn = GameObject.Find(GoalRunnerSpawnName)?.transform;
		Transform blockerSpawn = GameObject.Find(BlockerSpawnName)?.transform;
		List<PlayerController> players = new List<PlayerController>(InputManager.Instance.GetRegisteredPlayers());

		if (players.Count == 1)
		{
			PlayerController clone = Instantiate(players[0], blockerSpawn != null ? blockerSpawn.position : players[0].transform.position + new Vector3(1.5f, 0f, 0f), Quaternion.identity);
			clone.name = "Player2";
			players = new List<PlayerController>(InputManager.Instance.GetRegisteredPlayers());
		}

		if (players.Count == 0)
		{
			return;
		}

		if (goalRunnerSpawn != null)
		{
			players[0].transform.position = goalRunnerSpawn.position;
		}

		if (players.Count > 1 && blockerSpawn != null)
		{
			players[1].transform.position = blockerSpawn.position;
		}

		if (players.Count > 0)
		{
			players[0].ConfigureLocalInput(MatchSide.GoalRunner, false);
		}

		if (players.Count > 1)
		{
			players[1].ConfigureLocalInput(MatchSide.Blocker, true);
		}

		InputManager.Instance?.ResetDashAvailabilityForAllPlayers();
	}
}
