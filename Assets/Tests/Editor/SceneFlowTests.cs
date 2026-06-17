using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

public sealed class SceneFlowTests
{
	[Test]
	public void ProductionBuildContainsOnlyTitleAndMatchScenes()
	{
		string[] enabledScenePaths = EditorBuildSettings.scenes
			.Where(scene => scene.enabled)
			.Select(scene => scene.path)
			.ToArray();

		CollectionAssert.AreEqual(
			new[] { SceneCatalog.TitlePath, SceneCatalog.MatchPath },
			enabledScenePaths);
		Assert.That(SceneCatalog.IsProduction(SceneCatalog.Title), Is.True);
		Assert.That(SceneCatalog.IsProduction(SceneCatalog.Match), Is.True);
		Assert.That(SceneCatalog.IsProduction("Game"), Is.False);
		Assert.That(SceneCatalog.IsProduction("Takamasa"), Is.False);
	}

	[UnityTest]
	public IEnumerator TitleStartButtonLoadsProductionMatchScene()
	{
		EditorSceneManager.OpenScene(SceneCatalog.TitlePath, OpenSceneMode.Single);
		yield return new EnterPlayMode();

		Assert.That(SceneManager.GetActiveScene().name, Is.EqualTo(SceneCatalog.Title));
		Button startButton = Object.FindObjectsByType<Button>(FindObjectsInactive.Include, FindObjectsSortMode.None)
			.Single(button => button.name == "StartButton");

		startButton.onClick.Invoke();
		yield return null;

		Assert.That(SceneManager.GetActiveScene().name, Is.EqualTo(SceneCatalog.Match));
		Assert.That(GameManager.Instance, Is.Not.Null);
		Assert.That(GameManager.currentState, Is.EqualTo(GameState.Game));
		Assert.That(GameManager.Instance.IsMatchRunning, Is.True);

		yield return new ExitPlayMode();
	}

	[UnityTest]
	public IEnumerator TitleToFinalResultToRetry_ReinitializesMatchAndBoard()
	{
		EditorSceneManager.OpenScene(SceneCatalog.TitlePath, OpenSceneMode.Single);
		yield return new EnterPlayMode();

		Button startButton = Object.FindObjectsByType<Button>(FindObjectsInactive.Include, FindObjectsSortMode.None)
			.Single(button => button.name == "StartButton");
		startButton.onClick.Invoke();
		yield return null;

		CompleteMatchWithTimeouts();
		yield return null;

		MatchHudView hud = Object.FindObjectsByType<MatchHudView>(FindObjectsInactive.Include, FindObjectsSortMode.None).Single();
		Button retryButton = Object.FindObjectsByType<Button>(FindObjectsInactive.Include, FindObjectsSortMode.None)
			.Single(button => button.name == "ButtonRetry");
		GameObject resultUi = hud.transform.Find("ResultUI")?.gameObject;
		Assert.That(resultUi, Is.Not.Null);
		Assert.That(resultUi.activeSelf, Is.True);
		Assert.That(GameManager.Instance.IsFinalResultVisible, Is.True);

		EventSystem eventSystem = Object.FindFirstObjectByType<EventSystem>();
		Assert.That(eventSystem, Is.Not.Null);
		InputSystemUIInputModule uiInputModule = eventSystem.GetComponent<InputSystemUIInputModule>();
		Assert.That(uiInputModule, Is.Not.Null);
		Assert.That(uiInputModule.move, Is.Not.Null);
		Assert.That(uiInputModule.submit, Is.Not.Null);
		Assert.That(eventSystem.currentSelectedGameObject, Is.EqualTo(retryButton.gameObject));

		new GameObject("RetryRuntimeObject").AddComponent<RuntimeRoundObject>();
		retryButton.onClick.Invoke();
		yield return null;
		yield return null;

		Assert.That(SceneManager.GetActiveScene().name, Is.EqualTo(SceneCatalog.Match));
		Assert.That(GameManager.currentState, Is.EqualTo(GameState.Game));
		Assert.That(GameManager.Instance.CurrentRound, Is.EqualTo(1));
		Assert.That(GameManager.Instance.CurrentPhase, Is.EqualTo(MatchPhase.Draw));
		Assert.That(GameManager.Instance.CurrentScoreSummary.IsMatchComplete, Is.False);
		Assert.That(GameManager.Instance.IsFinalResultVisible, Is.False);
		Assert.That(Object.FindObjectsByType<RuntimeRoundObject>(FindObjectsInactive.Include, FindObjectsSortMode.None), Is.Empty);
		Assert.That(hud.gameObject.activeInHierarchy, Is.True);

		yield return new ExitPlayMode();
	}

	[UnityTest]
	public IEnumerator FinalResultTitleButton_ReturnsToTitleScene()
	{
		EditorSceneManager.OpenScene(SceneCatalog.TitlePath, OpenSceneMode.Single);
		yield return new EnterPlayMode();

		Button startButton = Object.FindObjectsByType<Button>(FindObjectsInactive.Include, FindObjectsSortMode.None)
			.Single(button => button.name == "StartButton");
		startButton.onClick.Invoke();
		yield return null;

		CompleteMatchWithTimeouts();
		yield return null;

		Button titleButton = Object.FindObjectsByType<Button>(FindObjectsInactive.Include, FindObjectsSortMode.None)
			.Single(button => button.name == "ButtonTitle");
		titleButton.onClick.Invoke();
		yield return null;

		Assert.That(SceneManager.GetActiveScene().name, Is.EqualTo(SceneCatalog.Title));
		Assert.That(GameManager.currentState, Is.EqualTo(GameState.Title));
		Assert.That(GameManager.Instance.IsMatchRunning, Is.False);

		yield return new ExitPlayMode();
	}

	private static void CompleteMatchWithTimeouts()
	{
		MatchSession session = GetSession();
		session.MarkTimeUp();
		ExpireCurrentPhase(session);
		session.MarkTimeUp();
	}

	private static MatchSession GetSession()
	{
		FieldInfo sessionField = typeof(GameManager).GetField("session", BindingFlags.Instance | BindingFlags.NonPublic);
		Assert.That(sessionField, Is.Not.Null);
		return (MatchSession)sessionField.GetValue(GameManager.Instance);
	}

	private static void ExpireCurrentPhase(MatchSession session)
	{
		if (session.TimeRemaining > 0f)
		{
			session.Tick(session.TimeRemaining);
		}

		session.Tick(0f);
	}
}
