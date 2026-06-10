using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneChanger : MonoBehaviour
{
	public void ChangeScene(string sceneName)
	{
		SceneManager.LoadScene(sceneName);
	}

	public void GameScene()
	{
		GameManager.Instance?.ChangeState(GameState.Game);
		SceneManager.LoadScene("Rema");
	}

	public void TitleScene()
	{
		GameManager.Instance?.ChangeState(GameState.Title);
		SceneManager.LoadScene("Title");
	}

	public void QuitGame()
	{
#if UNITY_EDITOR
		UnityEditor.EditorApplication.isPlaying = false;
#else
		Application.Quit();
#endif
	}
}
