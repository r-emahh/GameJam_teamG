using UnityEngine;
using UnityEngine.SceneManagement;

// タイトルや試合シーンへの遷移をまとめる操作用コンポーネント。
public class SceneChanger : MonoBehaviour
{
	// 任意のシーン名へ遷移する。
	public void ChangeScene(string sceneName)
	{
		SceneManager.LoadScene(sceneName);
	}

	// 試合状態を開始へ切り替えてゲームシーンを開く。
	public void GameScene()
	{
		GameManager.Instance?.BeginMatch();
		SceneManager.LoadScene("Rema");
	}

	// 試合状態をタイトルへ戻してタイトルシーンを開く。
	public void TitleScene()
	{
		GameManager.Instance?.ChangeState(GameState.Title);
		SceneManager.LoadScene("Title");
	}

	// 実行環境に応じてゲームを終了する。
	public void QuitGame()
	{
#if UNITY_EDITOR
		UnityEditor.EditorApplication.isPlaying = false;
#else
		Application.Quit();
#endif
	}
}
