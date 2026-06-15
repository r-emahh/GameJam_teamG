using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
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
}
