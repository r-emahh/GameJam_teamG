using System;
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
        SceneManager.LoadScene("Game");
    }

    public void TitleScene()
    {
        SceneManager.LoadScene("Title");
    }

    public void QuitGame()
    {
        #if UNITY_EDITOR
        // Unityエディタの再生モードを停止する
        UnityEditor.EditorApplication.isPlaying = false;
#else
        // 実際にビルドされたゲームを終了する
        Application.Quit();
#endif
    }
}